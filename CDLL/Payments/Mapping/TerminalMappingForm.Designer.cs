// =====================================================================================
// EN: Layout of the terminal mapping screen (Windows Forms Designer file).
//       - left:  the mapping being edited (workstation, terminal name, department, COM / TCP settings)
//       - right: every saved mapping; click a row to load it
//       - below: Save, Test connection, Remove, Close
// AR: تصميم شاشة ربط الأجهزة (ملف مصمم Windows Forms).
//       - يسار: الربط الجاري تعديله (الحاسوب، اسم الجهاز، القسم، إعدادات COM / TCP)
//       - يمين: كل الربطات المحفوظة، انقر على سطر لتحميله
//       - أسفل: حفظ، اختبار الاتصال، حذف، إغلاق
// =====================================================================================
namespace CDLL.Payments.Mapping
{
    partial class TerminalMappingForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.grpMapping = new System.Windows.Forms.GroupBox();
            this.lblWorkstation = new System.Windows.Forms.Label();
            this.txtWorkstation = new System.Windows.Forms.TextBox();
            this.lblTerminalName = new System.Windows.Forms.Label();
            this.txtTerminalName = new System.Windows.Forms.TextBox();
            this.lblDepartment = new System.Windows.Forms.Label();
            this.txtDepartment = new System.Windows.Forms.TextBox();
            this.lblTransport = new System.Windows.Forms.Label();
            this.cmbTransport = new System.Windows.Forms.ComboBox();
            this.lblComPort = new System.Windows.Forms.Label();
            this.cmbComPort = new System.Windows.Forms.ComboBox();
            this.btnRefreshPorts = new System.Windows.Forms.Button();
            this.lblBaudRate = new System.Windows.Forms.Label();
            this.numBaudRate = new System.Windows.Forms.NumericUpDown();
            this.lblParity = new System.Windows.Forms.Label();
            this.numParity = new System.Windows.Forms.NumericUpDown();
            this.lblDataBits = new System.Windows.Forms.Label();
            this.numDataBits = new System.Windows.Forms.NumericUpDown();
            this.lblStopBits = new System.Windows.Forms.Label();
            this.numStopBits = new System.Windows.Forms.NumericUpDown();
            this.lblTcpIp = new System.Windows.Forms.Label();
            this.txtTcpIp = new System.Windows.Forms.TextBox();
            this.lblTcpPort = new System.Windows.Forms.Label();
            this.txtTcpPort = new System.Windows.Forms.TextBox();
            this.lstMappings = new System.Windows.Forms.ListView();
            this.colWorkstation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTerminal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDepartment = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colConnection = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUpdatedBy = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnSave = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpMapping.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBaudRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDataBits)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStopBits)).BeginInit();
            this.SuspendLayout();
            //
            // grpMapping
            //
            this.grpMapping.Controls.Add(this.lblWorkstation);
            this.grpMapping.Controls.Add(this.txtWorkstation);
            this.grpMapping.Controls.Add(this.lblTerminalName);
            this.grpMapping.Controls.Add(this.txtTerminalName);
            this.grpMapping.Controls.Add(this.lblDepartment);
            this.grpMapping.Controls.Add(this.txtDepartment);
            this.grpMapping.Controls.Add(this.lblTransport);
            this.grpMapping.Controls.Add(this.cmbTransport);
            this.grpMapping.Controls.Add(this.lblComPort);
            this.grpMapping.Controls.Add(this.cmbComPort);
            this.grpMapping.Controls.Add(this.btnRefreshPorts);
            this.grpMapping.Controls.Add(this.lblBaudRate);
            this.grpMapping.Controls.Add(this.numBaudRate);
            this.grpMapping.Controls.Add(this.lblParity);
            this.grpMapping.Controls.Add(this.numParity);
            this.grpMapping.Controls.Add(this.lblDataBits);
            this.grpMapping.Controls.Add(this.numDataBits);
            this.grpMapping.Controls.Add(this.lblStopBits);
            this.grpMapping.Controls.Add(this.numStopBits);
            this.grpMapping.Controls.Add(this.lblTcpIp);
            this.grpMapping.Controls.Add(this.txtTcpIp);
            this.grpMapping.Controls.Add(this.lblTcpPort);
            this.grpMapping.Controls.Add(this.txtTcpPort);
            this.grpMapping.Location = new System.Drawing.Point(12, 12);
            this.grpMapping.Name = "grpMapping";
            this.grpMapping.Size = new System.Drawing.Size(410, 375);
            this.grpMapping.TabIndex = 0;
            this.grpMapping.TabStop = false;
            this.grpMapping.Text = "Terminal of this workstation / جهاز هذا الحاسوب";
            //
            // lblWorkstation
            //
            this.lblWorkstation.AutoSize = true;
            this.lblWorkstation.Location = new System.Drawing.Point(10, 31);
            this.lblWorkstation.Name = "lblWorkstation";
            this.lblWorkstation.Size = new System.Drawing.Size(150, 20);
            this.lblWorkstation.TabIndex = 0;
            this.lblWorkstation.Text = "Workstation / الحاسوب:";
            //
            // txtWorkstation
            //
            this.txtWorkstation.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtWorkstation.Location = new System.Drawing.Point(200, 28);
            this.txtWorkstation.MaxLength = 128;
            this.txtWorkstation.Name = "txtWorkstation";
            this.txtWorkstation.Size = new System.Drawing.Size(195, 27);
            this.txtWorkstation.TabIndex = 1;
            //
            // lblTerminalName
            //
            this.lblTerminalName.AutoSize = true;
            this.lblTerminalName.Location = new System.Drawing.Point(10, 61);
            this.lblTerminalName.Name = "lblTerminalName";
            this.lblTerminalName.Size = new System.Drawing.Size(160, 20);
            this.lblTerminalName.TabIndex = 2;
            this.lblTerminalName.Text = "Terminal name / اسم الجهاز:";
            //
            // txtTerminalName
            //
            this.txtTerminalName.Location = new System.Drawing.Point(200, 58);
            this.txtTerminalName.MaxLength = 100;
            this.txtTerminalName.Name = "txtTerminalName";
            this.txtTerminalName.Size = new System.Drawing.Size(195, 27);
            this.txtTerminalName.TabIndex = 3;
            //
            // lblDepartment
            //
            this.lblDepartment.AutoSize = true;
            this.lblDepartment.Location = new System.Drawing.Point(10, 91);
            this.lblDepartment.Name = "lblDepartment";
            this.lblDepartment.Size = new System.Drawing.Size(140, 20);
            this.lblDepartment.TabIndex = 4;
            this.lblDepartment.Text = "Department / القسم:";
            //
            // txtDepartment
            //
            this.txtDepartment.Location = new System.Drawing.Point(200, 88);
            this.txtDepartment.MaxLength = 100;
            this.txtDepartment.Name = "txtDepartment";
            this.txtDepartment.Size = new System.Drawing.Size(195, 27);
            this.txtDepartment.TabIndex = 5;
            //
            // lblTransport
            //
            this.lblTransport.AutoSize = true;
            this.lblTransport.Location = new System.Drawing.Point(10, 121);
            this.lblTransport.Name = "lblTransport";
            this.lblTransport.Size = new System.Drawing.Size(140, 20);
            this.lblTransport.TabIndex = 6;
            this.lblTransport.Text = "Connection / الاتصال:";
            //
            // cmbTransport
            //
            this.cmbTransport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTransport.FormattingEnabled = true;
            this.cmbTransport.Items.AddRange(new object[] {
            "Com",
            "Tcp"});
            this.cmbTransport.Location = new System.Drawing.Point(200, 118);
            this.cmbTransport.Name = "cmbTransport";
            this.cmbTransport.Size = new System.Drawing.Size(195, 28);
            this.cmbTransport.TabIndex = 7;
            //
            // lblComPort
            //
            this.lblComPort.AutoSize = true;
            this.lblComPort.Location = new System.Drawing.Point(10, 151);
            this.lblComPort.Name = "lblComPort";
            this.lblComPort.Size = new System.Drawing.Size(158, 20);
            this.lblComPort.TabIndex = 8;
            this.lblComPort.Text = "COM port / منفذ COM:";
            //
            // cmbComPort
            //
            this.cmbComPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbComPort.DropDownWidth = 380;
            this.cmbComPort.FormattingEnabled = true;
            this.cmbComPort.Location = new System.Drawing.Point(200, 148);
            this.cmbComPort.Name = "cmbComPort";
            this.cmbComPort.Size = new System.Drawing.Size(160, 28);
            this.cmbComPort.TabIndex = 9;
            //
            // btnRefreshPorts
            //
            this.btnRefreshPorts.Location = new System.Drawing.Point(365, 147);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new System.Drawing.Size(30, 30);
            this.btnRefreshPorts.TabIndex = 22;
            this.btnRefreshPorts.Text = "↻";
            this.btnRefreshPorts.UseVisualStyleBackColor = true;
            this.btnRefreshPorts.Click += new System.EventHandler(this.btnRefreshPorts_Click);
            //
            // lblBaudRate
            //
            this.lblBaudRate.AutoSize = true;
            this.lblBaudRate.Location = new System.Drawing.Point(10, 181);
            this.lblBaudRate.Name = "lblBaudRate";
            this.lblBaudRate.Size = new System.Drawing.Size(159, 20);
            this.lblBaudRate.TabIndex = 10;
            this.lblBaudRate.Text = "Baud rate / سرعة النقل:";
            //
            // numBaudRate
            //
            this.numBaudRate.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numBaudRate.Location = new System.Drawing.Point(200, 179);
            this.numBaudRate.Maximum = new decimal(new int[] {
            921600,
            0,
            0,
            0});
            this.numBaudRate.Minimum = new decimal(new int[] {
            110,
            0,
            0,
            0});
            this.numBaudRate.Name = "numBaudRate";
            this.numBaudRate.Size = new System.Drawing.Size(195, 27);
            this.numBaudRate.TabIndex = 11;
            this.numBaudRate.Value = new decimal(new int[] {
            38400,
            0,
            0,
            0});
            //
            // lblParity
            //
            this.lblParity.AutoSize = true;
            this.lblParity.Location = new System.Drawing.Point(10, 211);
            this.lblParity.Name = "lblParity";
            this.lblParity.Size = new System.Drawing.Size(169, 20);
            this.lblParity.TabIndex = 12;
            this.lblParity.Text = "Parity (0=none) / التماثل:";
            //
            // numParity
            //
            this.numParity.Location = new System.Drawing.Point(200, 209);
            this.numParity.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.numParity.Name = "numParity";
            this.numParity.Size = new System.Drawing.Size(195, 27);
            this.numParity.TabIndex = 13;
            //
            // lblDataBits
            //
            this.lblDataBits.AutoSize = true;
            this.lblDataBits.Location = new System.Drawing.Point(10, 241);
            this.lblDataBits.Name = "lblDataBits";
            this.lblDataBits.Size = new System.Drawing.Size(164, 20);
            this.lblDataBits.TabIndex = 14;
            this.lblDataBits.Text = "Data bits / بتات البيانات:";
            //
            // numDataBits
            //
            this.numDataBits.Location = new System.Drawing.Point(200, 239);
            this.numDataBits.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numDataBits.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numDataBits.Name = "numDataBits";
            this.numDataBits.Size = new System.Drawing.Size(195, 27);
            this.numDataBits.TabIndex = 15;
            this.numDataBits.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            //
            // lblStopBits
            //
            this.lblStopBits.AutoSize = true;
            this.lblStopBits.Location = new System.Drawing.Point(10, 271);
            this.lblStopBits.Name = "lblStopBits";
            this.lblStopBits.Size = new System.Drawing.Size(185, 20);
            this.lblStopBits.TabIndex = 16;
            this.lblStopBits.Text = "Stop bits (0=one) / التوقف:";
            //
            // numStopBits
            //
            this.numStopBits.Location = new System.Drawing.Point(200, 269);
            this.numStopBits.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numStopBits.Name = "numStopBits";
            this.numStopBits.Size = new System.Drawing.Size(195, 27);
            this.numStopBits.TabIndex = 17;
            //
            // lblTcpIp
            //
            this.lblTcpIp.AutoSize = true;
            this.lblTcpIp.Location = new System.Drawing.Point(10, 301);
            this.lblTcpIp.Name = "lblTcpIp";
            this.lblTcpIp.Size = new System.Drawing.Size(151, 20);
            this.lblTcpIp.TabIndex = 18;
            this.lblTcpIp.Text = "Terminal IP / عنوان IP:";
            //
            // txtTcpIp
            //
            this.txtTcpIp.Location = new System.Drawing.Point(200, 298);
            this.txtTcpIp.MaxLength = 15;
            this.txtTcpIp.Name = "txtTcpIp";
            this.txtTcpIp.Size = new System.Drawing.Size(195, 27);
            this.txtTcpIp.TabIndex = 19;
            //
            // lblTcpPort
            //
            this.lblTcpPort.AutoSize = true;
            this.lblTcpPort.Location = new System.Drawing.Point(10, 331);
            this.lblTcpPort.Name = "lblTcpPort";
            this.lblTcpPort.Size = new System.Drawing.Size(140, 20);
            this.lblTcpPort.TabIndex = 20;
            this.lblTcpPort.Text = "TCP port / منفذ TCP:";
            //
            // txtTcpPort
            //
            this.txtTcpPort.Location = new System.Drawing.Point(200, 328);
            this.txtTcpPort.MaxLength = 5;
            this.txtTcpPort.Name = "txtTcpPort";
            this.txtTcpPort.Size = new System.Drawing.Size(195, 27);
            this.txtTcpPort.TabIndex = 21;
            this.txtTcpPort.Text = "6100";
            //
            // lstMappings
            //
            this.lstMappings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colWorkstation,
            this.colTerminal,
            this.colDepartment,
            this.colConnection,
            this.colUpdatedBy});
            this.lstMappings.FullRowSelect = true;
            this.lstMappings.HideSelection = false;
            this.lstMappings.Location = new System.Drawing.Point(435, 18);
            this.lstMappings.MultiSelect = false;
            this.lstMappings.Name = "lstMappings";
            this.lstMappings.Size = new System.Drawing.Size(535, 369);
            this.lstMappings.TabIndex = 1;
            this.lstMappings.UseCompatibleStateImageBehavior = false;
            this.lstMappings.View = System.Windows.Forms.View.Details;
            this.lstMappings.SelectedIndexChanged += new System.EventHandler(this.lstMappings_SelectedIndexChanged);
            //
            // colWorkstation
            //
            this.colWorkstation.Text = "Workstation / الحاسوب";
            this.colWorkstation.Width = 120;
            //
            // colTerminal
            //
            this.colTerminal.Text = "Terminal / الجهاز";
            this.colTerminal.Width = 100;
            //
            // colDepartment
            //
            this.colDepartment.Text = "Department / القسم";
            this.colDepartment.Width = 100;
            //
            // colConnection
            //
            this.colConnection.Text = "Connection / الاتصال";
            this.colConnection.Width = 110;
            //
            // colUpdatedBy
            //
            this.colUpdatedBy.Text = "Updated by / بواسطة";
            this.colUpdatedBy.Width = 100;
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSave.Location = new System.Drawing.Point(12, 400);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(130, 46);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save / حفظ";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnTest
            //
            this.btnTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnTest.Location = new System.Drawing.Point(152, 400);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(130, 46);
            this.btnTest.TabIndex = 3;
            this.btnTest.Text = "Test connection\r\nاختبار الاتصال";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            //
            // btnRemove
            //
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRemove.Location = new System.Drawing.Point(292, 400);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(130, 46);
            this.btnRemove.TabIndex = 4;
            this.btnRemove.Text = "Remove / حذف";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(840, 400);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(130, 46);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "Close / إغلاق";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(435, 400);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(395, 46);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // TerminalMappingForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(982, 458);
            this.Controls.Add(this.grpMapping);
            this.Controls.Add(this.lstMappings);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnClose);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 505);
            this.Name = "TerminalMappingForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Terminal mapping / ربط الأجهزة بالحواسيب";
            this.Load += new System.EventHandler(this.TerminalMappingForm_Load);
            this.grpMapping.ResumeLayout(false);
            this.grpMapping.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBaudRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDataBits)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStopBits)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpMapping;
        private System.Windows.Forms.Label lblWorkstation;
        private System.Windows.Forms.TextBox txtWorkstation;
        private System.Windows.Forms.Label lblTerminalName;
        private System.Windows.Forms.TextBox txtTerminalName;
        private System.Windows.Forms.Label lblDepartment;
        private System.Windows.Forms.TextBox txtDepartment;
        private System.Windows.Forms.Label lblTransport;
        private System.Windows.Forms.ComboBox cmbTransport;
        private System.Windows.Forms.Label lblComPort;
        private System.Windows.Forms.ComboBox cmbComPort;
        private System.Windows.Forms.Button btnRefreshPorts;
        private System.Windows.Forms.Label lblBaudRate;
        private System.Windows.Forms.NumericUpDown numBaudRate;
        private System.Windows.Forms.Label lblParity;
        private System.Windows.Forms.NumericUpDown numParity;
        private System.Windows.Forms.Label lblDataBits;
        private System.Windows.Forms.NumericUpDown numDataBits;
        private System.Windows.Forms.Label lblStopBits;
        private System.Windows.Forms.NumericUpDown numStopBits;
        private System.Windows.Forms.Label lblTcpIp;
        private System.Windows.Forms.TextBox txtTcpIp;
        private System.Windows.Forms.Label lblTcpPort;
        private System.Windows.Forms.TextBox txtTcpPort;
        private System.Windows.Forms.ListView lstMappings;
        private System.Windows.Forms.ColumnHeader colWorkstation;
        private System.Windows.Forms.ColumnHeader colTerminal;
        private System.Windows.Forms.ColumnHeader colDepartment;
        private System.Windows.Forms.ColumnHeader colConnection;
        private System.Windows.Forms.ColumnHeader colUpdatedBy;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblStatus;
    }
}
