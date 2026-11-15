// =====================================================================================
// EN: Request strings sent to the terminal, and the SAR -> halalas conversion. Same strings as the
//     original Form1 buttons:
//       Check status "04!" | Purchase "<halalas>;0;1!" | Refund "<halalas>;<RRN>;<DDMMYYYY>;1;1!"
//       Repeat last "0B!"  | Reconciliation "09;0!"     | Merchant info "0A!" | Terminal ID "07!"
//     The meaning of the trailing "0;1" / "1;1" flags is not stated in the sample; confirm it in the
//     vendor's mada ECR document.
// AR: نصوص الطلبات المرسلة إلى الجهاز، وتحويل الريال إلى هللات. نفس نصوص أزرار Form1 الأصلية.
//     معنى العلامات الأخيرة "0;1" و "1;1" غير موضّح في المثال، تأكد منه في وثيقة ECR من المورّد.
// =====================================================================================

using System;
using System.Globalization;
using System.Text;

namespace CDLL.Payments.Terminal
{
    public static class TerminalRequests
    {
        public const string CheckStatus = "04!";
        public const string RepeatLast = "0B!";
        public const string Reconciliation = "09;0!";
        public const string MerchantInfo = "0A!";
        public const string TerminalId = "07!";

        /// <summary>EN: trnxType values for api_Request...Trxn.  AR: قيم trnxType لدوال العمليات المالية.</summary>
        public const int PurchaseType = 0;
        public const int RefundType = 2;

        /// <summary>EN: "&lt;halalas&gt;;0;1!", e.g. 43.71 SAR -> "4371;0;1!".  AR: مثال: 43.71 ريال -> "4371;0;1!".</summary>
        public static string Purchase(decimal amountSar)
        {
            return ToHalalas(amountSar).ToString(CultureInfo.InvariantCulture) + ";0;1!";
        }

        /// <summary>
        /// EN: "&lt;halalas&gt;;&lt;RRN&gt;;&lt;DDMMYYYY&gt;;1;1!". The date is formatted with InvariantCulture because
        ///     Arabic (Saudi) Windows uses the Hijri calendar by default.
        /// AR: التاريخ يُنسّق بـ InvariantCulture لأن Windows العربي (السعودية) يستخدم التقويم الهجري افتراضياً.
        /// </summary>
        public static string Refund(decimal amountSar, string originalRrn, DateTime originalDate)
        {
            if (!IsValidRrn(originalRrn))
                throw new ArgumentException("Original RRN must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط.", "originalRrn");

            return ToHalalas(amountSar).ToString(CultureInfo.InvariantCulture) + ";" + originalRrn + ";" +
                   originalDate.ToString("ddMMyyyy", CultureInfo.InvariantCulture) + ";1;1!";
        }

        /// <summary>
        /// EN: SAR -> halalas (43.71 -> 4371). Rounds to 2 decimals like the original sample; the amount must be above zero.
        /// AR: ريال -> هللة (43.71 -> 4371). يقرّب إلى منزلتين مثل المثال الأصلي، ويجب أن يكون المبلغ أكبر من صفر.
        /// </summary>
        public static long ToHalalas(decimal amountSar)
        {
            decimal halalas = Math.Round(amountSar, 2, MidpointRounding.AwayFromZero) * 100m;
            if (halalas <= 0)
                throw new ArgumentOutOfRangeException("amountSar", "Amount must be greater than zero. / المبلغ يجب أن يكون أكبر من صفر.");
            return (long)halalas;
        }

        public static bool IsValidRrn(string rrn)
        {
            if (rrn == null || rrn.Length != 12)
                return false;
            foreach (char c in rrn)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        /// <summary>EN: Request string as ASCII bytes (no '\0').  AR: نص الطلب كبايتات ASCII (بدون '\0').</summary>
        public static byte[] Ascii(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        /// <summary>
        /// EN: IP / TCP port as a C string (ASCII + '\0'). The DLL reads them as C strings.
        /// AR: عنوان IP أو منفذ TCP كنص C (ASCII مع '\0') لأن المكتبة تقرأهما كنصوص C.
        /// </summary>
        public static byte[] CString(string text)
        {
            return Encoding.ASCII.GetBytes((text ?? "").Trim() + "\0");
        }
    }
}
