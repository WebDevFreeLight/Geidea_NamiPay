// =====================================================================================
// EN: The SAME tests run against SQLite, SQL Server (LocalDB) and JSON, so all three behave the same:
//     every field round-trips, every outcome is saved, duplicates are ignored, date ranges work,
//     mappings insert / update / delete, and tables are created only once.
// AR: نفس الاختبارات تعمل على SQLite و SQL Server (LocalDB) و JSON حتى تتصرف الثلاث بنفس الطريقة.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using CDLL.Payments;
using CDLL.Payments.Storage;
using System.Data.SQLite;
using Newtonsoft.Json;
using Xunit;

namespace CDLL.Tests.Storage
{
    public abstract class StoreContractTests : IDisposable
    {
        protected abstract ITransactionStore Store { get; }

        public abstract void Dispose();

        [Fact]
        public void EnsureCreated_can_run_many_times()
        {
            Store.EnsureCreated();
            Store.EnsureCreated();
        }

        [Fact]
        public void Every_field_of_a_transaction_round_trips()
        {
            Store.EnsureCreated();
            PaymentTransactionRecord saved = TestRecords.Full(TransactionOutcome.Approved, TestRecords.UtcMs(2026, 11, 10, 9, 30, 15, 123));
            Store.SaveTransaction(saved);

            PaymentTransactionRecord loaded = Store.GetTransactions(saved.CreatedAtUtc.AddMinutes(-1), saved.CreatedAtUtc.AddMinutes(1)).Single();

            Assert.Equal(JsonConvert.SerializeObject(saved), JsonConvert.SerializeObject(loaded));
            Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
        }

        [Fact]
        public void Every_outcome_is_saved_paid_declined_cancelled_timeout_and_errors()
        {
            Store.EnsureCreated();
            DateTime start = TestRecords.UtcMs(2026, 11, 10, 10, 0, 0, 0);
            TransactionOutcome[] all = (TransactionOutcome[])Enum.GetValues(typeof(TransactionOutcome));
            for (int i = 0; i < all.Length; i++)
                Store.SaveTransaction(TestRecords.Full(all[i], start.AddSeconds(i)));

            IList<PaymentTransactionRecord> loaded = Store.GetTransactions(start, start.AddHours(1));

            Assert.Equal(all, loaded.Select(r => r.Outcome).ToArray());   // EN: oldest first   AR: الأقدم أولاً
        }

        [Fact]
        public void Nullable_fields_stay_null()
        {
            Store.EnsureCreated();
            var cash = PaymentTransactionRecord.Cash(500, new PaymentContext { Workstation = "RECEPTION-02", WindowsUser = "u", PatientMrn = "MRN1" });
            cash.CreatedAtUtc = TestRecords.UtcMs(2026, 11, 10, 11, 0, 0, 0);
            Store.SaveTransaction(cash);

            PaymentTransactionRecord loaded = Store.GetTransactions(cash.CreatedAtUtc, cash.CreatedAtUtc.AddSeconds(1)).Single();

            Assert.Equal(PaymentMethod.Cash, loaded.PaymentMethod);
            Assert.Equal(500L, loaded.AmountHalalas);
            Assert.Null(loaded.ReturnCode);
            Assert.Null(loaded.MaskedPan);
            Assert.Null(loaded.InvoiceNo);
        }

        [Fact]
        public void Saving_the_same_transaction_twice_keeps_one_row()
        {
            Store.EnsureCreated();
            PaymentTransactionRecord record = TestRecords.Full(TransactionOutcome.Approved, TestRecords.UtcMs(2026, 11, 10, 12, 0, 0, 0));
            Store.SaveTransaction(record);
            record.Outcome = TransactionOutcome.Declined;   // EN: a re-import must not change the original   AR: إعادة الاستيراد لا تغيّر الأصل
            Store.SaveTransaction(record);

            PaymentTransactionRecord loaded = Store.GetTransactions(record.CreatedAtUtc, record.CreatedAtUtc.AddSeconds(1)).Single();
            Assert.Equal(TransactionOutcome.Approved, loaded.Outcome);
        }

        [Fact]
        public void Date_range_includes_from_and_excludes_to()
        {
            Store.EnsureCreated();
            DateTime t = TestRecords.UtcMs(2026, 11, 9, 23, 59, 59, 999);
            Store.SaveTransaction(TestRecords.Full(TransactionOutcome.Approved, t));
            Store.SaveTransaction(TestRecords.Full(TransactionOutcome.Approved, t.AddMilliseconds(1)));

            Assert.Single(Store.GetTransactions(t, t.AddMilliseconds(1)));
            Assert.Equal(2, Store.GetTransactions(t, t.AddDays(1)).Count);
            Assert.Empty(Store.GetTransactions(t.AddDays(2), t.AddDays(3)));
        }

        [Fact]
        public void Errors_are_saved()
        {
            Store.EnsureCreated();
            Store.SaveError(ErrorLogRecord.FromException("test", new InvalidOperationException("card 4464041234561234 failed")));
            Store.SaveError(new ErrorLogRecord { Source = "test", Message = "no details" });
        }

        [Fact]
        public void Mapping_insert_update_get_list_delete()
        {
            Store.EnsureCreated();
            Store.SaveMapping(TestRecords.Mapping("CASHIER-01", "SPAN-ER-01"));
            Store.SaveMapping(TestRecords.Mapping("CASHIER-02", "SPAN-OPD-01"));

            TerminalMapping changed = TestRecords.Mapping("CASHIER-01", "SPAN-ER-02");
            changed.Transport = TerminalTransport.Tcp;
            Store.SaveMapping(changed);

            TerminalMapping loaded = Store.GetMapping("CASHIER-01");
            changed.IsActive = true;   // EN: the only mapping of the PC is always the active one   AR: الربط الوحيد للحاسوب فعّال دائماً
            Assert.Equal(JsonConvert.SerializeObject(changed), JsonConvert.SerializeObject(loaded));
            Assert.Equal("CASHIER-01|Geidea", loaded.Id);
            Assert.Equal(new[] { "CASHIER-01", "CASHIER-02" }, Store.GetMappings().Select(m => m.Workstation).ToArray());
            Assert.Null(Store.GetMapping("UNKNOWN-PC"));

            Store.DeleteMapping("cashier-01 ", TerminalProvider.Geidea);
            Assert.Null(Store.GetMapping("CASHIER-01"));
            Assert.Single(Store.GetMappings());
        }

        [Fact]
        public void Pc_keeps_both_providers_with_exactly_one_active()
        {
            Store.EnsureCreated();
            Store.SaveMapping(TestRecords.Mapping("cashier-01", "A920-1"));            // EN: first one becomes active   AR: الأول يصبح فعّالاً
            Store.SaveMapping(NamiPay("CASHIER-01", "SKY-1", active: false));
            Store.SaveMapping(NamiPay("CASHIER-02", "SKY-2", active: false));

            Assert.Equal(new[] { "CASHIER-01|Geidea", "CASHIER-01|NamiPay", "CASHIER-02|NamiPay" }, Store.GetMappings().Select(m => m.Id).ToArray());
            Assert.Equal(TerminalProvider.Geidea, Store.GetMapping("CASHIER-01").Provider);
            Assert.Equal("SKY-2", Store.GetMapping("CASHIER-02").TerminalName);
            Assert.Equal(1, Store.GetMappings().Count(m => m.Workstation == "CASHIER-01" && m.IsActive));

            // EN: Set active switches the default; the other PC is not touched.   AR: تعيين الفعّال يغيّر الافتراضي ولا يمس الحاسوب الآخر.
            Store.SetActiveMapping("CASHIER-01", TerminalProvider.NamiPay);
            Assert.Equal("SKY-1", Store.GetMapping("CASHIER-01").TerminalName);
            Assert.Equal(1, Store.GetMappings().Count(m => m.Workstation == "CASHIER-01" && m.IsActive));
            Assert.Equal("SKY-2", Store.GetMapping("CASHIER-02").TerminalName);

            // EN: Saving the inactive one unchanged does not steal "active"; saving it as active does.
            // AR: حفظ غير الفعّال بدون تفعيل لا يأخذ "الفعّال"، وحفظه كفعّال يأخذه.
            Store.SaveMapping(TestRecords.Mapping("CASHIER-01", "A920-2"));
            Assert.Equal(TerminalProvider.NamiPay, Store.GetMapping("CASHIER-01").Provider);
            TerminalMapping geidea = TestRecords.Mapping("CASHIER-01", "A920-2");
            geidea.IsActive = true;
            Store.SaveMapping(geidea);
            Assert.Equal("A920-2", Store.GetMapping("CASHIER-01").TerminalName);
            Assert.Equal(1, Store.GetMappings().Count(m => m.Workstation == "CASHIER-01" && m.IsActive));

            // EN: Deleting the active one promotes the other.   AR: حذف الفعّال يرقّي الآخر.
            Store.DeleteMapping("CASHIER-01", TerminalProvider.Geidea);
            Assert.Equal("SKY-1", Store.GetMapping("CASHIER-01").TerminalName);
            Store.DeleteMapping("CASHIER-01", TerminalProvider.NamiPay);
            Assert.Null(Store.GetMapping("CASHIER-01"));
            Assert.Equal(new[] { "CASHIER-02|NamiPay" }, Store.GetMappings().Select(m => m.Id).ToArray());
        }

        [Fact]
        public void Set_active_of_an_unsaved_provider_is_rejected()
        {
            Store.EnsureCreated();
            Store.SaveMapping(TestRecords.Mapping("CASHIER-01", "A920-1"));
            Assert.Throws<ArgumentException>(() => Store.SetActiveMapping("CASHIER-01", TerminalProvider.NamiPay));
            Assert.Equal(TerminalProvider.Geidea, Store.GetMapping("CASHIER-01").Provider);
        }

        private static TerminalMapping NamiPay(string workstation, string terminal, bool active)
        {
            TerminalMapping mapping = TestRecords.Mapping(workstation, terminal);
            mapping.Provider = TerminalProvider.NamiPay;
            mapping.ComPort = 9;
            mapping.CashRegisterNumber = "12345678";
            mapping.IsActive = active;
            return mapping;
        }

        [Fact]
        public void Mapping_without_workstation_is_rejected()
        {
            Store.EnsureCreated();
            Assert.Throws<ArgumentException>(() => Store.SaveMapping(TestRecords.Mapping(" ", "SPAN")));
        }
    }

    public sealed class SqliteStoreTests : StoreContractTests
    {
        private readonly string _folder = TestRecords.NewTempFolder();
        private readonly SqliteTransactionStore _store;

        public SqliteStoreTests()
        {
            _store = new SqliteTransactionStore(Path.Combine(_folder, "payments.db"));
        }

        protected override ITransactionStore Store { get { return _store; } }

        [Fact]
        public void Database_file_is_created()
        {
            _store.EnsureCreated();
            Assert.True(File.Exists(_store.DatabaseFile));
        }

        public override void Dispose()
        {
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }

    public sealed class JsonStoreTests : StoreContractTests
    {
        private readonly string _folder = TestRecords.NewTempFolder();
        private readonly JsonTransactionStore _store;

        public JsonStoreTests()
        {
            _store = new JsonTransactionStore(_folder);
        }

        protected override ITransactionStore Store { get { return _store; } }

        [Fact]
        public void Files_are_readable_json_with_enum_names_and_no_type_info()
        {
            _store.EnsureCreated();
            PaymentTransactionRecord record = TestRecords.Full(TransactionOutcome.Cancelled, TestRecords.UtcMs(2026, 11, 10, 9, 0, 0, 0));
            _store.SaveTransaction(record);

            string json = File.ReadAllText(Path.Combine(_folder, "transactions", "2026-11-10", record.Id + ".json"));
            Assert.Contains("\"Outcome\": \"Cancelled\"", json);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public void Workstation_name_cannot_escape_the_folder()
        {
            _store.EnsureCreated();
            _store.SaveMapping(TestRecords.Mapping(@"..\..\EVIL", "SPAN"));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_folder), "*.json"));
            Assert.Single(Directory.GetFiles(Path.Combine(_folder, "mappings")));
        }

        [Fact]
        public void Previous_version_mapping_file_becomes_the_active_mapping()
        {
            // EN: The previous version wrote mappings\<WORKSTATION>.json with no Id / IsActive.
            // AR: النسخة السابقة كتبت mappings\<WORKSTATION>.json بدون Id و IsActive.
            string folder = Path.Combine(_folder, "mappings");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "OLD-PC.json"),
                "{ \"Workstation\": \"OLD-PC\", \"TerminalName\": \"SPAN-1\", \"Transport\": \"Com\", \"ComPort\": 8 }");

            _store.EnsureCreated();

            TerminalMapping migrated = _store.GetMapping("OLD-PC");
            Assert.Equal("OLD-PC|Geidea", migrated.Id);
            Assert.True(migrated.IsActive);
            Assert.Equal("SPAN-1", migrated.TerminalName);
            Assert.Equal(new[] { "OLD-PC_Geidea.json" }, Directory.GetFiles(folder).Select(Path.GetFileName).ToArray());
        }

        public override void Dispose()
        {
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }

    /// <summary>
    /// EN: Real SQL Server: a fresh database on LocalDB ((localdb)\MSSQLLocalDB) per test class, dropped afterwards.
    /// AR: SQL Server فعلي: قاعدة بيانات جديدة على LocalDB لكل فئة اختبار، تُحذف بعد الانتهاء.
    /// </summary>
    public sealed class SqlServerStoreTests : StoreContractTests
    {
        // EN: LocalDB does not support Encrypt=True with System.Data.SqlClient (a real server should use it).
        // AR: LocalDB لا يدعم Encrypt=True مع System.Data.SqlClient (الخادم الفعلي يجب أن يستخدمه).
        private const string Server = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True";
        private readonly string _database = "CDLL_Payments_Tests_" + Guid.NewGuid().ToString("N");
        private readonly SqlServerTransactionStore _store;

        public SqlServerStoreTests()
        {
            Execute("CREATE DATABASE [" + _database + "]");
            _store = new SqlServerTransactionStore(Server + ";Initial Catalog=" + _database);
        }

        protected override ITransactionStore Store { get { return _store; } }

        [Fact]
        public void Tables_use_SQL_Server_types()
        {
            _store.EnsureCreated();
            using (var connection = new SqlConnection(Server + ";Initial Catalog=" + _database))
            using (var command = new SqlCommand("SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentTransactions' AND COLUMN_NAME = @c", connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@c", "CreatedAtUtc");
                Assert.Equal("datetime2", command.ExecuteScalar());
                command.Parameters["@c"].Value = "AmountHalalas";
                Assert.Equal("bigint", command.ExecuteScalar());
            }
        }

        public override void Dispose()
        {
            SqlConnection.ClearAllPools();
            Execute("ALTER DATABASE [" + _database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + _database + "]");
        }

        private static void Execute(string sql)
        {
            using (var connection = new SqlConnection(Server + ";Initial Catalog=master"))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
