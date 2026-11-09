using Mada.Api.Options;
using Mada.Contracts;
using Microsoft.Extensions.Options;

namespace Mada.Api.Services;

/// <summary>
/// EN: Fake terminal for development and demos: no DLL, no hardware, no money moves.
///     Every transaction is approved ("000") after a short delay that imitates waiting for the card.
///     Enabled by "Terminal:UseSimulator": true (the default in appsettings.Development.json).
/// AR: جهاز وهمي للتطوير والعروض: بدون مكتبة وبدون جهاز ولا تتحرك أي أموال.
///     كل عملية تُقبل ("000") بعد تأخير قصير يحاكي انتظار البطاقة.
///     يُفعَّل بالإعداد "Terminal:UseSimulator": true (الافتراضي في appsettings.Development.json).
/// </summary>
public sealed class SimulatedMadaTerminal(IOptions<TerminalOptions> options, ILogger<SimulatedMadaTerminal> logger)
    : MadaTerminalBase(options, logger)
{
    private const string TerminalId = "SIMULATOR0000001";
    private const string MerchantId = "000000000000001";

    private int _stan;
    // EN: Safe without extra locking: the base class runs only one *Core call at a time.
    // AR: آمن بدون قفل إضافي: الكلاس الأساسي ينفّذ استدعاء *Core واحداً فقط في كل مرة.
    private TransactionResult? _last;

    protected override StatusResult CheckStatusCore(Transport transport)
    {
        Thread.Sleep(300);
        return new StatusResult(0, transport);
    }

    protected override TerminalIdResult GetTerminalIdCore()
    {
        Thread.Sleep(300);
        return new TerminalIdResult(0, TerminalId);
    }

    protected override MerchantResult GetMerchantDetailsCore()
    {
        Thread.Sleep(300);
        return new MerchantResult(0, "DEMO STORE", "متجر تجريبي", "RIYADH", "الرياض", "", "");
    }

    protected override TransactionResult TransactionCore(Transport transport, int trnxType, string request)
    {
        // EN: Imitate the customer tapping the card.   AR: محاكاة تمرير العميل لبطاقته.
        Thread.Sleep(2000);

        string halalas = request.Split(';')[0];
        _stan = (_stan % 999_999) + 1;
        _last = new TransactionResult(
            StatusCode: 0,
            Transport: transport,
            Pan: "XXXXXXXXXXXX0000",
            Amount: halalas.PadLeft(12, '0'),
            Stan: _stan.ToString("D6"),
            DateTime: DateTime.Now.ToString("ddMMyyHHmmss", System.Globalization.CultureInfo.InvariantCulture),
            ExpiryDate: "XXXX",
            Rrn: Random.Shared.NextInt64(100_000_000_000, 999_999_999_999).ToString(),
            AuthCode: Random.Shared.Next(100_000, 999_999).ToString(),
            ResponseCode: "000",
            TerminalId: TerminalId,
            SchemeId: "P1",
            MerchantId: MerchantId,
            AdditionalAmount: "",
            EcrRefNo: "",
            Version: "SIM",
            CardScheme: null,
            RawResponse: Options.IncludeRawResponse ? $"SIMULATED trnxType={trnxType} request={request}" : null);
        return _last;
    }

    protected override TransactionResult RepeatLastCore(Transport transport)
    {
        Thread.Sleep(300);
        // EN: No previous transaction -> empty result with a non-zero status.
        // AR: لا توجد عملية سابقة -> نتيجة فارغة برمز حالة غير صفري.
        return _last is null
            ? new TransactionResult(1, transport, "", "", "", "", "", "", "", "", TerminalId, "", MerchantId, "", "", "SIM", null, null)
            : _last with { Transport = transport, CardScheme = transport == Transport.Tcp ? "MADA" : null };
    }

    protected override ReconciliationResult ReconcileCore(Transport transport)
    {
        Thread.Sleep(1000);
        return new ReconciliationResult(0, transport, "000", "SIMULATED TOTALS", "SIMULATED TOTALS",
            _stan.ToString("D6"), DateTime.Now.ToString("ddMMyyHHmmss", System.Globalization.CultureInfo.InvariantCulture),
            TerminalId, MerchantId);
    }
}
