// =====================================================================================
// EN: Shared ADO.NET code for SQLite and SQL Server. The two subclasses only say how to open a connection
//     and the few SQL differences (create-if-missing, insert-if-missing, date type).
//     Tables: PaymentTransactions (every operation, every outcome), ErrorLog, TerminalMappings.
//     SECURITY: every value is a parameter (@p...), never concatenated into SQL.
//     Each write runs in its own database transaction, so a failure never leaves a half-written row.
// AR: كود ADO.NET المشترك بين SQLite و SQL Server. الفئتان الفرعيتان تحددان فقط طريقة فتح الاتصال
//     والاختلافات البسيطة في SQL (الإنشاء عند عدم الوجود، الإدراج عند عدم الوجود، نوع التاريخ).
//     الجداول: PaymentTransactions (كل عملية وكل نتيجة)، ErrorLog، TerminalMappings.
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
        internal static readonly TableSchema Mappings = TableSchema.For<TerminalMapping>("TerminalMappings", "Workstation");

        public abstract string Name { get; }

        protected abstract bool IsSqlServer { get; }

        protected abstract DbConnection CreateConnection();

        /// <summary>EN: Quoted table name, e.g. [dbo].[PaymentTransactions] or "PaymentTransactions".  AR: اسم الجدول بين علامات الاقتباس.</summary>
        protected abstract string QuoteTable(string table);

        private protected abstract string CreateTableIfMissingSql(TableSchema schema, string columnsSql);

        protected abstract string CreateIndexIfMissingSql(string table, string index, string column);

        /// <summary>EN: INSERT that does nothing when the key already exists.  AR: إدراج لا يفعل شيئاً إذا كان المفتاح موجوداً.</summary>
        private protected abstract string InsertIfMissingSql(TableSchema schema, string columns, string values);

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
                }
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_CreatedAtUtc", "CreatedAtUtc"));
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_PatientMrn", "PatientMrn"));
                Execute(connection, null, CreateIndexIfMissingSql(Transactions.Table, "IX_PaymentTransactions_InvoiceNo", "InvoiceNo"));
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

            List<PropertyInfo> columns = Mappings.Columns.ToList();
            string assignments = string.Join(", ", columns.Where(c => c.Name != Mappings.Key).Select(c => c.Name + " = @" + c.Name));
            string update = "UPDATE " + QuoteTable(Mappings.Table) + " SET " + assignments + " WHERE " + Mappings.Key + " = @" + Mappings.Key;
            string insert = "INSERT INTO " + QuoteTable(Mappings.Table) + " (" + string.Join(", ", columns.Select(c => c.Name)) +
                            ") VALUES (" + string.Join(", ", columns.Select(c => "@" + c.Name)) + ")";

            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                if (Execute(connection, transaction, update, Parameters(Mappings, mapping)) == 0)
                    Execute(connection, transaction, insert, Parameters(Mappings, mapping));
                transaction.Commit();
            }
        }

        public void DeleteMapping(string workstation)
        {
            using (DbConnection connection = Open())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, "DELETE FROM " + QuoteTable(Mappings.Table) + " WHERE Workstation = @p0",
                    new[] { new KeyValuePair<string, object>("@p0", workstation) });
                transaction.Commit();
            }
        }

        public TerminalMapping GetMapping(string workstation)
        {
            return Query<TerminalMapping>(Mappings, " WHERE Workstation = @p0", workstation).FirstOrDefault();
        }

        public IList<TerminalMapping> GetMappings()
        {
            return Query<TerminalMapping>(Mappings, " ORDER BY Workstation");
        }

        public IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc)
        {
            return Query<PaymentTransactionRecord>(Transactions, " WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc < @p1 ORDER BY CreatedAtUtc",
                fromUtc, toUtc);
        }

        // =============================================================================
        // EN: HELPERS / دوال مساعدة
        // =============================================================================

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
