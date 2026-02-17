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
    /// 비트겟 API v2 컨트롤러 (ACCESS-KEY, ACCESS-SIGN, ACCESS-TIMESTAMP, ACCESS-PASSPHRASE)
    /// 서명: timestamp + method + requestPath + ("?" + queryString) + body → HMAC-SHA256 → Base64
    /// </summary>
    public class ExchangeAPIControllerBitget : ExchangeAPIControllerBase
    {
        private readonly EnumExchange m_exchange = EnumExchange.Bitget;
        private const string BASE_URL = "https://api.bitget.com";
        private string m_lastErrorMessage = "";

        public override string GetLastErrorMessage() => m_lastErrorMessage;

        private string CreateBitgetSign(string method, string requestPath, string queryString, string body)
        {
            string toSign = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                + method.ToUpperInvariant()
                + requestPath
                + (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)
                + (body ?? "");

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange))))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
                return Convert.ToBase64String(hash);
            }
        }

        private (string timestamp, string sign) CreateBitgetSignWithTimestamp(string method, string requestPath, string queryString, string body)
        {
            string ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string toSign = ts + method.ToUpperInvariant() + requestPath
                + (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)
                + (body ?? "");

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange))))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
                return (ts, Convert.ToBase64String(hash));
            }
        }

        private static string ParseError(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "알 수 없는 오류";
            try
            {
                var obj = JObject.Parse(content);
                var msg = obj["msg"]?.ToString() ?? obj["message"]?.ToString();
                var code = obj["code"]?.ToString();
                if (!string.IsNullOrEmpty(msg)) return code != null ? $"[{code}] {msg}" : msg;
            }
            catch { }
            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }

        public override async Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            m_lastErrorMessage = "";
            try
            {
                string path = "/api/v2/spot/account/assets";
                var (ts, sign) = CreateBitgetSignWithTimestamp("GET", path, "", "");

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Get);
                request.AddHeader("ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("ACCESS-SIGN", sign);
                request.AddHeader("ACCESS-TIMESTAMP", ts);
                request.AddHeader("ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");

                ApiRequestLogger.LogRequest("GET", BASE_URL + path, null);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, new List<Currency>());
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var data = jobj["data"];
                var list = new List<Currency>();
                JArray arr = data as JArray;
                if (arr == null && data is JObject dataObj)
                    arr = dataObj["assets"] as JArray ?? dataObj["spotAssets"] as JArray;
                if (arr != null)
                {
                    foreach (var item in arr)
                    {
                        string coin = item["coin"]?.ToString() ?? item["currency"]?.ToString() ?? item["coinName"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(coin)) continue;
                        double available = double.TryParse(item["available"]?.ToString() ?? item["availableBalance"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double a) ? a : 0;
                        double locked = double.TryParse(item["locked"]?.ToString() ?? item["frozen"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double l) ? l : 0;
                        list.Add(new Currency(coin, available, locked, 0, false));
                    }
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
                string path = "/api/v2/asset/currencies";
                string query = "coin=" + Uri.EscapeDataString(coinName.Trim());
                var (ts, sign) = CreateBitgetSignWithTimestamp("GET", path, query, "");

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Get);
                request.AddQueryParameter("coin", coinName.Trim());
                request.AddHeader("ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("ACCESS-SIGN", sign);
                request.AddHeader("ACCESS-TIMESTAMP", ts);
                request.AddHeader("ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");

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
                        string chain = item["chain"]?.ToString() ?? "";
                        decimal fee = decimal.TryParse(item["withdrawFee"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal f) ? f : 0m;
                        bool withdraw = item["withdrawable"]?.ToString() == "true";
                        list.Add(new NetworkInfo
                        {
                            ChainName = chain,
                            WithdrawFee = fee,
                            WithdrawEnabled = withdraw,
                            DepositEnabled = item["rechargeable"]?.ToString() == "true"
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
                string path = "/api/v2/asset/withdraw";
                var body = new JObject
                {
                    ["coin"] = coinName.Trim(),
                    ["chain"] = string.IsNullOrWhiteSpace(chainName) ? "default" : chainName.Trim(),
                    ["address"] = address.Trim(),
                    ["amount"] = volume.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.')
                };
                if (!string.IsNullOrWhiteSpace(tag))
                    body["tag"] = tag.Trim();

                string bodyStr = body.ToString();
                var (ts, sign) = CreateBitgetSignWithTimestamp("POST", path, "", bodyStr);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest(path, Method.Post);
                request.AddHeader("ACCESS-KEY", ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange));
                request.AddHeader("ACCESS-SIGN", sign);
                request.AddHeader("ACCESS-TIMESTAMP", ts);
                request.AddHeader("ACCESS-PASSPHRASE", ApikeySetting.GetInstance().GetExchangePassphrase(m_exchange) ?? "");
                request.AddHeader("Content-Type", "application/json");
                request.AddStringBody(bodyStr, "application/json");

                ApiRequestLogger.LogRequest("POST", BASE_URL + path, "coin=" + coinName);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 1500);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var data = jobj["data"] as JObject;
                string orderId = data?["orderId"]?.ToString() ?? data?["withdrawId"]?.ToString() ?? jobj["data"]?.ToString() ?? "";
                return (true, string.IsNullOrEmpty(orderId) ? "접수됨" : orderId);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
            }
        }

        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((false, ""));
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
            => throw new NotImplementedException("Bitget 미구현");

        public override (bool, List<string>) GetMarketSupportCoins()
            => throw new NotImplementedException("Bitget 미구현");

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
            => throw new NotImplementedException("Bitget 미구현");

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
            => throw new NotImplementedException("Bitget 미구현");

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
            => throw new NotImplementedException("Bitget 미구현");

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
            => throw new NotImplementedException("Bitget 미구현");

        public override double CalcOrderPrice(string coinCode, double price)
            => throw new NotImplementedException("Bitget 미구현");

        public override double CalcOrderVolume(string coinCode, double volume)
            => throw new NotImplementedException("Bitget 미구현");

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
            => throw new NotImplementedException("Bitget 미구현");
    }
}
