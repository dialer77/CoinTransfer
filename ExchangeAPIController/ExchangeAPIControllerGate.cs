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
    /// Gate.io API v4 컨트롤러 (HMAC-SHA512 서명: METHOD\nPATH\nQUERY\nBODY_SHA512_HEX\nTIMESTAMP)
    /// </summary>
    public class ExchangeAPIControllerGate : ExchangeAPIControllerBase
    {
        private readonly EnumExchange m_exchange = EnumExchange.Gate;
        private const string BASE_URL = "https://api.gateio.ws";
        private string m_lastErrorMessage = "";

        public override string GetLastErrorMessage() => m_lastErrorMessage;

        private static string Sha512Hex(string data)
        {
            using (var sha = SHA512.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        private static string HmacSha512Hex(string key, string data)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        private (string timestamp, string sign) CreateGateSign(string method, string path, string query, string body)
        {
            string ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string payloadHash = Sha512Hex(body ?? "");
            string signStr = $"{method}\n{path}\n{query ?? ""}\n{payloadHash}\n{ts}";
            string secret = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
            string sign = HmacSha512Hex(secret, signStr);
            return (ts, sign);
        }

        private static string ParseError(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "알 수 없는 오류";
            try
            {
                var obj = JObject.Parse(content);
                return obj["message"]?.ToString() ?? obj["label"]?.ToString() ?? content;
            }
            catch { }
            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }

        public override async Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            m_lastErrorMessage = "";
            try
            {
                string path = "/api/v4/spot/accounts";
                var (ts, sign) = CreateGateSign("GET", path, "", "");

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Get);
                request.AddHeader("KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("Timestamp", ts);
                request.AddHeader("SIGN", sign);

                ApiRequestLogger.LogRequest("GET", BASE_URL + path, null);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, new List<Currency>());
                }

                var arr = JArray.Parse(response.Content ?? "[]");
                var list = new List<Currency>();
                foreach (var item in arr)
                {
                    string currency = item["currency"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(currency)) continue;
                    double available = double.TryParse(item["available"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double a) ? a : 0;
                    double locked = double.TryParse(item["locked"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double l) ? l : 0;
                    list.Add(new Currency(currency, available, locked, 0, false));
                }
                return (true, list);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, new List<Currency>());
            }
        }

        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            m_lastErrorMessage = "";
            try
            {
                string path = "/api/v4/wallet/withdrawals/currencies/" + Uri.EscapeDataString(coinName.Trim());
                var (ts, sign) = CreateGateSign("GET", path, "", "");

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Get);
                request.AddHeader("KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("Timestamp", ts);
                request.AddHeader("SIGN", sign);

                RestResponse response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, null);
                }

                var arr = JArray.Parse(response.Content ?? "[]");
                var list = new List<NetworkInfo>();
                foreach (var item in arr)
                {
                    string chain = item["chain"]?.ToString() ?? "";
                    decimal fee = decimal.TryParse(item["withdraw_fee"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal f) ? f : 0m;
                    decimal min = decimal.TryParse(item["withdraw_day_limit_remain"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal m) ? m : 0m;
                    list.Add(new NetworkInfo
                    {
                        ChainName = chain,
                        WithdrawFee = fee,
                        WithdrawMin = min,
                        WithdrawEnabled = true,
                        DepositEnabled = true
                    });
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
                string path = "/api/v4/withdrawals";
                var body = new JObject
                {
                    ["currency"] = coinName.Trim(),
                    ["amount"] = volume.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'),
                    ["address"] = address.Trim()
                };
                if (!string.IsNullOrWhiteSpace(chainName))
                    body["chain"] = chainName.Trim();
                if (!string.IsNullOrWhiteSpace(tag))
                    body["memo"] = tag.Trim();

                string bodyStr = body.ToString();
                var (ts, sign) = CreateGateSign("POST", path, "", bodyStr);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Post);
                request.AddHeader("KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("Timestamp", ts);
                request.AddHeader("SIGN", sign);
                request.AddHeader("Content-Type", "application/json");
                request.AddStringBody(bodyStr, "application/json");

                ApiRequestLogger.LogRequest("POST", BASE_URL + path, "currency=" + coinName);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 1500);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                string id = jobj["id"]?.ToString() ?? jobj["withdrawal_id"]?.ToString() ?? "";
                return (true, string.IsNullOrEmpty(id) ? "접수됨" : id);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
            }
        }

        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((true, "출금 시 수취인 정보 필요할 수 있음. Gate 규정 참고."));
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
            => throw new NotImplementedException("Gate 미구현");

        public override (bool, List<string>) GetMarketSupportCoins()
            => throw new NotImplementedException("Gate 미구현");

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
            => throw new NotImplementedException("Gate 미구현");

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
            => throw new NotImplementedException("Gate 미구현");

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
            => throw new NotImplementedException("Gate 미구현");

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
            => throw new NotImplementedException("Gate 미구현");

        public override double CalcOrderPrice(string coinCode, double price)
            => throw new NotImplementedException("Gate 미구현");

        public override double CalcOrderVolume(string coinCode, double volume)
            => throw new NotImplementedException("Gate 미구현");

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
            => throw new NotImplementedException("Gate 미구현");
    }
}
