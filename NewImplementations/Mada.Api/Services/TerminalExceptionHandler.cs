using Microsoft.AspNetCore.Diagnostics;

namespace Mada.Api.Services;

/// <summary>
/// EN: Turns exceptions into RFC 7807 "problem+json" responses with a useful status code and message,
///     instead of a bare 500. The WinForms client shows the "detail" text to the user.
/// AR: يحوّل الاستثناءات إلى استجابات "problem+json" (RFC 7807) برمز حالة ورسالة مفيدة بدلاً من 500 فقط.
///     واجهة WinForms تعرض نص "detail" للمستخدم.
/// </summary>
internal sealed class TerminalExceptionHandler(IProblemDetailsService problemDetails, ILogger<TerminalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            // EN: Malformed JSON / missing body.   AR: JSON غير صالح أو جسم الطلب مفقود.
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", bad.Message),

            // EN: Another terminal call is in progress.   AR: يوجد استدعاء آخر للجهاز قيد التنفيذ.
            TerminalBusyException => (StatusCodes.Status409Conflict, "Terminal busy", exception.Message),

            // EN: Validation failures thrown by MadaRequests.   AR: أخطاء التحقق التي يرميها MadaRequests.
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),

            // EN: DLL missing, wrong bitness or missing export.   AR: المكتبة مفقودة أو بنظام بت خاطئ أو دالة غير موجودة.
            DllNotFoundException or BadImageFormatException or EntryPointNotFoundException =>
                (StatusCodes.Status503ServiceUnavailable, "mada DLL unavailable",
                 $"{exception.Message} The API process is {(Environment.Is64BitProcess ? "64" : "32")}-bit; the mada DLLs are 32-bit and must sit next to Mada.Api.exe."),

            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "See the API log for details."),
        };

        if (status >= 500)
            logger.LogError(exception, "Request failed: {Title}", title);
        else
            logger.LogWarning("Request rejected ({Status}): {Detail}", status, detail);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }
}
