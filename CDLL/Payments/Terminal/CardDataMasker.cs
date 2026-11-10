// =====================================================================================
// EN: SECURITY: masks card numbers before anything is shown, logged or saved.
//       - MaskPan keeps the first 6 and last 4 digits (the PCI DSS display rule): 4464041234561234 -> 446404******1234
//       - MaskText replaces any 13-19 digit run inside free text (raw responses, exception text).
//     The terminal normally sends a masked PAN already; masking again makes sure a full PAN can never
//     reach the database, the JSON files or the log, whatever the terminal sends.
// AR: الأمان: يُقنّع أرقام البطاقات قبل عرض أو تسجيل أو حفظ أي شيء.
//       - MaskPan يُبقي أول 6 وآخر 4 أرقام (قاعدة العرض في PCI DSS).
//       - MaskText يستبدل أي سلسلة من 13 إلى 19 رقماً داخل النص (الاستجابات الخام ونصوص الأخطاء).
//     الجهاز يرسل عادةً رقماً مُقنّعاً، والتقنيع مرة أخرى يضمن ألا يصل رقم كامل إلى قاعدة البيانات أو JSON أو السجل.
// =====================================================================================

using System.Text.RegularExpressions;

namespace CDLL.Payments
{
    public static class CardDataMasker
    {
        private static readonly Regex PanLike = new Regex(@"(?<!\d)\d{13,19}(?!\d)", RegexOptions.Compiled);

        /// <summary>EN: Keeps first 6 + last 4 characters; shorter values are fully masked.  AR: يُبقي أول 6 وآخر 4، والقيم الأقصر تُقنّع بالكامل.</summary>
        public static string MaskPan(string pan)
        {
            if (string.IsNullOrWhiteSpace(pan))
                return null;

            char[] chars = pan.Trim().ToCharArray();
            if (chars.Length < 13)
                return new string('*', chars.Length);

            for (int i = 6; i < chars.Length - 4; i++)
                chars[i] = '*';
            return new string(chars);
        }

        /// <summary>EN: Masks every card-number-like run of digits in a text.  AR: يُقنّع كل سلسلة أرقام تشبه رقم بطاقة داخل النص.</summary>
        public static string MaskText(string text)
        {
            return text == null ? null : PanLike.Replace(text, m => MaskPan(m.Value));
        }
    }
}
