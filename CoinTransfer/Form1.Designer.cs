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
            cmbExchange = new ComboBox();
            lblExchange = new Label();
            btnImportApiKey = new Button();
            btnExportApiKey = new Button();
            btnSaveApiKey = new Button();
            txtBinanceSecretKey = new TextBox();
            txtBinanceAccessKey = new TextBox();
            lblPassphrase = new Label();
            txtPassphrase = new TextBox();
            lblSecretKey = new Label();
            lblAccessKey = new Label();
            chkReservedWithdraw = new CheckBox();
            lblReservedWithdrawCount = new Label();
            numReservedWithdrawCount = new NumericUpDown();
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
            btnRemoveAddress = new Button();
            btnSimpleBalance = new Button();
            lblSimpleBalance = new Label();
            grpWithdraw = new GroupBox();
            btnWithdraw = new Button();
            lblTravelRuleExchange = new Label();
            txtTravelRuleExchange = new TextBox();
            lblTravelRuleWalletType = new Label();
            cmbTravelRuleWalletType = new ComboBox();
            lblTravelRuleReceiverType = new Label();
            cmbTravelRuleReceiverType = new ComboBox();
            lblTravelRuleReceiverName = new Label();
            txtTravelRuleReceiverName = new TextBox();
            lblTravelRuleReceiverNameEn = new Label();
            txtTravelRuleReceiverNameEn = new TextBox();
            lblTravelRuleStatus = new Label();
            btnCheckTravelRule = new Button();
            txtTag = new TextBox();
            lblTag = new Label();
            txtAddress = new TextBox();
            lblAddress = new Label();
            txtAmount = new TextBox();
            lblAmount = new Label();
            lblWithdrawFee = new Label();
            btnWithdrawAddressList = new Button();
            btnCheckWithdrawSupport = new Button();
            btnCheckChain = new Button();
            cmbNetwork = new ComboBox();
            lblNetwork = new Label();
            txtCoin = new TextBox();
            lblCoin = new Label();
            btnRefreshBalance = new Button();
            txtLog = new TextBox();
            grpApiSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numReservedWithdrawCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRetryInterval).BeginInit();
            grpWithdraw.SuspendLayout();
            SuspendLayout();
            // 
            // grpApiSetting
            // 
            grpApiSetting.Controls.Add(cmbExchange);
            grpApiSetting.Controls.Add(lblExchange);
            grpApiSetting.Controls.Add(btnImportApiKey);
            grpApiSetting.Controls.Add(btnExportApiKey);
            grpApiSetting.Controls.Add(btnSaveApiKey);
            grpApiSetting.Controls.Add(txtBinanceSecretKey);
            grpApiSetting.Controls.Add(txtBinanceAccessKey);
            grpApiSetting.Controls.Add(lblPassphrase);
            grpApiSetting.Controls.Add(txtPassphrase);
            grpApiSetting.Controls.Add(lblSecretKey);
            grpApiSetting.Controls.Add(lblAccessKey);
            grpApiSetting.Location = new Point(12, 15);
            grpApiSetting.Margin = new Padding(3, 4, 3, 4);
            grpApiSetting.Name = "grpApiSetting";
            grpApiSetting.Padding = new Padding(3, 4, 3, 4);
            grpApiSetting.Size = new Size(564, 165);
            grpApiSetting.TabIndex = 0;
            grpApiSetting.TabStop = false;
            grpApiSetting.Text = "API 설정";
            // 
            // cmbExchange
            // 
            cmbExchange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbExchange.FormattingEnabled = true;
            cmbExchange.Location = new Point(100, 28);
            cmbExchange.Name = "cmbExchange";
            cmbExchange.Size = new Size(150, 23);
            cmbExchange.TabIndex = 0;
            cmbExchange.SelectedIndexChanged += CmbExchange_SelectedIndexChanged;
            // 
            // lblExchange
            // 
            lblExchange.AutoSize = true;
            lblExchange.Location = new Point(15, 32);
            lblExchange.Name = "lblExchange";
            lblExchange.Size = new Size(43, 15);
            lblExchange.TabIndex = 7;
            lblExchange.Text = "거래소";
            // 
            // btnImportApiKey
            // 
            btnImportApiKey.Location = new Point(444, 56);
            btnImportApiKey.Name = "btnImportApiKey";
            btnImportApiKey.Size = new Size(102, 28);
            btnImportApiKey.TabIndex = 6;
            btnImportApiKey.Text = "파일 가져오기";
            btnImportApiKey.UseVisualStyleBackColor = true;
            btnImportApiKey.Click += BtnImportApiKey_Click;
            // 
            // btnExportApiKey
            // 
            btnExportApiKey.Location = new Point(444, 23);
            btnExportApiKey.Name = "btnExportApiKey";
            btnExportApiKey.Size = new Size(102, 28);
            btnExportApiKey.TabIndex = 5;
            btnExportApiKey.Text = "파일 내보내기";
            btnExportApiKey.UseVisualStyleBackColor = true;
            btnExportApiKey.Click += BtnExportApiKey_Click;
            // 
            // btnSaveApiKey
            // 
            btnSaveApiKey.Location = new Point(445, 88);
            btnSaveApiKey.Margin = new Padding(3, 4, 3, 4);
            btnSaveApiKey.Name = "btnSaveApiKey";
            btnSaveApiKey.Size = new Size(101, 28);
            btnSaveApiKey.TabIndex = 4;
            btnSaveApiKey.Text = "저장";
            btnSaveApiKey.UseVisualStyleBackColor = true;
            btnSaveApiKey.Click += BtnSaveApiKey_Click;
            // 
            // txtBinanceSecretKey
            // 
            txtBinanceSecretKey.Location = new Point(100, 88);
            txtBinanceSecretKey.Margin = new Padding(3, 4, 3, 4);
            txtBinanceSecretKey.Name = "txtBinanceSecretKey";
            txtBinanceSecretKey.PasswordChar = '*';
            txtBinanceSecretKey.Size = new Size(339, 23);
            txtBinanceSecretKey.TabIndex = 3;
            // 
            // txtBinanceAccessKey
            // 
            txtBinanceAccessKey.Location = new Point(100, 58);
            txtBinanceAccessKey.Margin = new Padding(3, 4, 3, 4);
            txtBinanceAccessKey.Name = "txtBinanceAccessKey";
            txtBinanceAccessKey.Size = new Size(339, 23);
            txtBinanceAccessKey.TabIndex = 2;
            // 
            // lblPassphrase
            // 
            lblPassphrase.AutoSize = true;
            lblPassphrase.Location = new Point(15, 122);
            lblPassphrase.Name = "lblPassphrase";
            lblPassphrase.Size = new Size(65, 15);
            lblPassphrase.TabIndex = 8;
            lblPassphrase.Text = "Passphrase";
            // 
            // txtPassphrase
            // 
            txtPassphrase.Location = new Point(100, 118);
            txtPassphrase.Margin = new Padding(3, 4, 3, 4);
            txtPassphrase.Name = "txtPassphrase";
            txtPassphrase.PasswordChar = '*';
            txtPassphrase.PlaceholderText = "Bitget, OKX 등 필요 시 입력";
            txtPassphrase.Size = new Size(339, 23);
            txtPassphrase.TabIndex = 4;
            // 
            // lblSecretKey
            // 
            lblSecretKey.AutoSize = true;
            lblSecretKey.Location = new Point(15, 92);
            lblSecretKey.Name = "lblSecretKey";
            lblSecretKey.Size = new Size(63, 15);
            lblSecretKey.TabIndex = 1;
            lblSecretKey.Text = "Secret Key";
            // 
            // lblAccessKey
            // 
            lblAccessKey.AutoSize = true;
            lblAccessKey.Location = new Point(15, 62);
            lblAccessKey.Name = "lblAccessKey";
            lblAccessKey.Size = new Size(66, 15);
            lblAccessKey.TabIndex = 0;
            lblAccessKey.Text = "Access Key";
            // 
            // chkReservedWithdraw
            // 
            chkReservedWithdraw.AutoSize = true;
            chkReservedWithdraw.Location = new Point(300, 365);
            chkReservedWithdraw.Name = "chkReservedWithdraw";
            chkReservedWithdraw.Size = new Size(78, 19);
            chkReservedWithdraw.TabIndex = 20;
            chkReservedWithdraw.Text = "예약 출금";
            chkReservedWithdraw.UseVisualStyleBackColor = true;
            chkReservedWithdraw.CheckedChanged += ChkReservedWithdraw_CheckedChanged;
            // 
            // lblReservedWithdrawCount
            // 
            lblReservedWithdrawCount.AutoSize = true;
            lblReservedWithdrawCount.Enabled = false;
            lblReservedWithdrawCount.Location = new Point(394, 366);
            lblReservedWithdrawCount.Name = "lblReservedWithdrawCount";
            lblReservedWithdrawCount.Size = new Size(87, 15);
            lblReservedWithdrawCount.TabIndex = 33;
            lblReservedWithdrawCount.Text = "예약 출금 횟수";
            // 
            // numReservedWithdrawCount
            // 
            numReservedWithdrawCount.Enabled = false;
            numReservedWithdrawCount.Location = new Point(491, 364);
            numReservedWithdrawCount.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numReservedWithdrawCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numReservedWithdrawCount.Name = "numReservedWithdrawCount";
            numReservedWithdrawCount.Size = new Size(55, 23);
            numReservedWithdrawCount.TabIndex = 34;
            numReservedWithdrawCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblDeadline
            // 
            lblDeadline.AutoSize = true;
            lblDeadline.Enabled = false;
            lblDeadline.Location = new Point(154, 332);
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
            dtpDeadline.Location = new Point(218, 328);
            dtpDeadline.Name = "dtpDeadline";
            dtpDeadline.Size = new Size(170, 23);
            dtpDeadline.TabIndex = 22;
            dtpDeadline.Value = new DateTime(2026, 1, 31, 17, 13, 8, 331);
            // 
            // lblRetryInterval
            // 
            lblRetryInterval.AutoSize = true;
            lblRetryInterval.Enabled = false;
            lblRetryInterval.Location = new Point(394, 332);
            lblRetryInterval.Name = "lblRetryInterval";
            lblRetryInterval.Size = new Size(91, 15);
            lblRetryInterval.TabIndex = 23;
            lblRetryInterval.Text = "재시도 간격(초)";
            // 
            // numRetryInterval
            // 
            numRetryInterval.Enabled = false;
            numRetryInterval.Location = new Point(491, 328);
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
            btnCancelReserved.Location = new Point(456, 402);
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
            // btnRemoveAddress
            // 
            btnRemoveAddress.Location = new Point(478, 132);
            btnRemoveAddress.Name = "btnRemoveAddress";
            btnRemoveAddress.Size = new Size(75, 27);
            btnRemoveAddress.TabIndex = 32;
            btnRemoveAddress.Text = "주소 삭제";
            btnRemoveAddress.UseVisualStyleBackColor = true;
            btnRemoveAddress.Click += BtnRemoveAddress_Click;
            // 
            // btnSimpleBalance
            // 
            btnSimpleBalance.Location = new Point(169, 409);
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
            lblSimpleBalance.Location = new Point(15, 417);
            lblSimpleBalance.Name = "lblSimpleBalance";
            lblSimpleBalance.Size = new Size(43, 15);
            lblSimpleBalance.TabIndex = 31;
            lblSimpleBalance.Text = "잔고: -";
            // 
            // grpWithdraw
            // 
            grpWithdraw.Controls.Add(btnCancelReserved);
            grpWithdraw.Controls.Add(chkReservedWithdraw);
            grpWithdraw.Controls.Add(lblReservedWithdrawCount);
            grpWithdraw.Controls.Add(numReservedWithdrawCount);
            grpWithdraw.Controls.Add(numRetryInterval);
            grpWithdraw.Controls.Add(lblRetryInterval);
            grpWithdraw.Controls.Add(dtpDeadline);
            grpWithdraw.Controls.Add(lblDeadline);
            grpWithdraw.Controls.Add(btnWithdraw);
            grpWithdraw.Controls.Add(lblTravelRuleExchange);
            grpWithdraw.Controls.Add(txtTravelRuleExchange);
            grpWithdraw.Controls.Add(lblTravelRuleWalletType);
            grpWithdraw.Controls.Add(cmbTravelRuleWalletType);
            grpWithdraw.Controls.Add(lblTravelRuleReceiverType);
            grpWithdraw.Controls.Add(cmbTravelRuleReceiverType);
            grpWithdraw.Controls.Add(lblTravelRuleReceiverName);
            grpWithdraw.Controls.Add(txtTravelRuleReceiverName);
            grpWithdraw.Controls.Add(lblTravelRuleReceiverNameEn);
            grpWithdraw.Controls.Add(txtTravelRuleReceiverNameEn);
            grpWithdraw.Controls.Add(lblTravelRuleStatus);
            grpWithdraw.Controls.Add(btnCheckTravelRule);
            grpWithdraw.Controls.Add(txtTag);
            grpWithdraw.Controls.Add(lblTag);
            grpWithdraw.Controls.Add(btnRemoveAddress);
            grpWithdraw.Controls.Add(btnSaveAddress);
            grpWithdraw.Controls.Add(txtAddressName);
            grpWithdraw.Controls.Add(lblAddressName);
            grpWithdraw.Controls.Add(cmbSavedAddresses);
            grpWithdraw.Controls.Add(lblSavedAddress);
            grpWithdraw.Controls.Add(txtAddress);
            grpWithdraw.Controls.Add(lblAddress);
            grpWithdraw.Controls.Add(txtAmount);
            grpWithdraw.Controls.Add(lblAmount);
            grpWithdraw.Controls.Add(lblWithdrawFee);
            grpWithdraw.Controls.Add(btnWithdrawAddressList);
            grpWithdraw.Controls.Add(btnCheckWithdrawSupport);
            grpWithdraw.Controls.Add(btnCheckChain);
            grpWithdraw.Controls.Add(cmbNetwork);
            grpWithdraw.Controls.Add(lblNetwork);
            grpWithdraw.Controls.Add(txtCoin);
            grpWithdraw.Controls.Add(lblCoin);
            grpWithdraw.Controls.Add(lblSimpleBalance);
            grpWithdraw.Controls.Add(btnSimpleBalance);
            grpWithdraw.Controls.Add(btnRefreshBalance);
            grpWithdraw.Location = new Point(12, 188);
            grpWithdraw.Margin = new Padding(3, 4, 3, 4);
            grpWithdraw.Name = "grpWithdraw";
            grpWithdraw.Padding = new Padding(3, 4, 3, 4);
            grpWithdraw.Size = new Size(564, 450);
            grpWithdraw.TabIndex = 1;
            grpWithdraw.TabStop = false;
            grpWithdraw.Text = "출금";
            // 
            // btnWithdraw
            // 
            btnWithdraw.BackColor = Color.FromArgb(240, 185, 11);
            btnWithdraw.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            btnWithdraw.Location = new Point(300, 402);
            btnWithdraw.Margin = new Padding(3, 4, 3, 4);
            btnWithdraw.Name = "btnWithdraw";
            btnWithdraw.Size = new Size(150, 38);
            btnWithdraw.TabIndex = 19;
            btnWithdraw.Text = "출금 실행";
            btnWithdraw.UseVisualStyleBackColor = false;
            btnWithdraw.Click += BtnWithdraw_Click;
            // 
            // lblTravelRuleExchange
            // 
            lblTravelRuleExchange.AutoSize = true;
            lblTravelRuleExchange.Location = new Point(15, 211);
            lblTravelRuleExchange.Name = "lblTravelRuleExchange";
            lblTravelRuleExchange.Size = new Size(115, 15);
            lblTravelRuleExchange.TabIndex = 17;
            lblTravelRuleExchange.Text = "출금 거래소명(영문)";
            // 
            // txtTravelRuleExchange
            // 
            txtTravelRuleExchange.Location = new Point(148, 208);
            txtTravelRuleExchange.Name = "txtTravelRuleExchange";
            txtTravelRuleExchange.PlaceholderText = "예: Binance, Upbit";
            txtTravelRuleExchange.Size = new Size(180, 23);
            txtTravelRuleExchange.TabIndex = 18;
            // 
            // lblTravelRuleWalletType
            // 
            lblTravelRuleWalletType.AutoSize = true;
            lblTravelRuleWalletType.Location = new Point(338, 211);
            lblTravelRuleWalletType.Name = "lblTravelRuleWalletType";
            lblTravelRuleWalletType.Size = new Size(71, 15);
            lblTravelRuleWalletType.TabIndex = 25;
            lblTravelRuleWalletType.Text = "수취처 지갑";
            // 
            // cmbTravelRuleWalletType
            // 
            cmbTravelRuleWalletType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTravelRuleWalletType.FormattingEnabled = true;
            cmbTravelRuleWalletType.Items.AddRange(new object[] { "거래소 지갑", "개인 지갑" });
            cmbTravelRuleWalletType.Location = new Point(415, 208);
            cmbTravelRuleWalletType.Name = "cmbTravelRuleWalletType";
            cmbTravelRuleWalletType.Size = new Size(95, 23);
            cmbTravelRuleWalletType.TabIndex = 26;
            // 
            // lblTravelRuleReceiverType
            // 
            lblTravelRuleReceiverType.AutoSize = true;
            lblTravelRuleReceiverType.Location = new Point(15, 238);
            lblTravelRuleReceiverType.Name = "lblTravelRuleReceiverType";
            lblTravelRuleReceiverType.Size = new Size(71, 15);
            lblTravelRuleReceiverType.TabIndex = 19;
            lblTravelRuleReceiverType.Text = "수취인 구분";
            // 
            // cmbTravelRuleReceiverType
            // 
            cmbTravelRuleReceiverType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTravelRuleReceiverType.FormattingEnabled = true;
            cmbTravelRuleReceiverType.Items.AddRange(new object[] { "personal", "corporation" });
            cmbTravelRuleReceiverType.Location = new Point(148, 235);
            cmbTravelRuleReceiverType.Name = "cmbTravelRuleReceiverType";
            cmbTravelRuleReceiverType.Size = new Size(100, 23);
            cmbTravelRuleReceiverType.TabIndex = 20;
            cmbTravelRuleReceiverType.SelectedIndexChanged += CmbTravelRuleReceiverType_SelectedIndexChanged;
            // 
            // lblTravelRuleReceiverName
            // 
            lblTravelRuleReceiverName.AutoSize = true;
            lblTravelRuleReceiverName.Location = new Point(15, 267);
            lblTravelRuleReceiverName.Name = "lblTravelRuleReceiverName";
            lblTravelRuleReceiverName.Size = new Size(83, 15);
            lblTravelRuleReceiverName.TabIndex = 21;
            lblTravelRuleReceiverName.Text = "수취인 국문명";
            // 
            // txtTravelRuleReceiverName
            // 
            txtTravelRuleReceiverName.Location = new Point(148, 262);
            txtTravelRuleReceiverName.Name = "txtTravelRuleReceiverName";
            txtTravelRuleReceiverName.PlaceholderText = "예: 홍길동";
            txtTravelRuleReceiverName.Size = new Size(250, 23);
            txtTravelRuleReceiverName.TabIndex = 22;
            // 
            // lblTravelRuleReceiverNameEn
            // 
            lblTravelRuleReceiverNameEn.AutoSize = true;
            lblTravelRuleReceiverNameEn.Location = new Point(15, 294);
            lblTravelRuleReceiverNameEn.Name = "lblTravelRuleReceiverNameEn";
            lblTravelRuleReceiverNameEn.Size = new Size(83, 15);
            lblTravelRuleReceiverNameEn.TabIndex = 23;
            lblTravelRuleReceiverNameEn.Text = "수취인 영문명";
            // 
            // txtTravelRuleReceiverNameEn
            // 
            txtTravelRuleReceiverNameEn.Location = new Point(148, 291);
            txtTravelRuleReceiverNameEn.Name = "txtTravelRuleReceiverNameEn";
            txtTravelRuleReceiverNameEn.PlaceholderText = "예: HONG GILDONG";
            txtTravelRuleReceiverNameEn.Size = new Size(250, 23);
            txtTravelRuleReceiverNameEn.TabIndex = 24;
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
            btnCheckTravelRule.Location = new Point(348, 169);
            btnCheckTravelRule.Name = "btnCheckTravelRule";
            btnCheckTravelRule.Size = new Size(102, 28);
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
            txtTag.Size = new Size(220, 23);
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
            txtAddress.Size = new Size(352, 23);
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
            txtAmount.Location = new Point(62, 365);
            txtAmount.Margin = new Padding(3, 4, 3, 4);
            txtAmount.Name = "txtAmount";
            txtAmount.Size = new Size(101, 23);
            txtAmount.TabIndex = 7;
            // 
            // lblAmount
            // 
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(15, 368);
            lblAmount.Name = "lblAmount";
            lblAmount.Size = new Size(31, 15);
            lblAmount.TabIndex = 6;
            lblAmount.Text = "수량";
            // 
            // lblWithdrawFee
            // 
            lblWithdrawFee.AutoSize = true;
            lblWithdrawFee.Location = new Point(15, 392);
            lblWithdrawFee.Name = "lblWithdrawFee";
            lblWithdrawFee.Size = new Size(55, 15);
            lblWithdrawFee.TabIndex = 32;
            lblWithdrawFee.Text = "수수료: -";
            // 
            // btnWithdrawAddressList
            // 
            btnWithdrawAddressList.Location = new Point(348, 31);
            btnWithdrawAddressList.Name = "btnWithdrawAddressList";
            btnWithdrawAddressList.Size = new Size(140, 28);
            btnWithdrawAddressList.TabIndex = 22;
            btnWithdrawAddressList.Text = "출금 허용 주소 리스트";
            btnWithdrawAddressList.UseVisualStyleBackColor = true;
            btnWithdrawAddressList.Click += BtnWithdrawAddressList_Click;
            // 
            // btnCheckWithdrawSupport
            // 
            btnCheckWithdrawSupport.Location = new Point(454, 66);
            btnCheckWithdrawSupport.Name = "btnCheckWithdrawSupport";
            btnCheckWithdrawSupport.Size = new Size(110, 28);
            btnCheckWithdrawSupport.TabIndex = 21;
            btnCheckWithdrawSupport.Text = "출금 지원 확인";
            btnCheckWithdrawSupport.UseVisualStyleBackColor = true;
            btnCheckWithdrawSupport.Click += BtnCheckWithdrawSupport_Click;
            // 
            // btnCheckChain
            // 
            btnCheckChain.Location = new Point(348, 66);
            btnCheckChain.Name = "btnCheckChain";
            btnCheckChain.Size = new Size(100, 28);
            btnCheckChain.TabIndex = 20;
            btnCheckChain.Text = "체인 조회";
            btnCheckChain.UseVisualStyleBackColor = true;
            btnCheckChain.Click += BtnCheckChain_Click;
            // 
            // cmbNetwork
            // 
            cmbNetwork.Location = new Point(120, 68);
            cmbNetwork.Margin = new Padding(3, 4, 3, 4);
            cmbNetwork.Name = "cmbNetwork";
            cmbNetwork.Size = new Size(220, 23);
            cmbNetwork.TabIndex = 5;
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
            txtCoin.Size = new Size(220, 23);
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
            btnRefreshBalance.Location = new Point(169, 360);
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
            txtLog.Location = new Point(582, 15);
            txtLog.Margin = new Padding(3, 4, 3, 4);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(362, 623);
            txtLog.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(956, 654);
            Controls.Add(txtLog);
            Controls.Add(grpWithdraw);
            Controls.Add(grpApiSetting);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(600, 553);
            Name = "Form1";
            Text = "CoinTransfer - 출금 봇";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            grpApiSetting.ResumeLayout(false);
            grpApiSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numReservedWithdrawCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRetryInterval).EndInit();
            grpWithdraw.ResumeLayout(false);
            grpWithdraw.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox grpApiSetting;
        private System.Windows.Forms.ComboBox cmbExchange;
        private System.Windows.Forms.Label lblExchange;
        private System.Windows.Forms.Button btnSaveApiKey;
        private System.Windows.Forms.TextBox txtBinanceSecretKey;
        private System.Windows.Forms.TextBox txtBinanceAccessKey;
        private System.Windows.Forms.Label lblPassphrase;
        private System.Windows.Forms.TextBox txtPassphrase;
        private System.Windows.Forms.Label lblSecretKey;
        private System.Windows.Forms.Label lblAccessKey;
        private System.Windows.Forms.Button btnExportApiKey;
        private System.Windows.Forms.Button btnImportApiKey;
        private System.Windows.Forms.GroupBox grpWithdraw;
        private System.Windows.Forms.TextBox txtCoin;
        private System.Windows.Forms.Label lblCoin;
        private System.Windows.Forms.ComboBox cmbNetwork;
        private System.Windows.Forms.Label lblNetwork;
        private System.Windows.Forms.TextBox txtAmount;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.Label lblWithdrawFee;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.TextBox txtTag;
        private System.Windows.Forms.Label lblTag;
        private System.Windows.Forms.Button btnRefreshBalance;
        private System.Windows.Forms.Button btnWithdraw;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnCheckTravelRule;
        private System.Windows.Forms.Label lblTravelRuleStatus;
        private System.Windows.Forms.Label lblTravelRuleExchange;
        private System.Windows.Forms.TextBox txtTravelRuleExchange;
        private System.Windows.Forms.Label lblTravelRuleWalletType;
        private System.Windows.Forms.ComboBox cmbTravelRuleWalletType;
        private System.Windows.Forms.Label lblTravelRuleReceiverType;
        private System.Windows.Forms.ComboBox cmbTravelRuleReceiverType;
        private System.Windows.Forms.Label lblTravelRuleReceiverName;
        private System.Windows.Forms.TextBox txtTravelRuleReceiverName;
        private System.Windows.Forms.Label lblTravelRuleReceiverNameEn;
        private System.Windows.Forms.TextBox txtTravelRuleReceiverNameEn;
        private System.Windows.Forms.CheckBox chkReservedWithdraw;
        private System.Windows.Forms.Label lblReservedWithdrawCount;
        private System.Windows.Forms.NumericUpDown numReservedWithdrawCount;
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
        private System.Windows.Forms.Button btnRemoveAddress;
        private System.Windows.Forms.Button btnSimpleBalance;
        private System.Windows.Forms.Label lblSimpleBalance;
        private System.Windows.Forms.Button btnCheckChain;
        private System.Windows.Forms.Button btnCheckWithdrawSupport;
        private System.Windows.Forms.Button btnWithdrawAddressList;
    }
}
