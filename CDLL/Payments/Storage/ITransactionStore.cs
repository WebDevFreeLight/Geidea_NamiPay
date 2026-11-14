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

        /// <summary>
        /// EN: Insert or replace the mapping of one workstation + provider. Keeps exactly ONE active mapping per PC:
        ///     saving an active mapping deactivates the PC's other one; the first mapping of a PC becomes active.
        /// AR: إضافة أو استبدال ربط حاسوب + مزوّد. يضمن ربطاً فعّالاً واحداً لكل حاسوب: حفظ ربط فعّال يلغي تفعيل الآخر،
        ///     وأول ربط للحاسوب يصبح فعّالاً.
        /// </summary>
        void SaveMapping(TerminalMapping mapping);

        /// <summary>
        /// EN: Removes one mapping of a PC. If it was the active one, the PC's other mapping becomes active.
        /// AR: حذف ربط واحد للحاسوب. إذا كان الفعّال، يصبح الربط الآخر للحاسوب فعّالاً.
        /// </summary>
        void DeleteMapping(string workstation, TerminalProvider provider);

        /// <summary>EN: Makes this provider the active (default) one of the PC.  AR: يجعل هذا المزوّد الفعّال (الافتراضي) للحاسوب.</summary>
        void SetActiveMapping(string workstation, TerminalProvider provider);

        /// <summary>EN: The ACTIVE mapping of the workstation (null = none).  AR: الربط الفعّال للحاسوب (null = لا يوجد).</summary>
        TerminalMapping GetMapping(string workstation);

        /// <summary>EN: Every mapping of every PC (active and not).  AR: كل ربطات كل الحواسيب.</summary>
        IList<TerminalMapping> GetMappings();

        /// <summary>EN: Transactions with fromUtc &lt;= CreatedAtUtc &lt; toUtc, oldest first (audit / reports).  AR: العمليات ضمن الفترة، الأقدم أولاً (للتدقيق والتقارير).</summary>
        IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc);
    }
}
