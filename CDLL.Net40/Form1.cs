// TCP/IP Integration Sample Code
// =====================================================================================
// EN: Windows Forms ECR (Electronic Cash Register) window for SPAN / mada payment terminals.
//     The window only collects input and shows results. All work is done by reusable services
//     (folder Payments\), created in Program.cs from app.config:
//
//       Button click
//         -> IPaymentTerminalService (ProviderTerminalService) picks the company from "Provider":
//              Geidea  -> MadaTerminalService     -> madaapi DLLs  (SimulatorMadaApi when TerminalMode=Simulator)
//              NamiPay -> SkybandTerminalService  -> ECR.dll + COM/TCP (SkybandSimulator when TerminalMode=Simulator)
//            terminal call on a BACKGROUND thread, same pipeline for both
//         -> PaymentStorage                                  saves EVERY result on its own BACKGROUND thread
//              -> SQLite | SQL Server | JSON  (Storage.Provider)
//              -> database down? -> JSON file in pending\  -> "Sync pending" inserts it later
//         -> HisBillingRules / IHisNotifier                  hook to tell HIS (patient bill / balance)
//
//     Terminal of this PC: read from the TerminalMappings table at startup ("Terminal mapping" button to change it).
//     Every outcome is recorded: Approved, Declined, Cancelled, Timeout, NoResponse, Failed, Error, Ok,
//     with every field the terminal returned. Card numbers are masked; the expiry date is never stored.
// AR: نافذة نظام نقاط البيع (ECR) لأجهزة الدفع SPAN / مدى.
//     النافذة تجمع المدخلات وتعرض النتائج فقط، وكل العمل تقوم به خدمات قابلة لإعادة الاستخدام (مجلد Payments\)
//     يتم إنشاؤها في Program.cs من app.config:
//       نقر الزر -> خدمة الجهاز (على خيط خلفي) -> التخزين (على خيط خلفي آخر) -> SQLite أو SQL Server أو JSON
//       -> إذا تعطلت قاعدة البيانات يُحفظ ملف JSON في pending\ و"مزامنة المعلّق" يُدرجه لاحقاً -> ربط HIS.
//     جهاز هذا الحاسوب يُقرأ من جدول TerminalMappings عند التشغيل (زر "ربط الجهاز" لتغييره).
//     كل نتيجة تُسجَّل مع كل الحقول التي أعادها الجهاز. أرقام البطاقات مُقنّعة وتاريخ الانتهاء لا يُحفظ.
//
// -------------------------------------------------------------------------------------
// EN: Button -> handler -> operation (button texts come from Form1.Designer.cs)
// AR: الزر -> الدالة -> العملية
//
//   Check status (USB/TCP)      btnStatusUsb/Tcp       "04!"                              فحص حالة الجهاز
//   Purchase (USB/TCP)          btnPurchaseUsb/Tcp     "<amount>;0;1!"                    عملية شراء
//   Refund (USB/TCP)            btnRefundUsb/Tcp       "<amount>;<RRN>;<DDMMYYYY>;1;1!"   عملية استرجاع
//   Repeat last (USB/TCP)       btnRepeatUsb/Tcp       "0B!"                              إعادة آخر عملية
//   Reconciliation (USB/TCP)    btnReconUsb/Tcp        "09;0!"                            التسوية / إقفال اليوم
//   Merchant info (USB)         btnMerchant            "0A!"                              بيانات التاجر (COM فقط)
//   Terminal ID (TCP)           btnTerminalId          "07!"                              رقم الجهاز (TCP فقط)
//   Cash payment                btnCashPayment         (no terminal call)                 دفع نقدي عند الاستقبال
//   Terminal mapping            btnMapping             TerminalMappingForm                ربط الجهاز بالحاسوب
//   Sync pending                btnSyncPending         pending JSON -> database           مزامنة المعلّق
//   ↻ (next to COM port)        btnRefreshPorts        reload the COM port list           تحديث قائمة منافذ COM
//   Clear log / Save defaults   btnClearLog / btnSaveDefaults                              مسح السجل / حفظ الافتراضي
// =====================================================================================

using System;
using System.Configuration;             // EN: ConfigurationManager, reads/writes app.config   AR: لقراءة وكتابة إعدادات app.config
using System.Globalization;             // EN: InvariantCulture for numbers and dates   AR: لتنسيق الأرقام والتواريخ
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Payments;
using CDLL.Payments.Integration;
using CDLL.Payments.Mapping;
using CDLL.Payments.Storage;
using CDLL.Payments.Terminal;


namespace CDLL
{
    /// <summary>
    /// EN: Main window for the terminal. One click handler per operation; each one hands the work to the
    ///     payment services and prints the full result.
    /// AR: النافذة الرئيسية للجهاز. دالة نقر لكل عملية، وكل دالة تسلّم العمل لخدمات الدفع وتطبع النتيجة كاملة.
    /// </summary>
    public partial class Form1 : Form
    {
        private PaymentServices _services;
        private bool _ownsServices;
        private string _title;   // EN: window title without the "waiting" counter   AR: عنوان النافذة بدون عدّاد الانتظار

        // EN: This PC's terminal from TerminalMappings (null = app.config values).   AR: جهاز هذا الحاسوب (null = قيم app.config).
        private TerminalMapping _mapping;

        /// <summary>EN: Used by the Visual Studio designer; creates the services from app.config on Load.  AR: للمصمم، وتُنشأ الخدمات من app.config عند التحميل.</summary>
        public Form1() : this(null)
        {
        }

        public Form1(PaymentServices services)
        {
            InitializeComponent();
            _services = services;
        }

        /// <summary>
        /// EN: Runs when the window opens: fills every input with its default from app.config, then (in the
        ///     background) loads this PC's terminal mapping and the number of pending JSON records.
        /// AR: تُنفَّذ عند فتح النافذة: تملأ الخانات من app.config، ثم تحمّل (في الخلفية) ربط جهاز هذا الحاسوب
        ///     وعدد السجلات المعلّقة.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            int configuredPort;
            if (!int.TryParse(Setting("PaymentMachinePort", "0"), NumberStyles.None, CultureInfo.InvariantCulture, out configuredPort))
                configuredPort = 0;
            SetNumber(numBaudRate, "BaudRate", 38400);
            SetNumber(numParity, "Parity", 0);
            SetNumber(numDataBits, "DataBits", 8);
            SetNumber(numStopBits, "StopBits", 0);
            txtIp.Text = Setting("TerminalIp", "192.168.116.166");
            txtTcpPort.Text = Setting("TerminalTcpPort", "6100");
            cmbProvider.SelectedItem = PaymentServices.DefaultProvider.ToString();

            SetNumber(numPurchaseAmount, "PurchaseAmount", 00.01m);
            SetNumber(numRefundAmount, "RefundAmount", 0.01m);
            txtOriginalRrn.Text = Setting("OriginalRrn", "434615455456");
            DateTime originalDate;
            dtpOriginalDate.Value = DateTime.TryParseExact(Setting("OriginalDate", ""), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out originalDate)
                ? originalDate
                : new DateTime(2024, 12, 11);

            if (_services == null)
            {
                _services = PaymentServices.FromConfig();
                _ownsServices = true;
            }
            if (_services.IsSimulator)
                Text += "   [SIMULATOR - no real payments / محاكاة - لا مدفوعات حقيقية]";
            _title = Text;

            LogLine("Storage / التخزين: " + _services.Storage.StoreName + " -> " + _services.Storage.DataFolder);
            await ComPortPicker.RefreshAsync(cmbComPort, configuredPort);   // EN: background scan   AR: بحث في الخلفية
            await LoadMappingAsync();
            await RefreshPendingAsync();
        }

        /// <summary>
        /// EN: [↻] Reloads the COM port list (after plugging the terminal in), keeping the chosen port.
        /// AR: [↻] يعيد تحميل قائمة منافذ COM (بعد توصيل الجهاز) مع الإبقاء على المنفذ المختار.
        /// </summary>
        private async void btnRefreshPorts_Click(object sender, EventArgs e)
        {
            btnRefreshPorts.Enabled = false;
            try
            {
                await ComPortPicker.RefreshAsync(cmbComPort, ComPortPicker.SelectedPort(cmbComPort));
            }
            finally
            {
                btnRefreshPorts.Enabled = true;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_ownsServices && _services != null)
                _services.Dispose();
        }

        // =============================================================================
        // EN: TERMINAL OPERATIONS / عمليات الجهاز
        // =============================================================================

        private async void btnStatusUsb_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Check status (USB) / فحص الحالة", TerminalTransport.Com, c => _services.Terminal.CheckStatusAsync(c));
        }

        private async void btnStatusTcp_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Check status (TCP) / فحص الحالة", TerminalTransport.Tcp, c => _services.Terminal.CheckStatusAsync(c));
        }

        private async void btnPurchaseUsb_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Purchase (USB) / شراء", TerminalTransport.Com, c => _services.Terminal.PurchaseAsync(c, numPurchaseAmount.Value));
        }

        private async void btnPurchaseTcp_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Purchase (TCP) / شراء", TerminalTransport.Tcp, c => _services.Terminal.PurchaseAsync(c, numPurchaseAmount.Value));
        }

        private async void btnRefundUsb_Click(object sender, EventArgs e)
        {
            if (!ValidateRefundInputs())
                return;
            await RunTerminalAsync("Refund (USB) / استرجاع", TerminalTransport.Com,
                c => _services.Terminal.RefundAsync(c, numRefundAmount.Value, txtOriginalRrn.Text.Trim(), dtpOriginalDate.Value));
        }

        private async void btnRefundTcp_Click(object sender, EventArgs e)
        {
            if (!ValidateRefundInputs())
                return;
            await RunTerminalAsync("Refund (TCP) / استرجاع", TerminalTransport.Tcp,
                c => _services.Terminal.RefundAsync(c, numRefundAmount.Value, txtOriginalRrn.Text.Trim(), dtpOriginalDate.Value));
        }

        /// <summary>
        /// EN: Re-fetches the LAST transaction. Use it after a Timeout, before charging the customer again.
        /// AR: يعيد جلب آخر عملية. استخدمه بعد انتهاء المهلة، قبل الخصم من العميل مرة أخرى.
        /// </summary>
        private async void btnRepeatUsb_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Repeat last (USB) / إعادة آخر عملية", TerminalTransport.Com, c => _services.Terminal.RepeatLastAsync(c));
        }

        private async void btnRepeatTcp_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Repeat last (TCP) / إعادة آخر عملية", TerminalTransport.Tcp, c => _services.Terminal.RepeatLastAsync(c));
        }

        private async void btnReconUsb_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Reconciliation (USB) / التسوية", TerminalTransport.Com, c => _services.Terminal.ReconcileAsync(c));
        }

        private async void btnReconTcp_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Reconciliation (TCP) / التسوية", TerminalTransport.Tcp, c => _services.Terminal.ReconcileAsync(c));
        }

        private async void btnMerchant_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Merchant info (USB) / بيانات التاجر", TerminalTransport.Com, c => _services.Terminal.GetMerchantInfoAsync(c));
        }

        private async void btnTerminalId_Click(object sender, EventArgs e)
        {
            await RunTerminalAsync("Terminal ID (TCP) / رقم الجهاز", TerminalTransport.Tcp, c => _services.Terminal.GetTerminalIdAsync(c));
        }

        /// <summary>
        /// EN: One terminal operation from start to end: read + validate the connection, run it on the terminal
        ///     service (background thread), print every returned field, save it (storage thread), tell HIS.
        /// AR: عملية جهاز واحدة من البداية للنهاية: قراءة الاتصال والتحقق منه، التنفيذ عبر خدمة الجهاز (خيط خلفي)،
        ///     طباعة كل الحقول المعادة، الحفظ (خيط التخزين)، ثم إبلاغ HIS.
        /// </summary>
        private async Task RunTerminalAsync(string label, TerminalTransport transport, Func<TerminalConnection, Task<TerminalResult>> operation)
        {
            TerminalConnection connection = ReadConnection(transport);
            string invalid = connection.Validate();
            if (invalid != null)
            {
                ShowInputError(transport == TerminalTransport.Com ? (Control)cmbComPort : txtIp, invalid);
                return;
            }

            SetBusy(true);
            try
            {
                LogStart(label, connection.Describe());
                TerminalResult result;
                using (ShowWaiting(label))
                    result = await operation(connection);
                LogResult(result);
                await SaveAndNotifyAsync(PaymentTransactionRecord.FromResult(result, CurrentContext()));
                ShowOutcome(label, result);
            }
            catch (Exception ex)
            {
                await ReportErrorAsync("Form1." + label, ex);
            }
            finally
            {
                SetBusy(false);
                await RefreshPendingAsync();
            }
        }

        /// <summary>
        /// EN: Shows "label ... 7 s" in the title bar while the terminal works, so the window never looks frozen.
        ///     Dispose() puts the normal title back.
        /// AR: يعرض "العملية ... 7 ث" في شريط العنوان أثناء عمل الجهاز حتى لا تبدو النافذة متجمّدة، و Dispose() يعيد العنوان.
        /// </summary>
        private IDisposable ShowWaiting(string label)
        {
            return new WaitingCounter(seconds => Text = _title + "   —   " + label + " … " + seconds + " s",
                                      () => Text = _title);
        }

        // =============================================================================
        // EN: CASH, MAPPING, PENDING / النقد، الربط، المعلّق
        // =============================================================================

        /// <summary>
        /// EN: [Cash payment] Records a cash payment taken at reception (outside the terminal), for the
        ///     "Purchase amount" and the patient / invoice on screen. No terminal call.
        /// AR: [دفع نقدي] يسجّل دفعاً نقدياً عند الاستقبال (خارج الجهاز) بقيمة "مبلغ الشراء" للمريض والفاتورة الظاهرين.
        /// </summary>
        private async void btnCashPayment_Click(object sender, EventArgs e)
        {
            long halalas;
            try
            {
                halalas = TerminalRequests.ToHalalas(numPurchaseAmount.Value);
            }
            catch (ArgumentException ex)
            {
                ShowInputError(numPurchaseAmount, ex.Message);
                return;
            }

            PaymentContext context = CurrentContext();
            string question = string.Format(CultureInfo.InvariantCulture,
                "Record a CASH payment of {0:0.00} SAR (paid at reception, not on the terminal)?\r\nMRN: {1}   Invoice: {2}\r\n\r\nتسجيل دفع نقدي بمبلغ {0:0.00} ريال؟",
                numPurchaseAmount.Value, context.PatientMrn ?? "-", context.InvoiceNo ?? "-");
            if (MessageBox.Show(this, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SetBusy(true);
            try
            {
                PaymentTransactionRecord record = PaymentTransactionRecord.Cash(halalas, context);
                LogStart("Cash payment / دفع نقدي", "reception / الاستقبال");
                LogText("Outcome / النتيجة", record.Outcome.ToString());
                LogText("Amount (halalas) / المبلغ بالهللة", record.AmountHalalas.ToString());
                await SaveAndNotifyAsync(record);
            }
            catch (Exception ex)
            {
                await ReportErrorAsync("Form1.CashPayment", ex);
            }
            finally
            {
                SetBusy(false);
                await RefreshPendingAsync();
            }
        }

        /// <summary>
        /// EN: [Terminal mapping] Opens the mapping screen (authorized users only) and switches to the new
        ///     mapping of this PC if it was changed.
        /// AR: [ربط الجهاز] يفتح شاشة الربط (للمستخدمين المصرّح لهم فقط) ويستخدم الربط الجديد لهذا الحاسوب إن تغيّر.
        /// </summary>
        private async void btnMapping_Click(object sender, EventArgs e)
        {
            // EN: Windows group lookups can take seconds on a domain PC: done on a background thread.
            // AR: البحث في مجموعات Windows قد يستغرق ثوانٍ على حاسوب في نطاق، لذلك يتم على خيط خلفي.
            btnMapping.Enabled = false;
            Tuple<bool, string> access;
            try
            {
                access = await TaskEx.Run(() =>
                {
                    string why;
                    bool ok = MappingAuthorization.IsCurrentUserAllowed(out why);
                    return Tuple.Create(ok, why);
                });
            }
            finally
            {
                btnMapping.Enabled = true;
            }

            string reason = access.Item2;
            if (!access.Item1)
            {
                MessageBox.Show(this, reason, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await _services.Storage.SaveErrorAsync(new ErrorLogRecord { Source = "Form1.Mapping", Message = "Access denied / رفض الوصول", Details = reason });
                return;
            }

            using (var form = new TerminalMappingForm(_services))
            {
                form.ShowDialog(this);
                if (form.ThisPcChanged)
                {
                    _mapping = form.SavedMappingForThisPc;   // EN: this PC's ACTIVE mapping (null = none left)   AR: الربط الفعّال لهذا الحاسوب
                    ApplyMapping(_mapping);
                }
            }
            await RefreshPendingAsync();
        }

        /// <summary>
        /// EN: [Sync pending] Inserts the records saved as JSON while the database was down.
        /// AR: [مزامنة المعلّق] يُدرج السجلات التي حُفظت كملفات JSON أثناء تعطل قاعدة البيانات.
        /// </summary>
        private async void btnSyncPending_Click(object sender, EventArgs e)
        {
            SetBusy(true);
            try
            {
                PendingImportResult result = await _services.Storage.ImportPendingAsync();
                LogLine("Sync pending / مزامنة المعلّق: " + result);
                MessageBox.Show(this, "Sync pending / مزامنة المعلّق:\r\n" + result, Text, MessageBoxButtons.OK,
                    result.Failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                LogLine("Sync pending FAILED, database still not available / فشلت المزامنة، قاعدة البيانات غير متاحة: " + ex.Message);
                MessageBox.Show(this, "Database still not available / قاعدة البيانات غير متاحة:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false);
                await RefreshPendingAsync();
            }
        }

        // =============================================================================
        // EN: CLEAR / SAVE DEFAULTS / مسح السجل وحفظ الافتراضي
        // =============================================================================

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            Trans_richTextBox.Clear();
        }

        /// <summary>
        /// EN: Writes the current form values into CDLL.exe.config so they become the defaults next time.
        ///     Note: rebuilding the project copies app.config over CDLL.exe.config again. To change the
        ///     defaults permanently, edit app.config too. A terminal mapping (TerminalMappings) overrides
        ///     the connection values at startup.
        /// AR: يكتب القيم الحالية في CDLL.exe.config لتصبح القيم الافتراضية في المرة القادمة.
        ///     ملاحظة: إعادة البناء تنسخ app.config فوق CDLL.exe.config. ربط الجهاز (TerminalMappings) يتقدّم
        ///     على قيم الاتصال عند التشغيل.
        /// </summary>
        private void btnSaveDefaults_Click(object sender, EventArgs e)
        {
            string invalid = ReadConnection(TerminalTransport.Tcp).Validate();
            if (invalid != null)
            {
                ShowInputError(txtIp, invalid);
                return;
            }
            if (!ValidateRefundInputs())
                return;

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                SaveSetting(config, "PaymentMachinePort", ComPortPicker.SelectedPort(cmbComPort));
                SaveSetting(config, "BaudRate", numBaudRate.Value);
                SaveSetting(config, "Parity", numParity.Value);
                SaveSetting(config, "DataBits", numDataBits.Value);
                SaveSetting(config, "StopBits", numStopBits.Value);
                SaveSetting(config, "TerminalIp", txtIp.Text.Trim());
                SaveSetting(config, "TerminalTcpPort", txtTcpPort.Text.Trim());
                SaveSetting(config, "TerminalProvider", SelectedProvider);
                SaveSetting(config, "PurchaseAmount", numPurchaseAmount.Value);
                SaveSetting(config, "RefundAmount", numRefundAmount.Value);
                SaveSetting(config, "OriginalRrn", txtOriginalRrn.Text.Trim());
                SaveSetting(config, "OriginalDate", dtpOriginalDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MessageBox.Show("Defaults saved to / تم حفظ القيم الافتراضية في:\n" + config.FilePath, Text);
            }
            catch (ConfigurationErrorsException ex)
            {
                // EN: e.g. the exe folder is read-only (Program Files without admin rights).
                // AR: مثلاً مجلد البرنامج للقراءة فقط (Program Files بدون صلاحيات المسؤول).
                MessageBox.Show("Could not save defaults / تعذر حفظ القيم الافتراضية:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =============================================================================
        // EN: SAVE / HIS / ERRORS  الحفظ و HIS والأخطاء
        // =============================================================================

        /// <summary>
        /// EN: Saves the record (storage thread; JSON fallback if the database is down) and tells HIS when
        ///     HisBillingRules says so.
        /// AR: يحفظ السجل (على خيط التخزين، مع JSON احتياطي عند تعطل القاعدة) ويبلغ HIS عندما تسمح HisBillingRules.
        /// </summary>
        private async Task SaveAndNotifyAsync(PaymentTransactionRecord record)
        {
            StorageWriteResult saved = await _services.Storage.SaveTransactionAsync(record);
            LogText("Saved / الحفظ", saved.Describe());
            if (saved.Location == StorageLocation.Lost)
                MessageBox.Show(this, saved.Describe(), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (!HisBillingRules.ShouldNotifyHis(record))
                return;
            try
            {
                await _services.His.NotifyPaymentAsync(record);
                LogText("HIS", "notified / تم الإبلاغ");
            }
            catch (Exception ex)
            {
                await ReportErrorAsync("HIS.NotifyPayment " + record.Id, ex);
            }
        }

        /// <summary>EN: Logs an exception on screen, saves it to ErrorLog and shows it.  AR: يعرض الاستثناء ويحفظه في ErrorLog.</summary>
        private async Task ReportErrorAsync(string source, Exception ex)
        {
            LogText("ERROR / خطأ", ex.GetType().Name + ": " + CardDataMasker.MaskText(ex.Message));
            StorageWriteResult saved = await _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException(source, ex));
            LogText("Error saved / حفظ الخطأ", saved.Describe());
            MessageBox.Show(this, CardDataMasker.MaskText(ex.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // =============================================================================
        // EN: MAPPING + PENDING STATUS / حالة الربط والمعلّق
        // =============================================================================

        private async Task LoadMappingAsync()
        {
            try
            {
                _mapping = await _services.Storage.GetMappingAsync(Environment.MachineName.ToUpperInvariant());
            }
            catch (Exception ex)
            {
                LogLine("Could not read the terminal mapping, using app.config values / تعذرت قراءة ربط الجهاز: " + ex.Message);
                await _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException("Form1.LoadMapping", ex));
            }
            ApplyMapping(_mapping);
        }

        /// <summary>EN: Puts the mapped terminal's settings in the connection boxes.  AR: يضع إعدادات الجهاز المرتبط في خانات الاتصال.</summary>
        private void ApplyMapping(TerminalMapping mapping)
        {
            // EN: A mapped PC always uses its ACTIVE (default) provider; change it on the mapping screen ("Set active / default").
            // AR: الحاسوب المرتبط يستخدم دائماً مزوّده الفعّال (الافتراضي)، ويُغيَّر من شاشة الربط ("تعيين كافتراضي").
            cmbProvider.Enabled = mapping == null;
            if (mapping == null)
            {
                lblTerminal.Text = "No terminal mapped to " + Environment.MachineName + " (app.config values) / لا يوجد جهاز مرتبط";
                return;
            }

            ComPortPicker.Select(cmbComPort, mapping.ComPort);
            cmbProvider.SelectedItem = mapping.Provider.ToString();
            numBaudRate.Value = Clamp(numBaudRate, mapping.BaudRate);
            numParity.Value = Clamp(numParity, mapping.Parity);
            numDataBits.Value = Clamp(numDataBits, mapping.DataBits);
            numStopBits.Value = Clamp(numStopBits, mapping.StopBits);
            if (!string.IsNullOrWhiteSpace(mapping.TcpIp)) txtIp.Text = mapping.TcpIp;
            if (!string.IsNullOrWhiteSpace(mapping.TcpPort)) txtTcpPort.Text = mapping.TcpPort;

            lblTerminal.Text = "Active terminal / الجهاز الفعّال: " + mapping.Provider + " | " + mapping.TerminalName + " | " + mapping.Department + " | " + mapping.ToConnection().Describe();
            LogLine("Terminal mapping loaded / تم تحميل ربط الجهاز: " + lblTerminal.Text);
        }

        private async Task RefreshPendingAsync()
        {
            int pending = await _services.Storage.GetPendingCountAsync();
            lblStorage.Text = "Storage / التخزين: " + _services.Storage.StoreName + "   |   Pending JSON / معلّق: " + pending;
            lblStorage.ForeColor = pending == 0 ? System.Drawing.SystemColors.ControlText : System.Drawing.Color.Firebrick;
        }

        // =============================================================================
        // EN: VALUES READ FROM THE FORM / القيم المقروءة من النافذة
        // =============================================================================

        private TerminalConnection ReadConnection(TerminalTransport transport)
        {
            return new TerminalConnection
            {
                Transport = transport,
                ComPort = ComPortPicker.SelectedPort(cmbComPort),
                BaudRate = (int)numBaudRate.Value,
                Parity = (int)numParity.Value,
                DataBits = (int)numDataBits.Value,
                StopBits = (int)numStopBits.Value,
                TcpIp = txtIp.Text.Trim(),
                TcpPort = txtTcpPort.Text.Trim(),
                Provider = SelectedProvider,
                CashRegisterNumber = _mapping != null && !string.IsNullOrWhiteSpace(_mapping.CashRegisterNumber)
                    ? _mapping.CashRegisterNumber
                    : PaymentServices.DefaultCashRegisterNumber
            };
        }

        /// <summary>EN: Geidea or NamiPay from the drop-down (decides which service runs the call).  AR: Geidea أو NamiPay من القائمة.</summary>
        private TerminalProvider SelectedProvider
        {
            get { return string.Equals(cmbProvider.SelectedItem as string, "NamiPay", StringComparison.Ordinal) ? TerminalProvider.NamiPay : TerminalProvider.Geidea; }
        }

        private PaymentContext CurrentContext()
        {
            return new PaymentContext
            {
                TerminalName = _mapping == null ? null : _mapping.TerminalName,
                Department = _mapping == null ? null : _mapping.Department,
                PatientMrn = NullIfEmpty(txtPatientMrn.Text),
                InvoiceNo = NullIfEmpty(txtInvoiceNo.Text)
            };
        }

        private bool ValidateRefundInputs()
        {
            if (TerminalRequests.IsValidRrn(txtOriginalRrn.Text.Trim()))
                return true;
            ShowInputError(txtOriginalRrn, "Original RRN must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط.");
            return false;
        }

        /// <summary>EN: Disables every action button while an operation runs (one at a time).  AR: يعطّل أزرار العمليات أثناء التنفيذ (عملية واحدة في كل مرة).</summary>
        private void SetBusy(bool busy)
        {
            foreach (Button button in new[] { btnStatusUsb, btnStatusTcp, btnPurchaseUsb, btnPurchaseTcp, btnRefundUsb, btnRefundTcp,
                                              btnRepeatUsb, btnRepeatTcp, btnReconUsb, btnReconTcp, btnMerchant, btnTerminalId,
                                              btnCashPayment, btnMapping, btnSyncPending, btnSaveDefaults, btnRefreshPorts })
                button.Enabled = !busy;
            UseWaitCursor = busy;
        }

        private void ShowInputError(Control control, string message)
        {
            MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            control.Focus();
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal Clamp(NumericUpDown box, int value)
        {
            return Math.Max(box.Minimum, Math.Min(box.Maximum, value));
        }

        // ---- app.config defaults / القيم الافتراضية من app.config

        private static string Setting(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        /// <summary>EN: Sets a number box from app.config, clamped to the box's allowed range.  AR: يضبط خانة رقمية من app.config ضمن المدى المسموح.</summary>
        private static void SetNumber(NumericUpDown box, string key, decimal fallback)
        {
            decimal value;
            if (!decimal.TryParse(Setting(key, ""), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                value = fallback;
            box.Value = Math.Max(box.Minimum, Math.Min(box.Maximum, value));
        }

        private static void SaveSetting(Configuration config, string key, object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, text);
            else
                config.AppSettings.Settings[key].Value = text;
        }

        // =============================================================================
        // EN: OUTPUT / عرض النتائج
        // =============================================================================

        private void LogStart(string operation, string target)
        {
            Trans_richTextBox.AppendText(
                "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " + operation + Environment.NewLine +
                "  -> " + target + Environment.NewLine);
        }

        /// <summary>
        /// EN: Prints the outcome and EVERY field the terminal returned (empty fields are skipped).
        /// AR: يطبع النتيجة وكل حقل أعاده الجهاز (الحقول الفارغة لا تُطبع).
        /// </summary>
        private void LogResult(TerminalResult r)
        {
            LogText("Request / الطلب", r.Request);
            LogText("Outcome / النتيجة", r.Outcome.ToString());
            LogText("Return code / رمز الإرجاع", r.ReturnCode.HasValue ? r.ReturnCode.Value.ToString(CultureInfo.InvariantCulture) : null);
            LogText("Response code / رمز الاستجابة", r.ResponseCode);
            LogText("Error / الخطأ", r.ErrorMessage);
            LogText("PAN NO / رقم البطاقة", r.MaskedPan);
            LogText("CARD ID / نوع البطاقة", r.SchemeId);
            LogText("Card scheme / شبكة البطاقة", r.CardScheme);
            LogText("TrxRrn / الرقم المرجعي", r.Rrn);
            LogText("Amount / المبلغ", r.TerminalAmount);
            LogText("Additional amount / مبلغ إضافي", r.AdditionalAmount);
            LogText("STAN / رقم التتبع", r.Stan);
            LogText("Date-time / التاريخ والوقت", r.TerminalDateTime);
            LogText("Auth code / رمز الموافقة", r.AuthCode);
            LogText("Terminal ID / رقم الجهاز", r.TerminalId);
            LogText("Merchant ID / رقم التاجر", r.MerchantId);
            LogText("ECR REF NO / مرجع نقاط البيع", r.EcrRefNo);
            LogText("Version / الإصدار", r.Version);
            LogText("Terminal total / مجاميع الجهاز", r.TerminalTotal);
            LogText("Host total / مجاميع البنك", r.HostTotal);
            LogText("Merchant name EN / اسم التاجر EN", r.MerchantNameEn);
            LogText("Merchant name AR / اسم التاجر AR", r.MerchantNameAr);
            LogText("Address EN / العنوان EN", r.MerchantAddressEn);
            LogText("Address AR / العنوان AR", r.MerchantAddressAr);
            LogText("Duration / المدة", r.DurationMs.ToString(CultureInfo.InvariantCulture) + " ms");
        }

        /// <summary>
        /// EN: Message box with the outcome (as the original sample showed the return code after each call).
        /// AR: رسالة بالنتيجة (كما كان المثال الأصلي يعرض رمز الإرجاع بعد كل استدعاء).
        /// </summary>
        private void ShowOutcome(string label, TerminalResult r)
        {
            string text = label + Environment.NewLine +
                          "Result / النتيجة: " + r.Outcome + Environment.NewLine +
                          "Return code / رمز الإرجاع: " + (r.ReturnCode.HasValue ? r.ReturnCode.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                          (r.ResponseCode == null ? "" : Environment.NewLine + "Response code / رمز الاستجابة: " + r.ResponseCode) +
                          (r.ErrorMessage == null ? "" : Environment.NewLine + r.ErrorMessage);
            MessageBox.Show(this, text, Text, MessageBoxButtons.OK, r.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void LogLine(string text)
        {
            Trans_richTextBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " + text + Environment.NewLine);
            Trans_richTextBox.ScrollToCaret();
        }

        private void LogText(string label, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            Trans_richTextBox.AppendText("  " + label + ": " + value + Environment.NewLine);
            Trans_richTextBox.ScrollToCaret();
        }
    }
}
