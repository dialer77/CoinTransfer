namespace CoinTransfer
{
    partial class Form1
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
            grpApiSetting = new GroupBox();
            btnImportApiKey = new Button();
            btnExportApiKey = new Button();
            btnSaveApiKey = new Button();
            txtBinanceSecretKey = new TextBox();
            txtBinanceAccessKey = new TextBox();
            lblSecretKey = new Label();
            lblAccessKey = new Label();
            chkReservedWithdraw = new CheckBox();
            lblDeadline = new Label();
            dtpDeadline = new DateTimePicker();
            lblRetryInterval = new Label();
            numRetryInterval = new NumericUpDown();
            btnCancelReserved = new Button();
            lblSavedAddress = new Label();
            cmbSavedAddresses = new ComboBox();
            lblAddressName = new Label();
            txtAddressName = new TextBox();
            btnSaveAddress = new Button();
            btnSimpleBalance = new Button();
            lblSimpleBalance = new Label();
            grpWithdraw = new GroupBox();
            btnWithdraw = new Button();
            txtQuestionnaire = new TextBox();
            lblQuestionnaire = new Label();
            lblTravelRuleStatus = new Label();
            btnCheckTravelRule = new Button();
            txtTag = new TextBox();
            lblTag = new Label();
            txtAddress = new TextBox();
            lblAddress = new Label();
            txtAmount = new TextBox();
            lblAmount = new Label();
            txtNetwork = new TextBox();
            lblNetwork = new Label();
            txtCoin = new TextBox();
            lblCoin = new Label();
            btnRefreshBalance = new Button();
            txtLog = new TextBox();
            grpApiSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numRetryInterval).BeginInit();
            grpWithdraw.SuspendLayout();
            SuspendLayout();
            // 
            // grpApiSetting
            // 
            grpApiSetting.Controls.Add(btnImportApiKey);
            grpApiSetting.Controls.Add(btnExportApiKey);
            grpApiSetting.Controls.Add(btnSaveApiKey);
            grpApiSetting.Controls.Add(txtBinanceSecretKey);
            grpApiSetting.Controls.Add(txtBinanceAccessKey);
            grpApiSetting.Controls.Add(lblSecretKey);
            grpApiSetting.Controls.Add(lblAccessKey);
            grpApiSetting.Location = new Point(12, 15);
            grpApiSetting.Margin = new Padding(3, 4, 3, 4);
            grpApiSetting.Name = "grpApiSetting";
            grpApiSetting.Padding = new Padding(3, 4, 3, 4);
            grpApiSetting.Size = new Size(564, 135);
            grpApiSetting.TabIndex = 0;
            grpApiSetting.TabStop = false;
            grpApiSetting.Text = "Binance API 설정";
            // 
            // btnImportApiKey
            // 
            btnImportApiKey.Location = new Point(456, 61);
            btnImportApiKey.Name = "btnImportApiKey";
            btnImportApiKey.Size = new Size(90, 28);
            btnImportApiKey.TabIndex = 6;
            btnImportApiKey.Text = "파일 가져오기";
            btnImportApiKey.UseVisualStyleBackColor = true;
            btnImportApiKey.Click += BtnImportApiKey_Click;
            // 
            // btnExportApiKey
            // 
            btnExportApiKey.Location = new Point(456, 28);
            btnExportApiKey.Name = "btnExportApiKey";
            btnExportApiKey.Size = new Size(90, 28);
            btnExportApiKey.TabIndex = 5;
            btnExportApiKey.Text = "파일 내보내기";
            btnExportApiKey.UseVisualStyleBackColor = true;
            btnExportApiKey.Click += BtnExportApiKey_Click;
            // 
            // btnSaveApiKey
            // 
            btnSaveApiKey.Location = new Point(456, 94);
            btnSaveApiKey.Margin = new Padding(3, 4, 3, 4);
            btnSaveApiKey.Name = "btnSaveApiKey";
            btnSaveApiKey.Size = new Size(90, 28);
            btnSaveApiKey.TabIndex = 4;
            btnSaveApiKey.Text = "저장";
            btnSaveApiKey.UseVisualStyleBackColor = true;
            btnSaveApiKey.Click += BtnSaveApiKey_Click;
            // 
            // txtBinanceSecretKey
            // 
            txtBinanceSecretKey.Location = new Point(100, 65);
            txtBinanceSecretKey.Margin = new Padding(3, 4, 3, 4);
            txtBinanceSecretKey.Name = "txtBinanceSecretKey";
            txtBinanceSecretKey.PasswordChar = '*';
            txtBinanceSecretKey.Size = new Size(350, 23);
            txtBinanceSecretKey.TabIndex = 3;
            // 
            // txtBinanceAccessKey
            // 
            txtBinanceAccessKey.Location = new Point(100, 28);
            txtBinanceAccessKey.Margin = new Padding(3, 4, 3, 4);
            txtBinanceAccessKey.Name = "txtBinanceAccessKey";
            txtBinanceAccessKey.Size = new Size(350, 23);
            txtBinanceAccessKey.TabIndex = 2;
            // 
            // lblSecretKey
            // 
            lblSecretKey.AutoSize = true;
            lblSecretKey.Location = new Point(15, 69);
            lblSecretKey.Name = "lblSecretKey";
            lblSecretKey.Size = new Size(63, 15);
            lblSecretKey.TabIndex = 1;
            lblSecretKey.Text = "Secret Key";
            // 
            // lblAccessKey
            // 
            lblAccessKey.AutoSize = true;
            lblAccessKey.Location = new Point(15, 31);
            lblAccessKey.Name = "lblAccessKey";
            lblAccessKey.Size = new Size(66, 15);
            lblAccessKey.TabIndex = 0;
            lblAccessKey.Text = "Access Key";
            // 
            // chkReservedWithdraw
            // 
            chkReservedWithdraw.AutoSize = true;
            chkReservedWithdraw.Location = new Point(300, 312);
            chkReservedWithdraw.Name = "chkReservedWithdraw";
            chkReservedWithdraw.Size = new Size(78, 19);
            chkReservedWithdraw.TabIndex = 20;
            chkReservedWithdraw.Text = "예약 출금";
            chkReservedWithdraw.UseVisualStyleBackColor = true;
            chkReservedWithdraw.CheckedChanged += ChkReservedWithdraw_CheckedChanged;
            // 
            // lblDeadline
            // 
            lblDeadline.AutoSize = true;
            lblDeadline.Enabled = false;
            lblDeadline.Location = new Point(154, 275);
            lblDeadline.Name = "lblDeadline";
            lblDeadline.Size = new Size(58, 15);
            lblDeadline.TabIndex = 21;
            lblDeadline.Text = "마감시간:";
            // 
            // dtpDeadline
            // 
            dtpDeadline.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpDeadline.Enabled = false;
            dtpDeadline.Format = DateTimePickerFormat.Custom;
            dtpDeadline.Location = new Point(218, 271);
            dtpDeadline.Name = "dtpDeadline";
            dtpDeadline.Size = new Size(170, 23);
            dtpDeadline.TabIndex = 22;
            dtpDeadline.Value = new DateTime(2026, 1, 31, 17, 13, 8, 331);
            // 
            // lblRetryInterval
            // 
            lblRetryInterval.AutoSize = true;
            lblRetryInterval.Enabled = false;
            lblRetryInterval.Location = new Point(394, 275);
            lblRetryInterval.Name = "lblRetryInterval";
            lblRetryInterval.Size = new Size(91, 15);
            lblRetryInterval.TabIndex = 23;
            lblRetryInterval.Text = "재시도 간격(초)";
            // 
            // numRetryInterval
            // 
            numRetryInterval.Enabled = false;
            numRetryInterval.Location = new Point(491, 271);
            numRetryInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            numRetryInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRetryInterval.Name = "numRetryInterval";
            numRetryInterval.Size = new Size(55, 23);
            numRetryInterval.TabIndex = 24;
            numRetryInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnCancelReserved
            // 
            btnCancelReserved.Enabled = false;
            btnCancelReserved.Location = new Point(456, 341);
            btnCancelReserved.Name = "btnCancelReserved";
            btnCancelReserved.Size = new Size(100, 38);
            btnCancelReserved.TabIndex = 25;
            btnCancelReserved.Text = "예약 취소";
            btnCancelReserved.UseVisualStyleBackColor = true;
            btnCancelReserved.Click += BtnCancelReserved_Click;
            // 
            // lblSavedAddress
            // 
            lblSavedAddress.AutoSize = true;
            lblSavedAddress.Location = new Point(15, 105);
            lblSavedAddress.Name = "lblSavedAddress";
            lblSavedAddress.Size = new Size(71, 15);
            lblSavedAddress.TabIndex = 27;
            lblSavedAddress.Text = "저장된 주소";
            // 
            // cmbSavedAddresses
            // 
            cmbSavedAddresses.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSavedAddresses.FormattingEnabled = true;
            cmbSavedAddresses.Location = new Point(120, 101);
            cmbSavedAddresses.Name = "cmbSavedAddresses";
            cmbSavedAddresses.Size = new Size(220, 23);
            cmbSavedAddresses.TabIndex = 28;
            cmbSavedAddresses.SelectedIndexChanged += CmbSavedAddresses_SelectedIndexChanged;
            // 
            // lblAddressName
            // 
            lblAddressName.AutoSize = true;
            lblAddressName.Location = new Point(348, 105);
            lblAddressName.Name = "lblAddressName";
            lblAddressName.Size = new Size(59, 15);
            lblAddressName.TabIndex = 30;
            lblAddressName.Text = "저장 이름";
            // 
            // txtAddressName
            // 
            txtAddressName.Location = new Point(408, 101);
            txtAddressName.Name = "txtAddressName";
            txtAddressName.PlaceholderText = "키(이름)";
            txtAddressName.Size = new Size(64, 23);
            txtAddressName.TabIndex = 31;
            // 
            // btnSaveAddress
            // 
            btnSaveAddress.Location = new Point(478, 99);
            btnSaveAddress.Name = "btnSaveAddress";
            btnSaveAddress.Size = new Size(75, 27);
            btnSaveAddress.TabIndex = 29;
            btnSaveAddress.Text = "주소 저장";
            btnSaveAddress.UseVisualStyleBackColor = true;
            btnSaveAddress.Click += BtnSaveAddress_Click;
            // 
            // btnSimpleBalance
            // 
            btnSimpleBalance.Location = new Point(169, 345);
            btnSimpleBalance.Name = "btnSimpleBalance";
            btnSimpleBalance.Size = new Size(120, 31);
            btnSimpleBalance.TabIndex = 30;
            btnSimpleBalance.Text = "단순 잔고 조회";
            btnSimpleBalance.UseVisualStyleBackColor = true;
            btnSimpleBalance.Click += BtnSimpleBalance_Click;
            // 
            // lblSimpleBalance
            // 
            lblSimpleBalance.AutoSize = true;
            lblSimpleBalance.ForeColor = Color.Gray;
            lblSimpleBalance.Location = new Point(15, 351);
            lblSimpleBalance.Name = "lblSimpleBalance";
            lblSimpleBalance.Size = new Size(43, 15);
            lblSimpleBalance.TabIndex = 31;
            lblSimpleBalance.Text = "잔고: -";
            // 
            // grpWithdraw
            // 
            grpWithdraw.Controls.Add(btnCancelReserved);
            grpWithdraw.Controls.Add(chkReservedWithdraw);
            grpWithdraw.Controls.Add(numRetryInterval);
            grpWithdraw.Controls.Add(lblRetryInterval);
            grpWithdraw.Controls.Add(dtpDeadline);
            grpWithdraw.Controls.Add(lblDeadline);
            grpWithdraw.Controls.Add(btnWithdraw);
            grpWithdraw.Controls.Add(txtQuestionnaire);
            grpWithdraw.Controls.Add(lblQuestionnaire);
            grpWithdraw.Controls.Add(lblTravelRuleStatus);
            grpWithdraw.Controls.Add(btnCheckTravelRule);
            grpWithdraw.Controls.Add(txtTag);
            grpWithdraw.Controls.Add(lblTag);
            grpWithdraw.Controls.Add(btnSaveAddress);
            grpWithdraw.Controls.Add(txtAddressName);
            grpWithdraw.Controls.Add(lblAddressName);
            grpWithdraw.Controls.Add(cmbSavedAddresses);
            grpWithdraw.Controls.Add(lblSavedAddress);
            grpWithdraw.Controls.Add(txtAddress);
            grpWithdraw.Controls.Add(lblAddress);
            grpWithdraw.Controls.Add(txtAmount);
            grpWithdraw.Controls.Add(lblAmount);
            grpWithdraw.Controls.Add(txtNetwork);
            grpWithdraw.Controls.Add(lblNetwork);
            grpWithdraw.Controls.Add(txtCoin);
            grpWithdraw.Controls.Add(lblCoin);
            grpWithdraw.Controls.Add(lblSimpleBalance);
            grpWithdraw.Controls.Add(btnSimpleBalance);
            grpWithdraw.Controls.Add(btnRefreshBalance);
            grpWithdraw.Location = new Point(12, 158);
            grpWithdraw.Margin = new Padding(3, 4, 3, 4);
            grpWithdraw.Name = "grpWithdraw";
            grpWithdraw.Padding = new Padding(3, 4, 3, 4);
            grpWithdraw.Size = new Size(564, 386);
            grpWithdraw.TabIndex = 1;
            grpWithdraw.TabStop = false;
            grpWithdraw.Text = "Binance 출금";
            // 
            // btnWithdraw
            // 
            btnWithdraw.BackColor = Color.FromArgb(240, 185, 11);
            btnWithdraw.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            btnWithdraw.Location = new Point(300, 341);
            btnWithdraw.Margin = new Padding(3, 4, 3, 4);
            btnWithdraw.Name = "btnWithdraw";
            btnWithdraw.Size = new Size(150, 38);
            btnWithdraw.TabIndex = 19;
            btnWithdraw.Text = "출금 실행";
            btnWithdraw.UseVisualStyleBackColor = false;
            btnWithdraw.Click += BtnWithdraw_Click;
            // 
            // txtQuestionnaire
            // 
            txtQuestionnaire.Font = new Font("Consolas", 8.25F);
            txtQuestionnaire.Location = new Point(15, 228);
            txtQuestionnaire.Multiline = true;
            txtQuestionnaire.Name = "txtQuestionnaire";
            txtQuestionnaire.PlaceholderText = "필요한 경우에만 입력 (예: {\"country\":\"AE\", ...})";
            txtQuestionnaire.ScrollBars = ScrollBars.Vertical;
            txtQuestionnaire.Size = new Size(505, 35);
            txtQuestionnaire.TabIndex = 18;
            // 
            // lblQuestionnaire
            // 
            lblQuestionnaire.AutoSize = true;
            lblQuestionnaire.Location = new Point(15, 210);
            lblQuestionnaire.Name = "lblQuestionnaire";
            lblQuestionnaire.Size = new Size(184, 15);
            lblQuestionnaire.TabIndex = 17;
            lblQuestionnaire.Text = "Travel Rule Questionnaire (JSON)";
            // 
            // lblTravelRuleStatus
            // 
            lblTravelRuleStatus.AutoSize = true;
            lblTravelRuleStatus.ForeColor = Color.Gray;
            lblTravelRuleStatus.Location = new Point(452, 176);
            lblTravelRuleStatus.Name = "lblTravelRuleStatus";
            lblTravelRuleStatus.Size = new Size(108, 15);
            lblTravelRuleStatus.TabIndex = 16;
            lblTravelRuleStatus.Text = "Travel Rule: 미확인";
            // 
            // btnCheckTravelRule
            // 
            btnCheckTravelRule.Location = new Point(326, 169);
            btnCheckTravelRule.Name = "btnCheckTravelRule";
            btnCheckTravelRule.Size = new Size(120, 28);
            btnCheckTravelRule.TabIndex = 15;
            btnCheckTravelRule.Text = "Travel Rule 확인";
            btnCheckTravelRule.UseVisualStyleBackColor = true;
            btnCheckTravelRule.Click += BtnCheckTravelRule_Click;
            // 
            // txtTag
            // 
            txtTag.Location = new Point(120, 172);
            txtTag.Margin = new Padding(3, 4, 3, 4);
            txtTag.Name = "txtTag";
            txtTag.Size = new Size(200, 23);
            txtTag.TabIndex = 11;
            // 
            // lblTag
            // 
            lblTag.AutoSize = true;
            lblTag.Location = new Point(15, 176);
            lblTag.Name = "lblTag";
            lblTag.Size = new Size(102, 15);
            lblTag.TabIndex = 10;
            lblTag.Text = "Tag/Memo (선택)";
            // 
            // txtAddress
            // 
            txtAddress.Location = new Point(120, 135);
            txtAddress.Margin = new Padding(3, 4, 3, 4);
            txtAddress.Name = "txtAddress";
            txtAddress.Size = new Size(400, 23);
            txtAddress.TabIndex = 9;
            // 
            // lblAddress
            // 
            lblAddress.AutoSize = true;
            lblAddress.Location = new Point(15, 139);
            lblAddress.Name = "lblAddress";
            lblAddress.Size = new Size(59, 15);
            lblAddress.TabIndex = 8;
            lblAddress.Text = "출금 주소";
            // 
            // txtAmount
            // 
            txtAmount.Location = new Point(62, 312);
            txtAmount.Margin = new Padding(3, 4, 3, 4);
            txtAmount.Name = "txtAmount";
            txtAmount.Size = new Size(101, 23);
            txtAmount.TabIndex = 7;
            // 
            // lblAmount
            // 
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(15, 315);
            lblAmount.Name = "lblAmount";
            lblAmount.Size = new Size(31, 15);
            lblAmount.TabIndex = 6;
            lblAmount.Text = "수량";
            // 
            // txtNetwork
            // 
            txtNetwork.Location = new Point(120, 68);
            txtNetwork.Margin = new Padding(3, 4, 3, 4);
            txtNetwork.Name = "txtNetwork";
            txtNetwork.Size = new Size(200, 23);
            txtNetwork.TabIndex = 5;
            // 
            // lblNetwork
            // 
            lblNetwork.AutoSize = true;
            lblNetwork.Location = new Point(15, 72);
            lblNetwork.Name = "lblNetwork";
            lblNetwork.Size = new Size(55, 15);
            lblNetwork.TabIndex = 4;
            lblNetwork.Text = "네트워크";
            // 
            // txtCoin
            // 
            txtCoin.Location = new Point(120, 31);
            txtCoin.Margin = new Padding(3, 4, 3, 4);
            txtCoin.Name = "txtCoin";
            txtCoin.Size = new Size(200, 23);
            txtCoin.TabIndex = 3;
            // 
            // lblCoin
            // 
            lblCoin.AutoSize = true;
            lblCoin.Location = new Point(15, 35);
            lblCoin.Name = "lblCoin";
            lblCoin.Size = new Size(31, 15);
            lblCoin.TabIndex = 2;
            lblCoin.Text = "코인";
            // 
            // btnRefreshBalance
            // 
            btnRefreshBalance.Location = new Point(169, 307);
            btnRefreshBalance.Margin = new Padding(3, 4, 3, 4);
            btnRefreshBalance.Name = "btnRefreshBalance";
            btnRefreshBalance.Size = new Size(120, 31);
            btnRefreshBalance.TabIndex = 13;
            btnRefreshBalance.Text = "잔고 새로고침";
            btnRefreshBalance.UseVisualStyleBackColor = true;
            btnRefreshBalance.Click += BtnRefreshBalance_Click;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new Point(12, 552);
            txtLog.Margin = new Padding(3, 4, 3, 4);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(564, 153);
            txtLog.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(588, 718);
            Controls.Add(txtLog);
            Controls.Add(grpWithdraw);
            Controls.Add(grpApiSetting);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(600, 553);
            Name = "Form1";
            Text = "CoinTransfer - Binance 출금 봇";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            grpApiSetting.ResumeLayout(false);
            grpApiSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numRetryInterval).EndInit();
            grpWithdraw.ResumeLayout(false);
            grpWithdraw.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox grpApiSetting;
        private System.Windows.Forms.Button btnSaveApiKey;
        private System.Windows.Forms.TextBox txtBinanceSecretKey;
        private System.Windows.Forms.TextBox txtBinanceAccessKey;
        private System.Windows.Forms.Label lblSecretKey;
        private System.Windows.Forms.Label lblAccessKey;
        private System.Windows.Forms.Button btnExportApiKey;
        private System.Windows.Forms.Button btnImportApiKey;
        private System.Windows.Forms.GroupBox grpWithdraw;
        private System.Windows.Forms.TextBox txtCoin;
        private System.Windows.Forms.Label lblCoin;
        private System.Windows.Forms.TextBox txtNetwork;
        private System.Windows.Forms.Label lblNetwork;
        private System.Windows.Forms.TextBox txtAmount;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.TextBox txtTag;
        private System.Windows.Forms.Label lblTag;
        private System.Windows.Forms.Button btnRefreshBalance;
        private System.Windows.Forms.Button btnWithdraw;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnCheckTravelRule;
        private System.Windows.Forms.Label lblTravelRuleStatus;
        private System.Windows.Forms.Label lblQuestionnaire;
        private System.Windows.Forms.TextBox txtQuestionnaire;
        private System.Windows.Forms.CheckBox chkReservedWithdraw;
        private System.Windows.Forms.Label lblDeadline;
        private System.Windows.Forms.DateTimePicker dtpDeadline;
        private System.Windows.Forms.Label lblRetryInterval;
        private System.Windows.Forms.NumericUpDown numRetryInterval;
        private System.Windows.Forms.Button btnCancelReserved;
        private System.Windows.Forms.Label lblSavedAddress;
        private System.Windows.Forms.ComboBox cmbSavedAddresses;
        private System.Windows.Forms.Label lblAddressName;
        private System.Windows.Forms.TextBox txtAddressName;
        private System.Windows.Forms.Button btnSaveAddress;
        private System.Windows.Forms.Button btnSimpleBalance;
        private System.Windows.Forms.Label lblSimpleBalance;
    }
}
