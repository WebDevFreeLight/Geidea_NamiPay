// =====================================================================================
// EN: FAKE NamiPay terminal for "TerminalMode = Simulator": answers every SkyBand request with a canned,
//     approved answer in the real field layout. No port, no ECR.dll, NO real payments.
// AR: جهاز NamiPay وهمي لوضع "TerminalMode = Simulator": يرد على كل طلب SkyBand بإجابة مقبولة بنفس ترتيب
//     الحقول الحقيقي. بدون منفذ ولا ECR.dll ولا مدفوعات حقيقية.
// =====================================================================================

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace CDLL.Payments.Terminal.Skyband
{
    public sealed class SkybandSimulator : ISkybandCodec, ISkybandLink
    {
        public const string TerminalId = "NAMISIM00000001";
        private readonly Random _random = new Random();
        private int _lastType;

        /// <summary>EN: Delay imitating the card tap, in ms.  AR: تأخير يحاكي تمرير البطاقة.</summary>
        public int CardDelayMs { get; set; } = 1500;

        public byte[] Pack(string request, int transactionType, string signature)
        {
            _lastType = transactionType;
            return Encoding.ASCII.GetBytes(transactionType.ToString(CultureInfo.InvariantCulture) + "|" + request);
        }

        public byte[] Exchange(TerminalConnection connection, byte[] frame, int readTimeoutMs)
        {
            string request = Encoding.ASCII.GetString(frame);
            string[] parts = request.Substring(request.IndexOf('|') + 1).TrimEnd('!').Split(';');
            string time = parts[0];
            string ecr = parts[parts.Length - 1];

            switch (_lastType)
            {
                case SkybandProtocol.Register:
                    return Answer("", "A0", "00", TerminalId);
                case SkybandProtocol.StartSession:
                    return Answer("", "B6", "00", "SESSION STARTED");
                case SkybandProtocol.CheckStatus:
                    return Answer("", "C3", "00", "READY", time, ecr, "SIG");
                case SkybandProtocol.Reconciliation:
                    return Answer("", "B1", "500", "RECONCILED", time, "1", "PUR", "1", "1");
                case SkybandProtocol.Repeat:
                    return Answer("", "C2", "00", "SUCCESS", time, "APPROVED", parts.Length > 1 ? parts[1] : "", ecr, "SIG");
                default:
                    Thread.Sleep(CardDelayMs);
                    string command = _lastType == SkybandProtocol.Refund ? "A6" : "A1";
                    string amount = parts.Length > 1 ? parts[1] : "0";
                    string rrn = _random.Next(100000, 999999).ToString(CultureInfo.InvariantCulture) + "000000";
                    var fields = Enumerable.Repeat("", 37).ToArray();
                    fields[1] = command; fields[2] = "000"; fields[3] = "APPROVED"; fields[4] = "4464040000000000";
                    fields[5] = amount; fields[7] = "000123"; fields[8] = time; fields[9] = "2812"; fields[10] = rrn;
                    fields[11] = "123456"; fields[12] = TerminalId; fields[13] = "000000000000001"; fields[27] = "mada";
                    fields[29] = "SIM"; fields[31] = "NAMIPAY SIMULATOR"; fields[32] = "RIYADH"; fields[35] = ecr;
                    return Encoding.ASCII.GetBytes(string.Join(";", fields));
            }
        }

        /// <summary>EN: The simulator's answers are already the parsed text.  AR: إجابات المحاكي نص محلَّل مسبقاً.</summary>
        public byte[] Parse(byte[] response)
        {
            return response;
        }

        private static byte[] Answer(params string[] fields)
        {
            return Encoding.ASCII.GetBytes(string.Join(";", fields));
        }
    }
}
