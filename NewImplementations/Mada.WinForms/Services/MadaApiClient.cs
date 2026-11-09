using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mada.Contracts;

namespace Mada.WinForms.Services;

/// <summary>
/// EN: Typed HTTP client for Mada.Api. One method per endpoint, using the shared Mada.Contracts types.
///     Non-2xx responses become MadaApiException carrying the API's problem+json "detail" message.
/// AR: عميل HTTP مُنمَّط للخدمة Mada.Api. دالة لكل نقطة، باستخدام أنواع Mada.Contracts المشتركة.
///     الاستجابات غير الناجحة تتحول إلى MadaApiException تحمل رسالة "detail" من problem+json.
/// </summary>
public sealed class MadaApiClient : IDisposable
{
    /// <summary>EN: Must match the API: camelCase + enums as strings.  AR: يجب أن تطابق الخدمة: camelCase والقيم المعدودة كنصوص.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _http;

    public Uri BaseAddress { get; }

    public MadaApiClient(Uri baseAddress)
    {
        BaseAddress = baseAddress;
        _http = new HttpClient
        {
            BaseAddress = baseAddress,
            // EN: A purchase waits for the customer to tap the card, so the default 100 s can be too short.
            // AR: عملية الشراء تنتظر العميل حتى يمرّر بطاقته، لذلك قد تكون المهلة الافتراضية (100 ثانية) قصيرة.
            Timeout = TimeSpan.FromMinutes(3),
        };
    }

    public Task<StatusResult> CheckStatusAsync(Transport transport, CancellationToken ct = default) =>
        GetAsync<StatusResult>($"api/terminal/status?transport={transport}", ct);

    public Task<TerminalIdResult> GetTerminalIdAsync(CancellationToken ct = default) =>
        GetAsync<TerminalIdResult>("api/terminal/id", ct);

    public Task<MerchantResult> GetMerchantDetailsAsync(CancellationToken ct = default) =>
        GetAsync<MerchantResult>("api/terminal/merchant", ct);

    public Task<TransactionResult> PurchaseAsync(decimal amount, Transport transport, CancellationToken ct = default) =>
        PostAsync<PurchaseRequest, TransactionResult>("api/transactions/purchase", new PurchaseRequest(amount, transport), ct);

    public Task<TransactionResult> RefundAsync(decimal amount, string originalRrn, DateOnly originalDate, Transport transport, CancellationToken ct = default) =>
        PostAsync<RefundRequest, TransactionResult>("api/transactions/refund", new RefundRequest(amount, originalRrn, originalDate, transport), ct);

    public Task<TransactionResult> RepeatLastAsync(Transport transport, CancellationToken ct = default) =>
        PostAsync<TransportRequest, TransactionResult>("api/transactions/repeat", new TransportRequest(transport), ct);

    public Task<ReconciliationResult> ReconcileAsync(Transport transport, CancellationToken ct = default) =>
        PostAsync<TransportRequest, ReconciliationResult>("api/reconciliation", new TransportRequest(transport), ct);

    public void Dispose() => _http.Dispose();

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(path, ct);
        return await ReadAsync<T>(response, ct);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
                   ?? throw new MadaApiException((int)response.StatusCode, "Empty response from API. / استجابة فارغة من الخدمة.");
        }

        // EN: Try to read the API's problem+json body for a readable message.
        // AR: محاولة قراءة جسم problem+json من الخدمة للحصول على رسالة مفهومة.
        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            // EN: Not JSON (e.g. a proxy error page). Fall back to the HTTP reason phrase.
            // AR: ليس JSON (مثل صفحة خطأ من وسيط). نستخدم نص حالة HTTP بدلاً منه.
        }

        string message = problem?.Detail ?? problem?.Title ?? response.ReasonPhrase ?? "Unknown error";
        throw new MadaApiException((int)response.StatusCode, message);
    }

    /// <summary>EN: The fields we read from an RFC 7807 problem+json body.  AR: الحقول التي نقرأها من جسم problem+json.</summary>
    private sealed record ApiProblem(string? Title, string? Detail, int? Status);
}

/// <summary>
/// EN: The API answered with an error (400 invalid input, 409 terminal busy, 503 DLL problem, ...).
/// AR: الخدمة ردّت بخطأ (400 مدخلات غير صحيحة، 409 الجهاز مشغول، 503 مشكلة في المكتبة، ...).
/// </summary>
public sealed class MadaApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
