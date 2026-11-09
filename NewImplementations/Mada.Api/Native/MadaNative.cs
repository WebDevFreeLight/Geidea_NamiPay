using System.Runtime.InteropServices;
using System.Text;

namespace Mada.Api.Native;

/// <summary>
/// EN: P/Invoke declarations for the vendor DLLs, copied unchanged from the old sample (CDLL\Form1.cs).
///     Do NOT call these directly from endpoints. Go through NativeMadaTerminal, which owns buffer sizes,
///     encoding and the one-call-at-a-time lock. Parameter meanings are documented at the top of CDLL\Form1.cs.
/// AR: تعريفات P/Invoke لمكتبات المورّد، منسوخة كما هي من المثال القديم (CDLL\Form1.cs).
///     لا تستدعِها مباشرة من نقاط الخدمة. استخدم NativeMadaTerminal الذي يتولى أحجام المخازن والترميز
///     وقفل "استدعاء واحد في كل مرة". معاني المعاملات موثّقة أعلى ملف CDLL\Form1.cs.
/// </summary>
internal static class MadaNative
{
    private const string V19 = "madaapi_v1_9.dll";
    // EN: Repeat functions exist only in the older v1.8 DLL (same as the old sample).
    // AR: دوال إعادة آخر عملية موجودة فقط في المكتبة الأقدم v1.8 (كما في المثال القديم).
    private const string V18 = "madaapi_v1_8.dll";

    // ---- Financial transactions / العمليات المالية (trnxType: 0 = purchase شراء, 2 = refund استرجاع) ----

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

    // ---- Terminal info / معلومات الجهاز ----

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantNameArb, byte[] merchantAddEng1, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantAddArb1, byte[] merchantAddopt1, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder merchantAddopt2);

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

    // ---- Reconciliation without receipt / التسوية بدون إيصال ----

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

    [DllImport(V19, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

    // ---- Repeat last transaction / إعادة آخر عملية ----

    [DllImport(V18, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

    [DllImport(V18, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);
}
