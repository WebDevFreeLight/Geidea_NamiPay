// =====================================================================================
// EN: Fake NamiPay / SkyBand terminal: plays both ECR.dll (pack / parse) and the COM/TCP link.
//     Records every request (type, text, signature, read timeout) and answers each type with fields in the
//     real layout, using the real command letters (A0 register, B6 start session, A1 purchase, A6 refund...).
// AR: جهاز NamiPay / SkyBand وهمي: يقوم بدور ECR.dll ودور الاتصال، ويسجل كل طلب ويرد بحقول بنفس الترتيب الحقيقي.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CDLL.Payments;
using CDLL.Payments.Terminal.Skyband;

namespace CDLL.Tests.Fakes
{
    internal sealed class FakeSkyband : ISkybandCodec, ISkybandLink
    {
        public sealed class Sent
        {
            public int Type;
            public string Request;
            public string Signature;
            public int ReadTimeoutMs;
        }

        public readonly List<Sent> Requests = new List<Sent>();
        public string TerminalId = "SKY12345";
        public string PurchaseMessage = "APPROVED";
        public string PurchaseCode = "000";
        public string StartSessionCode = "00";
        public SkybandLinkException Fail;           // EN: next exchange throws this   AR: التبادل التالي يرمي هذا
        public string RawAnswer;                    // EN: next answer as-is (e.g. garbage)   AR: الرد التالي كما هو

        private int _packedType;

        public byte[] Pack(string request, int transactionType, string signature)
        {
            _packedType = transactionType;
            Requests.Add(new Sent { Type = transactionType, Request = request, Signature = signature });
            return Encoding.ASCII.GetBytes("FRAME");
        }

        public byte[] Exchange(TerminalConnection connection, byte[] frame, int readTimeoutMs)
        {
            Requests.Last().ReadTimeoutMs = readTimeoutMs;
            if (Fail != null)
            {
                SkybandLinkException fail = Fail;
                Fail = null;
                throw fail;
            }
            if (RawAnswer != null)
            {
                string raw = RawAnswer;
                RawAnswer = null;
                return Encoding.ASCII.GetBytes(raw);
            }
            return Encoding.ASCII.GetBytes(string.Join(";", Answer(_packedType)));
        }

        public byte[] Parse(byte[] response)
        {
            return response;
        }

        public Sent Last(int type)
        {
            return Requests.Last(r => r.Type == type);
        }

        private string[] Answer(int type)
        {
            switch (type)
            {
                case SkybandProtocol.Register: return new[] { "", "A0", "00", TerminalId };
                case SkybandProtocol.StartSession: return new[] { "", "B6", StartSessionCode, "SESSION STARTED" };
                case SkybandProtocol.CheckStatus: return new[] { "", "C3", "00", "READY", "101126120000", "12345678000003", "SIG" };
                case SkybandProtocol.Reconciliation: return new[] { "", "B1", "500", "RECONCILED", "101126120000", "1", "mada", "0", "1" };
                case SkybandProtocol.Repeat: return new[] { "", "C2", "00", "SUCCESS", "101126120000", "APPROVED", "12345678000001", "12345678000005", "SIG" };
                default:
                    string[] f = Enumerable.Repeat("", 37).ToArray();
                    f[1] = type == SkybandProtocol.Refund ? "A6" : "A1";
                    f[2] = PurchaseCode; f[3] = PurchaseMessage; f[4] = "4464041234561234"; f[5] = "000000000001";
                    f[6] = "5411"; f[7] = "000777"; f[8] = "101126120000"; f[9] = "2812"; f[10] = "631412345678";
                    f[11] = "A1B2C3"; f[12] = TerminalId; f[13] = "123456789012345"; f[14] = "000001"; f[27] = "mada";
                    f[29] = "2.0.1"; f[31] = "KING FAHAD HOSPITAL"; f[32] = "RIYADH"; f[33] = "E5D3CAD4E1E9"; f[34] = "C7E4D1EAC7D6";
                    f[35] = "12345678000001"; f[36] = "SIG";
                    return f;
            }
        }
    }
}
