using System.Text;
using Mada.Contracts;
using Mada.Simulator.Simulation;

namespace Mada.Simulator;

/// <summary>
/// EN: Simulator window: the same 12 buttons as the old CDLL Form1, answered by TerminalSimulator instead of
///     the mada terminal. Each log entry shows which COM port / IP the OLD sample would have used (nothing is
///     contacted), the exact request string the real DLL would receive, and every result field.
/// AR: نافذة المحاكي: نفس الأزرار الاثني عشر في Form1 القديمة، لكن TerminalSimulator هو من يرد بدلاً من جهاز مدى.
///     كل سجل يعرض منفذ COM أو عنوان IP الذي كان المثال القديم سيستخدمه (لا يتم الاتصال بأي شيء)، ونص الطلب
///     الذي كانت ستستلمه المكتبة الفعلية، وكل حقول النتيجة.
/// </summary>
public partial class SimulatorForm : Form
{
    private readonly TerminalSimulator _simulator = new();
    private readonly Button[] _actionButtons;

    /// <summary>EN: A scenario with its bilingual label for the combo box.  AR: سيناريو مع وصفه باللغتين لقائمة الاختيار.</summary>
    private sealed record ScenarioOption(SimulationScenario Value, string Text);

    public SimulatorForm()
    {
        InitializeComponent();

        cboScenario.DisplayMember = nameof(ScenarioOption.Text);
        cboScenario.ValueMember = nameof(ScenarioOption.Value);
        cboScenario.DataSource = new[]
        {
            new ScenarioOption(SimulationScenario.Approved, "Approved (000) / مقبولة"),
            new ScenarioOption(SimulationScenario.Declined, $"Declined ({TerminalSimulator.DeclinedResponseCode}) / مرفوضة"),
            new ScenarioOption(SimulationScenario.NoResponse, "No response, card charged / لا رد مع الخصم"),
        };
        dtpDate.Value = DateTime.Today;

        _actionButtons =
        [
            btnStatusUsb, btnStatusTcp, btnPurchaseUsb, btnPurchaseTcp, btnReconUsb, btnReconTcp,
            btnRefundUsb, btnRefundTcp, btnRepeatUsb, btnRepeatTcp, btnMerchant, btnTerminalId,
        ];
    }

    private SimulationScenario SelectedScenario => ((ScenarioOption)cboScenario.SelectedItem!).Value;

    // ------------------------------------------------------------------ buttons / الأزرار
    // EN: The second argument is the COM port / IP the OLD CDLL button had hard-coded. Shown for reference only.
    // AR: المعامل الثاني هو منفذ COM أو عنوان IP المكتوب مباشرة في زر CDLL القديم. يُعرض للمرجعية فقط.

    private async void btnStatusUsb_Click(object? sender, EventArgs e) =>
        await RunAsync("Check status", "COM5 (app.config)", () => _simulator.CheckStatusAsync(Transport.Com));

    private async void btnStatusTcp_Click(object? sender, EventArgs e) =>
        await RunAsync("TCP_Checkstatus", "192.168.208.157:6100", () => _simulator.CheckStatusAsync(Transport.Tcp));

    private async void btnPurchaseUsb_Click(object? sender, EventArgs e) =>
        await PurchaseAsync("Purchase_USB", "COM5", Transport.Com);

    private async void btnPurchaseTcp_Click(object? sender, EventArgs e) =>
        await PurchaseAsync("Purchase_TCP", "192.168.116.166:6100", Transport.Tcp);

    private async void btnReconUsb_Click(object? sender, EventArgs e) =>
        await RunAsync("Recon_USB", "COM9", () => _simulator.ReconcileAsync(Transport.Com));

    private async void btnReconTcp_Click(object? sender, EventArgs e) =>
        await RunAsync("Recon_TCP", "192.168.116.166:6100", () => _simulator.ReconcileAsync(Transport.Tcp));

    private async void btnRefundUsb_Click(object? sender, EventArgs e) =>
        await RefundAsync("Refund_USB", "COM5", Transport.Com);

    private async void btnRefundTcp_Click(object? sender, EventArgs e) =>
        await RefundAsync("Refund_TCP", "192.168.241.157:6100", Transport.Tcp);

    private async void btnRepeatUsb_Click(object? sender, EventArgs e) =>
        await RunAsync("Repeat_USB", "COM17", () => _simulator.RepeatLastAsync(Transport.Com));

    private async void btnRepeatTcp_Click(object? sender, EventArgs e) =>
        await RunAsync("Repeat_TCP", "192.168.8.174:6100", () => _simulator.RepeatLastAsync(Transport.Tcp));

    private async void btnMerchant_Click(object? sender, EventArgs e) =>
        await RunAsync("Get Merchant", "COM15", _simulator.GetMerchantDetailsAsync);

    private async void btnTerminalId_Click(object? sender, EventArgs e) =>
        await RunAsync("Terminal ID", "192.168.208.157:6100", _simulator.GetTerminalIdAsync);

    private void btnClear_Click(object? sender, EventArgs e) => txtOutput.Clear();

    // ------------------------------------------------------------------ purchase / refund

    private async Task PurchaseAsync(string button, string oldTarget, Transport transport)
    {
        decimal amount = numAmount.Value;
        await RunAsync(button, oldTarget, () => _simulator.PurchaseAsync(amount, transport), result =>
        {
            // EN: Prefill the refund fields with this purchase, so Refund can be tried straight away.
            // AR: تعبئة حقول الاسترجاع ببيانات هذه العملية، لتجربة الاسترجاع مباشرة.
            if (result.IsApproved)
            {
                txtRrn.Text = result.Rrn;
                dtpDate.Value = DateTime.Today;
            }
        });
    }

    private Task RefundAsync(string button, string oldTarget, Transport transport)
    {
        decimal amount = numAmount.Value;
        string rrn = txtRrn.Text.Trim();
        // EN: DateTimePicker.Value is always Gregorian, even if Windows shows a Hijri calendar.
        // AR: قيمة DateTimePicker.Value دائماً ميلادية، حتى لو كان Windows يعرض التقويم الهجري.
        DateOnly originalDate = DateOnly.FromDateTime(dtpDate.Value);
        return RunAsync(button, oldTarget, () => _simulator.RefundAsync(amount, rrn, originalDate, transport));
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// EN: Shared flow: apply the chosen scenario -> disable buttons -> run -> log -> onSuccess -> re-enable.
    ///     onSuccess runs BEFORE the buttons are re-enabled, so the form is fully updated by the time
    ///     the user can click again.
    /// AR: التسلسل المشترك: تطبيق السيناريو المختار -> تعطيل الأزرار -> التنفيذ -> التسجيل -> onSuccess -> إعادة التفعيل.
    ///     onSuccess تُنفَّذ قبل إعادة تفعيل الأزرار، لذلك تكون النافذة محدّثة بالكامل عندما يستطيع المستخدم الضغط مجدداً.
    /// </summary>
    private async Task RunAsync<T>(string button, string oldTarget, Func<Task<T>> operation, Action<T>? onSuccess = null)
        where T : class
    {
        _simulator.Scenario = SelectedScenario;
        SetBusy(true, $"{button}: waiting for simulated terminal... / بانتظار الجهاز الوهمي...");
        try
        {
            T result = await operation();
            Log(button, oldTarget, Describe(result));
            onSuccess?.Invoke(result);
            lblStatus.Text = $"{button}: done / تم";
        }
        catch (ArgumentException ex)
        {
            // EN: Invalid amount / RRN, same validation rules as Mada.Api (MadaRequests).
            // AR: مبلغ أو رقم مرجعي غير صحيح، بنفس قواعد التحقق في Mada.Api (MadaRequests).
            Log(button, oldTarget, "INPUT ERROR / خطأ في المدخلات: " + ex.Message);
            lblStatus.Text = $"{button}: invalid input / مدخلات غير صحيحة";
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// EN: Turns any result record into "Name: value" lines, plus a verdict for transactions.
    /// AR: يحوّل أي نتيجة إلى أسطر "الاسم: القيمة"، مع الحكم النهائي للعمليات المالية.
    /// </summary>
    private string Describe(object result)
    {
        var text = new StringBuilder();
        text.AppendLine("Request: " + _simulator.LastRequest);
        foreach (var property in result.GetType().GetProperties())
            text.AppendLine($"{property.Name}: {property.GetValue(result)}");

        int status = (int)result.GetType().GetProperty(nameof(StatusResult.StatusCode))!.GetValue(result)!;
        if (status == TerminalSimulator.NoResponseStatus)
        {
            text.AppendLine("=> NO RESPONSE from terminal. If this was a payment, press Repeat to see if the card was charged.");
            text.AppendLine("=> لا يوجد رد من الجهاز. إذا كانت عملية دفع، اضغط Repeat لمعرفة هل تم الخصم من البطاقة.");
        }
        else if (result is TransactionResult transaction && status == 0)
        {
            text.AppendLine(transaction.IsApproved ? "=> APPROVED / مقبولة" : "=> DECLINED / مرفوضة");
        }
        return text.ToString();
    }

    private void Log(string button, string oldTarget, string body)
    {
        txtOutput.AppendText(
            $"[{DateTime.Now:HH:mm:ss}] {button}   (old sample target: {oldTarget}, not contacted / لم يتم الاتصال){Environment.NewLine}" +
            body + Environment.NewLine);
        txtOutput.ScrollToCaret();
    }

    private void SetBusy(bool busy, string? status)
    {
        foreach (var button in _actionButtons)
            button.Enabled = !busy;
        UseWaitCursor = busy;
        if (status is not null)
            lblStatus.Text = status;
    }
}
