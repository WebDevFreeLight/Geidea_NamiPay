// =====================================================================================
// EN: NamiPay / SkyBand ECR protocol rules, taken from the NamiPay project (CoreSkybandECR + SkyBandECR):
//       Request  = "<ddMMyyHHmmss>;<fields>;<ECR ref>!"   packed by ECR.dll pack(request, type, signature)
//       ECR ref  = 8-digit cash register number + 6-digit counter (the counter goes up after every call
//                  except Register / Start session / End session)
//       Signature= SHA-256 hex of (6-digit counter + terminal ID); before Register: SHA-256 of "".
//       Response = ECR.dll parse(...) -> fields separated by ';'. fields[1] = command letters (A1, A6...)
//                  mapped to the type number, fields[2] = response code, fields[3] = message.
//       Purchase / refund fields: 4 PAN, 5 amount, 7 STAN, 8 date-time, 9 EXPIRY (never kept), 10 RRN,
//                  11 auth code, 12 terminal ID, 13 merchant ID, 27 scheme, 29 version, 31/32 merchant name/address,
//                  33/34 merchant name/address in Arabic (hex, ISO-8859-6), 35 ECR reference.
//     Types used here: 0 Purchase, 2 Refund, 10 Reconciliation, 17 Register, 18 Start session,
//                      23 Repeat, 24 Check status.
// AR: قواعد بروتوكول NamiPay / SkyBand المأخوذة من مشروع NamiPay: صيغة الطلب، رقم مرجع ECR (رقم الصندوق 8 أرقام +
//     عدّاد 6 أرقام يزيد بعد كل عملية عدا التسجيل وبدء/إنهاء الجلسة)، التوقيع SHA-256، وترتيب حقول الاستجابة.
//     الحقل 9 (تاريخ انتهاء البطاقة) لا يُحفظ أبداً.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CDLL.Payments.Terminal.Skyband
{
    public static class SkybandProtocol
    {
        public const int Purchase = 0;
        public const int Refund = 2;
        public const int Reconciliation = 10;
        public const int Register = 17;
        public const int StartSession = 18;
        public const int EndSession = 19;
        public const int Repeat = 23;
        public const int CheckStatus = 24;

        /// <summary>EN: Response fields of a full purchase / refund.  AR: حقول استجابة الشراء والاسترجاع الكاملة.</summary>
        public const int PanField = 4, AmountField = 5, StanField = 7, DateTimeField = 8, ExpiryField = 9, RrnField = 10,
                         AuthCodeField = 11, TerminalIdField = 12, MerchantIdField = 13, SchemeField = 27, VersionField = 29,
                         MerchantNameField = 31, MerchantAddressField = 32, MerchantNameArField = 33, MerchantAddressArField = 34,
                         EcrRefField = 35;

        private static readonly Dictionary<string, string> CommandTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A0", "17" }, { "A1", "0" }, { "A2", "1" }, { "A3", "8" }, { "A4", "3" }, { "A5", "9" }, { "A6", "2" },
            { "A7", "4" }, { "A8", "5" }, { "A9", "6" }, { "B1", "10" }, { "B2", "11" }, { "B3", "12" }, { "B4", "13" },
            { "B6", "18" }, { "B7", "19" }, { "B8", "20" }, { "B9", "21" }, { "C1", "22" }, { "C2", "23" }, { "C3", "24" }
        };

        /// <summary>EN: "ddMMyyHHmmss", Gregorian (InvariantCulture: Saudi Windows defaults to Hijri).  AR: ميلادي دائماً.</summary>
        public static string Timestamp(DateTime local)
        {
            return local.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture);
        }

        /// <summary>EN: Original transaction date for a refund: "ddMMyy".  AR: تاريخ العملية الأصلية للاسترجاع.</summary>
        public static string OriginalDate(DateTime date)
        {
            return date.ToString("ddMMyy", CultureInfo.InvariantCulture);
        }

        /// <summary>EN: SHA-256 hex of counter + terminal ID ("" before the terminal is registered).  AR: توقيع SHA-256.</summary>
        public static string Signature(string counter6, string terminalId)
        {
            string input = string.IsNullOrEmpty(terminalId) ? "" : counter6 + terminalId;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        /// <summary>EN: How long to wait for the terminal's answer, per request type (same as the NamiPay app).  AR: مدة انتظار الرد حسب نوع الطلب.</summary>
        public static int ReadTimeoutMs(int type)
        {
            switch (type)
            {
                case Register:
                case StartSession:
                case EndSession: return 10000;
                case 11: return 90000;
                case 22: return 180000;
                case CheckStatus: return 5000;
                default: return 50000;
            }
        }

        /// <summary>EN: Types that do NOT move the ECR reference counter.  AR: أنواع لا تزيد عدّاد المرجع.</summary>
        public static bool IsAdmin(int type)
        {
            return type == Register || type == StartSession || type == EndSession;
        }

        /// <summary>
        /// EN: Splits ECR.dll's parsed output into fields (on the ';' BYTE, unlike the original hex-string split that
        ///     could cut inside a byte), unwraps a full "Repeat" (C2) answer, and maps fields[1] to the type number.
        /// AR: يقسم مخرجات ECR.dll إلى حقول على بايت ';'، ويستخرج استجابة "إعادة" الكاملة، ويحوّل الحقل 1 إلى رقم النوع.
        /// </summary>
        public static string[] Decode(byte[] parsed)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            foreach (byte b in parsed ?? new byte[0])
            {
                if (b == 0)
                    break;
                if (b == (byte)';')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append((char)b);
                }
            }
            fields.Add(current.ToString());

            string[] result = fields.ToArray();
            if (result.Length > 15 && string.Equals(result[1], "C2", StringComparison.OrdinalIgnoreCase))
            {
                // EN: Repeat with the full previous transaction inside: fields 5..n-4 are that transaction.
                // AR: إعادة تحتوي العملية السابقة كاملة: الحقول من 5 إلى n-4 هي تلك العملية.
                var inner = new string[result.Length];
                int j = 1;
                for (int i = 5; i < result.Length - 3; i++, j++)
                    inner[j] = result[i];
                result = inner.Select(f => f ?? "").ToArray();
            }

            if (result.Length > 1)
                result[1] = CommandToType(result[1]);
            return result;
        }

        public static string CommandToType(string command)
        {
            string type;
            return CommandTypes.TryGetValue((command ?? "").Trim(), out type) ? type : "50";
        }

        /// <summary>EN: A full purchase / refund answer (also inside a Repeat).  AR: استجابة شراء / استرجاع كاملة.</summary>
        public static bool IsFinancial(string[] fields)
        {
            return fields.Length > 27 && (fields[1] == "0" || fields[1] == "2");
        }

        public static string Field(string[] fields, int index)
        {
            if (index >= fields.Length)
                return null;
            string value = (fields[index] ?? "").Trim();
            return value.Length == 0 ? null : value;
        }

        /// <summary>
        /// EN: The answer as one text for the audit: expiry field blanked, card numbers masked.
        /// AR: الاستجابة كنص واحد للتدقيق: حقل تاريخ الانتهاء فارغ وأرقام البطاقات مُقنّعة.
        /// </summary>
        public static string ForAudit(string[] fields)
        {
            string[] copy = (string[])fields.Clone();
            if (IsFinancial(copy) && copy.Length > ExpiryField)
                copy[ExpiryField] = "";
            if (copy.Length > PanField && IsFinancial(copy))
                copy[PanField] = CardDataMasker.MaskPan(copy[PanField]);
            return CardDataMasker.MaskText(string.Join(";", copy));
        }

        /// <summary>EN: Arabic text sent as hex of ISO-8859-6 bytes.  AR: نص عربي مرسل كـ hex بترميز ISO-8859-6.</summary>
        public static string ArabicFromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || hex.Length % 2 != 0)
                return null;
            try
            {
                byte[] bytes = Enumerable.Range(0, hex.Length / 2)
                    .Select(i => byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                    .ToArray();
                string text = Encoding.GetEncoding("ISO-8859-6").GetString(bytes).Trim();
                return text.Length == 0 ? null : text;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
