// EN: Designer-compatible layout file. Open MainForm.cs in the Visual Studio Designer to edit it visually.
//     The Designer rewrites InitializeComponent(), so do not put comments inside it.
// AR: ملف التصميم متوافق مع مصمم Visual Studio. افتح MainForm.cs في المصمم لتعديله مرئياً.
//     المصمم يعيد كتابة InitializeComponent()، لذلك لا تضع تعليقات داخلها.
namespace Mada.WinForms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _client?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblApiUrl = new System.Windows.Forms.Label();
            this.txtApiUrl = new System.Windows.Forms.TextBox();
            this.lblTransport = new System.Windows.Forms.Label();
            this.cboTransport = new System.Windows.Forms.ComboBox();
            this.grpTerminal = new System.Windows.Forms.GroupBox();
            this.btnStatus = new System.Windows.Forms.Button();
            this.btnTerminalId = new System.Windows.Forms.Button();
            this.btnMerchant = new System.Windows.Forms.Button();
            this.btnRepeat = new System.Windows.Forms.Button();
            this.btnRecon = new System.Windows.Forms.Button();
            this.grpPurchase = new System.Windows.Forms.GroupBox();
            this.lblAmount = new System.Windows.Forms.Label();
            this.numAmount = new System.Windows.Forms.NumericUpDown();
            this.btnPurchase = new System.Windows.Forms.Button();
            this.grpRefund = new System.Windows.Forms.GroupBox();
            this.lblRefundAmount = new System.Windows.Forms.Label();
            this.numRefundAmount = new System.Windows.Forms.NumericUpDown();
            this.lblRrn = new System.Windows.Forms.Label();
            this.txtRrn = new System.Windows.Forms.TextBox();
            this.lblOriginalDate = new System.Windows.Forms.Label();
            this.dtpOriginalDate = new System.Windows.Forms.DateTimePicker();
            this.btnRefund = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.grpTerminal.SuspendLayout();
            this.grpPurchase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.grpRefund.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRefundAmount)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // lblApiUrl
            //
            this.lblApiUrl.AutoSize = true;
            this.lblApiUrl.Location = new System.Drawing.Point(12, 16);
            this.lblApiUrl.Name = "lblApiUrl";
            this.lblApiUrl.Size = new System.Drawing.Size(136, 15);
            this.lblApiUrl.TabIndex = 0;
            this.lblApiUrl.Text = "API URL / عنوان الخدمة:";
            //
            // txtApiUrl
            //
            this.txtApiUrl.Location = new System.Drawing.Point(170, 12);
            this.txtApiUrl.Name = "txtApiUrl";
            this.txtApiUrl.Size = new System.Drawing.Size(250, 23);
            this.txtApiUrl.TabIndex = 1;
            this.txtApiUrl.Text = "http://localhost:5080";
            //
            // lblTransport
            //
            this.lblTransport.AutoSize = true;
            this.lblTransport.Location = new System.Drawing.Point(445, 16);
            this.lblTransport.Name = "lblTransport";
            this.lblTransport.Size = new System.Drawing.Size(122, 15);
            this.lblTransport.TabIndex = 2;
            this.lblTransport.Text = "Connection / الاتصال:";
            //
            // cboTransport
            //
            this.cboTransport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTransport.FormattingEnabled = true;
            this.cboTransport.Location = new System.Drawing.Point(580, 12);
            this.cboTransport.Name = "cboTransport";
            this.cboTransport.Size = new System.Drawing.Size(110, 23);
            this.cboTransport.TabIndex = 3;
            //
            // grpTerminal
            //
            this.grpTerminal.Controls.Add(this.btnStatus);
            this.grpTerminal.Controls.Add(this.btnTerminalId);
            this.grpTerminal.Controls.Add(this.btnMerchant);
            this.grpTerminal.Controls.Add(this.btnRepeat);
            this.grpTerminal.Controls.Add(this.btnRecon);
            this.grpTerminal.Location = new System.Drawing.Point(12, 50);
            this.grpTerminal.Name = "grpTerminal";
            this.grpTerminal.Size = new System.Drawing.Size(420, 125);
            this.grpTerminal.TabIndex = 4;
            this.grpTerminal.TabStop = false;
            this.grpTerminal.Text = "Terminal / الجهاز";
            //
            // btnStatus
            //
            this.btnStatus.Location = new System.Drawing.Point(10, 24);
            this.btnStatus.Name = "btnStatus";
            this.btnStatus.Size = new System.Drawing.Size(130, 44);
            this.btnStatus.TabIndex = 0;
            this.btnStatus.Text = "Check status\r\nفحص الحالة";
            this.btnStatus.UseVisualStyleBackColor = true;
            this.btnStatus.Click += new System.EventHandler(this.btnStatus_Click);
            //
            // btnTerminalId
            //
            this.btnTerminalId.Location = new System.Drawing.Point(145, 24);
            this.btnTerminalId.Name = "btnTerminalId";
            this.btnTerminalId.Size = new System.Drawing.Size(130, 44);
            this.btnTerminalId.TabIndex = 1;
            this.btnTerminalId.Text = "Terminal ID (TCP)\r\nرقم الجهاز";
            this.btnTerminalId.UseVisualStyleBackColor = true;
            this.btnTerminalId.Click += new System.EventHandler(this.btnTerminalId_Click);
            //
            // btnMerchant
            //
            this.btnMerchant.Location = new System.Drawing.Point(280, 24);
            this.btnMerchant.Name = "btnMerchant";
            this.btnMerchant.Size = new System.Drawing.Size(130, 44);
            this.btnMerchant.TabIndex = 2;
            this.btnMerchant.Text = "Merchant (COM)\r\nبيانات التاجر";
            this.btnMerchant.UseVisualStyleBackColor = true;
            this.btnMerchant.Click += new System.EventHandler(this.btnMerchant_Click);
            //
            // btnRepeat
            //
            this.btnRepeat.Location = new System.Drawing.Point(10, 72);
            this.btnRepeat.Name = "btnRepeat";
            this.btnRepeat.Size = new System.Drawing.Size(130, 44);
            this.btnRepeat.TabIndex = 3;
            this.btnRepeat.Text = "Repeat last\r\nإعادة آخر عملية";
            this.btnRepeat.UseVisualStyleBackColor = true;
            this.btnRepeat.Click += new System.EventHandler(this.btnRepeat_Click);
            //
            // btnRecon
            //
            this.btnRecon.Location = new System.Drawing.Point(145, 72);
            this.btnRecon.Name = "btnRecon";
            this.btnRecon.Size = new System.Drawing.Size(130, 44);
            this.btnRecon.TabIndex = 4;
            this.btnRecon.Text = "Reconciliation\r\nالتسوية";
            this.btnRecon.UseVisualStyleBackColor = true;
            this.btnRecon.Click += new System.EventHandler(this.btnRecon_Click);
            //
            // grpPurchase
            //
            this.grpPurchase.Controls.Add(this.lblAmount);
            this.grpPurchase.Controls.Add(this.numAmount);
            this.grpPurchase.Controls.Add(this.btnPurchase);
            this.grpPurchase.Location = new System.Drawing.Point(12, 185);
            this.grpPurchase.Name = "grpPurchase";
            this.grpPurchase.Size = new System.Drawing.Size(420, 80);
            this.grpPurchase.TabIndex = 5;
            this.grpPurchase.TabStop = false;
            this.grpPurchase.Text = "Purchase / شراء";
            //
            // lblAmount
            //
            this.lblAmount.AutoSize = true;
            this.lblAmount.Location = new System.Drawing.Point(10, 34);
            this.lblAmount.Name = "lblAmount";
            this.lblAmount.Size = new System.Drawing.Size(118, 15);
            this.lblAmount.TabIndex = 0;
            this.lblAmount.Text = "Amount SAR / المبلغ:";
            //
            // numAmount
            //
            this.numAmount.DecimalPlaces = 2;
            this.numAmount.Location = new System.Drawing.Point(150, 30);
            this.numAmount.Maximum = new decimal(new int[] { 999999999, 0, 0, 0 });
            this.numAmount.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numAmount.Name = "numAmount";
            this.numAmount.Size = new System.Drawing.Size(120, 23);
            this.numAmount.TabIndex = 1;
            this.numAmount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // btnPurchase
            //
            this.btnPurchase.Location = new System.Drawing.Point(280, 20);
            this.btnPurchase.Name = "btnPurchase";
            this.btnPurchase.Size = new System.Drawing.Size(130, 44);
            this.btnPurchase.TabIndex = 2;
            this.btnPurchase.Text = "Purchase\r\nشراء";
            this.btnPurchase.UseVisualStyleBackColor = true;
            this.btnPurchase.Click += new System.EventHandler(this.btnPurchase_Click);
            //
            // grpRefund
            //
            this.grpRefund.Controls.Add(this.lblRefundAmount);
            this.grpRefund.Controls.Add(this.numRefundAmount);
            this.grpRefund.Controls.Add(this.lblRrn);
            this.grpRefund.Controls.Add(this.txtRrn);
            this.grpRefund.Controls.Add(this.lblOriginalDate);
            this.grpRefund.Controls.Add(this.dtpOriginalDate);
            this.grpRefund.Controls.Add(this.btnRefund);
            this.grpRefund.Location = new System.Drawing.Point(12, 275);
            this.grpRefund.Name = "grpRefund";
            this.grpRefund.Size = new System.Drawing.Size(420, 135);
            this.grpRefund.TabIndex = 6;
            this.grpRefund.TabStop = false;
            this.grpRefund.Text = "Refund / استرجاع";
            //
            // lblRefundAmount
            //
            this.lblRefundAmount.AutoSize = true;
            this.lblRefundAmount.Location = new System.Drawing.Point(10, 32);
            this.lblRefundAmount.Name = "lblRefundAmount";
            this.lblRefundAmount.Size = new System.Drawing.Size(118, 15);
            this.lblRefundAmount.TabIndex = 0;
            this.lblRefundAmount.Text = "Amount SAR / المبلغ:";
            //
            // numRefundAmount
            //
            this.numRefundAmount.DecimalPlaces = 2;
            this.numRefundAmount.Location = new System.Drawing.Point(150, 28);
            this.numRefundAmount.Maximum = new decimal(new int[] { 999999999, 0, 0, 0 });
            this.numRefundAmount.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numRefundAmount.Name = "numRefundAmount";
            this.numRefundAmount.Size = new System.Drawing.Size(120, 23);
            this.numRefundAmount.TabIndex = 1;
            this.numRefundAmount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // lblRrn
            //
            this.lblRrn.AutoSize = true;
            this.lblRrn.Location = new System.Drawing.Point(10, 64);
            this.lblRrn.Name = "lblRrn";
            this.lblRrn.Size = new System.Drawing.Size(126, 15);
            this.lblRrn.TabIndex = 2;
            this.lblRrn.Text = "Original RRN / المرجع:";
            //
            // txtRrn
            //
            this.txtRrn.Location = new System.Drawing.Point(150, 60);
            this.txtRrn.MaxLength = 12;
            this.txtRrn.Name = "txtRrn";
            this.txtRrn.Size = new System.Drawing.Size(120, 23);
            this.txtRrn.TabIndex = 3;
            //
            // lblOriginalDate
            //
            this.lblOriginalDate.AutoSize = true;
            this.lblOriginalDate.Location = new System.Drawing.Point(10, 96);
            this.lblOriginalDate.Name = "lblOriginalDate";
            this.lblOriginalDate.Size = new System.Drawing.Size(130, 15);
            this.lblOriginalDate.TabIndex = 4;
            this.lblOriginalDate.Text = "Original date / التاريخ:";
            //
            // dtpOriginalDate
            //
            this.dtpOriginalDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpOriginalDate.Location = new System.Drawing.Point(150, 92);
            this.dtpOriginalDate.Name = "dtpOriginalDate";
            this.dtpOriginalDate.Size = new System.Drawing.Size(120, 23);
            this.dtpOriginalDate.TabIndex = 5;
            //
            // btnRefund
            //
            this.btnRefund.Location = new System.Drawing.Point(280, 28);
            this.btnRefund.Name = "btnRefund";
            this.btnRefund.Size = new System.Drawing.Size(130, 44);
            this.btnRefund.TabIndex = 6;
            this.btnRefund.Text = "Refund\r\nاسترجاع";
            this.btnRefund.UseVisualStyleBackColor = true;
            this.btnRefund.Click += new System.EventHandler(this.btnRefund_Click);
            //
            // btnClear
            //
            this.btnClear.Location = new System.Drawing.Point(12, 420);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(130, 32);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "Clear / مسح";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            //
            // txtOutput
            //
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.BackColor = System.Drawing.SystemColors.Window;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtOutput.Location = new System.Drawing.Point(445, 50);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(463, 515);
            this.txtOutput.TabIndex = 8;
            this.txtOutput.Text = "";
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 578);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(920, 22);
            this.statusStrip.TabIndex = 9;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(80, 17);
            this.lblStatus.Text = "Ready / جاهز";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 600);
            this.Controls.Add(this.lblApiUrl);
            this.Controls.Add(this.txtApiUrl);
            this.Controls.Add(this.lblTransport);
            this.Controls.Add(this.cboTransport);
            this.Controls.Add(this.grpTerminal);
            this.Controls.Add(this.grpPurchase);
            this.Controls.Add(this.grpRefund);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(936, 639);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "mada Terminal (.NET 8) / جهاز مدى";
            this.grpTerminal.ResumeLayout(false);
            this.grpPurchase.ResumeLayout(false);
            this.grpPurchase.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.grpRefund.ResumeLayout(false);
            this.grpRefund.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRefundAmount)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.TextBox txtApiUrl;
        private System.Windows.Forms.Label lblTransport;
        private System.Windows.Forms.ComboBox cboTransport;
        private System.Windows.Forms.GroupBox grpTerminal;
        private System.Windows.Forms.Button btnStatus;
        private System.Windows.Forms.Button btnTerminalId;
        private System.Windows.Forms.Button btnMerchant;
        private System.Windows.Forms.Button btnRepeat;
        private System.Windows.Forms.Button btnRecon;
        private System.Windows.Forms.GroupBox grpPurchase;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.NumericUpDown numAmount;
        private System.Windows.Forms.Button btnPurchase;
        private System.Windows.Forms.GroupBox grpRefund;
        private System.Windows.Forms.Label lblRefundAmount;
        private System.Windows.Forms.NumericUpDown numRefundAmount;
        private System.Windows.Forms.Label lblRrn;
        private System.Windows.Forms.TextBox txtRrn;
        private System.Windows.Forms.Label lblOriginalDate;
        private System.Windows.Forms.DateTimePicker dtpOriginalDate;
        private System.Windows.Forms.Button btnRefund;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
