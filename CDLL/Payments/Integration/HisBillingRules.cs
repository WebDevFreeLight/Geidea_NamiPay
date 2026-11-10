// =====================================================================================
// EN: Business rule: which saved payments must be sent to HIS (to mark the bill paid / update the balance).
//     Called by Form1 after every save; returns false = HIS is not told.
// AR: قاعدة العمل: أي عمليات الدفع المحفوظة يجب إرسالها إلى HIS (لتعليم الفاتورة كمدفوعة وتحديث الرصيد).
//     تستدعيها Form1 بعد كل حفظ، و false تعني أن HIS لن يُبلَّغ.
// =====================================================================================

namespace CDLL.Payments.Integration
{
    public static class HisBillingRules
    {
        public static bool ShouldNotifyHis(PaymentTransactionRecord record)
        {
            // TODO(you): decide which records change the patient's bill in HIS. Things to weigh:
            //   - Approved Purchase and Approved CashPayment clearly reduce the outstanding balance.
            //   - Approved Refund increases it again.
            //   - RepeatLast can return an APPROVED payment whose first answer was lost (timeout): if HIS never
            //     heard about it, the patient paid but the bill still shows unpaid. Should it be sent?
            //   - Without a PatientMrn or InvoiceNo, HIS cannot know which bill to update.
            //   - Declined / Cancelled / Timeout / Error never change the balance.
            return false;
        }
    }
}
