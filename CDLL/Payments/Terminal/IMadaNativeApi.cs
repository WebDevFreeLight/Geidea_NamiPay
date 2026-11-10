// =====================================================================================
// EN: The 10 functions of the vendor DLLs (madaapi_v1_9.dll / madaapi_v1_8.dll) as an INTERFACE.
//     MadaTerminalService only talks to this interface, so the real DLL (NativeMadaApi), the simulator
//     (SimulatorMadaApi) or a test fake can be plugged in without changing any other code.
//     Parameter meanings are documented in MadaNativeMethods.cs.
// AR: دوال مكتبات المورّد العشر (madaapi_v1_9.dll و madaapi_v1_8.dll) على شكل واجهة (Interface).
//     خدمة MadaTerminalService تتعامل مع هذه الواجهة فقط، لذلك يمكن استخدام المكتبة الفعلية (NativeMadaApi)
//     أو المحاكي (SimulatorMadaApi) أو نسخة وهمية للاختبار بدون تغيير أي كود آخر.
//     معاني المعاملات موضّحة في MadaNativeMethods.cs.
// =====================================================================================

using System;
using System.Text;

namespace CDLL.Payments.Terminal
{
    public interface IMadaNativeApi
    {
        IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen);

        IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen);

        IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, StringBuilder merchantNameArb, byte[] merchantAddEng1, StringBuilder merchantAddArb1, byte[] merchantAddopt1, StringBuilder merchantAddopt2);

        IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

        IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

        IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

        IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, StringBuilder outResp, byte[] outRespLen);

        IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen);
    }

    /// <summary>
    /// EN: The REAL terminal: forwards every call to the vendor DLL (MadaNativeMethods).
    /// AR: الجهاز الفعلي: يمرّر كل استدعاء إلى مكتبة المورّد (MadaNativeMethods).
    /// </summary>
    public sealed class NativeMadaApi : IMadaNativeApi
    {
        public IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => MadaNativeMethods.api_RequestCOMTrxn(port, Rate, x, y, z, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);

        public IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => MadaNativeMethods.api_RequestTCPIPTrxn(ip, port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);

        public IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, StringBuilder merchantNameArb, byte[] merchantAddEng1, StringBuilder merchantAddArb1, byte[] merchantAddopt1, StringBuilder merchantAddopt2)
            => MadaNativeMethods.api_GetMerchantDetails(port, Rate, x, y, z, inOutBuff, intval, merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2);

        public IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId)
            => MadaNativeMethods.api_TCPIPGetTerminalID(ip, port, inOutBuff, intval, terminalId);

        public IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval)
            => MadaNativeMethods.api_CheckStatus(port, Rate, x, y, z, inOutBuff, intval);

        public IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval)
            => MadaNativeMethods.api_TCPIPCheckStatus(ip, port, inOutBuff, intval);

        public IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
            => MadaNativeMethods.api_RequestReconTCPIPWORcpt(ip, port, inOutBuff, ref inReqlen, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

        public IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
            => MadaNativeMethods.api_RequestReconCOMWORcpt(port, Rate, x, y, z, inOutBuff, intval, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

        public IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, StringBuilder outResp, byte[] outRespLen)
            => MadaNativeMethods.api_RequestTCPIPRepeat(ip, port, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, Cardscheme, outResp, outRespLen);

        public IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => MadaNativeMethods.api_RequestCOMRepeat(port, Rate, x, y, z, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
    }

    /// <summary>
    /// EN: FAKE terminal: forwards every call to CDLL.Simulator.MadaApiSimulator. No device, NO real payments.
    ///     Selected with appSettings "TerminalMode" = "Simulator".
    /// AR: جهاز وهمي: يمرّر كل استدعاء إلى CDLL.Simulator.MadaApiSimulator. بدون جهاز، ولا مدفوعات حقيقية.
    ///     يُختار بالإعداد "TerminalMode" = "Simulator".
    /// </summary>
    public sealed class SimulatorMadaApi : IMadaNativeApi
    {
        public IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => Simulator.MadaApiSimulator.api_RequestCOMTrxn(port, Rate, x, y, z, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);

        public IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => Simulator.MadaApiSimulator.api_RequestTCPIPTrxn(ip, port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);

        public IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, StringBuilder merchantNameArb, byte[] merchantAddEng1, StringBuilder merchantAddArb1, byte[] merchantAddopt1, StringBuilder merchantAddopt2)
            => Simulator.MadaApiSimulator.api_GetMerchantDetails(port, Rate, x, y, z, inOutBuff, intval, merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2);

        public IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId)
            => Simulator.MadaApiSimulator.api_TCPIPGetTerminalID(ip, port, inOutBuff, intval, terminalId);

        public IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval)
            => Simulator.MadaApiSimulator.api_CheckStatus(port, Rate, x, y, z, inOutBuff, intval);

        public IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval)
            => Simulator.MadaApiSimulator.api_TCPIPCheckStatus(ip, port, inOutBuff, intval);

        public IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
            => Simulator.MadaApiSimulator.api_RequestReconTCPIPWORcpt(ip, port, inOutBuff, ref inReqlen, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

        public IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId)
            => Simulator.MadaApiSimulator.api_RequestReconCOMWORcpt(port, Rate, x, y, z, inOutBuff, intval, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

        public IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, StringBuilder outResp, byte[] outRespLen)
            => Simulator.MadaApiSimulator.api_RequestTCPIPRepeat(ip, port, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, Cardscheme, outResp, outRespLen);

        public IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, StringBuilder outResp, byte[] outRespLen)
            => Simulator.MadaApiSimulator.api_RequestCOMRepeat(port, Rate, x, y, z, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
    }
}
