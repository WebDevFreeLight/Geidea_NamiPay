using System.Globalization;
using Mada.Contracts;

namespace Mada.Simulator.Simulation;

/// <summary>
/// EN: What the simulated terminal should do on the next operation.
/// AR: ما يجب أن يفعله الجهاز الوهمي في العملية التالية.
/// </summary>
public enum SimulationScenario
{
    /// <summary>EN: Everything succeeds, transactions approved ("000").  AR: كل شيء ينجح والعمليات مقبولة ("000").</summary>
    Approved,

    /// <summary>EN: The terminal answers but the bank declines.  AR: الجهاز يرد لكن البنك يرفض العملية.</summary>
    Declined,

    /// <summary>
    /// EN: The card IS charged, but the answer never reaches the cash register (cable pulled, timeout).
    ///     The real-life reason the Repeat buttons exist: press Repeat to find the approved transaction.
    /// AR: يتم الخصم من البطاقة فعلاً، لكن الرد لا يصل إلى نظام نقاط البيع (فصل الكابل، انتهاء المهلة).
    ///     هذا هو السبب الحقيقي لوجود أزرار الإعادة: اضغط Repeat لإيجاد العملية المقبولة.
    /// </summary>
    NoResponse,
}

/// <summary>
/// EN: In-memory fake mada terminal. Same operations and request strings as the old CDLL sample,
///     returning the shared Mada.Contracts result types. Nothing leaves this process.
///     Used only from the UI thread (buttons are disabled while an operation runs), so no locking is needed.
/// AR: جهاز مدى وهمي داخل الذاكرة. نفس عمليات ونصوص طلبات مثال CDLL القديم، ويُعيد أنواع النتائج
///     المشتركة من Mada.Contracts. لا يخرج أي شيء من هذا البرنامج.
///     يُستخدم فقط من خيط الواجهة (الأزرار معطّلة أثناء التنفيذ)، لذلك لا حاجة لأقفال.
/// </summary>
public sealed class TerminalSimulator
{
    /// <summary>
    /// EN: Status code for "terminal did not answer". -3 is what the real madaapi_v1_9.dll returned
    ///     when tested with no terminal connected.
    /// AR: رمز الحالة لـ "الجهاز لم يرد". القيمة -3 هي ما أعادته المكتبة الفعلية madaapi_v1_9.dll
    ///     عند اختبارها بدون جهاز متصل.
    /// </summary>
    public const int NoResponseStatus = -3;

    /// <summary>
    /// EN: Response code used for the Declined scenario. ILLUSTRATIVE ONLY: take the real decline codes from the vendor spec.
    /// AR: رمز الاستجابة المستخدم في سيناريو الرفض. للتوضيح فقط: خذ رموز الرفض الحقيقية من وثيقة المورّد.
    /// </summary>
    public const string DeclinedResponseCode = "100";

    public const string TerminalId = "SIMULATOR0000001";
    public const string MerchantId = "000000000000001";

    private int _stan;
    private TransactionResult? _last;

    public SimulationScenario Scenario { get; set; } = SimulationScenario.Approved;

    /// <summary>EN: Delay that imitates the customer tapping the card.  AR: تأخير يحاكي تمرير العميل لبطاقته.</summary>
    public TimeSpan CardDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// EN: The request string the real DLL would have been given for the last operation (shown in the log).
    /// AR: نص الطلب الذي كانت ستستلمه المكتبة الفعلية في آخر عملية (يظهر في السجل).
    /// </summary>
    public string LastRequest { get; private set; } = "";

    // ------------------------------------------------------------------ terminal info / معلومات الجهاز

    public async Task<StatusResult> CheckStatusAsync(Transport transport)
    {
        LastRequest = MadaRequests.CheckStatus;
        await Task.Delay(300);
        return new StatusResult(NoAnswer ? NoResponseStatus : 0, transport);
    }

    public async Task<TerminalIdResult> GetTerminalIdAsync()
    {
        LastRequest = MadaRequests.TerminalId;
        await Task.Delay(300);
        return NoAnswer ? new TerminalIdResult(NoResponseStatus, "") : new TerminalIdResult(0, TerminalId);
    }

    public async Task<MerchantResult> GetMerchantDetailsAsync()
    {
        LastRequest = MadaRequests.MerchantDetails;
        await Task.Delay(300);
        return NoAnswer
            ? new MerchantResult(NoResponseStatus, "", "", "", "", "", "")
            : new MerchantResult(0, "DEMO STORE", "متجر تجريبي", "RIYADH", "الرياض", "", "");
    }

    // ------------------------------------------------------------------ transactions / العمليات المالية

    /// <summary>EN: Throws ArgumentException for an invalid amount (same rules as Mada.Api).  AR: يرمي ArgumentException لمبلغ غير صالح (نفس قواعد Mada.Api).</summary>
    public Task<TransactionResult> PurchaseAsync(decimal amount, Transport transport)
    {
        LastRequest = MadaRequests.Purchase(amount);
        return TransactionAsync(transport, MadaRequests.ToHalalas(amount));
    }

    /// <summary>EN: Throws ArgumentException for an invalid amount or RRN.  AR: يرمي ArgumentException لمبلغ أو رقم مرجعي غير صالح.</summary>
    public Task<TransactionResult> RefundAsync(decimal amount, string originalRrn, DateOnly originalDate, Transport transport)
    {
        LastRequest = MadaRequests.Refund(amount, originalRrn, originalDate);
        return TransactionAsync(transport, MadaRequests.ToHalalas(amount));
    }

    /// <summary>
    /// EN: Returns the last transaction the terminal processed, even one whose answer was "lost" (NoResponse).
    ///     Status 1 with empty fields if there was none yet.
    /// AR: تُعيد آخر عملية عالجها الجهاز، حتى لو "ضاع" ردها (NoResponse).
    ///     الحالة 1 مع حقول فارغة إذا لم توجد عملية بعد.
    /// </summary>
    public async Task<TransactionResult> RepeatLastAsync(Transport transport)
    {
        LastRequest = MadaRequests.RepeatLast;
        await Task.Delay(300);
        if (NoAnswer)
            return Empty(transport, NoResponseStatus);
        if (_last is null)
            return Empty(transport, 1);

        // EN: Only the TCP repeat function of the DLL fills CardScheme.   AR: فقط دالة الإعادة عبر TCP تملأ CardScheme.
        return _last with { Transport = transport, CardScheme = transport == Transport.Tcp ? "MADA" : null };
    }

    public async Task<ReconciliationResult> ReconcileAsync(Transport transport)
    {
        LastRequest = MadaRequests.Reconciliation;
        await Task.Delay(1000);
        return NoAnswer
            ? new ReconciliationResult(NoResponseStatus, transport, "", "", "", "", "", "", "")
            : new ReconciliationResult(0, transport, "000", "SIMULATED TOTALS", "SIMULATED TOTALS",
                _stan.ToString("D6", CultureInfo.InvariantCulture), Now(), TerminalId, MerchantId);
    }

    // ------------------------------------------------------------------ helpers

    private bool NoAnswer => Scenario == SimulationScenario.NoResponse;

    private async Task<TransactionResult> TransactionAsync(Transport transport, long halalas)
    {
        await Task.Delay(CardDelay);

        bool declined = Scenario == SimulationScenario.Declined;
        _stan = (_stan % 999_999) + 1;

        var result = new TransactionResult(
            StatusCode: 0,
            Transport: transport,
            Pan: "XXXXXXXXXXXX0000",
            Amount: halalas.ToString("D12", CultureInfo.InvariantCulture),
            Stan: _stan.ToString("D6", CultureInfo.InvariantCulture),
            DateTime: Now(),
            ExpiryDate: "XXXX",
            Rrn: Random.Shared.NextInt64(100_000_000_000, 1_000_000_000_000).ToString(CultureInfo.InvariantCulture),
            AuthCode: declined ? "" : Random.Shared.Next(100_000, 1_000_000).ToString(CultureInfo.InvariantCulture),
            ResponseCode: declined ? DeclinedResponseCode : "000",
            TerminalId: TerminalId,
            SchemeId: "P1",
            MerchantId: MerchantId,
            AdditionalAmount: "",
            EcrRefNo: "",
            Version: "SIM",
            CardScheme: null,
            RawResponse: null);

        // EN: The terminal remembers the transaction even when its answer is lost (that is what Repeat reads).
        // AR: الجهاز يحفظ العملية حتى لو ضاع ردها (وهذا ما يقرأه زر الإعادة).
        _last = result;
        return NoAnswer ? Empty(transport, NoResponseStatus) : result;
    }

    private static TransactionResult Empty(Transport transport, int status) =>
        new(status, transport, "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, null);

    // EN: InvariantCulture keeps the date Gregorian on Arabic (Hijri) Windows.   AR: InvariantCulture يُبقي التاريخ ميلادياً على Windows العربي (الهجري).
    private static string Now() => DateTime.Now.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture);
}
