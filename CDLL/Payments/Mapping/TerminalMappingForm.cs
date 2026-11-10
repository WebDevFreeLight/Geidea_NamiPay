// =====================================================================================
// EN: Terminal mapping screen: assigns a SPAN / mada terminal to a workstation, e.g. when a PC is replaced.
//     Opened from Form1 ("Terminal mapping"); only users allowed by MappingAuthorization can open it.
//       - Save:   stores the mapping of the workstation in the box. If the same terminal name is mapped to
//                 another workstation, it asks to REMOVE that old mapping (the terminal moved).
//       - Test:   runs Check status with the settings on screen (recorded like every other operation).
//       - Remove: deletes the selected workstation's mapping.
//     All storage and terminal work runs on background threads (PaymentStorage / IPaymentTerminalService).
// AR: شاشة ربط الأجهزة: تربط جهاز SPAN / مدى بحاسوب، مثلاً عند استبدال الحاسوب.
//     تُفتح من Form1 ("ربط الجهاز")، وفقط المستخدمون المسموح لهم في MappingAuthorization يمكنهم فتحها.
//       - حفظ: يحفظ ربط الحاسوب المكتوب. إذا كان نفس الجهاز مرتبطاً بحاسوب آخر، يسأل عن حذف الربط القديم.
//       - اختبار: ينفّذ فحص الحالة بالإعدادات الظاهرة (ويُسجَّل مثل كل العمليات).
//       - حذف: يحذف ربط الحاسوب المحدد.
//     كل أعمال التخزين والجهاز تتم على خيوط خلفية.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Payments.Storage;

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

        /// <summary>EN: The mapping of THIS PC after a save, so Form1 can switch to it.  AR: ربط هذا الحاسوب بعد الحفظ حتى تستخدمه Form1.</summary>
        public TerminalMapping SavedMappingForThisPc { get; private set; }

        private async void TerminalMappingForm_Load(object sender, EventArgs e)
        {
            if (_services == null)
                return;   // EN: designer   AR: المصمم

            txtWorkstation.Text = Environment.MachineName.ToUpperInvariant();
            cmbTransport.SelectedItem = TerminalTransport.Com.ToString();

            await ComPortPicker.RefreshAsync(cmbComPort, 0);   // EN: background scan, terminal port pre-selected   AR: بحث في الخلفية مع اختيار منفذ الجهاز
            IList<TerminalMapping> mappings = await ReloadListAsync();
            TerminalMapping mine = mappings == null ? null
                : mappings.FirstOrDefault(m => string.Equals(m.Workstation, txtWorkstation.Text, StringComparison.OrdinalIgnoreCase));
            if (mine != null)
                ShowMapping(mine);
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
                // EN: Reassignment: the same terminal on another workstation is the OLD PC.
                // AR: إعادة الربط: نفس الجهاز على حاسوب آخر يعني الحاسوب القديم.
                IList<TerminalMapping> all = await _services.Storage.GetMappingsAsync();
                TerminalMapping old = all.FirstOrDefault(m =>
                    !string.Equals(m.Workstation, mapping.Workstation, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.TerminalName, mapping.TerminalName, StringComparison.OrdinalIgnoreCase));

                StorageWriteResult saved = await _services.Storage.SaveMappingAsync(mapping);
                SetStatus(saved.Describe());

                if (old != null && saved.Location == StorageLocation.Database &&
                    MessageBox.Show(this,
                        "Terminal \"" + mapping.TerminalName + "\" is also mapped to " + old.Workstation + ".\r\nRemove that old mapping (the terminal moved to " + mapping.Workstation + ")?\r\n\r\n" +
                        "الجهاز مرتبط أيضاً بالحاسوب " + old.Workstation + ". هل تريد حذف الربط القديم؟",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await _services.Storage.DeleteMappingAsync(old.Workstation, mapping.UpdatedBy);
                    SetStatus(saved.Describe() + " | removed " + old.Workstation + " / تم حذف " + old.Workstation);
                }

                if (string.Equals(mapping.Workstation, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    SavedMappingForThisPc = mapping;

                await ReloadListAsync();
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

        private async void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstMappings.SelectedItems.Count != 1)
            {
                SetStatus("Select a mapping in the list first. / اختر ربطاً من القائمة أولاً.");
                return;
            }

            var mapping = (TerminalMapping)lstMappings.SelectedItems[0].Tag;
            if (MessageBox.Show(this, "Remove the mapping of " + mapping.Workstation + "? / حذف ربط " + mapping.Workstation + "؟",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            await RunBusyAsync(async () =>
            {
                await _services.Storage.DeleteMappingAsync(mapping.Workstation, PaymentContext.CurrentWindowsUser());
                SetStatus("Removed / تم الحذف: " + mapping.Workstation);
                await ReloadListAsync();
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
                    var item = new ListViewItem(new[] { m.Workstation, m.TerminalName, m.Department, m.ToConnection().Describe(), m.UpdatedBy }) { Tag = m };
                    lstMappings.Items.Add(item);
                }
                lstMappings.EndUpdate();
                return mappings;
            }
            catch (Exception ex)
            {
                SetStatus("Could not read mappings / تعذرت قراءة الربطات: " + ex.Message);
                await _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException("TerminalMappingForm.ReloadList", ex));
                return null;
            }
        }

        private void ShowMapping(TerminalMapping m)
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
                UpdatedBy = PaymentContext.CurrentWindowsUser()
            };

            string error = mapping.Workstation.Length == 0 ? "Workstation is required. / اسم الحاسوب مطلوب."
                         : mapping.TerminalName.Length == 0 ? "Terminal name is required. / اسم الجهاز مطلوب."
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
            btnSave.Enabled = btnTest.Enabled = btnRemove.Enabled = btnRefreshPorts.Enabled = false;
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
                btnSave.Enabled = btnTest.Enabled = btnRemove.Enabled = btnRefreshPorts.Enabled = true;
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
