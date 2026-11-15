// =====================================================================================
// EN: FALLBACK when the database is down: the record is written to <DataFolder>\pending\ as JSON instead
//     of being lost. When the database is back, ImportInto() inserts the files in time order and moves
//     each one to pending\imported\. It runs:
//       - automatically after the next successful database write, and
//       - manually with the "Sync pending" button on Form1.
//     Re-importing a file is safe: the stores ignore an Id that is already saved.
//     File name: yyyyMMdd-HHmmss-fff_<Kind>_<Id>.json, content: { Kind, SavedAtUtc, Reason, Payload }.
// AR: البديل عند تعطل قاعدة البيانات: يُكتب السجل في <DataFolder>\pending\ بصيغة JSON بدلاً من فقدانه.
//     عند عودة القاعدة، تقوم ImportInto() بإدراج الملفات حسب الترتيب الزمني ونقل كل ملف إلى pending\imported\.
//     يتم ذلك تلقائياً بعد أول عملية حفظ ناجحة، ويدوياً بزر "مزامنة المعلّق" في Form1.
//     إعادة استيراد الملف آمنة: التخزين يتجاهل أي Id محفوظ مسبقاً.
// =====================================================================================

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CDLL.Payments.Storage
{
    public sealed class PendingJsonQueue
    {
        public const string TransactionKind = "Transaction";
        public const string ErrorKind = "Error";
        public const string MappingKind = "Mapping";

        private readonly string _folder;

        public PendingJsonQueue(string folder)
        {
            _folder = folder;
        }

        public string Folder { get { return _folder; } }

        public int Count
        {
            get { return Directory.Exists(_folder) ? Directory.GetFiles(_folder, "*.json").Length : 0; }
        }

        /// <summary>EN: Saves one record that could not reach the database. Returns the file path.  AR: يحفظ سجلاً لم يصل إلى قاعدة البيانات ويُعيد مسار الملف.</summary>
        public string Add(string kind, string id, object payload, string reason)
        {
            var envelope = new PendingEnvelope
            {
                Kind = kind,
                SavedAtUtc = DateTime.UtcNow,
                Reason = CardDataMasker.MaskText(reason),
                Payload = JObject.FromObject(payload, JsonFiles.Serializer)
            };
            // EN: Dashes in the timestamp so the card-number masker never treats it as a 13-19 digit PAN.
            // AR: الشرطات في الطابع الزمني حتى لا يعتبره المُقنِّع رقم بطاقة.
            string name = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "_" + kind + "_" + JsonFiles.SafeName(id) + ".json";
            string path = Path.Combine(_folder, name);
            JsonFiles.WriteAtomic(path, envelope);
            return path;
        }

        /// <summary>
        /// EN: Inserts every pending file into the store, oldest first. Stops at the first failure (the database
        ///     is probably still down), so nothing is skipped or reordered.
        /// AR: يُدرج كل الملفات المعلّقة في التخزين، الأقدم أولاً. يتوقف عند أول فشل (القاعدة غالباً ما زالت متعطلة)
        ///     حتى لا يتم تخطي أي ملف أو تغيير الترتيب.
        /// </summary>
        public PendingImportResult ImportInto(ITransactionStore store)
        {
            var result = new PendingImportResult();
            if (!Directory.Exists(_folder))
                return result;

            string importedFolder = Path.Combine(_folder, "imported");
            foreach (string file in Directory.GetFiles(_folder, "*.json").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
            {
                try
                {
                    PendingEnvelope envelope = JsonFiles.Read<PendingEnvelope>(file);
                    switch (envelope.Kind)
                    {
                        case TransactionKind:
                            store.SaveTransaction(envelope.Payload.ToObject<PaymentTransactionRecord>(JsonFiles.Serializer));
                            break;
                        case ErrorKind:
                            store.SaveError(envelope.Payload.ToObject<ErrorLogRecord>(JsonFiles.Serializer));
                            break;
                        case MappingKind:
                            store.SaveMapping(envelope.Payload.ToObject<TerminalMapping>(JsonFiles.Serializer));
                            break;
                        default:
                            throw new InvalidDataException("Unknown pending record kind: " + envelope.Kind);
                    }

                    MoveTo(file, importedFolder);
                    result.Imported++;
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidDataException || ex is ArgumentException)
                {
                    // EN: The FILE is bad (damaged / edited), not the database: park it in pending\failed\ and go on,
                    //     so one bad file can never block the others.
                    // AR: الملف نفسه تالف (وليس قاعدة البيانات): يُنقل إلى pending\failed\ ونكمل، حتى لا يوقف ملف واحد البقية.
                    MoveTo(file, Path.Combine(_folder, "failed"));
                    result.Failed++;
                    result.LastError = CardDataMasker.MaskText(Path.GetFileName(file) + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.LastError = CardDataMasker.MaskText(ex.GetType().Name + ": " + ex.Message);
                    break;
                }
            }

            result.Remaining = Count;
            return result;
        }

        private static void MoveTo(string file, string folder)
        {
            Directory.CreateDirectory(folder);
            string target = Path.Combine(folder, Path.GetFileName(file));
            if (File.Exists(target))
                File.Delete(target);
            File.Move(file, target);
        }

        private sealed class PendingEnvelope
        {
            public string Kind { get; set; }
            public DateTime SavedAtUtc { get; set; }
            public string Reason { get; set; }
            public JObject Payload { get; set; }
        }
    }

    public sealed class PendingImportResult
    {
        public int Imported { get; set; }
        public int Failed { get; set; }
        public int Remaining { get; set; }
        public string LastError { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "imported {0}, failed {1}, remaining {2}{3}",
                Imported, Failed, Remaining, LastError == null ? "" : " (" + LastError + ")");
        }
    }
}
