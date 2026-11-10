// =====================================================================================
// EN: SQL Server back-end (central hospital database). Connection string = <connectionStrings> "PaymentsSqlServer".
//     The DATABASE must already exist (created by the DBA); EnsureCreated() only creates the 3 tables.
//     SECURITY: use Integrated Security (Windows login) and Encrypt=True; give the app login only
//     SELECT / INSERT / UPDATE on the 3 tables (CREATE TABLE only for the first run).
// AR: تخزين SQL Server (قاعدة بيانات المستشفى المركزية). نص الاتصال = "PaymentsSqlServer" في <connectionStrings>.
//     يجب أن تكون قاعدة البيانات موجودة مسبقاً (ينشئها مسؤول قواعد البيانات)، و EnsureCreated() ينشئ الجداول الثلاثة فقط.
//     الأمان: استخدم Integrated Security (حساب Windows) و Encrypt=True، وامنح حساب البرنامج صلاحيات
//     SELECT و INSERT و UPDATE فقط على الجداول الثلاثة (و CREATE TABLE في أول تشغيل فقط).
// =====================================================================================

using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace CDLL.Payments.Storage
{
    public sealed class SqlServerTransactionStore : SqlTransactionStore
    {
        private readonly string _connectionString;

        public SqlServerTransactionStore(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("SQL Server connection string is empty. / نص الاتصال بـ SQL Server فارغ.", "connectionString");
            _connectionString = connectionString;
        }

        public override string Name { get { return "SqlServer"; } }

        protected override bool IsSqlServer { get { return true; } }

        protected override DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        protected override string QuoteTable(string table)
        {
            return "[dbo].[" + table + "]";
        }

        private protected override string CreateTableIfMissingSql(TableSchema schema, string columnsSql)
        {
            return "IF OBJECT_ID(N'dbo." + schema.Table + "', N'U') IS NULL CREATE TABLE " + QuoteTable(schema.Table) + " (" + columnsSql + ")";
        }

        protected override string CreateIndexIfMissingSql(string table, string index, string column)
        {
            return "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'" + index + "' AND object_id = OBJECT_ID(N'dbo." + table + "')) " +
                   "CREATE INDEX " + index + " ON " + QuoteTable(table) + " (" + column + ")";
        }

        private protected override string InsertIfMissingSql(TableSchema schema, string columns, string values)
        {
            return "IF NOT EXISTS (SELECT 1 FROM " + QuoteTable(schema.Table) + " WHERE " + schema.Key + " = @" + schema.Key + ") " +
                   "INSERT INTO " + QuoteTable(schema.Table) + " (" + columns + ") VALUES (" + values + ")";
        }
    }
}
