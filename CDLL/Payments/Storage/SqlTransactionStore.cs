// =====================================================================================
// EN: Shared ADO.NET code for SQLite and SQL Server. The two subclasses only say how to open a connection
//     and the few SQL differences (create-if-missing, insert-if-missing, date type).
//     Tables: PaymentTransactions (every operation, every outcome), ErrorLog, WorkstationTerminals (one per PC + provider, one active per PC; the old TerminalMappings table is copied once).
//     SECURITY: every value is a parameter (@p...), never concatenated into SQL.
//     Each write runs in its own database transaction, so a failure never leaves a half-written row.
// AR: كود ADO.NET المشترك بين SQLite و SQL Server. الفئتان الفرعيتان تحددان فقط طريقة فتح الاتصال
//     والاختلافات البسيطة في SQL (الإنشاء عند عدم الوجود، الإدراج عند عدم الوجود، نوع التاريخ).
//     الجداول: PaymentTransactions (كل عملية وكل نتيجة)، ErrorLog، WorkstationTerminals (لكل حاسوب + مزوّد، وواحد فعّال لكل حاسوب).
//     الأمان: كل قيمة تُرسل كمعامل (@p...) ولا تُدمج داخل نص SQL أبداً.
//     كل عملية كتابة تتم داخل معاملة قاعدة بيانات خاصة بها، فلا يبقى سجل مكتوب جزئياً عند الفشل.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CDLL.Payments.Storage
{
    public abstract class SqlTransactionStore : ITransactionStore
    {
        internal static readonly TableSchema Transactions = TableSchema.For<PaymentTransactionRecord>("PaymentTransactions", "Id");
        internal static readonly TableSchema Errors = TableSchema.For<ErrorLogRecord>("ErrorLog", "Id");
        /// <summary>EN: One row per PC + provider ("WORKSTATION|Provider"), one active per PC.  AR: سجل لكل حاسوب + مزوّد، وواحد فعّال لكل حاسوب.</summary>
        internal static readonly TableSchema Mappings = TableSchema.For<TerminalMapping>("WorkstationTerminals", "Id");

        /// <summary>EN: Previous version: one row per PC. Copied into WorkstationTerminals once.  AR: النسخة السابقة: سجل لكل حاسوب، يُنسخ مرة واحدة.</summary>
        internal static readonly TableSchema LegacyMappings = TableSchema.For<TerminalMapping>("TerminalMappings", "Workstation");

        public abstract string Name { get; }

        protected abstract bool IsSqlServer { get; }

        protected abstract DbConnection CreateConnection();

        /// <summary>EN: Quoted table name, e.g. [dbo].[PaymentTransactions] or "PaymentTransactions".  AR: اسم الجدول بين علامات الاقتباس.</summary>
        protected abstract string QuoteTable(string table);

        private protected abstract string CreateTableIfMissingSql(TableSchema schema, string columnsSql);

        protected abstract string CreateIndexIfMissingSql(string table, string index, string column);

        /// <summary>EN: INSERT that does nothing when the key already exists.  AR: إدراج لا يفعل شيئاً إذا كان المفتاح موجوداً.</summary>
        private protected abstract string InsertIfMissingSql(TableSchema schema, string columns, string values);

        /// <summary>EN: Query returning the column names of a table (first column).  AR: استعلام يُعيد أسماء أعمدة الجدول.</summary>
        protected abstract string ColumnNamesSql(string table);

        protected abstract string AddColumnSql(string table, string columnDefinition);

        // =============================================================================
        // EN: ITransactionStore
        // =============================================================================

        public void EnsureCreated()
        {
            using (DbConnection connection = Open())
            {
                foreach (TableSchema schema in new[] { Transactions, Errors, Mappings })
                {
                    string columns = string.Join(", ", schema.Columns.Select(c => c.Name + " " + schema.SqlType(c, IsSqlServer)));
                    Execute(connection, null, CreateTableIfMissingSql(schema, columns));
                    AddMissingColumns(connection, schema);
                }
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_CreatedAtUtc", "CreatedAtUtc"));
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_PatientMrn", "PatientMrn"));
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_InvoiceNo", "InvoiceNo"));
                Execute(connection, null, CreateIndexIfMissingSql(Mappings.Table, "IX_WorkstationTerminals_Workstation", "Workstation"));
                MigrateLegacyMappings(connection);
            }
        }

        /// <summary>
        /// EN: Copies the previous version's one-terminal-per-PC table into WorkstationTerminals (each becomes the PC's
        ///     active mapping). Runs only while the new table is still empty; the old table is left untouched.
        /// AR: ينسخ جدول النسخة السابقة (جهاز واحد لكل حاسوب) إلى WorkstationTerminals (كل ربط يصبح الفعّال للحاسوب).
        ///     يعمل فقط والجدول الجديد فارغ، والجدول القديم لا يُعدّل.
        /// </summary>
        private void MigrateLegacyMappings(DbConnection connection)
        {
            if (ColumnNames(connection, LegacyMappings.Table).Count == 0)
                return;   // EN: no old table   AR: لا يوجد جدول قديم
            if (Convert.ToInt64(Scalar(connection, null, "SELECT COUNT(*) FROM " + QuoteTable(Mappings.Table)), CultureInfo.InvariantCulture) > 0)
                return;

            AddMissingColumns(connection, LegacyMappings);
            foreach (TerminalMapping old in Query<TerminalMapping>(LegacyMappings, ""))
            {
                old.Normalize();
                old.IsActive = true;
                Insert(Mappings, old);
            }
        }

        public void SaveTransaction(PaymentTransactionRecord record)
        {
            Insert(Transactions, record);
        }

        public void SaveError(ErrorLogRecord record)
        {
            Insert(Errors, record);
        }

        public void SaveMapping(TerminalMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.Workstation))
                throw new ArgumentException("Workstation is required. / اسم الحاسوب مطلوب.");
            mapping.Normalize();

            List<PropertyInfo> columns = Mappings.Columns.ToList();
            string assignments = string.Join(", ", columns.Where(c => c.Name != Mappings.Key).Select(c => c.Name + " = @" + c.Name));
            string update = "UPDATE " + QuoteTable(Mappings.Table) + " SET " + assignments + " WHERE " + Mappings.Key + " = @" + Mappings.Key;
            string insert = "INSERT INTO " + QuoteTable(Mappings.Table) + " (" + string.Join(", ", columns.Select(c => c.Name)) +
                            ") VALUES (" + string.Join(", ", columns.Select(c => "@" + c.Name)) + ")";

            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                if (mapping.IsActive)
                    Execute(connection, transaction, "UPDATE " + QuoteTable(Mappings.Table) + " SET IsActive = @p0 WHERE Workstation = @p1 AND Id <> @p2",
                        P(false, mapping.Workstation, mapping.Id));
                if (Execute(connection, transaction, update, Parameters(Mappings, mapping)) == 0)
                    Execute(connection, transaction, insert, Parameters(Mappings, mapping));
                KeepOneActive(connection, transaction, mapping.Workstation, mapping.Id);
                transaction.Commit();
            }
        }

        public void DeleteMapping(string workstation, TerminalProvider provider)
        {
            string ws = (workstation ?? "").Trim().ToUpperInvariant();
            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, "DELETE FROM " + QuoteTable(Mappings.Table) + " WHERE Id = @p0",
                    P(TerminalMapping.MakeId(ws, provider)));
                KeepOneActive(connection, transaction, ws, null);
                transaction.Commit();
            }
        }

        public void SetActiveMapping(string workstation, TerminalProvider provider)
        {
            string ws = (workstation ?? "").Trim().ToUpperInvariant();
            string id = TerminalMapping.MakeId(ws, provider);
            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                if (Execute(connection, transaction, "UPDATE " + QuoteTable(Mappings.Table) + " SET IsActive = @p0 WHERE Id = @p1", P(true, id)) == 0)
                    throw new ArgumentException("No " + provider + " mapping is saved for " + ws + ". / لا يوجد ربط " + provider + " محفوظ لهذا الحاسوب.");
                Execute(connection, transaction, "UPDATE " + QuoteTable(Mappings.Table) + " SET IsActive = @p0 WHERE Workstation = @p1 AND Id <> @p2",
                    P(false, ws, id));
                transaction.Commit();
            }
        }

        public TerminalMapping GetMapping(string workstation)
        {
            return Query<TerminalMapping>(Mappings, " WHERE Workstation = @p0 AND IsActive = @p1",
                (workstation ?? "").Trim().ToUpperInvariant(), true).FirstOrDefault();
        }

        public IList<TerminalMapping> GetMappings()
        {
            return Query<TerminalMapping>(Mappings, " ORDER BY Workstation, Provider");
        }

        /// <summary>
        /// EN: Rule "a PC with mappings has exactly one active": if none is active, activate the preferred one
        ///     (the one just saved) or else the PC's first one.
        /// AR: قاعدة "الحاسوب الذي له ربطات له ربط فعّال واحد": إذا لم يوجد فعّال، يُفعّل المفضّل (المحفوظ للتو) أو الأول.
        /// </summary>
        private void KeepOneActive(DbConnection connection, DbTransaction transaction, string workstation, string preferredId)
        {
            string table = QuoteTable(Mappings.Table);
            long active = Convert.ToInt64(Scalar(connection, transaction, "SELECT COUNT(*) FROM " + table + " WHERE Workstation = @p0 AND IsActive = @p1",
                P(workstation, true)), CultureInfo.InvariantCulture);
            if (active > 0)
                return;

            object id = preferredId ?? Scalar(connection, transaction, "SELECT MIN(Id) FROM " + table + " WHERE Workstation = @p0", P(workstation));
            if (id != null && id != DBNull.Value)
                Execute(connection, transaction, "UPDATE " + table + " SET IsActive = @p0 WHERE Id = @p1", P(true, id));
        }

        /// <summary>EN: Positional parameters @p0, @p1...  AR: معاملات بالترتيب @p0 و @p1...</summary>
        private static KeyValuePair<string, object>[] P(params object[] values)
        {
            return values.Select((v, i) => new KeyValuePair<string, object>("@p" + i.ToString(CultureInfo.InvariantCulture), v)).ToArray();
        }

        private object Scalar(DbConnection connection, DbTransaction transaction, string sql, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                if (parameters != null)
                    foreach (KeyValuePair<string, object> p in parameters)
                        AddParameter(command, p.Key, p.Value);
                return command.ExecuteScalar();
            }
        }

        public IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc)
        {
            return Query<PaymentTransactionRecord>(Transactions, " WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc < @p1 ORDER BY CreatedAtUtc",
                fromUtc, toUtc);
        }

        // =============================================================================
        // EN: HELPERS / دوال مساعدة
        // =============================================================================

        /// <summary>
        /// EN: Upgrades a table made by an older version (e.g. before Provider / ResponseMessage existed): every missing
        ///     column is added as NULLable; NULL reads back as the property's default (Provider = Geidea).
        /// AR: يرقّي جدولاً أنشأته نسخة أقدم: كل عمود ناقص يُضاف ويقبل NULL، و NULL يُقرأ كقيمة افتراضية (Provider = Geidea).
        /// </summary>
        private void AddMissingColumns(DbConnection connection, TableSchema schema)
        {
            HashSet<string> existing = ColumnNames(connection, schema.Table);
            foreach (PropertyInfo column in schema.Columns.Where(c => !existing.Contains(c.Name)))
            {
                string type = schema.SqlType(column, IsSqlServer).Replace(" NOT NULL", " NULL");
                Execute(connection, null, AddColumnSql(schema.Table, column.Name + " " + type));
            }
        }

        /// <summary>EN: Column names of a table; empty when the table does not exist.  AR: أسماء أعمدة الجدول، وفارغة إذا لم يوجد.</summary>
        private HashSet<string> ColumnNames(DbConnection connection, string table)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = ColumnNamesSql(table);
                using (DbDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        names.Add(Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture));
            }
            return names;
        }

        private void Insert(TableSchema schema, object record)
        {
            string columns = string.Join(", ", schema.Columns.Select(c => c.Name));
            string values = string.Join(", ", schema.Columns.Select(c => "@" + c.Name));

            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, InsertIfMissingSql(schema, columns, values), Parameters(schema, record));
                transaction.Commit();
            }
        }

        private List<T> Query<T>(TableSchema schema, string whereAndOrder, params object[] values) where T : new()
        {
            var rows = new List<T>();
            using (DbConnection connection = Open())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT " + string.Join(", ", schema.Columns.Select(c => c.Name)) +
                                      " FROM " + QuoteTable(schema.Table) + whereAndOrder;
                for (int i = 0; i < values.Length; i++)
                    AddParameter(command, "@p" + i, values[i]);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new T();
                        for (int i = 0; i < schema.Columns.Count; i++)
                        {
                            PropertyInfo column = schema.Columns[i];
                            if (!reader.IsDBNull(i))
                                column.SetValue(row, FromDb(reader.GetValue(i), column.PropertyType));
                        }
                        rows.Add(row);
                    }
                }
            }
            return rows;
        }

        private DbConnection Open()
        {
            DbConnection connection = CreateConnection();
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private int Execute(DbConnection connection, DbTransaction transaction, string sql, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                if (parameters != null)
                    foreach (KeyValuePair<string, object> p in parameters)
                        AddParameter(command, p.Key, p.Value);
                return command.ExecuteNonQuery();
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> Parameters(TableSchema schema, object record)
        {
            return schema.Columns.Select(c => new KeyValuePair<string, object>("@" + c.Name, c.GetValue(record)));
        }

        private void AddParameter(DbCommand command, string name, object value)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;

            if (value == null)
            {
                parameter.Value = DBNull.Value;
            }
            else if (value is DateTime)
            {
                DateTime utc = ToUtc((DateTime)value);
                if (IsSqlServer)
                {
                    parameter.DbType = DbType.DateTime2;
                    parameter.Value = utc;
                }
                else
                {
                    // EN: ISO-8601 "o" text sorts in time order, so range queries work on TEXT.
                    // AR: نص ISO-8601 بصيغة "o" يُرتّب زمنياً، لذلك يعمل البحث بالفترات على عمود TEXT.
                    parameter.Value = utc.ToString("o", CultureInfo.InvariantCulture);
                }
            }
            else if (value is Enum)
            {
                parameter.Value = value.ToString();
            }
            else
            {
                parameter.Value = value;
            }
            command.Parameters.Add(parameter);
        }

        private static object FromDb(object value, Type propertyType)
        {
            Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (type == typeof(string)) return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (type == typeof(int)) return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (type == typeof(long)) return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            if (type.IsEnum) return Enum.Parse(type, Convert.ToString(value, CultureInfo.InvariantCulture));
            if (type == typeof(DateTime))
            {
                return value is DateTime
                    ? DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc)
                    : DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        }
    }
}
