using Mada.Api.Options;
using Mada.Contracts;
using Microsoft.Extensions.Options;

namespace Mada.Api.Services;

/// <summary>
/// EN: Thrown when a request arrives while the terminal is still handling another one. Mapped to HTTP 409.
/// AR: يُرمى عند وصول طلب بينما الجهاز ما زال يعالج طلباً آخر. يتحول إلى HTTP 409.
/// </summary>
public sealed class TerminalBusyException()
    : Exception("The terminal is busy with another request. Try again when it finishes. / الجهاز مشغول بطلب آخر، حاول مرة أخرى بعد انتهائه.");

/// <summary>
/// EN: Shared logic for both terminal implementations:
///       1. validate input and build the request string BEFORE touching the terminal (fast HTTP 400),
///       2. allow only ONE terminal call at a time (a physical terminal handles one request at a time),
///       3. run the blocking DLL call on a worker thread, not on the request thread.
///     Subclasses only implement the *Core methods, which do the actual (blocking) work.
/// AR: منطق مشترك لكلا التطبيقين:
///       1. التحقق من المدخلات وبناء نص الطلب قبل التواصل مع الجهاز (رد سريع HTTP 400)،
///       2. السماح باستدعاء واحد فقط للجهاز في كل مرة (الجهاز الفعلي يعالج طلباً واحداً في كل مرة)،
///       3. تشغيل استدعاء المكتبة الحاجب على خيط عامل وليس على خيط الطلب.
///     الكلاسات الفرعية تطبّق فقط دوال *Core التي تنفّذ العمل الفعلي (الحاجب).
/// </summary>
public abstract class MadaTerminalBase : IMadaTerminal
{
    // EN: The service is registered as a singleton, so this one gate covers every HTTP request.
    // AR: الخدمة مسجّلة كـ Singleton، لذلك هذه البوابة الواحدة تشمل كل طلبات HTTP.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger _logger;

    protected TerminalOptions Options { get; }

    protected MadaTerminalBase(IOptions<TerminalOptions> options, ILogger logger)
    {
        Options = options.Value;
        _logger = logger;
    }

    // ------------------------------------------------------------------ public API

    public Task<StatusResult> CheckStatusAsync(Transport? transport, CancellationToken ct)
    {
        var t = Resolve(transport);
        return RunExclusiveAsync("CheckStatus", () => CheckStatusCore(t), r => r.StatusCode, ct);
    }

    public Task<TerminalIdResult> GetTerminalIdAsync(CancellationToken ct) =>
        RunExclusiveAsync("GetTerminalId", GetTerminalIdCore, r => r.StatusCode, ct);

    public Task<MerchantResult> GetMerchantDetailsAsync(CancellationToken ct) =>
        RunExclusiveAsync("GetMerchantDetails", GetMerchantDetailsCore, r => r.StatusCode, ct);

    public Task<TransactionResult> PurchaseAsync(decimal amount, Transport? transport, CancellationToken ct)
    {
        var t = Resolve(transport);
        string request = MadaRequests.Purchase(amount);   // EN: validates amount   AR: يتحقق من المبلغ
        return RunExclusiveAsync("Purchase", () => TransactionCore(t, MadaRequests.PurchaseType, request), r => r.StatusCode, ct);
    }

    public Task<TransactionResult> RefundAsync(decimal amount, string originalRrn, DateOnly originalDate, Transport? transport, CancellationToken ct)
    {
        var t = Resolve(transport);
        string request = MadaRequests.Refund(amount, originalRrn, originalDate);   // EN: validates input   AR: يتحقق من المدخلات
        return RunExclusiveAsync("Refund", () => TransactionCore(t, MadaRequests.RefundType, request), r => r.StatusCode, ct);
    }

    public Task<TransactionResult> RepeatLastAsync(Transport? transport, CancellationToken ct)
    {
        var t = Resolve(transport);
        return RunExclusiveAsync("RepeatLast", () => RepeatLastCore(t), r => r.StatusCode, ct);
    }

    public Task<ReconciliationResult> ReconcileAsync(Transport? transport, CancellationToken ct)
    {
        var t = Resolve(transport);
        return RunExclusiveAsync("Reconcile", () => ReconcileCore(t), r => r.StatusCode, ct);
    }

    // ------------------------------------------------------------------ for subclasses

    protected abstract StatusResult CheckStatusCore(Transport transport);
    protected abstract TerminalIdResult GetTerminalIdCore();
    protected abstract MerchantResult GetMerchantDetailsCore();
    protected abstract TransactionResult TransactionCore(Transport transport, int trnxType, string request);
    protected abstract TransactionResult RepeatLastCore(Transport transport);
    protected abstract ReconciliationResult ReconcileCore(Transport transport);

    // ------------------------------------------------------------------ helpers

    private Transport Resolve(Transport? transport) => transport ?? Options.DefaultTransport;

    /// <summary>
    /// EN: Runs one terminal call while holding the gate. Card fields are never logged; only the operation name and status code.
    /// AR: ينفّذ استدعاءً واحداً للجهاز مع حجز البوابة. لا يتم تسجيل بيانات البطاقة أبداً، فقط اسم العملية ورمز الحالة.
    /// </summary>
    private async Task<T> RunExclusiveAsync<T>(string operation, Func<T> terminalCall, Func<T, int> statusOf, CancellationToken ct)
    {
        // TODO(policy): What should happen when a second request arrives while the terminal is busy?
        //   Current behaviour: reject immediately -> HTTP 409, and the client may retry.
        //   Alternative: wait in line, e.g. `await _gate.WaitAsync(TimeSpan.FromSeconds(30), ct)`.
        //   Waiting feels smoother for a single cashier, but a queued PURCHASE could start on the
        //   terminal long after the cashier gave up on it, and the customer may get charged unexpectedly.
        // TODO(سياسة): ماذا يحدث عند وصول طلب ثانٍ والجهاز مشغول؟
        //   السلوك الحالي: الرفض فوراً -> HTTP 409، ويمكن للعميل إعادة المحاولة.
        //   البديل: الانتظار في طابور، مثلاً `await _gate.WaitAsync(TimeSpan.FromSeconds(30), ct)`.
        //   الانتظار أسهل للكاشير الواحد، لكن عملية شراء في الطابور قد تبدأ على الجهاز بعد أن تخلّى
        //   الكاشير عنها بوقت طويل، وقد يتم الخصم من العميل بشكل غير متوقع.
        if (!await _gate.WaitAsync(TimeSpan.Zero, ct))
            throw new TerminalBusyException();

        try
        {
            _logger.LogInformation("Terminal {Operation} started", operation);

            // EN: The DLL call blocks until the terminal answers (a purchase waits for the card) and cannot be
            //     cancelled. CancellationToken.None on purpose: if the HTTP client disconnects, the call still
            //     finishes and the gate is released only after the terminal is really free.
            //     A lost purchase result can be fetched later with the "repeat" endpoint.
            // AR: استدعاء المكتبة يحجب حتى يرد الجهاز (الشراء ينتظر البطاقة) ولا يمكن إلغاؤه.
            //     نستخدم CancellationToken.None عمداً: إذا انقطع اتصال العميل يكتمل الاستدعاء رغم ذلك،
            //     ولا تُحرَّر البوابة إلا بعد أن يصبح الجهاز متاحاً فعلاً.
            //     يمكن جلب نتيجة عملية شراء مفقودة لاحقاً عبر نقطة "repeat".
            T result = await Task.Run(terminalCall, CancellationToken.None);

            _logger.LogInformation("Terminal {Operation} finished with status {StatusCode}", operation, statusOf(result));
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }
}
