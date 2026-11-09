using Mada.Api.Services;
using Mada.Contracts;

namespace Mada.Api.Endpoints;

/// <summary>
/// EN: HTTP routes. Each one maps 1:1 to a button of the old sample (see the README for the table).
/// AR: مسارات HTTP. كل مسار يقابل زراً واحداً في المثال القديم (راجع README للجدول).
/// </summary>
public static class MadaEndpoints
{
    public static IEndpointRouteBuilder MapMadaEndpoints(this IEndpointRouteBuilder app)
    {
        var terminal = app.MapGroup("/api/terminal").WithTags("Terminal");

        // EN: GET /api/terminal/status?transport=Com|Tcp   AR: فحص حالة الجهاز
        terminal.MapGet("/status", (Transport? transport, IMadaTerminal t, CancellationToken ct) =>
                t.CheckStatusAsync(transport, ct))
            .WithSummary("Check terminal status / فحص حالة الجهاز");

        // EN: GET /api/terminal/id (TCP only)   AR: رقم الجهاز (TCP فقط)
        terminal.MapGet("/id", (IMadaTerminal t, CancellationToken ct) =>
                t.GetTerminalIdAsync(ct))
            .WithSummary("Get terminal ID, TCP only / رقم الجهاز، عبر TCP فقط");

        // EN: GET /api/terminal/merchant (COM only)   AR: بيانات التاجر (COM فقط)
        terminal.MapGet("/merchant", (IMadaTerminal t, CancellationToken ct) =>
                t.GetMerchantDetailsAsync(ct))
            .WithSummary("Get merchant name/address, COM only / بيانات التاجر، عبر COM فقط");

        var transactions = app.MapGroup("/api/transactions").WithTags("Transactions");

        // EN: POST /api/transactions/purchase  { "amount": 43.71, "transport": "Tcp" }
        transactions.MapPost("/purchase", (PurchaseRequest body, IMadaTerminal t, CancellationToken ct) =>
                t.PurchaseAsync(body.Amount, body.Transport, ct))
            .WithSummary("Purchase / عملية شراء");

        // EN: POST /api/transactions/refund  { "amount": 0.5, "originalRrn": "434615455456", "originalDate": "2024-12-11" }
        transactions.MapPost("/refund", (RefundRequest body, IMadaTerminal t, CancellationToken ct) =>
                t.RefundAsync(body.Amount, body.OriginalRrn, body.OriginalDate, body.Transport, ct))
            .WithSummary("Refund / عملية استرجاع");

        // EN: POST /api/transactions/repeat  { "transport": "Com" } (body optional)
        transactions.MapPost("/repeat", (TransportRequest? body, IMadaTerminal t, CancellationToken ct) =>
                t.RepeatLastAsync(body?.Transport, ct))
            .WithSummary("Repeat last transaction result / إعادة نتيجة آخر عملية");

        // EN: POST /api/reconciliation  { "transport": "Tcp" } (body optional)
        app.MapPost("/api/reconciliation", (TransportRequest? body, IMadaTerminal t, CancellationToken ct) =>
                t.ReconcileAsync(body?.Transport, ct))
            .WithTags("Reconciliation")
            .WithSummary("End-of-day reconciliation / التسوية وإقفال اليوم");

        return app;
    }
}
