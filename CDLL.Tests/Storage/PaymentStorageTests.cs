// =====================================================================================
// EN: PaymentStorage: every database call on ONE background thread, JSON fallback when the database is
//     down, automatic and manual import when it comes back, damaged files parked, nothing silently lost,
//     card numbers masked in the activity log.
// AR: PaymentStorage: كل استدعاء لقاعدة البيانات على خيط خلفي واحد، الحفظ الاحتياطي JSON عند تعطل القاعدة،
//     الاستيراد التلقائي واليدوي عند عودتها، عزل الملفات التالفة، عدم فقدان أي سجل، وتقنيع أرقام البطاقات في السجل.
// =====================================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CDLL.Payments;
using CDLL.Payments.Storage;
using Xunit;

namespace CDLL.Tests.Storage
{
    public sealed class PaymentStorageTests : IDisposable
    {
        private readonly string _folder = TestRecords.NewTempFolder();
        private readonly SwitchableStore _store;
        private readonly PaymentStorage _storage;

        public PaymentStorageTests()
        {
            _store = new SwitchableStore(new JsonTransactionStore(Path.Combine(_folder, "db")));
            _storage = new PaymentStorage(_store, _folder);
        }

        [Fact]
        public async Task Every_store_call_runs_on_the_single_storage_thread()
        {
            int testThread = Thread.CurrentThread.ManagedThreadId;

            await _storage.SaveTransactionAsync(TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow));
            await _storage.SaveErrorAsync(new ErrorLogRecord { Source = "t", Message = "m" });
            await _storage.SaveMappingAsync(TestRecords.Mapping("CASHIER-01", "SPAN-1"));
            await _storage.GetMappingAsync("CASHIER-01");
            await _storage.GetMappingsAsync();
            await _storage.GetTransactionsAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
            await _storage.DeleteMappingAsync("CASHIER-01", "t");

            Assert.NotEmpty(_store.CallThreads);
            Assert.All(_store.CallThreads, id => Assert.Equal(_storage.WorkerThreadId, id));
            Assert.DoesNotContain(testThread, _store.CallThreads);
        }

        [Fact]
        public async Task Writes_keep_their_order()
        {
            DateTime start = DateTime.UtcNow;
            Task[] writes = Enumerable.Range(0, 50)
                .Select(i => (Task)_storage.SaveTransactionAsync(TestRecords.Full(TransactionOutcome.Approved, start.AddMilliseconds(i))))
                .ToArray();
            await Task.WhenAll(writes);

            Assert.Equal(Enumerable.Range(0, 50).Select(i => start.AddMilliseconds(i)).ToArray(),
                         _store.SavedTransactions.Select(r => r.CreatedAtUtc).ToArray());
        }

        [Fact]
        public async Task Database_down_saves_JSON_then_Sync_pending_inserts_it()
        {
            _store.Down = true;
            PaymentTransactionRecord paid = TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow);
            ErrorLogRecord error = new ErrorLogRecord { Source = "t", Message = "boom" };

            StorageWriteResult result = await _storage.SaveTransactionAsync(paid);
            await _storage.SaveErrorAsync(error);

            Assert.Equal(StorageLocation.PendingJson, result.Location);
            Assert.True(File.Exists(result.FilePath));
            Assert.Contains("database is down", result.Error);
            Assert.Equal(2, await _storage.GetPendingCountAsync());

            // EN: Still down: manual sync fails and keeps the files.   AR: ما زالت متعطلة: المزامنة تفشل وتُبقي الملفات.
            await Assert.ThrowsAnyAsync<Exception>(() => _storage.ImportPendingAsync());
            Assert.Equal(2, await _storage.GetPendingCountAsync());

            _store.Down = false;
            PendingImportResult import = await _storage.ImportPendingAsync();

            Assert.Equal(2, import.Imported);
            Assert.Equal(0, import.Remaining);
            Assert.Equal(paid.Id, _store.SavedTransactions.Single().Id);
            Assert.Equal("446404******1234", _store.SavedTransactions.Single().MaskedPan);
            Assert.Equal(error.Id, _store.SavedErrors.Single().Id);
            Assert.Equal(2, Directory.GetFiles(Path.Combine(_storage.PendingFolder, "imported")).Length);
        }

        [Fact]
        public async Task Next_successful_write_also_imports_the_pending_records()
        {
            _store.Down = true;
            PaymentTransactionRecord whileDown = TestRecords.Full(TransactionOutcome.Declined, DateTime.UtcNow);
            await _storage.SaveTransactionAsync(whileDown);

            _store.Down = false;
            PaymentTransactionRecord afterwards = TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow);
            StorageWriteResult result = await _storage.SaveTransactionAsync(afterwards);

            Assert.Equal(StorageLocation.Database, result.Location);
            Assert.Equal(new[] { afterwards.Id, whileDown.Id }, _store.SavedTransactions.Select(r => r.Id).ToArray());
            Assert.Equal(0, await _storage.GetPendingCountAsync());
        }

        [Fact]
        public async Task Mapping_saved_while_down_is_applied_later()
        {
            _store.Down = true;
            await _storage.SaveMappingAsync(TestRecords.Mapping("NEW-PC", "SPAN-ER-01"));
            _store.Down = false;

            await _storage.ImportPendingAsync();

            Assert.Equal("SPAN-ER-01", (await _storage.GetMappingAsync("NEW-PC")).TerminalName);
        }

        [Fact]
        public async Task Damaged_pending_file_is_parked_and_does_not_block_the_others()
        {
            _store.Down = true;
            PaymentTransactionRecord good = TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow);
            await _storage.SaveTransactionAsync(good);
            File.WriteAllText(Path.Combine(_storage.PendingFolder, "00000000000000000_Transaction_bad.json"), "{ not json");
            _store.Down = false;

            PendingImportResult import = await _storage.ImportPendingAsync();

            Assert.Equal(1, import.Imported);
            Assert.Equal(1, import.Failed);
            Assert.Equal(0, import.Remaining);
            Assert.Equal(good.Id, _store.SavedTransactions.Single().Id);
            Assert.Single(Directory.GetFiles(Path.Combine(_storage.PendingFolder, "failed")));
        }

        [Fact]
        public async Task Pending_JSON_works_in_a_deep_folder_and_its_path_is_not_masked()
        {
            // EN: Pending file path ~245 chars: fits under Windows' 260 limit, the old "path + guid + .tmp" did not.
            // AR: مسار الملف المعلّق قرابة 245 حرفاً: يتسع تحت حد 260، والاسم المؤقت القديم كان يتجاوزه.
            const int pendingFileName = 69;   // EN: yyyyMMdd-HHmmss-fff_Transaction_<32 hex>.json
            string deep = Path.Combine(_folder, new string('d', 245 - pendingFileName - "\\pending\\".Length - _folder.Length - 1));
            var store = new SwitchableStore(new JsonTransactionStore(Path.Combine(deep, "db"))) { Down = true };
            using (var storage = new PaymentStorage(store, deep))
            {
                StorageWriteResult result = await storage.SaveTransactionAsync(TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow));

                Assert.Equal(StorageLocation.PendingJson, result.Location);
                Assert.InRange(result.FilePath.Length, 240, 259);
                Assert.True(File.Exists(result.FilePath));
                Assert.DoesNotContain("*", Path.GetFileName(result.FilePath));
            }
        }

        [Fact]
        public async Task Database_and_disk_down_is_reported_as_Lost_never_silent()
        {
            string folder = TestRecords.NewTempFolder();
            File.WriteAllText(Path.Combine(folder, "pending"), "a FILE where the pending folder should be");
            var store = new SwitchableStore(new JsonTransactionStore(Path.Combine(folder, "db"))) { Down = true };
            using (var storage = new PaymentStorage(store, folder))
            {
                StorageWriteResult result = await storage.SaveTransactionAsync(TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow));

                Assert.Equal(StorageLocation.Lost, result.Location);
                Assert.Contains("database is down", result.Error);
                Assert.Contains("JSON", result.Error);
            }
        }

        [Fact]
        public async Task Activity_log_has_one_line_per_record_and_no_full_card_number()
        {
            PaymentTransactionRecord record = TestRecords.Full(TransactionOutcome.Approved, DateTime.UtcNow);
            await _storage.SaveTransactionAsync(record);
            await _storage.SaveErrorAsync(ErrorLogRecord.FromException("t", new Exception("pan 4464041234561234")));

            string log = string.Join("\n", Directory.GetFiles(Path.Combine(_folder, "logs")).Select(File.ReadAllText));
            Assert.Contains(record.Id, log);
            Assert.Contains("TRANSACTION Purchase Card Approved", log);
            Assert.Contains("446404******1234", log);
            Assert.DoesNotContain("4464041234561234", log);
        }

        [Fact]
        public async Task Mapping_without_workstation_is_refused_not_queued()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveMappingAsync(TestRecords.Mapping("", "SPAN")));
            Assert.Equal(0, await _storage.GetPendingCountAsync());
        }

        [Fact]
        public void Provider_is_chosen_by_name()
        {
            Assert.IsType<SqliteTransactionStore>(PaymentServices.CreateStore("Sqlite", _folder));
            Assert.IsType<JsonTransactionStore>(PaymentServices.CreateStore("json", _folder));
            Assert.Throws<System.Configuration.ConfigurationErrorsException>(() => PaymentServices.CreateStore("Oracle", _folder));
        }

        public void Dispose()
        {
            _storage.Dispose();
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }
}
