// =====================================================================================
// EN: The REUSABLE terminal service. Any screen (Form1, the mapping screen, a future HIS billing screen,
//     a Windows service...) runs terminal operations through this interface only.
//     Every method:
//       - runs the blocking DLL call on a BACKGROUND thread, so the window never freezes,
//       - allows one call at a time per service (a second call gets Outcome = Failed "terminal busy"),
//       - never throws for terminal problems: the outcome and all returned fields are in TerminalResult.
// AR: خدمة الجهاز القابلة لإعادة الاستخدام. أي شاشة (Form1، شاشة الربط، شاشة فوترة HIS مستقبلاً، خدمة Windows...)
//     تنفّذ عمليات الجهاز عبر هذه الواجهة فقط. كل دالة:
//       - تشغّل استدعاء المكتبة على خيط خلفي، فلا تتجمّد النافذة،
//       - تسمح باستدعاء واحد في كل مرة (الاستدعاء الثاني يحصل على "الجهاز مشغول")،
//       - لا ترمي استثناءً لمشاكل الجهاز: النتيجة وكل الحقول المعادة موجودة في TerminalResult.
// =====================================================================================

using System;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal
{
    public interface IPaymentTerminalService
    {
        /// <summary>EN: "04!" Is the terminal connected and ready.  AR: هل الجهاز متصل وجاهز.</summary>
        Task<TerminalResult> CheckStatusAsync(TerminalConnection connection);

        /// <summary>EN: Card payment of amountSar.  AR: دفع بالبطاقة بمبلغ amountSar.</summary>
        Task<TerminalResult> PurchaseAsync(TerminalConnection connection, decimal amountSar);

        /// <summary>EN: Refund against the original 12-digit RRN and its date.  AR: استرجاع مقابل الرقم المرجعي الأصلي وتاريخه.</summary>
        Task<TerminalResult> RefundAsync(TerminalConnection connection, decimal amountSar, string originalRrn, DateTime originalDate);

        /// <summary>EN: Result of the last transaction again (use after a timeout).  AR: نتيجة آخر عملية مرة أخرى (بعد انتهاء المهلة).</summary>
        Task<TerminalResult> RepeatLastAsync(TerminalConnection connection);

        /// <summary>EN: End-of-day reconciliation: terminal totals vs host totals.  AR: التسوية: مجاميع الجهاز مقابل مجاميع البنك.</summary>
        Task<TerminalResult> ReconcileAsync(TerminalConnection connection);

        /// <summary>EN: Merchant name / address. USB/COM only (the DLL has no TCP version).  AR: بيانات التاجر، عبر USB/COM فقط.</summary>
        Task<TerminalResult> GetMerchantInfoAsync(TerminalConnection connection);

        /// <summary>EN: Terminal ID. TCP only (the DLL has no COM version).  AR: رقم الجهاز، عبر TCP فقط.</summary>
        Task<TerminalResult> GetTerminalIdAsync(TerminalConnection connection);
    }
}
