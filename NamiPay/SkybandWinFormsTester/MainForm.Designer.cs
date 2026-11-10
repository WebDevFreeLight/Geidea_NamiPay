using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace SkybandWinFormsTester
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private ComboBox cmbPort;
        private ComboBox cmbBaud;
        private ComboBox cmbParity;
        private ComboBox cmbDataBits;
        private ComboBox cmbStopBits;
        private Button btnRefresh;
        private Button btnConnect;
        private Label lblConnection;
        private TextBox txtAmount;
        private TextBox txtCashRegister;
        private TextBox txtTerminalId;
        private TextBox txtEcrRef;
        private Button btnPurchase;
        private RichTextBox txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmbPort = new ComboBox();
            this.cmbBaud = new ComboBox();
            this.cmbParity = new ComboBox();
            this.cmbDataBits = new ComboBox();
            this.cmbStopBits = new ComboBox();
            this.btnRefresh = new Button();
            this.btnConnect = new Button();
            this.lblConnection = new Label();
            this.txtAmount = new TextBox();
            this.txtCashRegister = new TextBox();
            this.txtTerminalId = new TextBox();
            this.txtEcrRef = new TextBox();
            this.btnPurchase = new Button();
            this.txtLog = new RichTextBox();
            Label heading = new Label();
            GroupBox connectionGroup = new GroupBox();
            GroupBox purchaseGroup = new GroupBox();
            Label logLabel = new Label();
            Label warning = new Label();

            this.SuspendLayout();
            heading.Text = "Skyband Payment Terminal Tester";
            heading.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            heading.AutoSize = true;
            heading.Location = new Point(20, 15);

            connectionGroup.Text = "USB connection (USB serial / COM port)";
            connectionGroup.Location = new Point(20, 55);
            connectionGroup.Size = new Size(710, 175);
            AddLabel(connectionGroup, "COM port", 18, 32);
            this.cmbPort.Location = new Point(125, 28);
            this.cmbPort.Size = new Size(330, 28);
            this.cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
            connectionGroup.Controls.Add(this.cmbPort);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Location = new Point(470, 27);
            this.btnRefresh.Size = new Size(95, 30);
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            connectionGroup.Controls.Add(this.btnRefresh);
            AddLabel(connectionGroup, "Baud rate", 18, 72);
            this.cmbBaud.Location = new Point(125, 68);
            this.cmbBaud.Size = new Size(130, 28);
            this.cmbBaud.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            this.cmbBaud.SelectedItem = "38400";
            connectionGroup.Controls.Add(this.cmbBaud);
            AddLabel(connectionGroup, "Parity", 285, 72);
            this.cmbParity.Location = new Point(355, 68);
            this.cmbParity.Size = new Size(100, 28);
            this.cmbParity.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            this.cmbParity.SelectedItem = "None";
            connectionGroup.Controls.Add(this.cmbParity);
            AddLabel(connectionGroup, "Data bits", 18, 112);
            this.cmbDataBits.Location = new Point(125, 108);
            this.cmbDataBits.Size = new Size(80, 28);
            this.cmbDataBits.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbDataBits.Items.AddRange(new object[] { "7", "8" });
            this.cmbDataBits.SelectedItem = "8";
            connectionGroup.Controls.Add(this.cmbDataBits);
            AddLabel(connectionGroup, "Stop bits", 285, 112);
            this.cmbStopBits.Location = new Point(355, 108);
            this.cmbStopBits.Size = new Size(100, 28);
            this.cmbStopBits.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbStopBits.Items.AddRange(new object[] { "One", "Two", "OnePointFive" });
            this.cmbStopBits.SelectedItem = "One";
            connectionGroup.Controls.Add(this.cmbStopBits);
            this.btnConnect.Text = "Test COM Port";
            this.btnConnect.Location = new Point(470, 105);
            this.btnConnect.Size = new Size(150, 34);
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            connectionGroup.Controls.Add(this.btnConnect);
            this.lblConnection.Text = "Not tested";
            this.lblConnection.ForeColor = Color.DarkOrange;
            this.lblConnection.AutoSize = true;
            this.lblConnection.Location = new Point(125, 145);
            connectionGroup.Controls.Add(this.lblConnection);

            purchaseGroup.Text = "Purchase transaction (real transaction)";
            purchaseGroup.Location = new Point(20, 245);
            purchaseGroup.Size = new Size(710, 185);
            AddLabel(purchaseGroup, "Amount", 18, 35);
            this.txtAmount.Location = new Point(125, 31);
            this.txtAmount.Size = new Size(160, 28);
            this.txtAmount.Text = "1.00";
            purchaseGroup.Controls.Add(this.txtAmount);
            AddLabel(purchaseGroup, "Cash register (8 digits)", 320, 35);
            this.txtCashRegister.Location = new Point(490, 31);
            this.txtCashRegister.Size = new Size(190, 28);
            this.txtCashRegister.MaxLength = 8;
            purchaseGroup.Controls.Add(this.txtCashRegister);
            AddLabel(purchaseGroup, "Terminal ID", 18, 78);
            this.txtTerminalId.Location = new Point(125, 74);
            this.txtTerminalId.Size = new Size(160, 28);
            purchaseGroup.Controls.Add(this.txtTerminalId);
            AddLabel(purchaseGroup, "ECR reference (6 digits)", 320, 78);
            this.txtEcrRef.Location = new Point(490, 74);
            this.txtEcrRef.Size = new Size(190, 28);
            this.txtEcrRef.MaxLength = 6;
            this.txtEcrRef.Text = "000001";
            purchaseGroup.Controls.Add(this.txtEcrRef);
            this.btnPurchase.Text = "Send Purchase";
            this.btnPurchase.Location = new Point(125, 119);
            this.btnPurchase.Size = new Size(170, 38);
            this.btnPurchase.BackColor = Color.LightSteelBlue;
            this.btnPurchase.Click += new System.EventHandler(this.btnPurchase_Click);
            purchaseGroup.Controls.Add(this.btnPurchase);
            warning.Text = "Warning: Send Purchase starts a payment on the terminal. Use only an approved test amount/environment.";
            warning.ForeColor = Color.Firebrick;
            warning.AutoSize = true;
            warning.Location = new Point(310, 130);
            purchaseGroup.Controls.Add(warning);

            logLabel.Text = "Output";
            logLabel.AutoSize = true;
            logLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            logLabel.Location = new Point(20, 442);
            this.txtLog.Location = new Point(20, 468);
            this.txtLog.Size = new Size(710, 150);
            this.txtLog.ReadOnly = true;
            this.txtLog.Font = new Font("Consolas", 9.5F);

            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(760, 650);
            this.MinimumSize = new Size(760, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "Skyband ECR - USB/COM Test";
            this.Controls.Add(heading);
            this.Controls.Add(connectionGroup);
            this.Controls.Add(purchaseGroup);
            this.Controls.Add(logLabel);
            this.Controls.Add(this.txtLog);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private static void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y + 4) });
        }
    }
}
