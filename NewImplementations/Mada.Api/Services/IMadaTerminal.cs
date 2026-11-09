using Mada.Contracts;

namespace Mada.Api.Services;

/// <summary>
/// EN: Everything the API can ask the terminal to do. Two implementations:
///     NativeMadaTerminal (real DLL + hardware) and SimulatedMadaTerminal (fake data, no hardware).
///     Program.cs picks one from "Terminal:UseSimulator".
///     A null transport means "use Terminal:DefaultTransport".
/// AR: كل ما تستطيع الخدمة طلبه من الجهاز. يوجد تطبيقان:
///     NativeMadaTerminal (المكتبة الفعلية + الجهاز) و SimulatedMadaTerminal (بيانات وهمية بدون جهاز).
///     ملف Program.cs يختار أحدهما حسب "Terminal:UseSimulator".
///     قيمة transport الفارغة (null) تعني "استخدم Terminal:DefaultTransport".
/// </summary>
public interface IMadaTerminal
{
    Task<StatusResult> CheckStatusAsync(Transport? transport, CancellationToken ct);

    /// <summary>EN: TCP only.  AR: عبر TCP فقط.</summary>
    Task<TerminalIdResult> GetTerminalIdAsync(CancellationToken ct);

    /// <summary>EN: COM only.  AR: عبر COM فقط.</summary>
    Task<MerchantResult> GetMerchantDetailsAsync(CancellationToken ct);

    Task<TransactionResult> PurchaseAsync(decimal amount, Transport? transport, CancellationToken ct);

    Task<TransactionResult> RefundAsync(decimal amount, string originalRrn, DateOnly originalDate, Transport? transport, CancellationToken ct);

    Task<TransactionResult> RepeatLastAsync(Transport? transport, CancellationToken ct);

    Task<ReconciliationResult> ReconcileAsync(Transport? transport, CancellationToken ct);
}
