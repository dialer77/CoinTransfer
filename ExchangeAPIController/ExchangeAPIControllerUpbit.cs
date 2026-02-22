using Newtonsoft.Json;
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
    /// 업비트 API 컨트롤러 (JWT 인증, 잔고/출금가능정보/출금)
    /// </summary>
    public class ExchangeAPIControllerUpbit : ExchangeAPIControllerBase
    {
        private readonly EnumExchange m_exchange = EnumExchange.Upbit;
        private const string BASE_URL = "https://api.upbit.com";
        private string m_lastErrorMessage = "";

        public override string GetLastErrorMessage() => m_lastErrorMessage;

        /// <summary>
        /// JWT 토큰 생성 (업비트: access_key, nonce, query_hash/query_hash_alg는 파라미터 있을 때만)
        /// </summary>
        private static string CreateJwtToken(string accessKey, string secretKey, string queryStringForHash = null)
        {
            var payload = new Dictionary<string, object>
            {
                { "access_key", accessKey },
                { "nonce", Guid.NewGuid().ToString() }
            };
            if (!string.IsNullOrEmpty(queryStringForHash))
            {
                using (var sha512 = SHA512.Create())
                {
                    byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryStringForHash));
                    string queryHash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
                    payload["query_hash"] = queryHash;
                    payload["query_hash_alg"] = "SHA512";
                }
            }

            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            string toSign = $"{headerB64}.{payloadB64}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
                string sigB64 = Base64UrlEncode(sig);
                return $"{toSign}.{sigB64}";
            }
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public override async Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            m_lastErrorMessage = "";
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    m_lastErrorMessage = "API 키를 입력한 뒤 저장해 주세요.";
                    return (false, new List<Currency>());
                }

                string jwt = CreateJwtToken(accessKey, secretKey, null);
                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/accounts", Method.Get);
                request.AddHeader("Authorization", "Bearer " + jwt);

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/accounts", "(JWT)");
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseUpbitError(response.Content, response.StatusCode);
                    return (false, new List<Currency>());
                }

                var arr = JArray.Parse(response.Content ?? "[]");
                var currencies = new List<Currency>();
                foreach (var item in arr)
                {
                    string currencyCode = item["currency"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(currencyCode)) continue;
                    double balance = ParseDouble(item["balance"]?.ToString());
                    double locked = ParseDouble(item["locked"]?.ToString());
                    currencies.Add(new Currency(currencyCode, balance, locked, 0, false));
                }
                return (true, currencies);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, new List<Currency>());
            }
        }

        private static double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0;
        }

        private static string ParseUpbitError(string content, System.Net.HttpStatusCode? statusCode = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return statusCode.HasValue ? $"HTTP {(int)statusCode}" : "알 수 없는 오류";
            try
            {
                var obj = JObject.Parse(content);
                var err = obj["error"]?["message"]?.ToString() ?? obj["message"]?.ToString();
                var name = obj["error"]?["name"]?.ToString();
                if (!string.IsNullOrEmpty(err))
                    return string.IsNullOrEmpty(name) ? err : $"[{name}] {err}";
            }
            catch { }
            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }

        /// <summary>
        /// 업비트: GET /v1/withdraws/chance?currency=XXX → 출금 가능 네트워크(net_type) 등
        /// </summary>
        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            m_lastErrorMessage = "";
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    m_lastErrorMessage = "API 키를 입력한 뒤 저장해 주세요.";
                    return (false, null);
                }

                string query = $"currency={Uri.EscapeDataString(coinName.Trim())}";
                string jwt = CreateJwtToken(accessKey, secretKey, query);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/chance", Method.Get);
                request.AddQueryParameter("currency", coinName.Trim());
                request.AddHeader("Authorization", "Bearer " + jwt);

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/chance", "currency=" + coinName);
                RestResponse response = client.Execute(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseUpbitError(response.Content, response.StatusCode);
                    return (false, null);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var list = new List<NetworkInfo>();
                // 업비트 응답: withdraw_limit (전체), currency, withdraw_fee 등. 멀티체인 시 withdraw_chance[] 또는 net_type 별 정보
                var withdrawChances = jobj["withdraw_chance"] as JArray;
                if (withdrawChances != null && withdrawChances.Count > 0)
                {
                    foreach (var w in withdrawChances)
                    {
                        string netType = w["net_type"]?.ToString() ?? "default";
                        decimal fee = ParseDecimal(w["withdraw_fee"]?.ToString());
                        decimal min = ParseDecimal(w["withdraw_limit_min"]?.ToString());
                        list.Add(new NetworkInfo
                        {
                            ChainName = netType,
                            WithdrawFee = fee,
                            WithdrawMin = min,
                            WithdrawEnabled = true,
                            DepositEnabled = true
                        });
                    }
                }
                else
                {
                    // 단일 네트워크 코인
                    decimal fee = ParseDecimal(jobj["withdraw_fee"]?.ToString());
                    decimal min = ParseDecimal(jobj["withdraw_limit_min"]?.ToString());
                    list.Add(new NetworkInfo
                    {
                        ChainName = "default",
                        WithdrawFee = fee,
                        WithdrawMin = min,
                        WithdrawEnabled = true,
                        DepositEnabled = true
                    });
                }
                return (true, list);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, null);
            }
        }

        private static decimal ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v) ? v : 0m;
        }

        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity = "", string kycName = "", string tag = null)
        {
            m_lastErrorMessage = "";
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    m_lastErrorMessage = "API 키를 입력한 뒤 저장해 주세요.";
                    return (false, m_lastErrorMessage);
                }

                // 출금 수수료 포함 금액
                var (netOk, netList) = GetCoinNetworksDetail(coinName);
                string netTypeForUpbit = string.IsNullOrWhiteSpace(chainName) ? "default" : chainName.Trim();
                NetworkInfo network = (netOk && netList != null && netList.Count > 0)
                    ? (netList.FirstOrDefault(n => string.Equals(n.ChainName, netTypeForUpbit, StringComparison.OrdinalIgnoreCase)) ?? netList[0])
                    : null;
                double totalAmount = network != null ? CalcWithdrawTotalAmount(volume, network.WithdrawFee, network.WithdrawPercentageFee) : volume;

                // POST body (JSON). query_hash는 query string 형식(키=값&키=값). 업비트는 JSON 키 순서대로 쿼리 문자열 구성 후 SHA512 해시.
                string amountStr = totalAmount.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
                var body = new Dictionary<string, object>
                {
                    { "currency", coinName.Trim() },
                    { "net_type", string.IsNullOrWhiteSpace(chainName) ? "default" : chainName.Trim() },
                    { "amount", amountStr },
                    { "address", address.Trim() }
                };
                if (!string.IsNullOrWhiteSpace(tag))
                    body["secondary_address"] = tag.Trim();

                string bodyJson = JsonConvert.SerializeObject(body);
                // query_hash: 업비트는 JSON body를 query string 형식으로 바꾼 뒤 SHA512 해시. (문서: query string, JSON 불가)
                // 키 순서 = JSON 전송 순서(삽입 순서). 값은 URL 인코딩 후 해시(업비트 검증과 동일하게).
                var queryParts = new List<string>();
                foreach (var kv in body)
                {
                    string val = kv.Value?.ToString() ?? "";
                    queryParts.Add($"{kv.Key}={Uri.EscapeDataString(val)}");
                }
                string queryStringForHash = string.Join("&", queryParts);
                ApiRequestLogger.Log($"[Upbit JWT] query string for hash: {queryStringForHash}");
                string jwt = CreateJwtToken(accessKey, secretKey, queryStringForHash);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/coin", Method.Post);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Content-Type", "application/json");
                request.AddStringBody(bodyJson, "application/json");

                ApiRequestLogger.LogRequest("POST", BASE_URL + "/v1/withdraws/coin", "currency=" + coinName + ", amount=" + volume);
                ApiRequestLogger.LogRequestBody(bodyJson, 500);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseUpbitError(response.Content, response.StatusCode);
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                string uuid = jobj["uuid"]?.ToString() ?? "";
                string state = jobj["state"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(uuid))
                    return (true, uuid);
                return (true, state);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
            }
        }

        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((true, "일부 지역(싱가포르 등) 검증 필요. 출금 전용 조회 API 없음."));
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
            => throw new NotImplementedException("Upbit 미구현");

        public override (bool, List<string>) GetMarketSupportCoins()
            => throw new NotImplementedException("Upbit 미구현");

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
            => throw new NotImplementedException("Upbit 미구현");

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
            => throw new NotImplementedException("Upbit 미구현");

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
            => throw new NotImplementedException("Upbit 미구현");

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
            => throw new NotImplementedException("Upbit 미구현");

        public override double CalcOrderPrice(string coinCode, double price)
            => throw new NotImplementedException("Upbit 미구현");

        public override double CalcOrderVolume(string coinCode, double volume)
            => throw new NotImplementedException("Upbit 미구현");

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
            => throw new NotImplementedException("Upbit 미구현");
    }
}
