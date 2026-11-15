// =====================================================================================
// EN: SimulatorForm: a NEW window in the CDLL project with the same 12 buttons as Form1, answered by
//     MadaApiSimulator instead of the mada terminal. No device and no madaapi DLL needed.
//     Form1 (the real terminal window) is unchanged.
//     Opened by Program.cs when app.config has <add key="UseSimulator" value="true"/>.
//
//     Each handler builds the same buffers and request strings as the matching Form1 button and calls a
//     function with the SAME NAME as the DLL function (MadaApiSimulator.api_...). Differences from Form1:
//       - amount / RRN / date come from the input panel instead of being hard-coded,
//       - the call runs on a worker thread (Task.Run), so the window does not freeze,
//       - the log shows the request string, the return code and all result fields.
// AR: SimulatorForm: نافذة جديدة في مشروع CDLL بنفس أزرار Form1 الاثني عشر، لكن MadaApiSimulator هو من يرد
//     بدلاً من جهاز مدى. لا حاجة لجهاز ولا لمكتبة madaapi. نافذة Form1 (الجهاز الفعلي) لم تتغير.
//     يفتحها Program.cs عندما يحتوي app.config على <add key="UseSimulator" value="true"/>.
//
//     كل دالة زر تبني نفس المخازن ونصوص الطلبات مثل زر Form1 المقابل، وتستدعي دالة بنفس اسم دالة المكتبة
//     (MadaApiSimulator.api_...). الفروق عن Form1:
//       - المبلغ والرقم المرجعي والتاريخ من لوحة الإدخال بدلاً من قيم مكتوبة مباشرة،
//       - الاستدعاء يعمل على خيط عامل (Task.Run)، فلا تتجمّد النافذة،
//       - السجل يعرض نص الطلب ورمز الإرجاع وكل حقول النتيجة.
// =====================================================================================

using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Simulator;

namespace CDLL
{
    public partial class SimulatorForm : Form
    {
        private readonly Button[] _actionButtons;

        public SimulatorForm()
        {
            InitializeComponent();

            // EN: Order must match SimulationScenario (Approved, Declined, NoResponse).
            // AR: الترتيب يجب أن يطابق SimulationScenario (Approved, Declined, NoResponse).
            cboScenario.Items.AddRange(new object[]
            {
                "Approved (000) / مقبولة",
                "Declined (" + MadaApiSimulator.DeclinedResponseCode + ") / مرفوضة",
                "No response, card charged / لا رد مع الخصم"
            });
            cboScenario.SelectedIndex = 0;
            dtpDate.Value = DateTime.Today;

            _actionButtons = new[]
            {
                btnStatusUsb, btnStatusTcp, btnPurchaseUsb, btnPurchaseTcp, btnReconUsb, btnReconTcp,
                btnRefundUsb, btnRefundTcp, btnRepeatUsb, btnRepeatTcp, btnMerchant, btnTerminalId
            };
        }

        // ============================================================== status / الحالة

        private async void btnStatusUsb_Click(object sender, EventArgs e)
        {
            byte[] inOutBuff = Ascii("04!");
            byte[] intval = Length(inOutBuff);
            // EN: COM5 = PaymentMachinePort in app.config, like Form1's "Check status".   AR: COM5 = PaymentMachinePort في app.config مثل زر Form1.
            await RunAsync("Check status", "COM5",
                () => MadaApiSimulator.api_CheckStatus(5, 38400, 0, 8, 0, inOutBuff, intval),
                rc => "");
        }

        private async void btnStatusTcp_Click(object sender, EventArgs e)
        {
            byte[] inOutBuff = Ascii("04!");
            byte[] intval = Length(inOutBuff);
            await RunAsync("TCP_Checkstatus", "192.168.208.157:6100",
                () => MadaApiSimulator.api_TCPIPCheckStatus(Ascii("192.168.208.157"), Ascii("6100"), inOutBuff, intval),
                rc => "");
        }

        // ============================================================== purchase / الشراء

        private async void btnPurchaseUsb_Click(object sender, EventArgs e)
        {
            await PurchaseAsync("Purchase_USB", 5, null);
        }

        private async void btnPurchaseTcp_Click(object sender, EventArgs e)
        {
            await PurchaseAsync("Purchase_TCP", 0, "192.168.116.166");
        }

        private async Task PurchaseAsync(string button, int comPort, string ip)
        {
            string request;
            if (!TryBuild(button, Target(comPort, ip), () => ToHalalas(numAmount.Value) + ";0;1!", out request))
                return;

            var b = new TransactionBuffers(Ascii(request));
            await RunAsync(button, Target(comPort, ip),
                () => ip == null
                    ? MadaApiSimulator.api_RequestCOMTrxn(comPort, 38400, 0, 8, 0, b.InOutBuff, b.Intval, 0, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, b.OutResp, b.OutRespLen)
                    : MadaApiSimulator.api_RequestTCPIPTrxn(Ascii(ip), Ascii("6100"), b.InOutBuff, b.Intval, 0, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, b.OutResp, b.OutRespLen),
                b.Describe,
                rc =>
                {
                    // EN: Prefill the refund fields so Refund can be tried straight away.
                    // AR: تعبئة حقول الاسترجاع لتجربة الاسترجاع مباشرة.
                    if (rc == IntPtr.Zero && Read(b.RspCode) == "000")
                    {
                        txtRrn.Text = Read(b.TrxRrn);
                        dtpDate.Value = DateTime.Today;
                    }
                });
        }

        // ============================================================== refund / الاسترجاع

        private async void btnRefundUsb_Click(object sender, EventArgs e)
        {
            await RefundAsync("Refund_USB", 5, null);
        }

        private async void btnRefundTcp_Click(object sender, EventArgs e)
        {
            await RefundAsync("Refund_TCP", 0, "192.168.241.157");
        }

        private async Task RefundAsync(string button, int comPort, string ip)
        {
            string request;
            if (!TryBuild(button, Target(comPort, ip), BuildRefundRequest, out request))
                return;

            var b = new TransactionBuffers(Ascii(request));
            await RunAsync(button, Target(comPort, ip),
                () => ip == null
                    ? MadaApiSimulator.api_RequestCOMTrxn(comPort, 38400, 0, 8, 0, b.InOutBuff, b.Intval, 2, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, b.OutResp, b.OutRespLen)
                    : MadaApiSimulator.api_RequestTCPIPTrxn(Ascii(ip), Ascii("6100"), b.InOutBuff, b.Intval, 2, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, b.OutResp, b.OutRespLen),
                b.Describe);
        }

        /// <summary>
        /// EN: "&lt;halalas&gt;;&lt;original RRN&gt;;&lt;DDMMYYYY&gt;;1;1!" (same format as Form1's Refund_USB).
        /// AR: "&lt;الهللات&gt;;&lt;الرقم المرجعي الأصلي&gt;;&lt;DDMMYYYY&gt;;1;1!" (نفس صيغة زر Refund_USB في Form1).
        /// </summary>
        private string BuildRefundRequest()
        {
            string rrn = txtRrn.Text.Trim();
            if (rrn.Length != 12 || !IsDigits(rrn))
                throw new ArgumentException("Original RRN must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط.");

            // EN: InvariantCulture: on Arabic (Saudi) Windows the default calendar is Hijri.
            // AR: InvariantCulture: على Windows العربي (السعودية) التقويم الافتراضي هجري.
            string date = dtpDate.Value.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
            return ToHalalas(numAmount.Value) + ";" + rrn + ";" + date + ";1;1!";
        }

        // ============================================================== repeat / إعادة آخر عملية

        private async void btnRepeatUsb_Click(object sender, EventArgs e)
        {
            var b = new TransactionBuffers(Ascii("0B!"));
            await RunAsync("Repeat_USB", "COM17",
                () => MadaApiSimulator.api_RequestCOMRepeat(17, 38400, 0, 8, 0, b.InOutBuff, b.Intval, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, b.OutResp, b.OutRespLen),
                b.Describe);
        }

        private async void btnRepeatTcp_Click(object sender, EventArgs e)
        {
            var b = new TransactionBuffers(Ascii("0B!"));
            byte[] cardscheme = new byte[10];
            await RunAsync("Repeat_TCP", "192.168.8.174:6100",
                () => MadaApiSimulator.api_RequestTCPIPRepeat(Ascii("192.168.8.174"), Ascii("6100"), b.InOutBuff, b.Intval, b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.Ecrrefno, b.Version, cardscheme, b.OutResp, b.OutRespLen),
                rc => b.Describe(rc) + "CARD SCHEME: " + Read(cardscheme) + Environment.NewLine);
        }

        // ============================================================== reconciliation / التسوية

        private async void btnReconUsb_Click(object sender, EventArgs e)
        {
            byte[] inOutBuff = Ascii("09;0!");
            byte[] intval = Length(inOutBuff);
            var r = new ReconBuffers();
            await RunAsync("Recon_USB", "COM9",
                () => MadaApiSimulator.api_RequestReconCOMWORcpt(9, 38400, 0, 8, 0, inOutBuff, intval, r.RspCode, r.TerminalTotal, r.HostTotal, r.StanNo, r.DataTime, r.TerminalId, r.MerchantId),
                r.Describe);
        }

        private async void btnReconTcp_Click(object sender, EventArgs e)
        {
            byte[] inReqbuff = Ascii("09;0!");
            var r = new ReconBuffers();
            await RunAsync("Recon_TCP", "192.168.116.166:6100",
                () =>
                {
                    // EN: The TCP recon function takes the length by reference.   AR: دالة التسوية عبر TCP تأخذ الطول بالمرجع.
                    int inReqlen = inReqbuff.Length;
                    return MadaApiSimulator.api_RequestReconTCPIPWORcpt(Ascii("192.168.116.166"), Ascii("6100"), inReqbuff, ref inReqlen, r.RspCode, r.TerminalTotal, r.HostTotal, r.StanNo, r.DataTime, r.TerminalId, r.MerchantId);
                },
                r.Describe);
        }

        // ============================================================== terminal info / معلومات الجهاز

        private async void btnMerchant_Click(object sender, EventArgs e)
        {
            byte[] inOutBuff = Ascii("0A!");
            byte[] intval = Length(inOutBuff);
            byte[] merchantNameEng = new byte[500];
            byte[] merchantAddEng1 = new byte[500];
            byte[] merchantAddopt1 = new byte[500];
            StringBuilder merchantNameArb = new StringBuilder(500);
            StringBuilder merchantAddArb1 = new StringBuilder(500);
            StringBuilder merchantAddopt2 = new StringBuilder(500);

            await RunAsync("Get Merchant", "COM15",
                () => MadaApiSimulator.api_GetMerchantDetails(15, 38400, 0, 8, 0, inOutBuff, intval, merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2),
                rc => "MerchantNameEng: " + Read(merchantNameEng) + Environment.NewLine +
                      "MerchantNameArb: " + merchantNameArb + Environment.NewLine +
                      "MerchantAddEng1: " + Read(merchantAddEng1) + Environment.NewLine +
                      "MerchantAddArb1: " + merchantAddArb1 + Environment.NewLine);
        }

        private async void btnTerminalId_Click(object sender, EventArgs e)
        {
            byte[] inOutBuff = Ascii("07!");
            byte[] intval = Length(inOutBuff);
            byte[] terminalId = new byte[17];
            await RunAsync("Terminal ID", "192.168.208.157:6100",
                () => MadaApiSimulator.api_TCPIPGetTerminalID(Ascii("192.168.208.157"), Ascii("6100"), inOutBuff, intval, terminalId),
                rc => "Terminal ID: " + Read(terminalId) + Environment.NewLine);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
        }

        // ============================================================== shared flow / التسلسل المشترك

        /// <summary>
        /// EN: Apply scenario -> disable buttons -> run the blocking call on a worker thread -> log -> onDone -> re-enable.
        ///     onDone runs before the buttons come back, so the form is fully updated when the user can click again.
        /// AR: تطبيق السيناريو -> تعطيل الأزرار -> تشغيل الاستدعاء الحاجب على خيط عامل -> التسجيل -> onDone -> إعادة التفعيل.
        ///     onDone تُنفَّذ قبل عودة الأزرار، لذلك تكون النافذة محدّثة بالكامل عندما يستطيع المستخدم الضغط مجدداً.
        /// </summary>
        private async Task RunAsync(string button, string target, Func<IntPtr> call, Func<IntPtr, string> describe, Action<IntPtr> onDone = null)
        {
            MadaApiSimulator.Scenario = (SimulationScenario)cboScenario.SelectedIndex;
            SetBusy(true, button + ": waiting for simulated terminal... / بانتظار الجهاز الوهمي...");
            try
            {
                IntPtr rc = await TaskEx.Run(call);
                int status = rc.ToInt32();

                var text = new StringBuilder();
                text.AppendLine("Request: " + MadaApiSimulator.LastRequest);
                text.AppendLine("Return code: " + status);
                text.Append(describe(rc));
                if (status == MadaApiSimulator.NoResponseStatus)
                {
                    text.AppendLine("=> NO RESPONSE from terminal. If this was a payment, press Repeat to see if the card was charged.");
                    text.AppendLine("=> لا يوجد رد من الجهاز. إذا كانت عملية دفع، اضغط Repeat لمعرفة هل تم الخصم من البطاقة.");
                }
                Log(button, target, text.ToString());

                if (onDone != null)
                    onDone(rc);
                lblStatus.Text = button + ": done / تم";
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        /// <summary>
        /// EN: Builds and validates a request string. Logs the error and returns false if the input is invalid.
        /// AR: يبني نص الطلب ويتحقق منه. يسجّل الخطأ ويُعيد false إذا كانت المدخلات غير صحيحة.
        /// </summary>
        private bool TryBuild(string button, string target, Func<string> build, out string request)
        {
            try
            {
                request = build();
                return true;
            }
            catch (ArgumentException ex)
            {
                Log(button, target, "INPUT ERROR / خطأ في المدخلات: " + ex.Message + Environment.NewLine);
                lblStatus.Text = button + ": invalid input / مدخلات غير صحيحة";
                request = null;
                return false;
            }
        }

        private void Log(string button, string target, string body)
        {
            txtOutput.AppendText("[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " + button +
                " -> " + target + "  (simulated, not contacted / محاكاة بدون اتصال)" + Environment.NewLine + body + Environment.NewLine);
            txtOutput.ScrollToCaret();
        }

        private void SetBusy(bool busy, string status)
        {
            foreach (Button button in _actionButtons)
                button.Enabled = !busy;
            UseWaitCursor = busy;
            if (status != null)
                lblStatus.Text = status;
        }

        // ============================================================== helpers / دوال مساعدة

        /// <summary>
        /// EN: SAR -> halalas (43.71 -> 4371). Rejects zero/negative, more than 2 decimals, more than 12 digits.
        /// AR: تحويل الريال إلى هللات (43.71 -> 4371). يرفض الصفر والسالب، وأكثر من خانتين عشريتين، وأكثر من 12 رقماً.
        /// </summary>
        private static string ToHalalas(decimal amountSar)
        {
            if (amountSar <= 0)
                throw new ArgumentException("Amount must be greater than zero. / المبلغ يجب أن يكون أكبر من صفر.");
            decimal halalas = amountSar * 100m;
            if (halalas != decimal.Truncate(halalas))
                throw new ArgumentException("Amount can have at most 2 decimal places. / المبلغ يقبل خانتين عشريتين كحد أقصى.");
            if (halalas > 999999999999m)
                throw new ArgumentException("Amount is too large. / المبلغ كبير جداً.");
            return ((long)halalas).ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsDigits(string value)
        {
            foreach (char c in value)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        private static string Target(int comPort, string ip)
        {
            return ip == null ? "COM" + comPort : ip + ":6100";
        }

        private static byte[] Ascii(string value)
        {
            return Encoding.ASCII.GetBytes(value);
        }

        private static byte[] Length(byte[] request)
        {
            return new[] { (byte)request.Length };
        }

        /// <summary>EN: Text of a DLL output buffer up to the first '\0'.  AR: نص مخزن المخرجات حتى أول '\0'.</summary>
        private static string Read(byte[] buffer)
        {
            int end = Array.IndexOf(buffer, (byte)0);
            return Encoding.ASCII.GetString(buffer, 0, end < 0 ? buffer.Length : end);
        }

        /// <summary>
        /// EN: Request + output buffers for purchase/refund/repeat, same sizes as Form1.
        /// AR: الطلب ومخازن المخرجات للشراء والاسترجاع والإعادة، بنفس أحجام Form1.
        /// </summary>
        private sealed class TransactionBuffers
        {
            public readonly byte[] InOutBuff;
            public readonly byte[] Intval;
            public readonly byte[] PanNo = new byte[23];
            public readonly byte[] PurAmount = new byte[13];
            public readonly byte[] StanNo = new byte[7];
            public readonly byte[] DataTime = new byte[13];
            public readonly byte[] ExpDate = new byte[5];
            public readonly byte[] TrxRrn = new byte[13];
            public readonly byte[] AuthCode = new byte[7];
            public readonly byte[] RspCode = new byte[4];
            public readonly byte[] TerminalId = new byte[17];
            public readonly byte[] SchemeId = new byte[3];
            public readonly byte[] MerchantId = new byte[16];
            public readonly byte[] AddtlAmount = new byte[13];
            public readonly byte[] Ecrrefno = new byte[17];
            public readonly byte[] Version = new byte[10];
            public readonly byte[] OutRespLen = new byte[1];
            public readonly StringBuilder OutResp = new StringBuilder(15000);

            public TransactionBuffers(byte[] request)
            {
                InOutBuff = request;
                Intval = Length(request);
            }

            /// <summary>EN: Same labels as Form1 ("PAN NO", "CARD ID", "TrxRrn") plus the other fields.  AR: نفس عناوين Form1 بالإضافة لبقية الحقول.</summary>
            public string Describe(IntPtr rc)
            {
                string rsp = Read(RspCode);
                var text = new StringBuilder();
                text.AppendLine("PAN NO: " + Read(PanNo));
                text.AppendLine("CARD ID: " + Read(SchemeId));
                text.AppendLine("TrxRrn : " + Read(TrxRrn));
                text.AppendLine("Amount : " + Read(PurAmount));
                text.AppendLine("STAN   : " + Read(StanNo));
                text.AppendLine("Date   : " + Read(DataTime));
                text.AppendLine("Auth   : " + Read(AuthCode));
                text.AppendLine("RspCode: " + rsp);
                text.AppendLine("Terminal ID: " + Read(TerminalId));
                text.AppendLine("Raw    : " + OutResp);
                if (rc == IntPtr.Zero && rsp.Length > 0)
                    text.AppendLine(rsp == "000" ? "=> APPROVED / مقبولة" : "=> DECLINED / مرفوضة");
                return text.ToString();
            }
        }

        /// <summary>EN: Reconciliation output buffers (sizes from Form1).  AR: مخازن مخرجات التسوية (الأحجام من Form1).</summary>
        private sealed class ReconBuffers
        {
            public readonly byte[] RspCode = new byte[30];
            public readonly byte[] TerminalTotal = new byte[500];
            public readonly byte[] HostTotal = new byte[500];
            public readonly byte[] StanNo = new byte[30];
            public readonly byte[] DataTime = new byte[30];
            public readonly byte[] TerminalId = new byte[30];
            public readonly byte[] MerchantId = new byte[30];

            public string Describe(IntPtr rc)
            {
                return "TerminalTotal: " + Read(TerminalTotal) + Environment.NewLine +
                       "HostTotal: " + Read(HostTotal) + Environment.NewLine +
                       "RspCode: " + Read(RspCode) + Environment.NewLine;
            }
        }
    }
}
