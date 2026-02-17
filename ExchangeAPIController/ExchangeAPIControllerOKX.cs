using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    /// <summary>
    /// OKX API v5 컨트롤러 (OK-ACCESS-SIGN: base64(HMAC-SHA256(timestamp+method+path+body, secret)), Passphrase 필요)
    /// </summary>
    public class ExchangeAPIControllerOKX : ExchangeAPIControllerBase
    {
        private readonly EnumExchange m_exchange = EnumExchange.OKX;
        private const string BASE_URL = "https://www.okx.com";
        private string m_lastErrorMessage = "";

        public override string GetLastErrorMessage() => m_lastErrorMessage;

        private string CreateOkxSign(string method, string path, string body, string timestamp)
        {
            string prehash = timestamp + method + path + (body ?? "");
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange))))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>OKX 에러 메시지 파싱. 51000 시 msg에 'Parameter xxx error' 형태로 원인 포함.</summary>
        private static string ParseError(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "알 수 없는 오류";
            try
            {
                var obj = JObject.Parse(content);
                var msg = obj["msg"]?.ToString() ?? "";
                var code = obj["code"]?.ToString();
                if (!string.IsNullOrEmpty(msg))
                {
                    string result = code != null ? $"[{code}] {msg}" : msg;
                    // 51000 Parameter error 시 data에 상세가 있을 수 있음
                    if (code == "51000" && obj["data"] != null)
                    {
                        var dataStr = obj["data"].ToString();
                        if (!string.IsNullOrEmpty(dataStr)) result += " | " + dataStr;
                    }
                    // 체인/네트워크 관련 오류 시 안내
                    if (code == "51000" || (msg.IndexOf("chain", StringComparison.OrdinalIgnoreCase) >= 0))
                        result += " (OKX: 네트워크는 '해당 코인 조회' 후 목록에 표시된 체인명을 그대로 입력)";
                    return result;
                }
            }
            catch { }
            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }

        public override async Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            m_lastErrorMessage = "";
            try
            {
                // Funding 계정 잔고 사용 (출금 가능 잔고, 파라미터 없음 → 51000 방지). 실패 시 Trading 계정으로 재시도
                var (ok, list) = await GetFundingBalanceAsync();
                if (ok) return (true, list);

                // Fallback: Trading 계정 잔고 (acctType 없이 호출 시 51000 나는 계정이면 실패 유지)
                return await GetTradingBalanceAsync();
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, new List<Currency>());
            }
        }

        /// <summary>Funding 계정 잔고. GET /api/v5/asset/balances (파라미터 없음)</summary>
        private async Task<(bool, List<Currency>)> GetFundingBalanceAsync()
        {
            string path = "/api/v5/asset/balances";
            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string sign = CreateOkxSign("GET", path, "", ts);

            var client = new RestClient(BASE_URL);
            var request = new RestRequest(path, Method.Get);
            request.AddHeader("OK-ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
            request.AddHeader("OK-ACCESS-SIGN", sign);
            request.AddHeader("OK-ACCESS-TIMESTAMP", ts);
            request.AddHeader("OK-ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");

            ApiRequestLogger.LogRequest("GET", BASE_URL + path, null);
            RestResponse response = await client.ExecuteAsync(request);
            ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

            if (!response.IsSuccessful)
            {
                m_lastErrorMessage = ParseError(response.Content ?? "");
                return (false, new List<Currency>());
            }

            var jobj = JObject.Parse(response.Content ?? "{}");
            if (jobj["code"]?.ToString() != "0")
            {
                m_lastErrorMessage = ParseError(response.Content ?? "");
                return (false, new List<Currency>());
            }

            var data = jobj["data"] as JArray;
            var list = new List<Currency>();
            if (data != null)
            {
                foreach (var d in data)
                {
                    string ccy = d["ccy"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(ccy)) continue;
                    double bal = double.TryParse(d["bal"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double b) ? b : 0;
                    double frozen = double.TryParse(d["frozenBal"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double f) ? f : 0;
                    list.Add(new Currency(ccy, bal, frozen, 0, false));
                }
            }
            return (true, list);
        }

        /// <summary>Trading 계정 잔고. GET /api/v5/account/balance (details[].cashBal, frozenBal)</summary>
        private async Task<(bool, List<Currency>)> GetTradingBalanceAsync()
        {
            string path = "/api/v5/account/balance";
            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string sign = CreateOkxSign("GET", path, "", ts);

            var client = new RestClient(BASE_URL);
            var request = new RestRequest(path, Method.Get);
            request.AddHeader("OK-ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
            request.AddHeader("OK-ACCESS-SIGN", sign);
            request.AddHeader("OK-ACCESS-TIMESTAMP", ts);
            request.AddHeader("OK-ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");

            RestResponse response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                m_lastErrorMessage = ParseError(response.Content ?? "");
                return (false, new List<Currency>());
            }

            var jobj = JObject.Parse(response.Content ?? "{}");
            var data = jobj["data"] as JArray;
            var list = new List<Currency>();
            if (data != null && data.Count > 0)
            {
                var details = data[0]["details"] as JArray;
                if (details != null)
                {
                    foreach (var d in details)
                    {
                        string ccy = d["ccy"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(ccy)) continue;
                        double bal = double.TryParse(d["cashBal"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double b) ? b : 0;
                        double frozen = double.TryParse(d["frozenBal"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double f) ? f : 0;
                        list.Add(new Currency(ccy, bal, frozen, 0, false));
                    }
                }
            }
            return (true, list);
        }

        /// <summary>
        /// OKX GET /api/v5/asset/currencies. 반환되는 chain 값을 출금 시 그대로 사용해야 함.
        /// 예: USDT → "USDT-ERC20", "USDT-TRC20" 등 / ETH → "ETH" / BTC → "BTC". 대소문자·하이픈 정확히 일치.
        /// </summary>
        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            m_lastErrorMessage = "";
            try
            {
                string ccy = (coinName ?? "").Trim().ToUpperInvariant();
                string pathNoQuery = "/api/v5/asset/currencies";
                string pathWithQuery = string.IsNullOrEmpty(ccy) ? pathNoQuery : pathNoQuery + "?ccy=" + Uri.EscapeDataString(ccy);
                string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string sign = CreateOkxSign("GET", pathWithQuery, "", ts);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(pathNoQuery, Method.Get);
                if (!string.IsNullOrEmpty(ccy))
                    request.AddQueryParameter("ccy", ccy);
                request.AddHeader("OK-ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("OK-ACCESS-SIGN", sign);
                request.AddHeader("OK-ACCESS-TIMESTAMP", ts);
                request.AddHeader("OK-ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");

                RestResponse response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, null);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var data = jobj["data"] as JArray;
                var list = new List<NetworkInfo>();
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        // API 응답의 chain 값을 변경 없이 사용 (출금 요청 시 동일 값 필수)
                        string chain = item["chain"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(chain)) continue;
                        decimal fee = decimal.TryParse(item["minFee"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal feeVal) ? feeVal : 0m;
                        bool withdraw = item["canWd"]?.ToString() == "true";
                        list.Add(new NetworkInfo
                        {
                            ChainName = chain,
                            WithdrawFee = fee,
                            WithdrawEnabled = withdraw,
                            DepositEnabled = item["canDep"]?.ToString() == "true"
                        });
                    }
                }
                if (list.Count == 0)
                    list.Add(new NetworkInfo { ChainName = "default", WithdrawEnabled = true, DepositEnabled = true });
                return (true, list);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, null);
            }
        }

        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity = "", string kycName = "", string tag = null)
        {
            m_lastErrorMessage = "";
            try
            {
                string ccy = (coinName ?? "").Trim().ToUpperInvariant();
                string chain = (chainName ?? "").Trim();

                // OKX: chain은 GET /api/v5/asset/currencies 응답의 chain 값과 정확히 일치해야 함 (대소문자·하이픈 포함)
                var (ok, networks) = GetCoinNetworksDetail(coinName);
                if (ok && networks != null && networks.Count > 0 && !networks.Any(n => n.ChainName == "default"))
                {
                    var allChains = networks.Select(n => n.ChainName).ToList();
                    var withdrawChains = networks.Where(n => n.WithdrawEnabled).Select(n => n.ChainName).ToList();
                    if (string.IsNullOrWhiteSpace(chain) && withdrawChains.Count == 1)
                        chain = withdrawChains[0];
                    else if (!string.IsNullOrWhiteSpace(chain) && !allChains.Any(c => string.Equals(c, chain, StringComparison.Ordinal)))
                    {
                        m_lastErrorMessage = $"OKX 체인 명칭 오류. 입력값: [{chain}]. 해당 코인 체인(API와 동일하게 입력): {string.Join(", ", allChains)}. (출금 가능: {string.Join(", ", withdrawChains)})";
                        return (false, m_lastErrorMessage);
                    }
                }
                if (string.IsNullOrWhiteSpace(chain) && (networks == null || networks.Count == 0))
                    chain = "";

                // dest: 2=외부(온체인 출금), 4=내부 전송(OKX 계정 간). 외부 지갑으로 보낼 땐 2 사용.
                string path = "/api/v5/asset/withdrawal";
                var body = new JObject
                {
                    ["ccy"] = ccy,
                    ["amt"] = volume.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'),
                    ["dest"] = "2",
                    ["toAddr"] = (address ?? "").Trim()
                };
                if (!string.IsNullOrWhiteSpace(chain))
                    body["chain"] = chain;
                if (!string.IsNullOrWhiteSpace(tag))
                    body["memo"] = tag.Trim();

                string bodyStr = body.ToString();
                string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string sign = CreateOkxSign("POST", path, bodyStr, ts);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Post);
                request.AddHeader("OK-ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("OK-ACCESS-SIGN", sign);
                request.AddHeader("OK-ACCESS-TIMESTAMP", ts);
                request.AddHeader("OK-ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");
                request.AddHeader("Content-Type", "application/json");
                request.AddStringBody(bodyStr, "application/json");

                ApiRequestLogger.LogRequest("POST", BASE_URL + path, "ccy=" + coinName);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 1500);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var data = jobj["data"] as JArray;
                string wdId = "";
                string state = "";
                if (data != null && data.Count > 0)
                {
                    var first = data[0];
                    wdId = first["wdId"]?.ToString() ?? "";
                    state = first["state"]?.ToString() ?? first["status"]?.ToString() ?? "";
                }
                string msg = string.IsNullOrEmpty(wdId) ? "접수됨" : wdId;
                if (!string.IsNullOrEmpty(state))
                    msg += " (상태:" + state + ")";
                return (true, msg);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
            }
        }

        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((true, "규제 지역: 수취인 성명·지갑/거래소 구분 필요."));
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
            => throw new NotImplementedException("OKX 미구현");

        public override (bool, List<string>) GetMarketSupportCoins()
            => throw new NotImplementedException("OKX 미구현");

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
            => throw new NotImplementedException("OKX 미구현");

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
            => throw new NotImplementedException("OKX 미구현");

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
            => throw new NotImplementedException("OKX 미구현");

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
            => throw new NotImplementedException("OKX 미구현");

        public override double CalcOrderPrice(string coinCode, double price)
            => throw new NotImplementedException("OKX 미구현");

        public override double CalcOrderVolume(string coinCode, double volume)
            => throw new NotImplementedException("OKX 미구현");

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
            => throw new NotImplementedException("OKX 미구현");
    }
}
