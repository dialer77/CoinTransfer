using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExchangeAPIController;
using Newtonsoft.Json;

namespace CoinTransfer
{
    public partial class Form1 : Form
    {
        private List<Currency> m_binanceBalances = new List<Currency>();
        private bool m_isRefreshing = false;
        private bool m_isWithdrawing = false;
        private CancellationTokenSource m_reservedWithdrawCts;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ApiRequestLogger.OnLog = (msg) => AppendLog(msg);

            ApikeySetting.GetInstance().LoadSetting();

            cmbExchange.BeginUpdate();
            cmbExchange.Items.Clear();
            foreach (EnumExchange ex in Enum.GetValues(typeof(EnumExchange)))
            {
                if (ex == EnumExchange.Debug) continue;
                cmbExchange.Items.Add(ex);
            }
            cmbExchange.EndUpdate();
            if (cmbExchange.Items.Count > 0)
                cmbExchange.SelectedIndex = 0;

            LoadApiKeysForSelectedExchange();
            UpdateGroupTitleForExchange();
            UpdateExchangeSpecificUIState();
            LoadSavedAddresses();
            if (cmbTravelRuleReceiverType.Items.Count > 0 && cmbTravelRuleReceiverType.SelectedIndex < 0)
                cmbTravelRuleReceiverType.SelectedIndex = 0;
            if (cmbTravelRuleWalletType.Items.Count > 0 && cmbTravelRuleWalletType.SelectedIndex < 0)
                cmbTravelRuleWalletType.SelectedIndex = 1; // 기본: 개인 지갑
            UpdateTravelRuleNameLabels();

            AppendLog("프로그램 시작. 거래소를 선택하고 API 키를 입력한 뒤 저장 후 잔고를 새로고침하세요.");
        }

        private EnumExchange GetSelectedExchange()
        {
            if (cmbExchange?.SelectedItem is EnumExchange ex)
                return ex;
            return EnumExchange.Binance;
        }

        private void LoadApiKeysForSelectedExchange()
        {
            var exchange = GetSelectedExchange();
            txtBinanceAccessKey.Text = ApikeySetting.GetInstance().GetExchangeAccessKey(exchange);
            txtBinanceSecretKey.Text = ApikeySetting.GetInstance().GetExchangeSecretKey(exchange);
            txtPassphrase.Text = ApikeySetting.GetInstance().GetExchangePassphrase(exchange);
        }

        private void UpdateGroupTitleForExchange()
        {
            grpApiSetting.Text = $"{GetSelectedExchange()} API 설정";
        }

        private void CmbExchange_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadApiKeysForSelectedExchange();
            UpdateGroupTitleForExchange();
            UpdateExchangeSpecificUIState();
        }

        /// <summary>
        /// 거래소별로 해당 거래소에서만 쓰는 UI만 활성화하고 나머지는 비활성화해 혼선을 줄임.
        /// </summary>
        private void UpdateExchangeSpecificUIState()
        {
            var ex = GetSelectedExchange();

            // 체인 조회: 빗썸은 체인 목록 API 미지원
            btnCheckChain.Enabled = ex != EnumExchange.Bithumb;

            // 출금 허용 주소 리스트: 빗썸 전용
            btnWithdrawAddressList.Enabled = ex == EnumExchange.Bithumb;

            // Travel Rule/수취인 정보: 빗썸·OKX만 사용
            bool travelRuleUsed = (ex == EnumExchange.Bithumb || ex == EnumExchange.OKX);
            lblTravelRuleExchange.Enabled = travelRuleUsed;
            txtTravelRuleExchange.Enabled = travelRuleUsed;
            lblTravelRuleReceiverType.Enabled = travelRuleUsed;
            cmbTravelRuleReceiverType.Enabled = travelRuleUsed;
            lblTravelRuleReceiverName.Enabled = travelRuleUsed;
            txtTravelRuleReceiverName.Enabled = travelRuleUsed;
            lblTravelRuleReceiverNameEn.Enabled = travelRuleUsed;
            txtTravelRuleReceiverNameEn.Enabled = travelRuleUsed;
            lblTravelRuleStatus.Enabled = travelRuleUsed;
            btnCheckTravelRule.Enabled = travelRuleUsed;

            // 수취처 지갑(거래소/개인): OKX 전용
            bool walletTypeUsed = (ex == EnumExchange.OKX);
            lblTravelRuleWalletType.Enabled = walletTypeUsed;
            cmbTravelRuleWalletType.Enabled = walletTypeUsed;

            // Passphrase: OKX, Bitget만 사용
            bool passphraseUsed = (ex == EnumExchange.OKX || ex == EnumExchange.Bitget);
            lblPassphrase.Enabled = passphraseUsed;
            txtPassphrase.Enabled = passphraseUsed;
        }

        /// <summary>
        /// 빗썸은 체인 목록 API를 지원하지 않으므로 체인 조회 버튼 비활성화.
        /// </summary>
        private void UpdateChainButtonState()
        {
            btnCheckChain.Enabled = GetSelectedExchange() != EnumExchange.Bithumb;
        }

        /// <summary>
        /// 출금 허용 주소 리스트 조회 버튼은 빗썸에서만 활성화.
        /// </summary>
        private void UpdateWithdrawAddressListButtonState()
        {
            btnWithdrawAddressList.Enabled = GetSelectedExchange() == EnumExchange.Bithumb;
        }

        private void CmbTravelRuleReceiverType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTravelRuleNameLabels();
        }

        private void UpdateTravelRuleNameLabels()
        {
            bool isCorp = string.Equals(cmbTravelRuleReceiverType.SelectedItem?.ToString(), "corporation", StringComparison.OrdinalIgnoreCase);
            if (isCorp)
            {
                lblTravelRuleReceiverName.Text = "법인 대표자 국문명";
                lblTravelRuleReceiverNameEn.Text = "법인 대표자 영문명";
                txtTravelRuleReceiverName.PlaceholderText = "대표자 국문명 (법인명도 동일 값 전송)";
                txtTravelRuleReceiverNameEn.PlaceholderText = "대표자 영문명 (법인명도 동일 값 전송)";
            }
            else
            {
                lblTravelRuleReceiverName.Text = "수취인 국문명";
                lblTravelRuleReceiverNameEn.Text = "수취인 영문명";
                txtTravelRuleReceiverName.PlaceholderText = "예: 홍길동";
                txtTravelRuleReceiverNameEn.PlaceholderText = "예: HONG GILDONG";
            }
        }

        /// <summary>
        /// 트래블룰 개별 칸을 JSON으로 조합. 개인/법인 구분에 따라 같은 2개 필드(국문명, 영문명)를 receiver_ko_name/receiver_en_name 및 법인 시 receiver_corp_ko_name/receiver_corp_en_name에 사용.
        /// </summary>
        private string BuildTravelRuleQuestionnaire()
        {
            string exchangeName = txtTravelRuleExchange.Text?.Trim();
            string walletTypeDisplay = cmbTravelRuleWalletType.SelectedItem?.ToString()?.Trim();
            string receiverType = cmbTravelRuleReceiverType.SelectedItem?.ToString()?.Trim();
            string receiverKoName = txtTravelRuleReceiverName.Text?.Trim();
            string receiverEnName = txtTravelRuleReceiverNameEn.Text?.Trim();
            bool isCorp = string.Equals(receiverType, "corporation", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(exchangeName) && string.IsNullOrEmpty(walletTypeDisplay) && string.IsNullOrEmpty(receiverType) && string.IsNullOrEmpty(receiverKoName) && string.IsNullOrEmpty(receiverEnName))
                return "";
            var obj = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(walletTypeDisplay))
                obj["wallet_type"] = string.Equals(walletTypeDisplay, "거래소 지갑", StringComparison.Ordinal) ? "exchange" : "private";
            if (!string.IsNullOrEmpty(exchangeName)) obj["exchange_name"] = exchangeName;
            if (!string.IsNullOrEmpty(receiverType)) obj["receiver_type"] = receiverType;
            if (!string.IsNullOrEmpty(receiverKoName)) obj["receiver_ko_name"] = receiverKoName;
            if (!string.IsNullOrEmpty(receiverEnName)) obj["receiver_en_name"] = receiverEnName;
            if (isCorp)
            {
                if (!string.IsNullOrEmpty(receiverKoName)) obj["receiver_corp_ko_name"] = receiverKoName;
                if (!string.IsNullOrEmpty(receiverEnName)) obj["receiver_corp_en_name"] = receiverEnName;
            }
            return obj.Count == 0 ? "" : JsonConvert.SerializeObject(obj);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void BtnSaveApiKey_Click(object sender, EventArgs e)
        {
            var exchange = GetSelectedExchange();
            ApikeySetting.GetInstance().SetExchangeAccessKey(exchange, txtBinanceAccessKey.Text.Trim());
            ApikeySetting.GetInstance().SetExchangeSecretKey(exchange, txtBinanceSecretKey.Text.Trim());
            ApikeySetting.GetInstance().SetExchangePassphrase(exchange, txtPassphrase.Text.Trim());
            ApikeySetting.GetInstance().SaveSetting();
            AppendLog($"{exchange} API 키가 저장되었습니다.");
        }

        private void BtnExportApiKey_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "JSON 파일|*.json|모든 파일|*.*",
                FileName = "api_key_backup.json",
                Title = "API 키 내보내기"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var exchange = GetSelectedExchange();
                var data = new Dictionary<string, object>
                {
                    { "Exchange", exchange.ToString() },
                    { exchange.ToString(), new { AccessKey = txtBinanceAccessKey.Text.Trim(), SecretKey = txtBinanceSecretKey.Text.Trim(), Passphrase = txtPassphrase.Text.Trim() } },
                    { "ExportedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };
                File.WriteAllText(dlg.FileName, JsonConvert.SerializeObject(data, Formatting.Indented));
                AppendLog($"API 키가 저장되었습니다: {dlg.FileName}");
                MessageBox.Show("API 키 내보내기가 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"API 키 내보내기 실패: {ex.Message}");
                MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImportApiKey_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "JSON 파일|*.json|모든 파일|*.*",
                Title = "API 키 가져오기"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
                string accessKey = "";
                string secretKey = "";
                string passphrase = "";
                if (jobj["Binance"] is Newtonsoft.Json.Linq.JObject binance)
                {
                    accessKey = binance["AccessKey"]?.ToString() ?? "";
                    secretKey = binance["SecretKey"]?.ToString() ?? "";
                    passphrase = binance["Passphrase"]?.ToString() ?? "";
                }
                string exchangeInFile = jobj["Exchange"]?.ToString();
                if (!string.IsNullOrEmpty(exchangeInFile) && jobj[exchangeInFile] is Newtonsoft.Json.Linq.JObject exObj)
                {
                    accessKey = exObj["AccessKey"]?.ToString() ?? "";
                    secretKey = exObj["SecretKey"]?.ToString() ?? "";
                    passphrase = exObj["Passphrase"]?.ToString() ?? "";
                }
                if (!string.IsNullOrEmpty(accessKey) || !string.IsNullOrEmpty(secretKey))
                {
                    txtBinanceAccessKey.Text = accessKey;
                    txtBinanceSecretKey.Text = secretKey;
                    txtPassphrase.Text = passphrase;
                    AppendLog($"API 키를 불러왔습니다: {dlg.FileName}");
                    MessageBox.Show("API 키 가져오기가 완료되었습니다. '저장' 버튼을 눌러 config.ini에 반영하세요.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("지원하는 형식의 파일이 아닙니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"API 키 가져오기 실패: {ex.Message}");
                MessageBox.Show($"불러오기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSavedAddresses()
        {
            cmbSavedAddresses.Items.Clear();
            cmbSavedAddresses.DisplayMember = "Key";
            var addrs = AddressStorage.Load();
            foreach (var a in addrs)
            {
                cmbSavedAddresses.Items.Add(a);
            }
            if (cmbSavedAddresses.Items.Count > 0)
                cmbSavedAddresses.SelectedIndex = 0;
        }

        private void CmbSavedAddresses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSavedAddresses.SelectedItem is AddressStorage.SavedAddress addr && !string.IsNullOrEmpty(addr.Address))
            {
                txtAddress.Text = addr.Address;
                txtTag.Text = addr.Tag ?? "";
            }
        }

        private void BtnSaveAddress_Click(object sender, EventArgs e)
        {
            var key = txtAddressName.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("저장 이름(Key)을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var address = txtAddress.Text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                MessageBox.Show("저장할 주소를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                AddressStorage.Add(key, address, txtTag.Text?.Trim() ?? "");
                LoadSavedAddresses();
                AppendLog($"주소 저장됨: [{key}] {address}");
                MessageBox.Show("주소가 저장되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveAddress_Click(object sender, EventArgs e)
        {
            if (cmbSavedAddresses.SelectedItem is not AddressStorage.SavedAddress addr)
            {
                MessageBox.Show("삭제할 주소를 콤보박스에서 선택하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var key = addr.Key;
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("선택한 항목에 저장 이름이 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var result = MessageBox.Show($"저장된 주소 \"{key}\"을(를) 삭제하시겠습니까?", "주소 삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            try
            {
                AddressStorage.Remove(key);
                LoadSavedAddresses();
                if (txtAddress.Text == addr.Address && (string.IsNullOrEmpty(addr.Tag) || txtTag.Text == addr.Tag))
                {
                    txtAddress.Clear();
                    txtTag.Clear();
                }
                AppendLog($"주소 삭제됨: [{key}]");
                MessageBox.Show("선택한 주소가 삭제되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetCoinText() => txtCoin.Text?.Trim() ?? "";

        /// <summary>
        /// 코인·네트워크·수량에 따라 출금 수수료를 조회해 lblWithdrawFee에 표시.
        /// </summary>
        private void UpdateWithdrawFeeLabel()
        {
            if (lblWithdrawFee == null) return;
            string coin = GetCoinText();
            string chainName = cmbNetwork?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(coin))
            {
                lblWithdrawFee.Text = "수수료: -";
                return;
            }
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());
            var (ok, networks) = controller.GetCoinNetworksDetail(coin);
            if (!ok || networks == null || networks.Count == 0)
            {
                lblWithdrawFee.Text = "수수료: -";
                return;
            }
            var network = networks.FirstOrDefault(n => string.Equals(n.ChainName, chainName, StringComparison.OrdinalIgnoreCase))
                ?? networks.FirstOrDefault(n => n.WithdrawEnabled) ?? networks[0];
            double amount = 0;
            double.TryParse(txtAmount?.Text?.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount);
            decimal feeFixed = network.WithdrawFee;
            decimal feePct = network.WithdrawPercentageFee;
            decimal totalFee = feeFixed + (decimal)(amount * (double)feePct / 100.0);
            string coinUpper = coin.ToUpperInvariant();
            if (totalFee == 0 && feeFixed == 0 && feePct == 0)
                lblWithdrawFee.Text = "수수료: 0 " + coinUpper;
            else if (feePct != 0)
                lblWithdrawFee.Text = $"수수료: {totalFee:G29} {coinUpper} (고정 {feeFixed} + 비율 {feePct}%)";
            else
                lblWithdrawFee.Text = $"수수료: {totalFee:G29} {coinUpper}";
        }

        private void UpdateBalanceDisplay()
        {
            var coin = GetCoinText();
            if (string.IsNullOrEmpty(coin))
            {
                return;
            }
            var cur = m_binanceBalances.FirstOrDefault(c => string.Equals(c.CurrencyCode, coin, StringComparison.OrdinalIgnoreCase));
            if (cur != null)
                txtAmount.Text = cur.Balance.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
            else
                txtAmount.Text = "0";
        }

        private async void BtnCheckTravelRule_Click(object sender, EventArgs e)
        {
            var exchange = GetSelectedExchange();
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(exchange);
            lblTravelRuleStatus.Text = "Travel Rule: 확인 중...";
            lblTravelRuleStatus.ForeColor = Color.Gray;
            AppendLog($"[Travel Rule] {exchange} 확인 중...");
            try
            {
                var (required, info) = await controller.CheckTravelRuleRequiredAsync();
                AppendLog($"[Travel Rule] 결과: required={required}, info=\"{info}\"");
                if (!string.IsNullOrEmpty(info) && !required)
                {
                    lblTravelRuleStatus.Text = $"Travel Rule: 해당 없음 ({info})";
                    lblTravelRuleStatus.ForeColor = Color.Gray;
                }
                else if (required)
                {
                    lblTravelRuleStatus.Text = $"Travel Rule: 필요 ({info})";
                    lblTravelRuleStatus.ForeColor = Color.DarkOrange;
                    AppendLog($"Travel Rule 필요 - {info}. Questionnaire(또는 수취인 정보) 입력 후 출금 가능.");
                }
                else
                {
                    lblTravelRuleStatus.Text = "Travel Rule: 불필요";
                    lblTravelRuleStatus.ForeColor = Color.Green;
                    AppendLog("[Travel Rule] 해당 거래소 기준 불필요.");
                }
            }
            catch (Exception ex)
            {
                lblTravelRuleStatus.Text = "Travel Rule: 확인 실패";
                lblTravelRuleStatus.ForeColor = Color.Gray;
                AppendLog($"[Travel Rule] 확인 실패: {ex.Message}");
            }
        }

        private void BtnCheckChain_Click(object sender, EventArgs e)
        {
            var coin = GetCoinText();
            if (string.IsNullOrEmpty(coin))
            {
                MessageBox.Show("코인을 먼저 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());
            var (ok, networks) = controller.GetCoinNetworksDetail(coin);
            if (ok && networks != null && networks.Count > 0)
            {
                cmbNetwork.Items.Clear();
                string firstWithdrawChain = null;
                foreach (var n in networks)
                {
                    cmbNetwork.Items.Add(n.ChainName);
                    if (n.WithdrawEnabled && firstWithdrawChain == null)
                        firstWithdrawChain = n.ChainName;
                }
                if (!string.IsNullOrEmpty(firstWithdrawChain))
                    cmbNetwork.Text = firstWithdrawChain;
                else
                    cmbNetwork.SelectedIndex = 0;
                AppendLog($"[체인 조회] {coin}: {networks.Count}개 - {string.Join(", ", networks.Select(x => x.ChainName))}");
                MessageBox.Show($"[{coin}] 체인 목록을 불러왔습니다. 드롭다운에서 선택하거나 직접 입력하세요.", "체인 조회", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string err = controller.GetLastErrorMessage();
                if (string.IsNullOrEmpty(err)) err = "지원하지 않거나 조회 실패";
                AppendLog($"[체인 조회] {coin} 실패: {err}");
                MessageBox.Show($"체인 조회 실패: {err}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCheckWithdrawSupport_Click(object sender, EventArgs e)
        {
            var coin = GetCoinText();
            var netName = cmbNetwork.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(coin))
            {
                MessageBox.Show("코인을 먼저 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(netName))
            {
                MessageBox.Show("네트워크를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());
            AppendLog($"[출금 지원 확인] {GetSelectedExchange()} | 코인={coin}, 네트워크={netName}");
            var (supported, message) = controller.ValidateWithdrawSupport(coin, netName);

            if (supported)
            {
                AppendLog("[출금 지원 확인] 해당 코인·네트워크로 출금이 지원됩니다.");
                MessageBox.Show("해당 코인·네트워크로 출금이 지원됩니다.", "출금 지원 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AppendLog($"[출금 지원 확인] 미지원 또는 오류: {message}");
                MessageBox.Show($"출금 지원 여부 확인 실패:\n{message}", "출금 지원 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnWithdrawAddressList_Click(object sender, EventArgs e)
        {
            if (GetSelectedExchange() != EnumExchange.Bithumb)
                return;

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(EnumExchange.Bithumb);
            AppendLog("[출금 허용 주소] 빗썸 출금 허용 주소 리스트 조회 중...");
            var (ok, list) = controller.GetWithdrawAllowedAddresses();

            if (!ok || list == null)
            {
                string err = controller.GetLastErrorMessage();
                if (string.IsNullOrEmpty(err)) err = "조회 실패";
                AppendLog($"[출금 허용 주소] 실패: {err}");
                MessageBox.Show($"출금 허용 주소 리스트 조회 실패:\n{err}", "출금 허용 주소", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppendLog($"[출금 허용 주소] 총 {list.Count}개");
            foreach (var item in list)
                AppendLog($"  {item}");

            string summary = list.Count == 0
                ? "등록된 출금 허용 주소가 없습니다."
                : $"총 {list.Count}개 등록.\n\n" + string.Join("\n", list.Take(20).Select(x => x.ToString())) + (list.Count > 20 ? $"\n... 외 {list.Count - 20}개 (로그 참조)" : "");
            MessageBox.Show(summary, "출금 허용 주소 리스트 (빗썸)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnSimpleBalance_Click(object sender, EventArgs e)
        {
            var coin = GetCoinText();
            if (string.IsNullOrEmpty(coin))
            {
                MessageBox.Show("코인을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblSimpleBalance.Text = "조회 중...";
            lblSimpleBalance.ForeColor = Color.Gray;

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());
            var (ok, currencies) = await controller.GetCoinHoldingForMyAccount();

            if (ok && currencies != null)
            {
                var cur = currencies.FirstOrDefault(c => string.Equals(c.CurrencyCode, coin, StringComparison.OrdinalIgnoreCase));
                if (cur != null)
                {
                    lblSimpleBalance.Text = $"잔고: {cur.Balance:F8} {cur.CurrencyCode}";
                    lblSimpleBalance.ForeColor = Color.Black;
                }
                else
                {
                    lblSimpleBalance.Text = $"잔고: 0 {coin}";
                    lblSimpleBalance.ForeColor = Color.Gray;
                }
            }
            else
            {
                lblSimpleBalance.Text = $"조회 실패: {controller.GetLastErrorMessage()}";
                lblSimpleBalance.ForeColor = Color.Red;
            }
            UpdateWithdrawFeeLabel();
        }

        private async void BtnRefreshBalance_Click(object sender, EventArgs e)
        {
            if (m_isRefreshing) return;
            m_isRefreshing = true;
            btnRefreshBalance.Enabled = false;

            AppendLog($"{GetSelectedExchange()} 잔고 조회 중...");

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());
            var (ok, currencies) = await controller.GetCoinHoldingForMyAccount();

            if (ok && currencies != null)
            {
                m_binanceBalances = currencies;
                UpdateBalanceDisplay();
                int nonZeroCount = currencies.Count(c => c.Balance > 0 || c.Locked > 0);
                AppendLog($"잔고 조회 완료. {nonZeroCount}개 코인");
                UpdateWithdrawFeeLabel();
            }
            else
            {
                AppendLog($"잔고 조회 실패: {controller.GetLastErrorMessage()}");
            }

            m_isRefreshing = false;
            btnRefreshBalance.Enabled = true;
        }

        private async void BtnWithdraw_Click(object sender, EventArgs e)
        {
            if (m_isWithdrawing) return;

            var coin = GetCoinText();
            var networkStr = cmbNetwork.Text?.Trim() ?? "";
            var amountStr = txtAmount.Text.Trim();
            var address = txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(coin))
            {
                MessageBox.Show("코인을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(networkStr))
            {
                MessageBox.Show("네트워크를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!double.TryParse(amountStr, out double amount) || amount <= 0)
            {
                MessageBox.Show("올바른 수량을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(address))
            {
                MessageBox.Show("출금 주소를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var chainName = networkStr.Trim();
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(GetSelectedExchange());

            // 빗썸 등: 출금 전 해당 코인·네트워크 지원 여부 검증 (coinName, netName 만 사용)
            var (supported, supportMsg) = controller.ValidateWithdrawSupport(coin, chainName);
            if (!supported)
            {
                MessageBox.Show($"출금이 지원되지 않거나 검증에 실패했습니다.\n\n{supportMsg}\n\n코인·네트워크를 확인한 뒤 '출금 지원 확인' 버튼으로 먼저 확인하세요.", "출금 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tag = string.IsNullOrWhiteSpace(txtTag.Text) ? null : txtTag.Text.Trim();
            var questionnaire = BuildTravelRuleQuestionnaire();

            var result = MessageBox.Show(
                $"[출금 확인]\n코인: {coin}\n네트워크: {chainName}\n수량: {amount}\n주소: {address}\n\n출금을 실행하시겠습니까?",
                "출금 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            m_isWithdrawing = true;
            btnWithdraw.Enabled = false;
            btnCancelReserved.Enabled = false;

            AppendLog($"[출금] {GetSelectedExchange()} | 코인={coin}, 네트워크={chainName}, 수량={amount}, 주소={address}" + (string.IsNullOrEmpty(tag) ? "" : $", Tag={tag}"));
            if (!string.IsNullOrEmpty(questionnaire))
                AppendLog($"[출금] Travel Rule/수취인 정보 포함: {questionnaire}");
            if (!string.IsNullOrEmpty(tag))
                AppendLog("[출금] Tag/Memo 포함.");
            bool useReservedWithdraw = chkReservedWithdraw.Checked;
            DateTime deadline = dtpDeadline.Value;
            int retryIntervalSeconds = (int)numRetryInterval.Value;
            int maxSuccesses = (int)numReservedWithdrawCount.Value;
            int successCount = 0;
            int attempt = 0;
            bool success = false;
            string msg = "";

            if (useReservedWithdraw && deadline <= DateTime.Now)
            {
                MessageBox.Show("마감시간은 현재 시각 이후로 설정해 주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                m_isWithdrawing = false;
                btnWithdraw.Enabled = true;
                return;
            }

            m_reservedWithdrawCts = new CancellationTokenSource();

            try
            {
                do
                {
                    attempt++;
                    AppendLog($"출금 요청 중... {coin} {amount} → {address}" + (attempt > 1 ? $" (예약 출금 {attempt}회차, 성공 {successCount}/{maxSuccesses})" : ""));

                    (success, msg) = await controller.WithdrawCoin(coin, chainName, amount, address, "", questionnaire ?? "", tag);
                    AppendLog($"[출금] API 응답: success={success}, msg=\"{msg}\"");

                    if (success)
                    {
                        successCount++;
                        AppendLog($"출금 요청 접수됨! ID: {msg} (성공 {successCount}/{maxSuccesses})");
                        if (successCount >= maxSuccesses)
                        {
                            AppendLog($"예약 출금 완료: 목표 {maxSuccesses}회 모두 성공.");
                            AppendLog("※ 이메일/2FA 확인이 필요한 경우, 등록된 이메일 또는 2FA 앱에서 출금을 승인해 주세요.");
                            MessageBox.Show(
                                $"출금 요청 {maxSuccesses}회 모두 접수되었습니다.\n\n마지막 출금 ID: {msg}\n\n이메일 또는 2FA 확인이 필요한 경우, 해당 거래소에서 안내하는 대로 확인을 완료해 주세요.",
                                "예약 출금 완료",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            break;
                        }
                        AppendLog($"다음 출금까지 {retryIntervalSeconds}초 대기 중... (마감: {deadline:HH:mm})");
                        btnWithdraw.Text = $"대기 중... (성공 {successCount}/{maxSuccesses})";
                        btnCancelReserved.Enabled = true;
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(retryIntervalSeconds), m_reservedWithdrawCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            AppendLog("예약 출금이 취소되었습니다.");
                            break;
                        }
                        continue;
                    }

                    AppendLog($"출금 실패 ({attempt}회차): {msg}");

                    if (!useReservedWithdraw || DateTime.Now >= deadline)
                    {
                        MessageBox.Show($"출금 실패:\n{msg}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    var nextRetry = DateTime.Now.AddSeconds(retryIntervalSeconds);
                    if (nextRetry > deadline)
                    {
                        AppendLog($"마감시간({deadline:HH:mm}) 도달. 예약 출금을 종료합니다. (성공 {successCount}/{maxSuccesses})");
                        MessageBox.Show($"출금 실패 (마감시간 경과). 성공 {successCount}회.\n{msg}", "예약 출금 종료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }

                    AppendLog($"{retryIntervalSeconds}초 후 재시도 예정 (마감: {deadline:HH:mm})...");
                    btnWithdraw.Text = $"재시도 대기 중... (성공 {successCount}/{maxSuccesses})";
                    btnCancelReserved.Enabled = true;

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryIntervalSeconds), m_reservedWithdrawCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        AppendLog("예약 출금이 취소되었습니다.");
                        break;
                    }
                }
                while (useReservedWithdraw && successCount < maxSuccesses && DateTime.Now < deadline);
            }
            finally
            {
                ResetWithdrawUIAfterReserved();
            }
        }

        /// <summary>
        /// 예약 출금 종료/취소 후 버튼·상태를 UI 스레드에서 갱신 (취소 시 스레드 풀에서 돌아올 수 있음).
        /// </summary>
        private void ResetWithdrawUIAfterReserved()
        {
            void Apply()
            {
                m_isWithdrawing = false;
                btnWithdraw.Enabled = true;
                btnWithdraw.Text = "출금 실행";
                btnCancelReserved.Enabled = false;
                btnCancelReserved.Text = "예약 취소";
                m_reservedWithdrawCts?.Dispose();
                m_reservedWithdrawCts = null;
            }
            if (InvokeRequired)
                BeginInvoke(new Action(Apply));
            else
                Apply();
        }

        private void BtnCancelReserved_Click(object sender, EventArgs e)
        {
            if (m_reservedWithdrawCts == null) return;
            AppendLog("예약 취소를 요청했습니다.");
            btnCancelReserved.Enabled = false;
            btnCancelReserved.Text = "취소 중...";
            btnCancelReserved.Refresh();
            m_reservedWithdrawCts.Cancel();
        }

        private void ChkReservedWithdraw_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkReservedWithdraw.Checked;
            lblReservedWithdrawCount.Enabled = enabled;
            numReservedWithdrawCount.Enabled = enabled;
            lblDeadline.Enabled = enabled;
            dtpDeadline.Enabled = enabled;
            lblRetryInterval.Enabled = enabled;
            numRetryInterval.Enabled = enabled;
            if (enabled)
            {
                dtpDeadline.MinDate = DateTime.Now;
                if (dtpDeadline.Value < DateTime.Now)
                    dtpDeadline.Value = DateTime.Now.AddHours(1);
            }
        }

        private void AppendLog(string text)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(text)));
                return;
            }
            var line = $"[{DateTime.Now:HH:mm:ss}] {text}\r\n";
            txtLog.AppendText(line);
        }
    }
}
