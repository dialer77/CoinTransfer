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

            txtBinanceAccessKey.Text = ApikeySetting.GetInstance().GetExchangeAccessKey(EnumExchange.Binance);
            txtBinanceSecretKey.Text = ApikeySetting.GetInstance().GetExchangeSecretKey(EnumExchange.Binance);

            LoadSavedAddresses();

            AppendLog("프로그램 시작. Binance API 키를 입력하고 저장 후 잔고를 새로고침하세요.");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_reservedWithdrawCts?.Cancel();
        }

        private void BtnSaveApiKey_Click(object sender, EventArgs e)
        {
            ApikeySetting.GetInstance().SetExchangeAccessKey(EnumExchange.Binance, txtBinanceAccessKey.Text.Trim());
            ApikeySetting.GetInstance().SetExchangeSecretKey(EnumExchange.Binance, txtBinanceSecretKey.Text.Trim());
            ApikeySetting.GetInstance().SaveSetting();
            AppendLog("Binance API 키가 저장되었습니다.");
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
                var data = new
                {
                    Binance = new { AccessKey = txtBinanceAccessKey.Text.Trim(), SecretKey = txtBinanceSecretKey.Text.Trim() },
                    ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
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
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                if (data?.Binance != null)
                {
                    txtBinanceAccessKey.Text = (string)data.Binance.AccessKey ?? "";
                    txtBinanceSecretKey.Text = (string)data.Binance.SecretKey ?? "";
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

        private string GetCoinText() => txtCoin.Text?.Trim() ?? "";

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
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(EnumExchange.Binance);
            if (controller is not ExchangeAPIControllerBinance binance)
            {
                lblTravelRuleStatus.Text = "Travel Rule: 해당 없음";
                return;
            }

            lblTravelRuleStatus.Text = "Travel Rule: 확인 중...";
            try
            {
                var (required, info) = await binance.CheckTravelRuleRequiredAsync();
                if (required)
                {
                    lblTravelRuleStatus.Text = $"Travel Rule: 필요 ({info})";
                    lblTravelRuleStatus.ForeColor = Color.DarkOrange;
                    AppendLog($"Travel Rule 필요 - {info}. Questionnaire JSON을 입력해 주세요.");
                }
                else
                {
                    lblTravelRuleStatus.Text = "Travel Rule: 불필요";
                    lblTravelRuleStatus.ForeColor = Color.Green;
                }
            }
            catch
            {
                lblTravelRuleStatus.Text = "Travel Rule: 확인 실패";
                lblTravelRuleStatus.ForeColor = Color.Gray;
            }
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

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(EnumExchange.Binance);
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
        }

        private async void BtnRefreshBalance_Click(object sender, EventArgs e)
        {
            if (m_isRefreshing) return;
            m_isRefreshing = true;
            btnRefreshBalance.Enabled = false;

            AppendLog("Binance 잔고 조회 중...");

            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(EnumExchange.Binance);
            var (ok, currencies) = await controller.GetCoinHoldingForMyAccount();

            if (ok && currencies != null)
            {
                m_binanceBalances = currencies;
                UpdateBalanceDisplay();
                int nonZeroCount = currencies.Count(c => c.Balance > 0 || c.Locked > 0);
                AppendLog($"잔고 조회 완료. {nonZeroCount}개 코인");
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
            var networkStr = txtNetwork.Text?.Trim() ?? "";
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
            var tag = string.IsNullOrWhiteSpace(txtTag.Text) ? null : txtTag.Text.Trim();

            var result = MessageBox.Show(
                $"[출금 확인]\n코인: {coin}\n네트워크: {chainName}\n수량: {amount}\n주소: {address}\n\n출금을 실행하시겠습니까?",
                "출금 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            m_isWithdrawing = true;
            btnWithdraw.Enabled = false;
            btnCancelReserved.Enabled = false;

            var questionnaire = txtQuestionnaire.Text?.Trim();
            var controller = ExchangeApiManager.GetInstance().GetExchangeAPIController(EnumExchange.Binance);
            bool useReservedWithdraw = chkReservedWithdraw.Checked;
            DateTime deadline = dtpDeadline.Value;
            int retryIntervalSeconds = (int)numRetryInterval.Value;
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
                    AppendLog($"출금 요청 중... {coin} {amount} → {address}" + (attempt > 1 ? $" (예약 출금 {attempt}회차)" : ""));

                    (success, msg) = await controller.WithdrawCoin(coin, chainName, amount, address, "", questionnaire ?? "", tag);

                    if (success)
                    {
                        AppendLog($"출금 요청 접수됨! ID: {msg}");
                        AppendLog("※ 이메일/2FA 확인이 필요한 경우, 등록된 이메일 또는 2FA 앱에서 출금을 승인해 주세요.");
                        MessageBox.Show(
                            $"출금 요청이 접수되었습니다.\n\n출금 ID: {msg}\n\n이메일 또는 2FA 확인이 필요한 경우, Binance에서 안내하는 대로 확인을 완료해 주세요.",
                            "출금 접수",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        break;
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
                        AppendLog($"마감시간({deadline:HH:mm}) 도달. 예약 출금을 종료합니다.");
                        MessageBox.Show($"출금 실패 (마감시간 경과):\n{msg}", "예약 출금 종료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }

                    AppendLog($"{retryIntervalSeconds}초 후 재시도 예정 (마감: {deadline:HH:mm})...");
                    btnWithdraw.Text = $"재시도 대기 중... ({attempt}회차)";
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
                while (useReservedWithdraw && !success && DateTime.Now < deadline);
            }
            finally
            {
                m_isWithdrawing = false;
                btnWithdraw.Enabled = true;
                btnWithdraw.Text = "출금 실행";
                btnCancelReserved.Enabled = false;
                m_reservedWithdrawCts?.Dispose();
            }
        }

        private void BtnCancelReserved_Click(object sender, EventArgs e)
        {
            m_reservedWithdrawCts?.Cancel();
        }

        private void ChkReservedWithdraw_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkReservedWithdraw.Checked;
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
