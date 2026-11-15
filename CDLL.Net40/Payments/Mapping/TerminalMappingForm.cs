// =====================================================================================
// EN: Terminal mapping screen: assigns a SPAN / mada terminal to a workstation, e.g. when a PC is replaced.
//     Opened from Form1 ("Terminal mapping"); only users allowed by MappingAuthorization can open it.
//     A PC can keep ONE Geidea and ONE NamiPay mapping; exactly one of them is ACTIVE (the default Form1 uses).
//       - Save:   stores the workstation + provider in the boxes. The PC's first mapping becomes active; a second
//                 one is kept inactive until "Set active". If the same terminal is mapped to another
//                 workstation, it asks to REMOVE that old mapping (the terminal moved).
//       - Set active / default: makes the selected mapping the PC's active one.
//       - Test:   runs Check status with the settings on screen (recorded like every other operation).
//       - Remove: deletes the selected mapping (the PC's other provider becomes active).
//       - Detect: finds out whether a Geidea or a NamiPay terminal is on the port (fills Provider).
//     Changing Provider shows that provider's saved settings for the workstation, if any.
//     All storage and terminal work runs on background threads (PaymentStorage / IPaymentTerminalService).
// AR: شاشة ربط الأجهزة: تربط جهاز SPAN / مدى بحاسوب، مثلاً عند استبدال الحاسوب.
//     تُفتح من Form1 ("ربط الجهاز")، وفقط المستخدمون المسموح لهم في MappingAuthorization يمكنهم فتحها.
//     الحاسوب يحتفظ بربط Geidea وربط NamiPay، وواحد فقط منهما فعّال (الافتراضي الذي تستخدمه Form1).
//       - حفظ: يحفظ الحاسوب + المزوّد. أول ربط للحاسوب يصبح فعّالاً، والثاني يبقى غير فعّال حتى "تعيين كافتراضي".
//         إذا كان نفس الجهاز مرتبطاً بحاسوب آخر، يسأل عن حذف الربط القديم.
//       - تعيين كافتراضي: يجعل الربط المحدد هو الفعّال للحاسوب.
//       - اختبار: ينفّذ فحص الحالة بالإعدادات الظاهرة (ويُسجَّل مثل كل العمليات).
//       - حذف: يحذف الربط المحدد (ويصبح المزوّد الآخر للحاسوب فعّالاً).
//       - كشف: يحدد هل الجهاز على المنفذ من Geidea أو NamiPay (ويملأ المزوّد).
//     كل أعمال التخزين والجهاز تتم على خيوط خلفية.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Payments.Storage;
using CDLL.Payments.Terminal;

namespace CDLL.Payments.Mapping
{
    public partial class TerminalMappingForm : Form
    {
        private readonly PaymentServices _services;

        /// <summary>EN: For the Visual Studio designer only.  AR: لمصمم Visual Studio فقط.</summary>
        public TerminalMappingForm()
        {
            InitializeComponent();
        }

        public TerminalMappingForm(PaymentServices services) : this()
        {
            _services = services;
        }

        /// <summary>EN: THIS PC's active mapping after a change (null = none left), so Form1 can switch to it.  AR: الربط الفعّال لهذا الحاسوب بعد التغيير.</summary>
        public TerminalMapping SavedMappingForThisPc { get; private set; }

        /// <summary>EN: True when this PC's mappings were changed on this screen.  AR: صحيح إذا تغيّرت ربطات هذا الحاسوب.</summary>
        public bool ThisPcChanged { get; private set; }

        private IList<TerminalMapping> _mappings = new List<TerminalMapping>();
        private bool _showing;   // EN: boxes are being filled by code, not by the user   AR: الخانات تُملأ بالكود وليس بالمستخدم

        private async void TerminalMappingForm_Load(object sender, EventArgs e)
        {
            if (_services == null)
                return;   // EN: designer   AR: المصمم

            txtWorkstation.Text = Environment.MachineName.ToUpperInvariant();
            cmbTransport.SelectedItem = TerminalTransport.Com.ToString();
            cmbProvider.SelectedItem = PaymentServices.DefaultProvider.ToString();
            txtCashRegister.Text = PaymentServices.DefaultCashRegisterNumber;

            await ComPortPicker.RefreshAsync(cmbComPort, 0);   // EN: background scan, terminal port pre-selected   AR: بحث في الخلفية مع اختيار منفذ الجهاز
            await ReloadListAsync();
            TerminalMapping active = _mappings.FirstOrDefault(m => m.IsActive && IsWorkstation(m, txtWorkstation.Text));
            if (active != null)
                ShowMapping(active);
        }

        /// <summary>
        /// EN: Provider changed by the user: show that provider's saved mapping of this workstation (if any),
        ///     so Geidea and NamiPay settings of one PC are edited separately.
        /// AR: غيّر المستخدم المزوّد: تُعرض إعدادات هذا المزوّد المحفوظة للحاسوب (إن وجدت).
        /// </summary>
        private void cmbProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_showing || _services == null)
                return;
            TerminalMapping saved = Find(txtWorkstation.Text, SelectedProvider);
            if (saved != null)
                ShowMapping(saved);
            else
            {
                if (SelectedProvider == TerminalProvider.NamiPay && txtCashRegister.Text.Trim().Length == 0)
                    txtCashRegister.Text = PaymentServices.DefaultCashRegisterNumber;
                SetStatus("New " + SelectedProvider + " mapping for " + txtWorkstation.Text.Trim().ToUpperInvariant() + " (not saved yet). / ربط جديد غير محفوظ بعد.");
            }
        }

        /// <summary>EN: [↻] Reloads the COM port list, keeping the chosen port.  AR: [↻] يعيد تحميل قائمة منافذ COM مع الإبقاء على المنفذ المختار.</summary>
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

        private void lstMappings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstMappings.SelectedItems.Count == 1)
                ShowMapping((TerminalMapping)lstMappings.SelectedItems[0].Tag);
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            TerminalMapping mapping = ReadMapping();
            if (mapping == null)
                return;

            await RunBusyAsync(async () =>
            {
                // EN: Reassignment: the same terminal (same provider + name) on another workstation is the OLD PC.
                // AR: إعادة الربط: نفس الجهاز (نفس المزوّد والاسم) على حاسوب آخر يعني الحاسوب القديم.
                IList<TerminalMapping> all = await _services.Storage.GetMappingsAsync();
                TerminalMapping old = all.FirstOrDefault(m =>
                    !IsWorkstation(m, mapping.Workstation) && m.Provider == mapping.Provider &&
                    string.Equals(m.TerminalName, mapping.TerminalName, StringComparison.OrdinalIgnoreCase));

                StorageWriteResult saved = await _services.Storage.SaveMappingAsync(mapping);
                string status = saved.Describe();
                SetStatus(status);

                if (old != null && saved.Location == StorageLocation.Database &&
                    MessageBox.Show(this,
                        "Terminal \"" + mapping.TerminalName + "\" is also mapped to " + old.Workstation + ".\r\nRemove that old mapping (the terminal moved to " + mapping.Workstation + ")?\r\n\r\n" +
                        "الجهاز مرتبط أيضاً بالحاسوب " + old.Workstation + ". هل تريد حذف الربط القديم؟",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await _services.Storage.DeleteMappingAsync(old.Workstation, old.Provider, mapping.UpdatedBy);
                    status += " | removed " + old.Workstation + " " + old.Provider + " / تم حذف " + old.Workstation;
                    if (IsWorkstation(old, Environment.MachineName))
                        await RefreshThisPcAsync(null);
                }

                if (IsWorkstation(mapping, Environment.MachineName))
                    await RefreshThisPcAsync(saved.Location == StorageLocation.Database ? null : mapping);

                await ReloadListAsync();
                TerminalMapping stored = Find(mapping.Workstation, mapping.Provider);
                if (stored != null && !stored.IsActive)
                    status += " | not active: press \"Set active / default\" to use it / غير فعّال: اضغط \"تعيين كافتراضي\" لاستخدامه";
                SetStatus(status);
            });
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {
            TerminalMapping mapping = ReadMapping();
            if (mapping == null)
                return;

            await RunBusyAsync(async () =>
            {
                string target = mapping.ToConnection().Describe();
                TerminalResult result;
                using (new WaitingCounter(seconds => SetStatus("Testing / جارٍ الاختبار: " + target + " … " + seconds + " s"), () => { }))
                    result = await _services.Terminal.CheckStatusAsync(mapping.ToConnection());
                var context = new PaymentContext { TerminalName = mapping.TerminalName, Department = mapping.Department };
                await _services.Storage.SaveTransactionAsync(PaymentTransactionRecord.FromResult(result, context));

                SetStatus((result.IsSuccess ? "CONNECTED / متصل" : "NOT CONNECTED / غير متصل") +
                          " | " + result.Outcome + " | return code " + result.ReturnCode +
                          (result.ErrorMessage == null ? "" : " | " + result.ErrorMessage));
            });
        }

        /// <summary>
        /// EN: [Detect] Which company's terminal is on this port: Geidea Check status first, then NamiPay
        ///     (NamiPay needs the cash register number). Every attempt is saved like any other operation.
        /// AR: [كشف] أي شركة على هذا المنفذ: فحص حالة Geidea أولاً ثم NamiPay (يحتاج رقم الصندوق). كل محاولة تُحفظ.
        /// </summary>
        private async void btnDetect_Click(object sender, EventArgs e)
        {
            TerminalConnection connection = ReadConnectionOnly();
            if (connection == null)
                return;
            if (_services.Detector == null)
            {
                SetStatus("Detection is not available. / الكشف غير متاح.");
                return;
            }

            await RunBusyAsync(async () =>
            {
                ProviderDetection detection;
                using (new WaitingCounter(seconds => SetStatus("Detecting Geidea / NamiPay on / جارٍ الكشف على " + connection.Describe() + " … " + seconds + " s"), () => { }))
                    detection = await _services.Detector.DetectAsync(connection);

                var context = new PaymentContext { TerminalName = txtTerminalName.Text.Trim(), Department = txtDepartment.Text.Trim() };
                foreach (TerminalResult attempt in detection.Attempts)
                    await _services.Storage.SaveTransactionAsync(PaymentTransactionRecord.FromResult(attempt, context));

                if (detection.Provider.HasValue)
                {
                    // EN: keep the detected port in the boxes (do not load the saved settings)   AR: إبقاء المنفذ المكتشف
                    _showing = true;
                    try { cmbProvider.SelectedItem = detection.Provider.Value.ToString(); }
                    finally { _showing = false; }
                }
                SetStatus(detection.Describe());
            });
        }

        private async void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstMappings.SelectedItems.Count != 1)
            {
                SetStatus("Select a mapping in the list first. / اختر ربطاً من القائمة أولاً.");
                return;
            }

            var mapping = (TerminalMapping)lstMappings.SelectedItems[0].Tag;
            string what = mapping.Workstation + " " + mapping.Provider + " (" + mapping.TerminalName + ")";
            if (MessageBox.Show(this, "Remove the mapping " + what + "? / حذف ربط " + what + "؟",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            await RunBusyAsync(async () =>
            {
                await _services.Storage.DeleteMappingAsync(mapping.Workstation, mapping.Provider, PaymentContext.CurrentWindowsUser());
                if (IsWorkstation(mapping, Environment.MachineName))
                    await RefreshThisPcAsync(null);
                await ReloadListAsync();

                TerminalMapping nowActive = _mappings.FirstOrDefault(m => m.IsActive && IsWorkstation(m, mapping.Workstation));
                SetStatus("Removed / تم الحذف: " + what +
                          (nowActive == null ? "" : " | active now / الفعّال الآن: " + nowActive.Provider + " " + nowActive.TerminalName));
            });
        }

        /// <summary>
        /// EN: [Set active / default] The selected mapping (or the workstation + provider in the boxes) becomes the
        ///     PC's active one; the PC's other mapping stays saved but inactive.
        /// AR: [تعيين كافتراضي] الربط المحدد (أو الحاسوب + المزوّد في الخانات) يصبح الفعّال، والآخر يبقى محفوظاً غير فعّال.
        /// </summary>
        private async void btnSetActive_Click(object sender, EventArgs e)
        {
            TerminalMapping target = lstMappings.SelectedItems.Count == 1
                ? (TerminalMapping)lstMappings.SelectedItems[0].Tag
                : Find(txtWorkstation.Text, SelectedProvider);
            if (target == null)
            {
                SetStatus("Save this mapping first, or select one in the list. / احفظ الربط أولاً أو اختر من القائمة.");
                return;
            }
            if (target.IsActive)
            {
                SetStatus(target.Workstation + " already uses " + target.Provider + ". / هذا هو الفعّال مسبقاً.");
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _services.Storage.SetActiveMappingAsync(target.Workstation, target.Provider, PaymentContext.CurrentWindowsUser());
                if (IsWorkstation(target, Environment.MachineName))
                    await RefreshThisPcAsync(null);
                await ReloadListAsync();
                SetStatus("Active / الفعّال: " + target.Workstation + " -> " + target.Provider + " " + target.TerminalName);
            });
        }

        // =============================================================================
        // EN: HELPERS / دوال مساعدة
        // =============================================================================

        private async Task<IList<TerminalMapping>> ReloadListAsync()
        {
            try
            {
                IList<TerminalMapping> mappings = await _services.Storage.GetMappingsAsync();
                lstMappings.BeginUpdate();
                lstMappings.Items.Clear();
                foreach (TerminalMapping m in mappings)
                {
                    var item = new ListViewItem(new[]
                    {
                        m.Workstation, m.Provider.ToString(), m.TerminalName, m.IsActive ? "✔ default" : "",
                        m.Department, m.ToConnection().Describe(), m.UpdatedBy
                    }) { Tag = m };
                    if (m.IsActive)
                        item.Font = new System.Drawing.Font(lstMappings.Font, System.Drawing.FontStyle.Bold);
                    lstMappings.Items.Add(item);
                }
                lstMappings.EndUpdate();
                _mappings = mappings;
                return mappings;
            }
            catch (Exception ex)
            {
                SetStatus("Could not read mappings / تعذرت قراءة الربطات: " + ex.Message);
                await _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException("TerminalMappingForm.ReloadList", ex));
                return null;
            }
        }

        /// <summary>
        /// EN: Re-reads this PC's active mapping for Form1. If the store is down, uses the fallback (a mapping just
        ///     parked as pending JSON) so Form1 still switches to it.
        /// AR: يعيد قراءة الربط الفعّال لهذا الحاسوب لـ Form1. إذا كان التخزين متعطلاً يستخدم البديل (ربط حُفظ معلّقاً للتو).
        /// </summary>
        private async Task RefreshThisPcAsync(TerminalMapping fallback)
        {
            ThisPcChanged = true;
            SavedMappingForThisPc = fallback ?? await _services.Storage.GetMappingAsync(Environment.MachineName.ToUpperInvariant());
        }

        /// <summary>EN: The saved mapping of a workstation + provider (from the last list load), or null.  AR: الربط المحفوظ لحاسوب + مزوّد أو null.</summary>
        private TerminalMapping Find(string workstation, TerminalProvider provider)
        {
            return _mappings.FirstOrDefault(m => m.Provider == provider && IsWorkstation(m, workstation));
        }

        private static bool IsWorkstation(TerminalMapping m, string workstation)
        {
            return string.Equals(m.Workstation, (workstation ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>EN: Fills the boxes without triggering the provider-change load.  AR: يملأ الخانات بدون تشغيل تحميل تغيير المزوّد.</summary>
        private void ShowMapping(TerminalMapping m)
        {
            _showing = true;
            try
            {
                FillBoxes(m);
            }
            finally
            {
                _showing = false;
            }
        }

        private void FillBoxes(TerminalMapping m)
        {
            txtWorkstation.Text = m.Workstation;
            txtTerminalName.Text = m.TerminalName;
            txtDepartment.Text = m.Department;
            cmbTransport.SelectedItem = m.Transport.ToString();
            ComPortPicker.Select(cmbComPort, m.ComPort);
            numBaudRate.Value = Clamp(numBaudRate, m.BaudRate);
            numParity.Value = Clamp(numParity, m.Parity);
            numDataBits.Value = Clamp(numDataBits, m.DataBits);
            numStopBits.Value = Clamp(numStopBits, m.StopBits);
            txtTcpIp.Text = m.TcpIp;
            txtTcpPort.Text = m.TcpPort;
            cmbProvider.SelectedItem = m.Provider.ToString();
            txtCashRegister.Text = m.CashRegisterNumber;
        }

        private TerminalProvider SelectedProvider
        {
            get { return string.Equals(cmbProvider.SelectedItem as string, "NamiPay", StringComparison.Ordinal) ? TerminalProvider.NamiPay : TerminalProvider.Geidea; }
        }

        /// <summary>EN: Connection settings only (for Detect), or null with a message.  AR: إعدادات الاتصال فقط (للكشف)، أو null مع رسالة.</summary>
        private TerminalConnection ReadConnectionOnly()
        {
            var connection = new TerminalConnection
            {
                Transport = string.Equals(cmbTransport.SelectedItem as string, "Tcp", StringComparison.Ordinal) ? TerminalTransport.Tcp : TerminalTransport.Com,
                ComPort = ComPortPicker.SelectedPort(cmbComPort),
                BaudRate = (int)numBaudRate.Value,
                Parity = (int)numParity.Value,
                DataBits = (int)numDataBits.Value,
                StopBits = (int)numStopBits.Value,
                TcpIp = txtTcpIp.Text.Trim(),
                TcpPort = txtTcpPort.Text.Trim(),
                Provider = SelectedProvider,
                CashRegisterNumber = txtCashRegister.Text.Trim()
            };
            string error = connection.Validate();
            if (error != null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return connection;
        }

        /// <summary>EN: Mapping from the boxes, or null (with a message) when something is missing / invalid.  AR: الربط من الخانات، أو null مع رسالة عند وجود خطأ.</summary>
        private TerminalMapping ReadMapping()
        {
            var mapping = new TerminalMapping
            {
                Workstation = txtWorkstation.Text.Trim().ToUpperInvariant(),
                TerminalName = txtTerminalName.Text.Trim(),
                Department = txtDepartment.Text.Trim(),
                Transport = string.Equals(cmbTransport.SelectedItem as string, "Tcp", StringComparison.Ordinal) ? TerminalTransport.Tcp : TerminalTransport.Com,
                ComPort = ComPortPicker.SelectedPort(cmbComPort),
                BaudRate = (int)numBaudRate.Value,
                Parity = (int)numParity.Value,
                DataBits = (int)numDataBits.Value,
                StopBits = (int)numStopBits.Value,
                TcpIp = txtTcpIp.Text.Trim(),
                TcpPort = txtTcpPort.Text.Trim(),
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = PaymentContext.CurrentWindowsUser(),
                Provider = SelectedProvider,
                CashRegisterNumber = txtCashRegister.Text.Trim().Length == 0 ? null : txtCashRegister.Text.Trim()
            };

            string error = mapping.Workstation.Length == 0 ? "Workstation is required. / اسم الحاسوب مطلوب."
                         : mapping.TerminalName.Length == 0 ? "Terminal name is required. / اسم الجهاز مطلوب."
                         : mapping.Provider == TerminalProvider.NamiPay && !TerminalConnection.IsValidCashRegister(mapping.CashRegisterNumber)
                             ? "NamiPay needs the 8-digit cash register number. / NamiPay يحتاج رقم الصندوق المكوّن من 8 أرقام."
                         : mapping.ToConnection().Validate();
            if (error != null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return mapping;
        }

        private async Task RunBusyAsync(Func<Task> work)
        {
            UseWaitCursor = true;
            btnSave.Enabled = btnTest.Enabled = btnRemove.Enabled = btnRefreshPorts.Enabled = btnDetect.Enabled = btnSetActive.Enabled = false;
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                SetStatus("Error / خطأ: " + ex.Message);
                await _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException("TerminalMappingForm", ex));
            }
            finally
            {
                btnSave.Enabled = btnTest.Enabled = btnRemove.Enabled = btnRefreshPorts.Enabled = btnDetect.Enabled = btnSetActive.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        private static decimal Clamp(NumericUpDown box, int value)
        {
            return Math.Max(box.Minimum, Math.Min(box.Maximum, value));
        }
    }
}
