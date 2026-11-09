using System.Text.Encodings.Web;
using System.Text.Json;
using Mada.Contracts;
using Mada.WinForms.Services;

namespace Mada.WinForms;

/// <summary>
/// EN: Main window. Same operations as the old CDLL sample, but every button calls Mada.Api over HTTP
///     asynchronously, so the window stays responsive while the terminal waits for a card.
/// AR: النافذة الرئيسية. نفس عمليات مثال CDLL القديم، لكن كل زر يستدعي Mada.Api عبر HTTP بشكل غير متزامن،
///     فتبقى النافذة مستجيبة بينما ينتظر الجهاز البطاقة.
/// </summary>
public partial class MainForm : Form
{
    // EN: Pretty JSON for the output box. UnsafeRelaxedJsonEscaping keeps Arabic readable
    //     ("متجر") instead of "مت..."; safe here because the text is only displayed, never put into HTML.
    // AR: JSON منسّق لمربع النتائج. UnsafeRelaxedJsonEscaping يُبقي النص العربي مقروءاً ("متجر") بدلاً من
    //     "مت..."؛ وهذا آمن هنا لأن النص يُعرض فقط ولا يُضاف إلى HTML.
    private static readonly JsonSerializerOptions DisplayJson = new(MadaApiClient.JsonOptions)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Button[] _actionButtons;
    private MadaApiClient? _client;

    public MainForm()
    {
        InitializeComponent();

        cboTransport.DataSource = Enum.GetValues<Transport>();
        dtpOriginalDate.Value = DateTime.Today;
        _actionButtons = [btnStatus, btnTerminalId, btnMerchant, btnRepeat, btnRecon, btnPurchase, btnRefund];
    }

    private Transport SelectedTransport => (Transport)cboTransport.SelectedItem!;

    // ------------------------------------------------------------------ button handlers / دوال الأزرار

    private async void btnStatus_Click(object? sender, EventArgs e) =>
        await RunAsync("Check status / فحص الحالة", (c, t) => c.CheckStatusAsync(t), SelectedTransport);

    private async void btnTerminalId_Click(object? sender, EventArgs e) =>
        await RunAsync("Terminal ID / رقم الجهاز", (c, _) => c.GetTerminalIdAsync(), Transport.Tcp);

    private async void btnMerchant_Click(object? sender, EventArgs e) =>
        await RunAsync("Merchant / بيانات التاجر", (c, _) => c.GetMerchantDetailsAsync(), Transport.Com);

    private async void btnRepeat_Click(object? sender, EventArgs e) =>
        await RunAsync("Repeat last / إعادة آخر عملية", (c, t) => c.RepeatLastAsync(t), SelectedTransport);

    private async void btnRecon_Click(object? sender, EventArgs e) =>
        await RunAsync("Reconciliation / التسوية", (c, t) => c.ReconcileAsync(t), SelectedTransport);

    private async void btnPurchase_Click(object? sender, EventArgs e)
    {
        decimal amount = numAmount.Value;
        await RunAsync($"Purchase {amount:0.00} SAR / شراء", (c, t) => c.PurchaseAsync(amount, t), SelectedTransport);
    }

    private async void btnRefund_Click(object? sender, EventArgs e)
    {
        decimal amount = numRefundAmount.Value;
        string rrn = txtRrn.Text.Trim();
        // EN: DateTimePicker.Value is always a Gregorian DateTime, even if Windows displays a Hijri calendar.
        // AR: قيمة DateTimePicker.Value دائماً تاريخ ميلادي، حتى لو كان Windows يعرض التقويم الهجري.
        DateOnly originalDate = DateOnly.FromDateTime(dtpOriginalDate.Value);
        await RunAsync($"Refund {amount:0.00} SAR / استرجاع", (c, t) => c.RefundAsync(amount, rrn, originalDate, t), SelectedTransport);
    }

    private void btnClear_Click(object? sender, EventArgs e) => txtOutput.Clear();

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// EN: Shared flow for every button: disable buttons -> call API -> print result or error -> re-enable.
    ///     Buttons are disabled so the cashier cannot start a second payment while one is in progress
    ///     (the API would reject it with 409 anyway).
    /// AR: التسلسل المشترك لكل زر: تعطيل الأزرار -> استدعاء الخدمة -> طباعة النتيجة أو الخطأ -> إعادة التفعيل.
    ///     تُعطَّل الأزرار حتى لا يبدأ الكاشير عملية دفع ثانية أثناء تنفيذ الأولى
    ///     (والخدمة سترفضها بالرمز 409 على أي حال).
    /// </summary>
    private async Task RunAsync<T>(string operation, Func<MadaApiClient, Transport, Task<T>> call, Transport transport)
    {
        SetBusy(true, $"{operation} ({transport})...");
        try
        {
            T result = await call(GetClient(), transport);
            Append(operation, transport, JsonSerializer.Serialize(result, DisplayJson));
            lblStatus.Text = $"{operation}: done / تم";
        }
        catch (MadaApiException ex)
        {
            ShowError(operation, transport, $"API error {ex.StatusCode}: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            ShowError(operation, transport, $"Cannot reach the API. Is Mada.Api running? / تعذر الاتصال بالخدمة. هل Mada.Api يعمل؟\n{ex.Message}");
        }
        catch (TaskCanceledException)
        {
            ShowError(operation, transport, "Timed out waiting for the API. / انتهت مهلة انتظار الخدمة.\nUse \"Repeat last\" to check whether a payment went through. / استخدم \"إعادة آخر عملية\" للتأكد من نجاح الدفع.");
        }
        catch (UriFormatException ex)
        {
            ShowError(operation, transport, ex.Message);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// EN: Reuses one HttpClient per API URL; a new one is created only if the URL text changes.
    /// AR: يعيد استخدام HttpClient واحد لكل عنوان خدمة، ويُنشئ عميلاً جديداً فقط عند تغيير العنوان.
    /// </summary>
    private MadaApiClient GetClient()
    {
        string text = txtApiUrl.Text.Trim();
        if (!text.EndsWith('/'))
            text += "/";
        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
            throw new UriFormatException("Invalid API URL. / عنوان الخدمة غير صحيح.");

        if (_client is null || _client.BaseAddress != uri)
        {
            _client?.Dispose();
            _client = new MadaApiClient(uri);
        }
        return _client;
    }

    private void SetBusy(bool busy, string? status)
    {
        foreach (var button in _actionButtons)
            button.Enabled = !busy;
        UseWaitCursor = busy;
        if (status is not null)
            lblStatus.Text = status;
    }

    private void Append(string operation, Transport transport, string body)
    {
        txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {operation} ({transport}){Environment.NewLine}{body}{Environment.NewLine}{Environment.NewLine}");
        txtOutput.ScrollToCaret();
    }

    private void ShowError(string operation, Transport transport, string message)
    {
        Append(operation, transport, "ERROR / خطأ: " + message);
        lblStatus.Text = $"{operation}: failed / فشل";
    }
}
