// =====================================================================================
// EN: Table layout built from a record class: one column per public property, in declaration order.
//     SQLite and SQL Server both use it, so adding a property to PaymentTransactionRecord adds the column
//     to both databases (for a NEW database; existing tables are not altered).
//     Column names come from code, never from user input; all VALUES are sent as parameters (no SQL injection).
// AR: تصميم الجدول مبني من فئة السجل: عمود لكل خاصية عامة بنفس ترتيب التعريف.
//     يستخدمه SQLite و SQL Server معاً، لذلك إضافة خاصية إلى PaymentTransactionRecord تضيف العمود في القاعدتين
//     (لقاعدة بيانات جديدة، الجداول الموجودة لا تُعدّل).
//     أسماء الأعمدة من الكود وليست من إدخال المستخدم، وكل القيم تُرسل كمعاملات (لا يوجد حقن SQL).
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CDLL.Payments.Storage
{
    internal sealed class TableSchema
    {
        // EN: Columns that can hold long text.   AR: الأعمدة التي قد تحتوي نصاً طويلاً.
        private static readonly HashSet<string> LongTextColumns = new HashSet<string>
        {
            "RawResponse", "ErrorMessage", "Message", "Details", "TerminalTotal", "HostTotal",
            "MerchantAddressEn", "MerchantAddressAr", "Request"
        };

        public string Table { get; private set; }
        public string Key { get; private set; }
        public Type RecordType { get; private set; }
        public IList<PropertyInfo> Columns { get; private set; }

        public static TableSchema For<T>(string table, string key)
        {
            return new TableSchema
            {
                Table = table,
                Key = key,
                RecordType = typeof(T),
                Columns = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .OrderBy(p => p.MetadataToken)
                    .ToList()
            };
        }

        /// <summary>EN: Column type for SQLite or SQL Server.  AR: نوع العمود في SQLite أو SQL Server.</summary>
        public string SqlType(PropertyInfo column, bool sqlServer)
        {
            Type type = Nullable.GetUnderlyingType(column.PropertyType) ?? column.PropertyType;
            bool isKey = column.Name == Key;
            string nullability = isKey ? " NOT NULL PRIMARY KEY" : (IsNullable(column) ? " NULL" : " NOT NULL");

            if (!sqlServer)
            {
                if (type == typeof(int) || type == typeof(long) || type == typeof(bool)) return "INTEGER" + nullability;
                return "TEXT" + nullability;   // EN: strings, enums (name), dates (ISO-8601 UTC)   AR: النصوص والتعدادات والتواريخ
            }

            if (type == typeof(bool)) return "BIT" + nullability;
            if (type == typeof(int)) return "INT" + nullability;
            if (type == typeof(long)) return "BIGINT" + nullability;
            if (type == typeof(DateTime)) return "DATETIME2(3)" + nullability;
            if (type.IsEnum) return "NVARCHAR(30)" + nullability;
            if (isKey) return "NVARCHAR(128)" + nullability;
            return (LongTextColumns.Contains(column.Name) ? "NVARCHAR(MAX)" : "NVARCHAR(400)") + nullability;
        }

        private static bool IsNullable(PropertyInfo column)
        {
            return !column.PropertyType.IsValueType || Nullable.GetUnderlyingType(column.PropertyType) != null;
        }
    }
}
