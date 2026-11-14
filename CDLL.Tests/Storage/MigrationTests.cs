// =====================================================================================
// EN: A database made by the previous version (no Provider / ResponseMessage / CashRegisterNumber columns, one
//     TerminalMappings row per PC) is upgraded in place by EnsureCreated(): old rows still load (Provider = Geidea),
//     the old mapping becomes the PC's active one in WorkstationTerminals, new rows keep NamiPay.
//     Run against SQLite and a real SQL Server (LocalDB).
// AR: قاعدة بيانات من النسخة السابقة (بدون أعمدة Provider و ResponseMessage و CashRegisterNumber) تُرقّى في مكانها:
//     السجلات القديمة تُقرأ (Provider = Geidea) والجديدة تحتفظ بـ NamiPay. يُختبر على SQLite و SQL Server.
// =====================================================================================

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using CDLL.Payments;
using CDLL.Payments.Storage;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CDLL.Tests.Storage
{
    public sealed class MigrationTests : IDisposable
    {
        private static readonly string[] NewColumns = { "Provider", "ResponseMessage", "CashRegisterNumber" };
        private static readonly string[] NewMappingColumns = { "Id", "IsActive" };   // EN: old TerminalMappings only   AR: لجدول الربط القديم فقط
        private const string LocalDb = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True";
        private readonly string _folder = TestRecords.NewTempFolder();
        private readonly string _database = "CDLL_Payments_Migration_" + Guid.NewGuid().ToString("N");
        private bool _sqlServerCreated;

        [Fact]
        public void Sqlite_old_tables_get_the_new_columns()
        {
            string file = Path.Combine(_folder, "old.db");
            using (var connection = new SqliteConnection("Data Source=" + file))
            {
                connection.Open();
                CreateOldTables(connection, false);
            }

            Verify(new SqliteTransactionStore(file));
        }

        [Fact]
        public void SqlServer_old_tables_get_the_new_columns()
        {
            Execute(LocalDb + ";Initial Catalog=master", "CREATE DATABASE [" + _database + "]");
            _sqlServerCreated = true;
            string connectionString = LocalDb + ";Initial Catalog=" + _database;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                CreateOldTables(connection, true);
            }

            Verify(new SqlServerTransactionStore(connectionString));
        }

        private static void Verify(ITransactionStore store)
        {
            DateTime t = TestRecords.UtcMs(2026, 11, 10, 9, 0, 0, 0);

            store.EnsureCreated();
            store.EnsureCreated();   // EN: second run finds nothing to add   AR: التشغيل الثاني لا يجد شيئاً لإضافته

            PaymentTransactionRecord old = store.GetTransactions(t, t.AddSeconds(1)).Single();
            Assert.Equal(TerminalProvider.Geidea, old.Provider);
            Assert.Null(old.ResponseMessage);

            PaymentTransactionRecord namiPay = TestRecords.Full(TransactionOutcome.Approved, t.AddSeconds(5));
            namiPay.Provider = TerminalProvider.NamiPay;
            namiPay.ResponseMessage = "APPROVED";
            store.SaveTransaction(namiPay);
            PaymentTransactionRecord loaded = store.GetTransactions(t.AddSeconds(5), t.AddSeconds(6)).Single();
            Assert.Equal(TerminalProvider.NamiPay, loaded.Provider);
            Assert.Equal("APPROVED", loaded.ResponseMessage);

            // EN: The old one-row-per-PC mapping is now the PC's active Geidea mapping.
            // AR: ربط النسخة القديمة (سجل لكل حاسوب) أصبح ربط Geidea الفعّال للحاسوب.
            TerminalMapping legacy = store.GetMapping("old-pc");
            Assert.Equal("OLD-PC|Geidea", legacy.Id);
            Assert.True(legacy.IsActive);
            Assert.Equal("SPAN-1", legacy.TerminalName);
            Assert.Equal(8, legacy.ComPort);
            Assert.Single(store.GetMappings());

            TerminalMapping mapping = TestRecords.Mapping("NAMI-PC", "SKY-1");
            mapping.Provider = TerminalProvider.NamiPay;
            mapping.CashRegisterNumber = "12345678";
            store.SaveMapping(mapping);
            TerminalMapping back = store.GetMapping("NAMI-PC");
            Assert.Equal(TerminalProvider.NamiPay, back.Provider);
            Assert.Equal("12345678", back.CashRegisterNumber);
        }

        /// <summary>EN: Tables exactly as the previous version created them, plus one old row.  AR: الجداول كما أنشأتها النسخة السابقة مع سجل قديم.</summary>
        private static void CreateOldTables(DbConnection connection, bool sqlServer)
        {
            foreach (var schema in new[] { SqlTransactionStore.Transactions, SqlTransactionStore.Errors, SqlTransactionStore.LegacyMappings })
            {
                string columns = string.Join(", ", schema.Columns
                    .Where(c => !NewColumns.Contains(c.Name) && !(schema == SqlTransactionStore.LegacyMappings && NewMappingColumns.Contains(c.Name)))
                    .Select(c => c.Name + " " + schema.SqlType(c, sqlServer)));
                Run(connection, "CREATE TABLE " + (sqlServer ? "[dbo].[" + schema.Table + "]" : "\"" + schema.Table + "\"") + " (" + columns + ")");
            }

            string created = sqlServer ? "'2026-11-10T09:00:00'" : "'2026-11-10T09:00:00.0000000Z'";
            Run(connection, "INSERT INTO PaymentTransactions (Id, CreatedAtUtc, DurationMs, PaymentMethod, Operation, Transport, Outcome) " +
                            "VALUES ('old1', " + created + ", 10, 'Card', 'Purchase', 'Com', 'Approved')");
            Run(connection, "INSERT INTO TerminalMappings (Workstation, TerminalName, Transport, ComPort, BaudRate, Parity, DataBits, StopBits, UpdatedAtUtc) " +
                            "VALUES ('OLD-PC', 'SPAN-1', 'Com', 8, 38400, 0, 8, 1, " + created + ")");
        }

        private static void Run(DbConnection connection, string sql)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private static void Execute(string connectionString, string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Run(connection, sql);
            }
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            SqlConnection.ClearAllPools();
            if (_sqlServerCreated)
                Execute(LocalDb + ";Initial Catalog=master", "ALTER DATABASE [" + _database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + _database + "]");
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }
}
