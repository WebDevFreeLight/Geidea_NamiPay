// EN: Designer-compatible layout file. Open SimulatorForm.cs in the Visual Studio Designer to edit it visually.
//     The Designer rewrites InitializeComponent(), so do not put comments inside it.
//     Button names/texts follow the old CDLL form: left column = USB/COM, right column = TCP/IP.
// AR: ملف التصميم متوافق مع مصمم Visual Studio. افتح SimulatorForm.cs في المصمم لتعديله مرئياً.
//     المصمم يعيد كتابة InitializeComponent()، لذلك لا تضع تعليقات داخلها.
//     أسماء ونصوص الأزرار تتبع نافذة CDLL القديمة: العمود الأيسر = USB/COM، والأيمن = TCP/IP.
namespace Mada.Simulator
{
    partial class SimulatorForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.grpInput = new System.Windows.Forms.GroupBox();
            this.lblAmount = new System.Windows.Forms.Label();
            this.numAmount = new System.Windows.Forms.NumericUpDown();
            this.lblRrn = new System.Windows.Forms.Label();
            this.txtRrn = new System.Windows.Forms.TextBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblScenario = new System.Windows.Forms.Label();
            this.cboScenario = new System.Windows.Forms.ComboBox();
            this.lblUsb = new System.Windows.Forms.Label();
            this.lblTcp = new System.Windows.Forms.Label();
            this.btnStatusUsb = new System.Windows.Forms.Button();
            this.btnStatusTcp = new System.Windows.Forms.Button();
            this.btnPurchaseUsb = new System.Windows.Forms.Button();
            this.btnPurchaseTcp = new System.Windows.Forms.Button();
            this.btnReconUsb = new System.Windows.Forms.Button();
            this.btnReconTcp = new System.Windows.Forms.Button();
            this.btnRefundUsb = new System.Windows.Forms.Button();
            this.btnRefundTcp = new System.Windows.Forms.Button();
            this.btnRepeatUsb = new System.Windows.Forms.Button();
            this.btnRepeatTcp = new System.Windows.Forms.Button();
            this.btnMerchant = new System.Windows.Forms.Button();
            this.btnTerminalId = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.grpInput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // grpInput
            //
            this.grpInput.Controls.Add(this.lblAmount);
            this.grpInput.Controls.Add(this.numAmount);
            this.grpInput.Controls.Add(this.lblRrn);
            this.grpInput.Controls.Add(this.txtRrn);
            this.grpInput.Controls.Add(this.lblDate);
            this.grpInput.Controls.Add(this.dtpDate);
            this.grpInput.Controls.Add(this.lblScenario);
            this.grpInput.Controls.Add(this.cboScenario);
            this.grpInput.Location = new System.Drawing.Point(12, 12);
            this.grpInput.Name = "grpInput";
            this.grpInput.Size = new System.Drawing.Size(430, 160);
            this.grpInput.TabIndex = 0;
            this.grpInput.TabStop = false;
            this.grpInput.Text = "Simulation input / مدخلات المحاكاة";
            //
            // lblAmount
            //
            this.lblAmount.AutoSize = true;
            this.lblAmount.Location = new System.Drawing.Point(12, 30);
            this.lblAmount.Name = "lblAmount";
            this.lblAmount.Size = new System.Drawing.Size(118, 15);
            this.lblAmount.TabIndex = 0;
            this.lblAmount.Text = "Amount SAR / المبلغ:";
            //
            // numAmount
            //
            this.numAmount.DecimalPlaces = 2;
            this.numAmount.Location = new System.Drawing.Point(200, 26);
            this.numAmount.Maximum = new decimal(new int[] { 999999999, 0, 0, 0 });
            this.numAmount.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numAmount.Name = "numAmount";
            this.numAmount.Size = new System.Drawing.Size(120, 23);
            this.numAmount.TabIndex = 1;
            this.numAmount.Value = new decimal(new int[] { 4371, 0, 0, 131072 });
            //
            // lblRrn
            //
            this.lblRrn.AutoSize = true;
            this.lblRrn.Location = new System.Drawing.Point(12, 62);
            this.lblRrn.Name = "lblRrn";
            this.lblRrn.Size = new System.Drawing.Size(180, 15);
            this.lblRrn.TabIndex = 2;
            this.lblRrn.Text = "Original RRN (refund) / المرجع:";
            //
            // txtRrn
            //
            this.txtRrn.Location = new System.Drawing.Point(200, 58);
            this.txtRrn.MaxLength = 12;
            this.txtRrn.Name = "txtRrn";
            this.txtRrn.Size = new System.Drawing.Size(120, 23);
            this.txtRrn.TabIndex = 3;
            //
            // lblDate
            //
            this.lblDate.AutoSize = true;
            this.lblDate.Location = new System.Drawing.Point(12, 94);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(182, 15);
            this.lblDate.TabIndex = 4;
            this.lblDate.Text = "Original date (refund) / التاريخ:";
            //
            // dtpDate
            //
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDate.Location = new System.Drawing.Point(200, 90);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(120, 23);
            this.dtpDate.TabIndex = 5;
            //
            // lblScenario
            //
            this.lblScenario.AutoSize = true;
            this.lblScenario.Location = new System.Drawing.Point(12, 126);
            this.lblScenario.Name = "lblScenario";
            this.lblScenario.Size = new System.Drawing.Size(155, 15);
            this.lblScenario.TabIndex = 6;
            this.lblScenario.Text = "Simulated result / النتيجة:";
            //
            // cboScenario
            //
            this.cboScenario.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScenario.FormattingEnabled = true;
            this.cboScenario.Location = new System.Drawing.Point(200, 122);
            this.cboScenario.Name = "cboScenario";
            this.cboScenario.Size = new System.Drawing.Size(218, 23);
            this.cboScenario.TabIndex = 7;
            //
            // lblUsb
            //
            this.lblUsb.AutoSize = true;
            this.lblUsb.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsb.Location = new System.Drawing.Point(12, 184);
            this.lblUsb.Name = "lblUsb";
            this.lblUsb.Size = new System.Drawing.Size(65, 15);
            this.lblUsb.TabIndex = 1;
            this.lblUsb.Text = "USB / COM";
            //
            // lblTcp
            //
            this.lblTcp.AutoSize = true;
            this.lblTcp.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTcp.Location = new System.Drawing.Point(237, 184);
            this.lblTcp.Name = "lblTcp";
            this.lblTcp.Size = new System.Drawing.Size(44, 15);
            this.lblTcp.TabIndex = 2;
            this.lblTcp.Text = "TCP/IP";
            //
            // btnStatusUsb
            //
            this.btnStatusUsb.Location = new System.Drawing.Point(12, 205);
            this.btnStatusUsb.Name = "btnStatusUsb";
            this.btnStatusUsb.Size = new System.Drawing.Size(205, 42);
            this.btnStatusUsb.TabIndex = 3;
            this.btnStatusUsb.Text = "Check status\r\nفحص الحالة";
            this.btnStatusUsb.UseVisualStyleBackColor = true;
            this.btnStatusUsb.Click += new System.EventHandler(this.btnStatusUsb_Click);
            //
            // btnStatusTcp
            //
            this.btnStatusTcp.Location = new System.Drawing.Point(237, 205);
            this.btnStatusTcp.Name = "btnStatusTcp";
            this.btnStatusTcp.Size = new System.Drawing.Size(205, 42);
            this.btnStatusTcp.TabIndex = 4;
            this.btnStatusTcp.Text = "TCP_Checkstatus\r\nفحص الحالة";
            this.btnStatusTcp.UseVisualStyleBackColor = true;
            this.btnStatusTcp.Click += new System.EventHandler(this.btnStatusTcp_Click);
            //
            // btnPurchaseUsb
            //
            this.btnPurchaseUsb.Location = new System.Drawing.Point(12, 253);
            this.btnPurchaseUsb.Name = "btnPurchaseUsb";
            this.btnPurchaseUsb.Size = new System.Drawing.Size(205, 42);
            this.btnPurchaseUsb.TabIndex = 5;
            this.btnPurchaseUsb.Text = "Purchase_USB\r\nشراء";
            this.btnPurchaseUsb.UseVisualStyleBackColor = true;
            this.btnPurchaseUsb.Click += new System.EventHandler(this.btnPurchaseUsb_Click);
            //
            // btnPurchaseTcp
            //
            this.btnPurchaseTcp.Location = new System.Drawing.Point(237, 253);
            this.btnPurchaseTcp.Name = "btnPurchaseTcp";
            this.btnPurchaseTcp.Size = new System.Drawing.Size(205, 42);
            this.btnPurchaseTcp.TabIndex = 6;
            this.btnPurchaseTcp.Text = "Purchase_TCP\r\nشراء";
            this.btnPurchaseTcp.UseVisualStyleBackColor = true;
            this.btnPurchaseTcp.Click += new System.EventHandler(this.btnPurchaseTcp_Click);
            //
            // btnReconUsb
            //
            this.btnReconUsb.Location = new System.Drawing.Point(12, 301);
            this.btnReconUsb.Name = "btnReconUsb";
            this.btnReconUsb.Size = new System.Drawing.Size(205, 42);
            this.btnReconUsb.TabIndex = 7;
            this.btnReconUsb.Text = "Recon_USB\r\nالتسوية";
            this.btnReconUsb.UseVisualStyleBackColor = true;
            this.btnReconUsb.Click += new System.EventHandler(this.btnReconUsb_Click);
            //
            // btnReconTcp
            //
            this.btnReconTcp.Location = new System.Drawing.Point(237, 301);
            this.btnReconTcp.Name = "btnReconTcp";
            this.btnReconTcp.Size = new System.Drawing.Size(205, 42);
            this.btnReconTcp.TabIndex = 8;
            this.btnReconTcp.Text = "Recon_TCP\r\nالتسوية";
            this.btnReconTcp.UseVisualStyleBackColor = true;
            this.btnReconTcp.Click += new System.EventHandler(this.btnReconTcp_Click);
            //
            // btnRefundUsb
            //
            this.btnRefundUsb.Location = new System.Drawing.Point(12, 349);
            this.btnRefundUsb.Name = "btnRefundUsb";
            this.btnRefundUsb.Size = new System.Drawing.Size(205, 42);
            this.btnRefundUsb.TabIndex = 9;
            this.btnRefundUsb.Text = "Refund_USB\r\nاسترجاع";
            this.btnRefundUsb.UseVisualStyleBackColor = true;
            this.btnRefundUsb.Click += new System.EventHandler(this.btnRefundUsb_Click);
            //
            // btnRefundTcp
            //
            this.btnRefundTcp.Location = new System.Drawing.Point(237, 349);
            this.btnRefundTcp.Name = "btnRefundTcp";
            this.btnRefundTcp.Size = new System.Drawing.Size(205, 42);
            this.btnRefundTcp.TabIndex = 10;
            this.btnRefundTcp.Text = "Refund_TCP\r\nاسترجاع";
            this.btnRefundTcp.UseVisualStyleBackColor = true;
            this.btnRefundTcp.Click += new System.EventHandler(this.btnRefundTcp_Click);
            //
            // btnRepeatUsb
            //
            this.btnRepeatUsb.Location = new System.Drawing.Point(12, 397);
            this.btnRepeatUsb.Name = "btnRepeatUsb";
            this.btnRepeatUsb.Size = new System.Drawing.Size(205, 42);
            this.btnRepeatUsb.TabIndex = 11;
            this.btnRepeatUsb.Text = "Repeat_USB\r\nإعادة آخر عملية";
            this.btnRepeatUsb.UseVisualStyleBackColor = true;
            this.btnRepeatUsb.Click += new System.EventHandler(this.btnRepeatUsb_Click);
            //
            // btnRepeatTcp
            //
            this.btnRepeatTcp.Location = new System.Drawing.Point(237, 397);
            this.btnRepeatTcp.Name = "btnRepeatTcp";
            this.btnRepeatTcp.Size = new System.Drawing.Size(205, 42);
            this.btnRepeatTcp.TabIndex = 12;
            this.btnRepeatTcp.Text = "Repeat_TCP\r\nإعادة آخر عملية";
            this.btnRepeatTcp.UseVisualStyleBackColor = true;
            this.btnRepeatTcp.Click += new System.EventHandler(this.btnRepeatTcp_Click);
            //
            // btnMerchant
            //
            this.btnMerchant.Location = new System.Drawing.Point(12, 445);
            this.btnMerchant.Name = "btnMerchant";
            this.btnMerchant.Size = new System.Drawing.Size(205, 42);
            this.btnMerchant.TabIndex = 13;
            this.btnMerchant.Text = "Get Merchant\r\nبيانات التاجر";
            this.btnMerchant.UseVisualStyleBackColor = true;
            this.btnMerchant.Click += new System.EventHandler(this.btnMerchant_Click);
            //
            // btnTerminalId
            //
            this.btnTerminalId.Location = new System.Drawing.Point(237, 445);
            this.btnTerminalId.Name = "btnTerminalId";
            this.btnTerminalId.Size = new System.Drawing.Size(205, 42);
            this.btnTerminalId.TabIndex = 14;
            this.btnTerminalId.Text = "Terminal ID\r\nرقم الجهاز";
            this.btnTerminalId.UseVisualStyleBackColor = true;
            this.btnTerminalId.Click += new System.EventHandler(this.btnTerminalId_Click);
            //
            // btnClear
            //
            this.btnClear.Location = new System.Drawing.Point(12, 500);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(120, 30);
            this.btnClear.TabIndex = 15;
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
            this.txtOutput.Location = new System.Drawing.Point(455, 12);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(493, 570);
            this.txtOutput.TabIndex = 16;
            this.txtOutput.Text = "";
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 588);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(960, 22);
            this.statusStrip.TabIndex = 17;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(300, 17);
            this.lblStatus.Text = "Ready (no device needed) / جاهز (بدون جهاز)";
            //
            // SimulatorForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 610);
            this.Controls.Add(this.grpInput);
            this.Controls.Add(this.lblUsb);
            this.Controls.Add(this.lblTcp);
            this.Controls.Add(this.btnStatusUsb);
            this.Controls.Add(this.btnStatusTcp);
            this.Controls.Add(this.btnPurchaseUsb);
            this.Controls.Add(this.btnPurchaseTcp);
            this.Controls.Add(this.btnReconUsb);
            this.Controls.Add(this.btnReconTcp);
            this.Controls.Add(this.btnRefundUsb);
            this.Controls.Add(this.btnRefundTcp);
            this.Controls.Add(this.btnRepeatUsb);
            this.Controls.Add(this.btnRepeatTcp);
            this.Controls.Add(this.btnMerchant);
            this.Controls.Add(this.btnTerminalId);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(976, 649);
            this.Name = "SimulatorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "mada Terminal SIMULATOR - no device, no real payments / محاكي جهاز مدى - بدون جهاز";
            this.grpInput.ResumeLayout(false);
            this.grpInput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpInput;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.NumericUpDown numAmount;
        private System.Windows.Forms.Label lblRrn;
        private System.Windows.Forms.TextBox txtRrn;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.Label lblScenario;
        private System.Windows.Forms.ComboBox cboScenario;
        private System.Windows.Forms.Label lblUsb;
        private System.Windows.Forms.Label lblTcp;
        private System.Windows.Forms.Button btnStatusUsb;
        private System.Windows.Forms.Button btnStatusTcp;
        private System.Windows.Forms.Button btnPurchaseUsb;
        private System.Windows.Forms.Button btnPurchaseTcp;
        private System.Windows.Forms.Button btnReconUsb;
        private System.Windows.Forms.Button btnReconTcp;
        private System.Windows.Forms.Button btnRefundUsb;
        private System.Windows.Forms.Button btnRefundTcp;
        private System.Windows.Forms.Button btnRepeatUsb;
        private System.Windows.Forms.Button btnRepeatTcp;
        private System.Windows.Forms.Button btnMerchant;
        private System.Windows.Forms.Button btnTerminalId;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
