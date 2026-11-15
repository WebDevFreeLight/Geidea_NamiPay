// =====================================================================================
// EN: The ONLY way screens touch storage. Every read, write, log line and pending import runs on ONE
//     dedicated background thread ("PaymentStorage"), in the order it was requested, so:
//       - the UI thread never waits for the database,
//       - two writes never run at the same time (no locking needed in the stores),
//       - a slow or dead database only delays this thread, never the cashier.
//     Saving NEVER loses a record:
//       database OK      -> saved in the database (+ older pending JSON files are imported),
//       database failed  -> saved as JSON in <DataFolder>\pending\ (Location = PendingJson),
//       disk failed too  -> Location = Lost, with both error messages (shown to the user).
// AR: الطريقة الوحيدة التي تتعامل بها الشاشات مع التخزين. كل قراءة وكتابة وسطر سجل واستيراد للمعلّق يتم على
//     خيط خلفي واحد مخصص ("PaymentStorage") وبنفس ترتيب الطلب، لذلك:
//       - خيط الواجهة لا ينتظر قاعدة البيانات أبداً،
//       - لا تتم عمليتا كتابة في نفس الوقت (لا حاجة لأقفال في التخزين)،
//       - القاعدة البطيئة أو المتعطلة تؤخر هذا الخيط فقط وليس أمين الصندوق.
//     الحفظ لا يفقد أي سجل: القاعدة تعمل -> يُحفظ فيها، القاعدة متعطلة -> يُحفظ JSON في pending،
//     والقرص متعطل أيضاً -> Location = Lost مع رسالتي الخطأ (تُعرض للمستخدم).
// =====================================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CDLL.Payments.Storage
{
    public enum StorageLocation { Database, PendingJson, Lost }

    /// <summary>EN: Where a record ended up.  AR: أين انتهى السجل.</summary>
    public sealed class StorageWriteResult
    {
        public StorageLocation Location { get; private set; }
        public string Store { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }

        public static StorageWriteResult Database(string store) { return new StorageWriteResult { Location = StorageLocation.Database, Store = store }; }
        public static StorageWriteResult Pending(string store, string file, string error) { return new StorageWriteResult { Location = StorageLocation.PendingJson, Store = store, FilePath = file, Error = error }; }
        public static StorageWriteResult Lost(string store, string error) { return new StorageWriteResult { Location = StorageLocation.Lost, Store = store, Error = error }; }

        public string Describe()
        {
            switch (Location)
            {
                case StorageLocation.Database:
                    return "Saved to " + Store + " / تم الحفظ في " + Store;
                case StorageLocation.PendingJson:
                    return "DATABASE NOT AVAILABLE, saved as JSON (press Sync pending later) / قاعدة البيانات غير متاحة، تم الحفظ كملف JSON: " + FilePath + " (" + Error + ")";
                default:
                    return "NOT SAVED / لم يُحفظ: " + Error;
            }
        }
    }

    public sealed class PaymentStorage : IDisposable
    {
        private readonly ITransactionStore _store;
        private readonly PendingJsonQueue _pending;
        private readonly ActivityLog _log;
        private readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();
        private readonly Thread _worker;
        private bool _created;   // EN: touched only on the worker thread   AR: يُستخدم فقط على الخيط الخلفي

        public PaymentStorage(ITransactionStore store, string dataFolder)
        {
            if (store == null) throw new ArgumentNullException("store");
            _store = store;
            DataFolder = dataFolder;
            _pending = new PendingJsonQueue(Path.Combine(dataFolder, "pending"));
            _log = new ActivityLog(Path.Combine(dataFolder, "logs"));

            _worker = new Thread(WorkLoop) { IsBackground = true, Name = "PaymentStorage" };
            _worker.Start();
        }

        public string StoreName { get { return _store.Name; } }
        public string DataFolder { get; private set; }
        public string PendingFolder { get { return _pending.Folder; } }
        public int WorkerThreadId { get { return _worker.ManagedThreadId; } }

        // =============================================================================
        // EN: WRITES (never throw, never lose data) / الكتابة (لا ترمي استثناءً ولا تفقد بيانات)
        // =============================================================================

        public Task<StorageWriteResult> SaveTransactionAsync(PaymentTransactionRecord record)
        {
            return Enqueue(() => Write(PendingJsonQueue.TransactionKind, record.Id, record, s => s.SaveTransaction(record),
                string.Format(CultureInfo.InvariantCulture, "TRANSACTION {0} {1} {2} amount={3} rc={4} rsp={5} rrn={6} pan={7} mrn={8} invoice={9} {10}",
                    record.Operation, record.PaymentMethod, record.Outcome, record.AmountHalalas, record.ReturnCode, record.ResponseCode,
                    record.Rrn, record.MaskedPan, record.PatientMrn, record.InvoiceNo, record.Id)));
        }

        public Task<StorageWriteResult> SaveErrorAsync(ErrorLogRecord record)
        {
            return Enqueue(() => Write(PendingJsonQueue.ErrorKind, record.Id, record, s => s.SaveError(record),
                "ERROR " + record.Source + " " + record.Message + " " + record.Id));
        }

        public Task<StorageWriteResult> SaveMappingAsync(TerminalMapping mapping)
        {
            // EN: Bad input is the caller's problem, not a database outage: do not park it as pending JSON.
            // AR: الإدخال الخاطئ مشكلة المستدعي وليس تعطلاً في القاعدة، لذلك لا يُحفظ كملف JSON معلّق.
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.Workstation))
                throw new ArgumentException("Workstation is required. / اسم الحاسوب مطلوب.", "mapping");

            mapping.Normalize();
            return Enqueue(() => Write(PendingJsonQueue.MappingKind, mapping.Id, mapping, s => s.SaveMapping(mapping),
                "MAPPING " + mapping.Workstation + " -> " + mapping.Provider + " " + mapping.TerminalName + " (" + mapping.Department + ") " +
                mapping.ToConnection().Describe() + (mapping.IsActive ? " ACTIVE" : "") + " by " + mapping.UpdatedBy));
        }

        // =============================================================================
        // EN: READS (throw if the store fails; the caller shows the message) / القراءة
        // =============================================================================

        public Task<TerminalMapping> GetMappingAsync(string workstation)
        {
            return Enqueue(() => { EnsureCreated(); return _store.GetMapping(workstation); });
        }

        /// <summary>
        /// EN: Removes a mapping. Throws if the store is down (no pending fallback: a delete must not run later by surprise).
        /// AR: حذف ربط. يرمي استثناءً إذا كان التخزين متعطلاً (بدون حفظ معلّق: الحذف يجب ألا يتم لاحقاً بشكل مفاجئ).
        /// </summary>
        public Task<bool> DeleteMappingAsync(string workstation, TerminalProvider provider, string deletedBy)
        {
            return Enqueue(() =>
            {
                EnsureCreated();
                _store.DeleteMapping(workstation, provider);
                _log.Write("MAPPING DELETED " + workstation + " " + provider + " by " + deletedBy);
                return true;
            });
        }

        /// <summary>
        /// EN: Makes this provider the PC's active (default) terminal. Throws if the store is down or the mapping
        ///     is not saved (no pending fallback, same reason as delete).
        /// AR: يجعل هذا المزوّد الجهاز الفعّال (الافتراضي) للحاسوب. يرمي استثناءً إذا كان التخزين متعطلاً أو الربط غير محفوظ.
        /// </summary>
        public Task<bool> SetActiveMappingAsync(string workstation, TerminalProvider provider, string changedBy)
        {
            return Enqueue(() =>
            {
                EnsureCreated();
                _store.SetActiveMapping(workstation, provider);
                _log.Write("MAPPING ACTIVE " + workstation + " -> " + provider + " by " + changedBy);
                return true;
            });
        }

        public Task<IList<TerminalMapping>> GetMappingsAsync()
        {
            return Enqueue(() => { EnsureCreated(); return _store.GetMappings(); });
        }

        public Task<IList<PaymentTransactionRecord>> GetTransactionsAsync(DateTime fromUtc, DateTime toUtc)
        {
            return Enqueue(() => { EnsureCreated(); return _store.GetTransactions(fromUtc, toUtc); });
        }

        public Task<int> GetPendingCountAsync()
        {
            return Enqueue(() => _pending.Count);
        }

        /// <summary>EN: "Sync pending": inserts the pending JSON files into the database.  AR: "مزامنة المعلّق": إدراج ملفات JSON المعلّقة في قاعدة البيانات.</summary>
        public Task<PendingImportResult> ImportPendingAsync()
        {
            return Enqueue(() =>
            {
                EnsureCreated();
                PendingImportResult result = _pending.ImportInto(_store);
                _log.Write("IMPORT PENDING into " + _store.Name + ": " + result);
                return result;
            });
        }

        /// <summary>EN: Runs any work on the storage thread (used by tests).  AR: تشغيل أي عمل على خيط التخزين (للاختبارات).</summary>
        public Task<T> RunOnStorageThreadAsync<T>(Func<T> work)
        {
            return Enqueue(work);
        }

        // =============================================================================
        // EN: WORKER / الخيط الخلفي
        // =============================================================================

        private Task<T> Enqueue<T>(Func<T> work)
        {
            // EN: .NET 4.0 has no RunContinuationsAsynchronously: the result is handed over on a thread-pool thread,
            //     so the caller's code after "await" never runs on (and never blocks) the storage thread.
            // AR: لا يوجد RunContinuationsAsynchronously في 4.0: تُسلَّم النتيجة على خيط من مجمع الخيوط حتى لا
            //     يعمل كود المستدعي بعد "await" على خيط التخزين.
            var completion = new TaskCompletionSource<T>();
            try
            {
                _work.Add(() =>
                {
                    T result;
                    try { result = work(); }
                    catch (Exception ex)
                    {
                        ThreadPool.QueueUserWorkItem(_ => completion.SetException(ex));
                        return;
                    }
                    ThreadPool.QueueUserWorkItem(_ => completion.SetResult(result));
                });
            }
            catch (InvalidOperationException)
            {
                completion.SetException(new ObjectDisposedException("PaymentStorage"));
            }
            return completion.Task;
        }

        private void WorkLoop()
        {
            foreach (Action action in _work.GetConsumingEnumerable())
                action();
        }

        private StorageWriteResult Write(string kind, string id, object payload, Action<ITransactionStore> save, string logLine)
        {
            try
            {
                EnsureCreated();
                save(_store);
                _log.Write("SAVED " + _store.Name + " | " + logLine);
                ImportPendingQuietly();
                return StorageWriteResult.Database(_store.Name);
            }
            catch (Exception databaseError)
            {
                _created = false;   // EN: re-check the tables next time   AR: إعادة فحص الجداول في المرة القادمة
                string reason = databaseError.GetType().Name + ": " + databaseError.Message;
                try
                {
                    string file = _pending.Add(kind, id, payload, reason);
                    _log.Write("PENDING (" + reason + ") | " + logLine + " -> " + file);
                    return StorageWriteResult.Pending(_store.Name, file, reason);
                }
                catch (Exception fileError)
                {
                    string both = reason + " / JSON: " + fileError.GetType().Name + ": " + fileError.Message;
                    _log.Write("LOST (" + both + ") | " + logLine);
                    return StorageWriteResult.Lost(_store.Name, CardDataMasker.MaskText(both));
                }
            }
        }

        /// <summary>EN: After a successful write the database is up, so older pending files go in too.  AR: بعد حفظ ناجح تكون القاعدة متاحة، فتُدرج الملفات المعلّقة الأقدم أيضاً.</summary>
        private void ImportPendingQuietly()
        {
            if (_pending.Count == 0)
                return;
            PendingImportResult result = _pending.ImportInto(_store);
            _log.Write("AUTO IMPORT PENDING into " + _store.Name + ": " + result);
        }

        private void EnsureCreated()
        {
            if (_created)
                return;
            _store.EnsureCreated();
            _created = true;
        }

        /// <summary>EN: Finishes the queued writes (up to 15 s) before the app closes.  AR: ينهي عمليات الحفظ المنتظرة (حتى 15 ثانية) قبل إغلاق البرنامج.</summary>
        public void Dispose()
        {
            if (_work.IsAddingCompleted)
                return;
            _work.CompleteAdding();
            _worker.Join(TimeSpan.FromSeconds(15));
        }
    }
}
