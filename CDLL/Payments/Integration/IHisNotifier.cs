// =====================================================================================
// EN: Hook to the Hospital Information System (HIS). After a payment is saved, Form1 asks HisBillingRules
//     whether HIS must hear about it, and if so calls IHisNotifier.NotifyPaymentAsync(record).
//     The HIS side (API, HL7, database...) is not known yet, so the default NullHisNotifier does nothing.
//     To connect a real HIS: implement IHisNotifier and pass it in PaymentServices.FromConfig().
//     The record carries PatientMrn, InvoiceNo, amount, outcome, RRN and auth code, which is what HIS needs
//     to show the payment status in the bill and update the outstanding balance.
// AR: نقطة الربط مع نظام المعلومات الصحية (HIS). بعد حفظ الدفع تسأل Form1 الفئة HisBillingRules هل يجب إبلاغ HIS،
//     وإذا كان كذلك تستدعي IHisNotifier.NotifyPaymentAsync(record).
//     طريقة الربط مع HIS (API أو HL7 أو قاعدة بيانات...) غير معروفة بعد، لذلك NullHisNotifier الافتراضي لا يفعل شيئاً.
//     لربط HIS فعلي: طبّق IHisNotifier ومرّره في PaymentServices.FromConfig().
// =====================================================================================

using System.Threading.Tasks;

namespace CDLL.Payments.Integration
{
    public interface IHisNotifier
    {
        Task NotifyPaymentAsync(PaymentTransactionRecord record);
    }

    /// <summary>EN: Does nothing until a real HIS connector exists.  AR: لا يفعل شيئاً حتى يتوفر ربط فعلي مع HIS.</summary>
    public sealed class NullHisNotifier : IHisNotifier
    {
        public Task NotifyPaymentAsync(PaymentTransactionRecord record)
        {
            return Task.CompletedTask;
        }
    }
}
