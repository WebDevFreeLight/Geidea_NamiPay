using System.Text;
using Mada.Api.Native;
using Mada.Api.Options;
using Mada.Contracts;
using Microsoft.Extensions.Options;

namespace Mada.Api.Services;

/// <summary>
/// EN: Talks to the real terminal through madaapi_v1_9.dll / madaapi_v1_8.dll.
///     Buffer sizes are the same as the old sample (field max length + 1 for '\0').
/// AR: يتواصل مع الجهاز الفعلي عبر madaapi_v1_9.dll و madaapi_v1_8.dll.
///     أحجام المخازن نفسها كما في المثال القديم (أقصى طول للحقل + 1 لحرف '\0').
/// </summary>
public sealed class NativeMadaTerminal : MadaTerminalBase
{
    private readonly ComOptions _com;
    private readonly byte[] _ip;
    private readonly byte[] _port;

    public NativeMadaTerminal(IOptions<TerminalOptions> options, ILogger<NativeMadaTerminal> logger)
        : base(options, logger)
    {
        _com = Options.Com;
        _ip = NativeText.ToCString(Options.Tcp.Ip);
        _port = NativeText.ToCString(Options.Tcp.Port);
    }

    protected override StatusResult CheckStatusCore(Transport transport)
    {
        var req = new NativeRequest(MadaRequests.CheckStatus);
        IntPtr rc = transport == Transport.Com
            ? MadaNative.api_CheckStatus(_com.Port, _com.BaudRate, _com.Parity, _com.DataBits, _com.StopBits, req.Buffer, req.LengthByte)
            : MadaNative.api_TCPIPCheckStatus(_ip, _port, req.Buffer, req.LengthByte);
        return new StatusResult((int)rc, transport);
    }

    protected override TerminalIdResult GetTerminalIdCore()
    {
        var req = new NativeRequest(MadaRequests.TerminalId);
        var terminalId = new byte[17];
        IntPtr rc = MadaNative.api_TCPIPGetTerminalID(_ip, _port, req.Buffer, req.LengthByte, terminalId);
        return new TerminalIdResult((int)rc, NativeText.Read(terminalId));
    }

    protected override MerchantResult GetMerchantDetailsCore()
    {
        var req = new NativeRequest(MadaRequests.MerchantDetails);
        // EN: English fields -> byte[]; Arabic fields -> wide-char StringBuilder (Unicode).
        // AR: الحقول الإنجليزية -> byte[]، والحقول العربية -> StringBuilder بأحرف عريضة (يونيكود).
        var nameEng = new byte[500];
        var addEng1 = new byte[500];
        var addOpt1 = new byte[500];
        var nameArb = new StringBuilder(500);
        var addArb1 = new StringBuilder(500);
        var addOpt2 = new StringBuilder(500);

        IntPtr rc = MadaNative.api_GetMerchantDetails(_com.Port, _com.BaudRate, _com.Parity, _com.DataBits, _com.StopBits,
            req.Buffer, req.LengthByte, nameEng, nameArb, addEng1, addArb1, addOpt1, addOpt2);

        return new MerchantResult((int)rc,
            NativeText.Read(nameEng), nameArb.ToString().Trim(),
            NativeText.Read(addEng1), addArb1.ToString().Trim(),
            NativeText.Read(addOpt1), addOpt2.ToString().Trim());
    }

    protected override TransactionResult TransactionCore(Transport transport, int trnxType, string request)
    {
        var req = new NativeRequest(request);
        var b = new TransactionBuffers();
        IntPtr rc = transport == Transport.Com
            ? MadaNative.api_RequestCOMTrxn(_com.Port, _com.BaudRate, _com.Parity, _com.DataBits, _com.StopBits,
                req.Buffer, req.LengthByte, trnxType, b.Pan, b.Amount, b.Stan, b.DateTime, b.ExpiryDate, b.Rrn, b.AuthCode,
                b.ResponseCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AdditionalAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen)
            : MadaNative.api_RequestTCPIPTrxn(_ip, _port,
                req.Buffer, req.LengthByte, trnxType, b.Pan, b.Amount, b.Stan, b.DateTime, b.ExpiryDate, b.Rrn, b.AuthCode,
                b.ResponseCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AdditionalAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen);
        return b.ToResult((int)rc, transport, Options.IncludeRawResponse);
    }

    protected override TransactionResult RepeatLastCore(Transport transport)
    {
        var req = new NativeRequest(MadaRequests.RepeatLast);
        var b = new TransactionBuffers();
        IntPtr rc;
        if (transport == Transport.Com)
        {
            rc = MadaNative.api_RequestCOMRepeat(_com.Port, _com.BaudRate, _com.Parity, _com.DataBits, _com.StopBits,
                req.Buffer, req.LengthByte, b.Pan, b.Amount, b.Stan, b.DateTime, b.ExpiryDate, b.Rrn, b.AuthCode,
                b.ResponseCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AdditionalAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen);
            return b.ToResult((int)rc, transport, Options.IncludeRawResponse);
        }

        // EN: Only the TCP repeat has the extra Cardscheme output.   AR: فقط إعادة TCP فيها المخرج الإضافي Cardscheme.
        var cardScheme = new byte[10];
        rc = MadaNative.api_RequestTCPIPRepeat(_ip, _port,
            req.Buffer, req.LengthByte, b.Pan, b.Amount, b.Stan, b.DateTime, b.ExpiryDate, b.Rrn, b.AuthCode,
            b.ResponseCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AdditionalAmount, b.EcrRefNo, b.Version, cardScheme, b.OutResp, b.OutRespLen);
        return b.ToResult((int)rc, transport, Options.IncludeRawResponse, NativeText.Read(cardScheme));
    }

    protected override ReconciliationResult ReconcileCore(Transport transport)
    {
        var req = new NativeRequest(MadaRequests.Reconciliation);
        // EN: Generous sizes copied from the old sample.   AR: أحجام كبيرة احتياطاً منسوخة من المثال القديم.
        var rspCode = new byte[30];
        var terminalTotal = new byte[500];
        var hostTotal = new byte[500];
        var stan = new byte[30];
        var dateTime = new byte[30];
        var terminalId = new byte[30];
        var merchantId = new byte[30];

        IntPtr rc;
        if (transport == Transport.Com)
        {
            rc = MadaNative.api_RequestReconCOMWORcpt(_com.Port, _com.BaudRate, _com.Parity, _com.DataBits, _com.StopBits,
                req.Buffer, req.LengthByte, rspCode, terminalTotal, hostTotal, stan, dateTime, terminalId, merchantId);
        }
        else
        {
            // EN: The TCP variant takes the length as "ref int" instead of a 1-byte array.
            // AR: نسخة TCP تأخذ الطول كـ "ref int" بدلاً من مصفوفة ببايت واحد.
            int length = req.Length;
            rc = MadaNative.api_RequestReconTCPIPWORcpt(_ip, _port, req.Buffer, ref length,
                rspCode, terminalTotal, hostTotal, stan, dateTime, terminalId, merchantId);
        }

        return new ReconciliationResult((int)rc, transport,
            NativeText.Read(rspCode), NativeText.Read(terminalTotal), NativeText.Read(hostTotal),
            NativeText.Read(stan), NativeText.Read(dateTime), NativeText.Read(terminalId), NativeText.Read(merchantId));
    }

    /// <summary>
    /// EN: Output buffers shared by purchase, refund and repeat (same sizes as the old sample).
    /// AR: مخازن المخرجات المشتركة بين الشراء والاسترجاع وإعادة العملية (نفس أحجام المثال القديم).
    /// </summary>
    private sealed class TransactionBuffers
    {
        public readonly byte[] Pan = new byte[23];
        public readonly byte[] Amount = new byte[13];
        public readonly byte[] Stan = new byte[7];
        public readonly byte[] DateTime = new byte[13];
        public readonly byte[] ExpiryDate = new byte[5];
        public readonly byte[] Rrn = new byte[13];
        public readonly byte[] AuthCode = new byte[7];
        public readonly byte[] ResponseCode = new byte[4];
        public readonly byte[] TerminalId = new byte[17];
        public readonly byte[] SchemeId = new byte[3];
        public readonly byte[] MerchantId = new byte[16];
        public readonly byte[] AdditionalAmount = new byte[13];
        public readonly byte[] EcrRefNo = new byte[17];
        public readonly byte[] Version = new byte[10];
        public readonly byte[] OutRespLen = new byte[1];
        public readonly StringBuilder OutResp = new(15000);

        public TransactionResult ToResult(int statusCode, Transport transport, bool includeRaw, string? cardScheme = null) =>
            new(statusCode, transport,
                NativeText.Read(Pan), NativeText.Read(Amount), NativeText.Read(Stan), NativeText.Read(DateTime),
                NativeText.Read(ExpiryDate), NativeText.Read(Rrn), NativeText.Read(AuthCode), NativeText.Read(ResponseCode),
                NativeText.Read(TerminalId), NativeText.Read(SchemeId), NativeText.Read(MerchantId),
                NativeText.Read(AdditionalAmount), NativeText.Read(EcrRefNo), NativeText.Read(Version),
                cardScheme,
                includeRaw ? OutResp.ToString() : null);
    }
}
