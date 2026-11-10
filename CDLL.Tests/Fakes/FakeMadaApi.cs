// =====================================================================================
// EN: Fake terminal for tests. Set ReturnCode / ResponseCode / the field values, then call the service.
//     Every DLL function records what it received (function, request, port/IP, thread) and writes the
//     configured values into the output buffers exactly like the real DLL (ASCII + '\0').
//     It can also throw (DLL missing) or block (terminal never answers).
// AR: جهاز وهمي للاختبارات. اضبط رمز الإرجاع ورمز الاستجابة وقيم الحقول ثم استدعِ الخدمة.
//     كل دالة تسجّل ما استلمته وتكتب القيم في مخازن المخرجات مثل المكتبة الفعلية، ويمكنها رمي استثناء أو التوقف.
// =====================================================================================

using System;
using System.Text;
using System.Threading;
using CDLL.Payments.Terminal;

namespace CDLL.Tests.Fakes
{
    internal sealed class FakeMadaApi : IMadaNativeApi
    {
        // EN: What the "terminal" answers.   AR: ما يجيب به "الجهاز".
        public int ReturnCode = 0;
        public string ResponseCode = "000";
        public string Pan = "4464041234561234";            // EN: a FULL pan on purpose: the service must mask it   AR: رقم كامل عمداً للتأكد من التقنيع
        public string Amount = "000000000001";
        public string Stan = "000123";
        public string DateTime = "261110120000";
        public string Expiry = "2812";
        public string Rrn = "631412345678";
        public string AuthCode = "A1B2C3";
        public string TerminalId = "1234567812345678";
        public string SchemeId = "P1";
        public string MerchantId = "123456789012345";
        public string AdditionalAmount = "000000000000";
        public string EcrRefNo = "ECR0000000000001";
        public string Version = "1.9";
        public string CardScheme = "MADA";
        public string RawResponse = "RAW|4464041234561234|000|APPROVED";
        public string TerminalTotal = "PUR 001 000000000001";
        public string HostTotal = "PUR 001 000000000001";
        public string MerchantNameEn = "KING FAHAD HOSPITAL";
        public string MerchantNameAr = "مستشفى الملك فهد";
        public string MerchantAddressEn = "RIYADH";
        public string MerchantAddressAr = "الرياض";

        public Exception Throw;
        public ManualResetEventSlim Gate;   // EN: when set, calls wait on it (terminal never answers)   AR: عند تعيينه تنتظر الاستدعاءات

        // EN: What the "terminal" received.   AR: ما استلمه "الجهاز".
        public string LastFunction;
        public string LastRequest;
        public int LastComPort;
        public int LastBaudRate;
        public string LastIp;
        public string LastTcpPort;
        public byte[] LastIpBytes;
        public int LastTransactionType = -1;
        public int LastCallThreadId;
        public byte[] LastExpiryBuffer;
        public int Calls;

        public IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            Begin("api_RequestCOMTrxn", inOutBuff, intval);
            Com(port, Rate);
            LastTransactionType = trnxType;
            Transaction(panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp);
            return End();
        }

        public IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            Begin("api_RequestTCPIPTrxn", inOutBuff, intval);
            Tcp(ip, port);
            LastTransactionType = trnxType;
            Transaction(panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp);
            return End();
        }

        public IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, StringBuilder merchantNameArb, byte[] merchantAddEng1, StringBuilder merchantAddArb1, byte[] merchantAddopt1, StringBuilder merchantAddopt2)
        {
            Begin("api_GetMerchantDetails", inOutBuff, intval);
            Com(port, Rate);
            Put(merchantNameEng, MerchantNameEn);
            merchantNameArb.Append(MerchantNameAr);
            Put(merchantAddEng1, MerchantAddressEn);
            merchantAddArb1.Append(MerchantAddressAr);
            return End();
        }

        public IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId)
        {
            Begin("api_TCPIPGetTerminalID", inOutBuff, intval);
            Tcp(ip, port);
            Put(terminalId, TerminalId);
            return End();
        }

        public IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval)
        {
            Begin("api_CheckStatus", inOutBuff, intval);
            Com(port, Rate);
            return End();
        }

        public IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval)
        {
            Begin("api_TCPIPCheckStatus", inOutBuff, intval);
            Tcp(ip, port);
            return End();
        }

        public IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            Begin("api_RequestReconTCPIPWORcpt", inOutBuff, new[] { (byte)inReqlen });
            Tcp(ip, port);
            Recon(rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            return End();
        }

        public IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            Begin("api_RequestReconCOMWORcpt", inOutBuff, intval);
            Com(port, Rate);
            Recon(rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            return End();
        }

        public IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, StringBuilder outResp, byte[] outRespLen)
        {
            Begin("api_RequestTCPIPRepeat", inOutBuff, intval);
            Tcp(ip, port);
            Transaction(panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp);
            Put(Cardscheme, CardScheme);
            return End();
        }

        public IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            Begin("api_RequestCOMRepeat", inOutBuff, intval);
            Com(port, Rate);
            Transaction(panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp);
            return End();
        }

        // ---- helpers

        private void Begin(string function, byte[] request, byte[] length)
        {
            Interlocked.Increment(ref Calls);
            LastFunction = function;
            LastRequest = Encoding.ASCII.GetString(request, 0, length[0]);
            LastCallThreadId = Thread.CurrentThread.ManagedThreadId;
            if (Gate != null)
                Gate.Wait();
            if (Throw != null)
                throw Throw;
        }

        private IntPtr End()
        {
            return new IntPtr(ReturnCode);
        }

        private void Com(int port, int rate)
        {
            LastComPort = port;
            LastBaudRate = rate;
        }

        private void Tcp(byte[] ip, byte[] port)
        {
            LastIpBytes = ip;
            LastIp = CString(ip);
            LastTcpPort = CString(port);
        }

        private void Transaction(byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp)
        {
            Put(panNo, Pan);
            Put(purAmount, Amount);
            Put(stanNo, Stan);
            Put(dataTime, DateTime);
            Put(expDate, Expiry);
            LastExpiryBuffer = expDate;
            Put(trxRrn, Rrn);
            Put(authCode, AuthCode);
            Put(rspCode, ResponseCode);
            Put(terminalId, TerminalId);
            Put(schemeId, SchemeId);
            Put(merchantId, MerchantId);
            Put(addtlAmount, AdditionalAmount);
            Put(ecrrefno, EcrRefNo);
            Put(version, Version);
            if (RawResponse != null)
                outResp.Append(RawResponse);
        }

        private void Recon(byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            Put(rspCode, ResponseCode);
            Put(terminalTotal, TerminalTotal);
            Put(hostTotal, HostTotal);
            Put(stanNo, Stan);
            Put(dataTime, DateTime);
            Put(terminalId, TerminalId);
            Put(merchantId, MerchantId);
        }

        /// <summary>EN: Writes value + '\0' like the DLL (cut to the buffer size).  AR: يكتب القيمة مع '\0' مثل المكتبة.</summary>
        private static void Put(byte[] buffer, string value)
        {
            if (value == null)
                return;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            int length = Math.Min(bytes.Length, buffer.Length - 1);
            Array.Copy(bytes, buffer, length);
            buffer[length] = 0;
        }

        private static string CString(byte[] bytes)
        {
            int end = Array.IndexOf(bytes, (byte)0);
            return Encoding.ASCII.GetString(bytes, 0, end < 0 ? bytes.Length : end);
        }
    }
}
