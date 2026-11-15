// =====================================================================================
// EN: Turns what the terminal returned (DLL return code + host response code + call duration) into ONE outcome:
//     Approved / Declined / Cancelled / Timeout / NoResponse / Failed / Ok.
//       1. DLL return code first: -3 = no terminal / no answer, -4 = communication timeout,
//          8 = the terminal stopped the transaction:
//              8 + duration >= "Outcome.CardTimeoutSeconds" (default 15) -> Timeout   (no card presented in time)
//              8 otherwise                                                -> Cancelled (cancel pressed on the terminal)
//          other non-zero = Failed.
//       2. Return code 0 on a non-financial call (status, terminal ID, merchant, reconciliation) = Ok.
//       3. Return code 0 on a payment / refund / repeat: the host response code decides:
//            in "Outcome.CancelledResponseCodes" -> Cancelled, in "Outcome.TimeoutResponseCodes" -> Timeout,
//            in "Outcome.ApprovedResponseCodes" (default "000") -> Approved, empty -> Failed, anything else -> Declined.
//     The cancel / timeout response codes are vendor-specific, so they are read from app.config.
//     Fill them in from the vendor's mada ECR document.
// AR: يحوّل ما أعاده الجهاز (رمز الإرجاع + رمز استجابة البنك + مدة الاستدعاء) إلى نتيجة واحدة.
//       1. رمز الإرجاع أولاً: ‎-3 لا يوجد جهاز، ‎-4 انتهت مهلة الاتصال، 8 = الجهاز أوقف العملية:
//              8 مع مدة >= "Outcome.CardTimeoutSeconds" (الافتراضي 15) -> Timeout (لم تُقدَّم البطاقة في الوقت المحدد)
//              8 في غير ذلك                                              -> Cancelled (ضُغط الإلغاء على الجهاز)
//          وأي رمز آخر غير الصفر = Failed.
//       2. رمز 0 في عملية غير مالية (الحالة، رقم الجهاز، التاجر، التسوية) = Ok.
//       3. رمز 0 في شراء / استرجاع / إعادة: رمز استجابة البنك يحدد النتيجة حسب القوائم في app.config.
//     رموز الإلغاء وانتهاء المهلة تختلف حسب المورّد، لذلك تُقرأ من app.config. املأها من وثيقة ECR الخاصة بمدى.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace CDLL.Payments.Terminal
{
    public sealed class OutcomeClassifier
    {
        /// <summary>EN: DLL return code with no terminal / no answer.  AR: رمز الإرجاع عند عدم وجود جهاز أو رد.</summary>
        public const int NoTerminalReturnCode = -3;

        /// <summary>EN: DLL return code for "communication timeout" (seen with the wrong COM port).  AR: رمز انتهاء مهلة الاتصال.</summary>
        public const int CommunicationTimeoutReturnCode = -4;

        /// <summary>
        /// EN: DLL return code when the terminal stopped the transaction: cancel pressed, or no card in time.
        /// AR: رمز الإرجاع عندما يوقف الجهاز العملية: ضغط الإلغاء أو عدم تقديم البطاقة في الوقت المحدد.
        /// </summary>
        public const int CancelledOrTimeoutReturnCode = 8;

        /// <summary>EN: How long the terminal waits for a card (15 s on the A920Pro: return code 8 came after 15 505 ms).  AR: مدة انتظار الجهاز للبطاقة.</summary>
        public static readonly TimeSpan DefaultCardTimeout = TimeSpan.FromSeconds(15);

        private readonly HashSet<string> _approved;
        private readonly HashSet<string> _cancelled;
        private readonly HashSet<string> _timeout;
        private readonly TimeSpan _cardTimeout;

        public OutcomeClassifier(IEnumerable<string> approvedCodes, IEnumerable<string> cancelledCodes, IEnumerable<string> timeoutCodes, TimeSpan? cardTimeout = null)
        {
            _approved = ToSet(approvedCodes);
            _cancelled = ToSet(cancelledCodes);
            _timeout = ToSet(timeoutCodes);
            _cardTimeout = cardTimeout ?? DefaultCardTimeout;
        }

        public TimeSpan CardTimeout { get { return _cardTimeout; } }

        /// <summary>EN: Reads the code lists and the card timeout from appSettings.  AR: يقرأ قوائم الرموز ومهلة البطاقة من appSettings.</summary>
        public static OutcomeClassifier FromConfig()
        {
            double seconds;
            string value = ConfigurationManager.AppSettings["Outcome.CardTimeoutSeconds"];
            TimeSpan cardTimeout = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : DefaultCardTimeout;

            return new OutcomeClassifier(
                Codes("Outcome.ApprovedResponseCodes", "000"),
                Codes("Outcome.CancelledResponseCodes", ""),
                Codes("Outcome.TimeoutResponseCodes", ""),
                cardTimeout);
        }

        /// <param name="durationMs">EN: How long the DLL call took; null = unknown.  AR: مدة استدعاء المكتبة، و null = غير معروفة.</param>
        public TransactionOutcome Classify(TerminalOperation operation, int returnCode, string responseCode, long? durationMs = null)
        {
            if (returnCode == NoTerminalReturnCode)
                return TransactionOutcome.NoResponse;
            if (returnCode == CommunicationTimeoutReturnCode)
                return TransactionOutcome.Timeout;
            if (returnCode == CancelledOrTimeoutReturnCode)
            {
                return durationMs.HasValue && durationMs.Value >= _cardTimeout.TotalMilliseconds
                    ? TransactionOutcome.Timeout
                    : TransactionOutcome.Cancelled;
            }

            if (returnCode == 16)
                return TransactionOutcome.Cancelled;
            if (returnCode != 0)
                return TransactionOutcome.Failed;

            switch (operation)
            {
                case TerminalOperation.CheckStatus:
                case TerminalOperation.TerminalId:
                case TerminalOperation.MerchantInfo:
                case TerminalOperation.Reconciliation:
                    return TransactionOutcome.Ok;
            }

            string code = (responseCode ?? "").Trim();
            if (_cancelled.Contains(code))
                return TransactionOutcome.Cancelled;
            if (_timeout.Contains(code))
                return TransactionOutcome.Timeout;
            if (_approved.Contains(code))
                return TransactionOutcome.Approved;
            return code.Length == 0 ? TransactionOutcome.Failed : TransactionOutcome.Declined;
        }

        private static HashSet<string> ToSet(IEnumerable<string> codes)
        {
            return new HashSet<string>((codes ?? Enumerable.Empty<string>())
                .Select(c => (c ?? "").Trim())
                .Where(c => c.Length > 0), StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> Codes(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return (value ?? fallback).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
