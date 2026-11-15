// =====================================================================================
// EN: SQLite back-end: one local database file (default %ProgramData%\CDLL\Payments\payments.db).
//     Needs no server, so it works on any workstation. Uses System.Data.SQLite (NuGet; works on .NET 4.0).
//     Same file format as the .NET 4.8 build (dates as ISO-8601 text), so both can open the same payments.db.
// AR: تخزين SQLite: ملف قاعدة بيانات محلي واحد (افتراضياً %ProgramData%\CDLL\Payments\payments.db).
//     لا يحتاج خادماً، لذلك يعمل على أي حاسوب. يستخدم System.Data.SQLite (من NuGet ويعمل على ‎.NET 4.0).
//     نفس صيغة ملف نسخة ‎.NET 4.8، لذلك يمكن للنسختين فتح نفس payments.db.
// =====================================================================================

using System.Data.Common;
using System.IO;
using System.Data.SQLite;

namespace CDLL.Payments.Storage
{
    public sealed class SqliteTransactionStore : SqlTransactionStore
    {
        private readonly string _connectionString;

        public SqliteTransactionStore(string databaseFile)
        {
            string folder = Path.GetDirectoryName(Path.GetFullPath(databaseFile));
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            DatabaseFile = databaseFile;
            // EN: Pooling on (like Microsoft.Data.Sqlite); FailIfMissing=false creates the file.
            // AR: تجميع الاتصالات مفعّل، و FailIfMissing=false ينشئ الملف إذا لم يوجد.
            _connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFile,
                FailIfMissing = false,
                Pooling = true
            }.ToString();
        }

        public string DatabaseFile { get; private set; }

        public override string Name { get { return "Sqlite"; } }

        protected override bool IsSqlServer { get { return false; } }

        protected override DbConnection CreateConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        protected override string QuoteTable(string table)
        {
            return "\"" + table + "\"";
        }

        private protected override string CreateTableIfMissingSql(TableSchema schema, string columnsSql)
        {
            return "CREATE TABLE IF NOT EXISTS " + QuoteTable(schema.Table) + " (" + columnsSql + ")";
        }

        protected override string CreateIndexIfMissingSql(string table, string index, string column)
        {
            return "CREATE INDEX IF NOT EXISTS " + index + " ON " + QuoteTable(table) + " (" + column + ")";
        }

        private protected override string InsertIfMissingSql(TableSchema schema, string columns, string values)
        {
            return "INSERT OR IGNORE INTO " + QuoteTable(schema.Table) + " (" + columns + ") VALUES (" + values + ")";
        }

        protected override string ColumnNamesSql(string table)
        {
            return "SELECT name FROM pragma_table_info('" + table + "')";
        }

        protected override string AddColumnSql(string table, string columnDefinition)
        {
            return "ALTER TABLE " + QuoteTable(table) + " ADD COLUMN " + columnDefinition;
        }
    }
}
