// =====================================================================================
// EN: NATIVE DLL DECLARATIONS (P/Invoke), moved here from Form1.cs unchanged.
//     [DllImport] tells .NET to load the named DLL at runtime and call the exported C function with the
//     same name. StdCall = Win32 calling convention. The DLL is looked up next to CDLL.exe first, then in
//     the system PATH. byte[] is passed as a char* the DLL can write into; StringBuilder with LPWStr is
//     passed as a wchar_t* (wide/Unicode) buffer. Both DLLs are 32-bit, so CDLL must run as x86.
//     Only NativeMadaApi calls these; everything else goes through IMadaNativeApi.
// AR: تعريفات دوال المكتبة الأصلية (P/Invoke)، نُقلت هنا من Form1.cs بدون تغيير.
//     الخاصية [DllImport] تجعل ‎.NET تحمّل المكتبة المذكورة وقت التشغيل وتستدعي دالة C المُصدَّرة بنفس الاسم.
//     StdCall هي طريقة الاستدعاء القياسية في Win32. يتم البحث عن المكتبة أولاً بجانب CDLL.exe ثم في PATH.
//     المصفوفة byte[] تُمرَّر كمؤشر char* تكتب فيه المكتبة، و StringBuilder مع LPWStr تُمرَّر كمخزن wchar_t*.
//     المكتبتان بنظام 32-بت، لذلك يجب تشغيل CDLL كـ x86.
//     NativeMadaApi فقط يستدعي هذه الدوال، وكل ما عداه يمر عبر IMadaNativeApi.
//
// -------------------------------------------------------------------------------------
//   COM (USB/serial) functions take:  (int port, int Rate, int x, int y, int z, ...)
//     port = COM number (8 => "COM8"), Rate = baud (38400), x = parity (0 = none),
//     y = data bits (8), z = stop bits (0 = one stop bit).
//     AR: port = رقم منفذ COM، Rate = السرعة، x = التماثل، y = بتات البيانات، z = بتات التوقف.
//   TCP/IP functions take:  (byte[] ip, byte[] port, ...) as ASCII bytes + '\0'.
//     AR: دوال TCP/IP تأخذ عنوان IP والمنفذ كبايتات ASCII مع '\0'.
//   inOutBuff = request string as ASCII (ends with "!"); intval = 1-byte length of inOutBuff.
//     AR: inOutBuff = نص الطلب (ينتهي بـ "!")، intval = طول الطلب في بايت واحد.
//   trnxType: 0 = Purchase, 2 = Refund.   AR: 0 = شراء، 2 = استرجاع.
//   Output buffers (size = max length + 1 for '\0'):  AR: مخازن المخرجات (الحجم = أقصى طول + 1):
//     panNo[23] card number (masked by the terminal; masked again before saving), purAmount[13], stanNo[7],
//     dataTime[13], expDate[5] (NEVER read or saved), trxRrn[13], authCode[7], rspCode[4], terminalId[17],
//     schemeId[3], merchantId[16], addtlAmount[13], ecrrefno[17], version[10], outResp (StringBuilder 15000).
//   Return value: status code as IntPtr. 0 = the call worked. Seen on this site: -3 no terminal / no answer,
//     -4 communication timeout, 8 cancelled on the terminal or timed out waiting for the card
//     (after ~15 s = timeout, sooner = cancel; see OutcomeClassifier).
//     Other values come from the vendor document.
//     AR: القيمة المُعادة رمز حالة: 0 = نجح الاستدعاء. لوحظ: ‎-3 لا يوجد جهاز، ‎-4 انتهت مهلة الاتصال،
//         8 = أُلغيت على الجهاز أو انتهت مهلة انتظار البطاقة.
// =====================================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CDLL.Payments.Terminal
{
    internal static class MadaNativeMethods
    {
        /// <summary>
        /// EN: Financial transaction (Purchase trnxType=0, Refund trnxType=2) over USB/COM.
        /// AR: عملية مالية (شراء trnxType=0، استرجاع trnxType=2) عبر USB/COM.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>EN: Same as api_RequestCOMTrxn but over TCP/IP.  AR: مثل api_RequestCOMTrxn لكن عبر TCP/IP.</summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Merchant name and address, English (byte[]) and Arabic (Unicode StringBuilder). Request "0A!". USB/COM only.
        /// AR: اسم التاجر وعنوانه بالإنجليزية والعربية. الطلب "0A!". عبر USB/COM فقط.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantNameArb, byte[] merchantAddEng1, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantAddArb1, byte[] merchantAddopt1, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantAddopt2);

        /// <summary>EN: Terminal ID over TCP/IP. Request "07!".  AR: رقم الجهاز عبر TCP/IP. الطلب "07!".</summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

        /// <summary>EN: Is the terminal connected and ready (USB/COM). Request "04!".  AR: هل الجهاز متصل وجاهز (USB/COM). الطلب "04!".</summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

        /// <summary>EN: Same as api_CheckStatus but over TCP/IP.  AR: مثل api_CheckStatus لكن عبر TCP/IP.</summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

        /// <summary>
        /// EN: Reconciliation (end of day) over TCP/IP without receipt. The request length is passed by reference.
        /// AR: التسوية (إقفال اليوم) عبر TCP/IP بدون إيصال. طول الطلب يُمرَّر بالمرجع.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>EN: Reconciliation over USB/COM without receipt.  AR: التسوية عبر USB/COM بدون إيصال.</summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>
        /// EN: Result of the LAST transaction again, over TCP/IP ("0B!"). From the OLDER madaapi_v1_8.dll; extra Cardscheme buffer.
        ///     Use it when an answer was lost, before charging the customer again.
        /// AR: نتيجة آخر عملية مرة أخرى عبر TCP/IP ("0B!"). من المكتبة الأقدم madaapi_v1_8.dll، مع مخزن Cardscheme إضافي.
        ///     استخدمها عندما يضيع الرد، قبل الخصم من العميل مرة أخرى.
        /// </summary>
        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>EN: Repeat last transaction over USB/COM, from madaapi_v1_8.dll.  AR: إعادة آخر عملية عبر USB/COM من madaapi_v1_8.dll.</summary>
        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);
    }
}
