// =====================================================================================
// EN: Fake mada terminal for the CDLL sample. Every function has EXACTLY the same name and parameters
//     as the real DLL function declared in Form1.cs, and writes into the same output buffers the same way
//     (ASCII text + '\0'). So code written against the simulator looks exactly like code for the real DLL.
//     The real madaapi DLLs are never loaded and nothing is sent anywhere.
//     Used by SimulatorForm. Form1 (the real terminal form) does not use it and was not changed.
// AR: جهاز مدى وهمي لمثال CDLL. كل دالة لها نفس الاسم والمعاملات تماماً مثل دالة المكتبة الفعلية المعرّفة
//     في Form1.cs، وتكتب في نفس مخازن المخرجات بنفس الطريقة (نص ASCII + '\0'). لذلك الكود المكتوب للمحاكي
//     يبدو تماماً مثل الكود المكتوب للمكتبة الفعلية.
//     مكتبات madaapi الفعلية لا تُحمَّل أبداً ولا يُرسل أي شيء إلى أي مكان.
//     يستخدمه SimulatorForm. أما Form1 (نافذة الجهاز الفعلي) فلا تستخدمه ولم تتغير.
// =====================================================================================

using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace CDLL.Simulator
{
    /// <summary>
    /// EN: What the simulated terminal does on the next call.
    /// AR: ما يفعله الجهاز الوهمي في الاستدعاء التالي.
    /// </summary>
    public enum SimulationScenario
    {
        /// <summary>EN: Everything succeeds, transactions approved ("000").  AR: كل شيء ينجح والعمليات مقبولة ("000").</summary>
        Approved,

        /// <summary>EN: The terminal answers but the bank declines.  AR: الجهاز يرد لكن البنك يرفض.</summary>
        Declined,

        /// <summary>
        /// EN: The card IS charged but the answer never arrives (status -3). Press Repeat to find the transaction.
        /// AR: يتم الخصم من البطاقة فعلاً لكن الرد لا يصل (الحالة -3). اضغط Repeat لإيجاد العملية.
        /// </summary>
        NoResponse
    }

    /// <summary>
    /// EN: Drop-in fake for the madaapi DLL functions. Calls block (Thread.Sleep) just like the real DLL,
    ///     so callers should run them off the UI thread (SimulatorForm uses Task.Run).
    /// AR: بديل وهمي لدوال مكتبة madaapi. الاستدعاءات تحجب التنفيذ (Thread.Sleep) تماماً مثل المكتبة الفعلية،
    ///     لذلك يجب تشغيلها خارج خيط الواجهة (SimulatorForm يستخدم Task.Run).
    /// </summary>
    public static class MadaApiSimulator
    {
        /// <summary>
        /// EN: "Terminal did not answer". -3 is what the real madaapi_v1_9.dll returned when tested with no terminal connected.
        /// AR: "الجهاز لم يرد". القيمة -3 هي ما أعادته المكتبة الفعلية madaapi_v1_9.dll عند اختبارها بدون جهاز متصل.
        /// </summary>
        public const int NoResponseStatus = -3;

        /// <summary>
        /// EN: Response code for the Declined scenario. ILLUSTRATIVE ONLY: take real decline codes from the vendor spec.
        /// AR: رمز الاستجابة لسيناريو الرفض. للتوضيح فقط: خذ رموز الرفض الحقيقية من وثيقة المورّد.
        /// </summary>
        public const string DeclinedResponseCode = "100";

        public const string TerminalId = "SIMULATOR0000001";
        public const string MerchantId = "000000000000001";

        // EN: One call at a time, like a real terminal.   AR: استدعاء واحد في كل مرة، مثل الجهاز الفعلي.
        private static readonly object Gate = new object();
        private static readonly Random Rng = new Random();

        private static int _stan;
        // EN: Last transaction the "terminal" processed, even if its answer was lost. Read by Repeat.
        // AR: آخر عملية عالجها "الجهاز" حتى لو ضاع ردها. يقرأها زر الإعادة.
        private static string _lastPan, _lastAmount, _lastStan, _lastDateTime, _lastRrn, _lastAuthCode, _lastResponseCode;

        private static SimulationScenario _scenario = SimulationScenario.Approved;
        private static int _cardDelayMs = 2000;
        private static string _lastRequest = "";

        /// <summary>EN: Result of the next calls.  AR: نتيجة الاستدعاءات التالية.</summary>
        public static SimulationScenario Scenario
        {
            get { lock (Gate) return _scenario; }
            set { lock (Gate) _scenario = value; }
        }

        /// <summary>EN: Delay imitating the card tap, in ms.  AR: تأخير يحاكي تمرير البطاقة بالملي ثانية.</summary>
        public static int CardDelayMs
        {
            get { lock (Gate) return _cardDelayMs; }
            set { lock (Gate) _cardDelayMs = value; }
        }

        /// <summary>EN: Request string of the last call, as the real DLL would have received it.  AR: نص طلب آخر استدعاء كما كانت ستستلمه المكتبة الفعلية.</summary>
        public static string LastRequest
        {
            get { lock (Gate) return _lastRequest; }
        }

        // ============================================================== financial / العمليات المالية

        public static IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            return Transaction("COM" + port, trnxType, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
        }

        public static IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            return Transaction("TCP " + Ascii(ip), trnxType, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
        }

        // ============================================================== terminal info / معلومات الجهاز

        public static IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, StringBuilder merchantNameArb, byte[] merchantAddEng1, StringBuilder merchantAddArb1, byte[] merchantAddopt1, StringBuilder merchantAddopt2)
        {
            lock (Gate)
            {
                Begin(inOutBuff, intval, 300);
                bool answered = _scenario != SimulationScenario.NoResponse;
                Put(merchantNameEng, answered ? "DEMO STORE" : "");
                SetText(merchantNameArb, answered ? "متجر تجريبي" : "");
                Put(merchantAddEng1, answered ? "RIYADH" : "");
                SetText(merchantAddArb1, answered ? "الرياض" : "");
                Put(merchantAddopt1, "");
                SetText(merchantAddopt2, "");
                return Status();
            }
        }

        public static IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId)
        {
            lock (Gate)
            {
                Begin(inOutBuff, intval, 300);
                Put(terminalId, _scenario != SimulationScenario.NoResponse ? TerminalId : "");
                return Status();
            }
        }

        public static IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval)
        {
            lock (Gate)
            {
                Begin(inOutBuff, intval, 300);
                return Status();
            }
        }

        public static IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval)
        {
            lock (Gate)
            {
                Begin(inOutBuff, intval, 300);
                return Status();
            }
        }

        // ============================================================== reconciliation / التسوية

        public static IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            lock (Gate)
            {
                // EN: The TCP recon function takes the length as "ref int", not a 1-byte array.
                // AR: دالة التسوية عبر TCP تأخذ الطول كـ "ref int" وليس كمصفوفة ببايت واحد.
                Begin(inOutBuff, new[] { (byte)Math.Min(inReqlen, 255) }, 1000);
                return Recon(rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            }
        }

        public static IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            lock (Gate)
            {
                Begin(inOutBuff, intval, 1000);
                return Recon(rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            }
        }

        // ============================================================== repeat last / إعادة آخر عملية

        public static IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, StringBuilder outResp, byte[] outRespLen)
        {
            lock (Gate)
            {
                IntPtr result = Repeat(inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
                // EN: Only the TCP repeat fills the card scheme.   AR: فقط الإعادة عبر TCP تملأ نوع البطاقة.
                Put(Cardscheme, result == IntPtr.Zero ? "MADA" : "");
                return result;
            }
        }

        public static IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            lock (Gate)
            {
                return Repeat(inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            }
        }

        // ============================================================== helpers / دوال مساعدة

        private static IntPtr Transaction(string link, int trnxType, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            lock (Gate)
            {
                // EN: Imitate waiting for the customer's card.   AR: محاكاة انتظار بطاقة العميل.
                string request = Begin(inOutBuff, intval, _cardDelayMs);
                bool declined = _scenario == SimulationScenario.Declined;

                // EN: The terminal processes the card first; whether the answer arrives is decided afterwards.
                // AR: الجهاز يعالج البطاقة أولاً، ثم يتقرر هل يصل الرد أم لا.
                _stan = (_stan % 999999) + 1;
                _lastPan = "XXXXXXXXXXXX0000";
                _lastAmount = request.Split(';', '!')[0].PadLeft(12, '0');
                _lastStan = _stan.ToString("D6", CultureInfo.InvariantCulture);
                // EN: InvariantCulture keeps the date Gregorian on Arabic (Hijri) Windows.
                // AR: InvariantCulture يُبقي التاريخ ميلادياً على Windows العربي (الهجري).
                _lastDateTime = DateTime.Now.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture);
                _lastRrn = Rng.Next(100000, 1000000).ToString(CultureInfo.InvariantCulture) + Rng.Next(100000, 1000000).ToString(CultureInfo.InvariantCulture);
                _lastAuthCode = declined ? "" : Rng.Next(100000, 1000000).ToString(CultureInfo.InvariantCulture);
                _lastResponseCode = declined ? DeclinedResponseCode : "000";

                if (_scenario == SimulationScenario.NoResponse)
                {
                    WriteTransaction(false, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version);
                    WriteRaw(outResp, outRespLen, "SIMULATED: no response (card was charged, use Repeat)");
                    return new IntPtr(NoResponseStatus);
                }

                WriteTransaction(true, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version);
                WriteRaw(outResp, outRespLen, string.Format(CultureInfo.InvariantCulture,
                    "SIMULATED {0} trnxType={1} request={2} via {3}", trnxType == 2 ? "REFUND" : "PURCHASE", trnxType, request, link));
                return IntPtr.Zero;
            }
        }

        private static IntPtr Repeat(byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
        {
            Begin(inOutBuff, intval, 300);
            bool available = _scenario != SimulationScenario.NoResponse && _lastRrn != null;
            WriteTransaction(available, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version);

            if (_scenario == SimulationScenario.NoResponse)
            {
                WriteRaw(outResp, outRespLen, "SIMULATED REPEAT: no response");
                return new IntPtr(NoResponseStatus);
            }
            if (_lastRrn == null)
            {
                WriteRaw(outResp, outRespLen, "SIMULATED REPEAT: no previous transaction");
                return new IntPtr(1);
            }
            WriteRaw(outResp, outRespLen, "SIMULATED REPEAT of RRN " + _lastRrn);
            return IntPtr.Zero;
        }

        private static IntPtr Recon(byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
        {
            bool answered = _scenario != SimulationScenario.NoResponse;
            Put(rspCode, answered ? "000" : "");
            Put(terminalTotal, answered ? "SIMULATED TOTALS" : "");
            Put(hostTotal, answered ? "SIMULATED TOTALS" : "");
            Put(stanNo, answered ? _stan.ToString("D6", CultureInfo.InvariantCulture) : "");
            Put(dataTime, answered ? DateTime.Now.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture) : "");
            Put(terminalId, answered ? TerminalId : "");
            Put(merchantId, answered ? MerchantId : "");
            return Status();
        }

        /// <summary>
        /// EN: Records the request string (length from intval like the real DLL) and waits like a terminal would.
        /// AR: يسجّل نص الطلب (الطول من intval مثل المكتبة الفعلية) وينتظر كما يفعل الجهاز.
        /// </summary>
        private static string Begin(byte[] inOutBuff, byte[] intval, int delayMs)
        {
            _lastRequest = Encoding.ASCII.GetString(inOutBuff, 0, Math.Min(intval[0], inOutBuff.Length));
            Thread.Sleep(delayMs);
            return _lastRequest;
        }

        private static IntPtr Status()
        {
            return _scenario == SimulationScenario.NoResponse ? new IntPtr(NoResponseStatus) : IntPtr.Zero;
        }

        private static void WriteTransaction(bool filled, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version)
        {
            Put(panNo, filled ? _lastPan : "");
            Put(purAmount, filled ? _lastAmount : "");
            Put(stanNo, filled ? _lastStan : "");
            Put(dataTime, filled ? _lastDateTime : "");
            Put(expDate, filled ? "XXXX" : "");
            Put(trxRrn, filled ? _lastRrn : "");
            Put(authCode, filled ? _lastAuthCode : "");
            Put(rspCode, filled ? _lastResponseCode : "");
            Put(terminalId, filled ? TerminalId : "");
            Put(schemeId, filled ? "P1" : "");
            Put(merchantId, filled ? MerchantId : "");
            Put(addtlAmount, "");
            Put(ecrrefno, "");
            Put(version, filled ? "SIM" : "");
        }

        /// <summary>
        /// EN: Writes ASCII text into a DLL-style buffer: cut to fit, '\0'-terminated, rest cleared.
        /// AR: يكتب نص ASCII في مخزن بنفس أسلوب المكتبة: يُقصّ ليناسب الحجم، ثم '\0'، ويُمسح الباقي.
        /// </summary>
        private static void Put(byte[] buffer, string value)
        {
            Array.Clear(buffer, 0, buffer.Length);
            byte[] bytes = Encoding.ASCII.GetBytes(value ?? "");
            Array.Copy(bytes, buffer, Math.Min(bytes.Length, buffer.Length - 1));
        }

        private static void SetText(StringBuilder target, string value)
        {
            target.Length = 0;
            target.Append(value);
        }

        private static void WriteRaw(StringBuilder outResp, byte[] outRespLen, string value)
        {
            SetText(outResp, value);
            outRespLen[0] = (byte)Math.Min(value.Length, 255);
        }

        private static string Ascii(byte[] value)
        {
            int end = Array.IndexOf(value, (byte)0);
            return Encoding.ASCII.GetString(value, 0, end < 0 ? value.Length : end);
        }
    }
}
