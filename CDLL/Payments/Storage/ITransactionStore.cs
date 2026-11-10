// =====================================================================================
// EN: One storage back-end: SQLite, SQL Server or JSON files. All three implement this interface, so the
//     back-end is chosen in app.config ("Storage.Provider") without changing any code.
//     Methods are SYNCHRONOUS on purpose: they are only ever called on PaymentStorage's background thread,
//     never on the UI thread. Screens use PaymentStorage, not this interface.
// AR: وسيلة تخزين واحدة: SQLite أو SQL Server أو ملفات JSON. الثلاث تطبّق هذه الواجهة، لذلك يُختار نوع
//     التخزين في app.config ("Storage.Provider") بدون تغيير أي كود.
//     الدوال متزامنة عمداً: تُستدعى فقط على الخيط الخلفي لـ PaymentStorage وليس على خيط الواجهة أبداً.
//     الشاشات تستخدم PaymentStorage وليس هذه الواجهة.
// =====================================================================================

using System;
using System.Collections.Generic;

namespace CDLL.Payments.Storage
{
    public interface ITransactionStore
    {
        /// <summary>EN: "Sqlite", "SqlServer" or "Json".  AR: نوع التخزين.</summary>
        string Name { get; }

        /// <summary>EN: Creates the tables / folders if they do not exist yet.  AR: ينشئ الجداول أو المجلدات إذا لم تكن موجودة.</summary>
        void EnsureCreated();

        /// <summary>EN: Saves a transaction. Saving the same Id twice keeps the first one (safe to re-import).  AR: حفظ العملية نفسها مرتين يُبقي الأولى (إعادة الاستيراد آمنة).</summary>
        void SaveTransaction(PaymentTransactionRecord record);

        void SaveError(ErrorLogRecord record);

        /// <summary>EN: Insert or replace the mapping of one workstation.  AR: إضافة أو استبدال ربط حاسوب واحد.</summary>
        void SaveMapping(TerminalMapping mapping);

        /// <summary>EN: Removes a workstation's mapping (when its terminal moves to a new PC).  AR: حذف ربط حاسوب (عند نقل جهازه إلى حاسوب جديد).</summary>
        void DeleteMapping(string workstation);

        TerminalMapping GetMapping(string workstation);

        IList<TerminalMapping> GetMappings();

        /// <summary>EN: Transactions with fromUtc &lt;= CreatedAtUtc &lt; toUtc, oldest first (audit / reports).  AR: العمليات ضمن الفترة، الأقدم أولاً (للتدقيق والتقارير).</summary>
        IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc);
    }
}
