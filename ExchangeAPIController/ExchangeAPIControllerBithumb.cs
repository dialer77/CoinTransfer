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
    /// 빗썸 API 컨트롤러 (JWT 인증 v2.1.5: access_key, nonce, timestamp)
    /// </summary>
    public class ExchangeAPIControllerBithumb : ExchangeAPIControllerBase
    {
        private readonly EnumExchange m_exchange = EnumExchange.Bithumb;
        private const string BASE_URL = "https://api.bithumb.com";
        private string m_lastErrorMessage = "";

        public override string GetLastErrorMessage() => m_lastErrorMessage;

        /// <summary>
        /// 빗썸 JWT (문서 v2.1.5): access_key, nonce, timestamp(ms) 필수. 파라미터 있을 때 query_hash, query_hash_alg 추가.
        /// 서명: HS256, Secret Key로 UTF-8 바이트 사용. Authorization: Bearer {token}
        /// </summary>
        private static string CreateBithumbJwt(string accessKey, string secretKey, string queryStringForHash = null)
        {
            accessKey = (accessKey ?? "").Trim();
            secretKey = (secretKey ?? "").Trim();
            // 문서와 동일한 키 순서·타입 (timestamp는 Number로 직렬화)
            var payload = new JObject
            {
                ["access_key"] = accessKey,
                ["nonce"] = Guid.NewGuid().ToString(),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            if (!string.IsNullOrEmpty(queryStringForHash))
            {
                using (var sha512 = SHA512.Create())
                {
                    byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryStringForHash));
                    payload["query_hash"] = string.Concat(hashBytes.Select(b => b.ToString("x2")));
                    payload["query_hash_alg"] = "SHA512";
                }
            }

            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            string payloadJson = payload.ToString(Formatting.None);
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string toSign = $"{headerB64}.{payloadB64}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
                return $"{toSign}.{Base64UrlEncode(sig)}";
            }
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string ParseError(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "알 수 없는 오류";
            try
            {
                var obj = JObject.Parse(content);
                var err = obj["error"];
                if (err != null)
                {
                    var name = err["name"]?.ToString();
                    var msg = err["message"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return msg != null ? $"{name}: {msg}" : name;
                }
                var msg2 = obj["message"]?.ToString() ?? obj["error"]?.ToString();
                if (!string.IsNullOrEmpty(msg2)) return msg2;
            }
            catch { }
            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
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

                string jwt = CreateBithumbJwt(accessKey, secretKey, null);
                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/accounts", Method.Get);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json; charset=utf-8");

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/accounts", "(JWT)");
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    if (m_lastErrorMessage.IndexOf("invalid_access_key", StringComparison.OrdinalIgnoreCase) >= 0)
                        m_lastErrorMessage += " (API 키 재입력·저장, PC 시계 동기화(UTC), 또는 빗썸에서 API 키 재발급 후 시도.)";
                    return (false, new List<Currency>());
                }

                var arr = JArray.Parse(response.Content ?? "[]");
                var list = new List<Currency>();
                foreach (var item in arr)
                {
                    string currency = item["currency"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(currency)) continue;
                    double balance = double.TryParse(item["balance"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double b) ? b : 0;
                    double locked = double.TryParse(item["locked"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double l) ? l : 0;
                    list.Add(new Currency(currency, balance, locked, 0, false));
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
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    m_lastErrorMessage = "API 키를 입력한 뒤 저장해 주세요.";
                    return (false, null);
                }

                // 빗썸: 출금 가능 정보/네트워크 조회 API가 있으면 사용. 없으면 default 네트워크만 반환
                var list = new List<NetworkInfo>
                {
                    new NetworkInfo
                    {
                        ChainName = "default",
                        WithdrawFee = 0m,
                        WithdrawMin = 0m,
                        WithdrawEnabled = true,
                        DepositEnabled = true
                    }
                };
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
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    m_lastErrorMessage = "API 키를 입력한 뒤 저장해 주세요.";
                    return (false, m_lastErrorMessage);
                }

                // 문서: POST body는 JSON, query_hash는 querystring(key=value&...)을 SHA512 해싱. 서버는 JSON 파싱 후 동일 규칙으로 해시 검증.
                // Upbit와 동일하게 net_type(네트워크) 필수. 미지정 시 default. (USDT: trc20 등 체인별 값 필요할 수 있음)
                string amountStr = volume.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
                string currency = (coinName ?? "").Trim().ToUpperInvariant();
                string addr = (address ?? "").Trim();
                string netType = string.IsNullOrWhiteSpace(chainName) ? "default" : chainName.Trim().ToLowerInvariant();
                var formPairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("address", addr),
                    new KeyValuePair<string, string>("amount", amountStr),
                    new KeyValuePair<string, string>("currency", currency),
                    new KeyValuePair<string, string>("net_type", netType)
                };
                if (!string.IsNullOrWhiteSpace(tag))
                    formPairs.Add(new KeyValuePair<string, string>("destination_tag", tag.Trim()));
                formPairs = formPairs.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
                string queryStringForHash = string.Join("&", formPairs.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                string jwt = CreateBithumbJwt(accessKey, secretKey, queryStringForHash);

                var bodyObj = new JObject();
                foreach (var p in formPairs)
                    bodyObj[p.Key] = p.Value;
                string bodyJson = bodyObj.ToString(Formatting.None);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/coin", Method.Post);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Content-Type", "application/json; charset=utf-8");
                request.AddStringBody(bodyJson, "application/json");

                ApiRequestLogger.LogRequest("POST", BASE_URL + "/v1/withdraws/coin", "currency=" + coinName);
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 1500);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        m_lastErrorMessage += " (엔드포인트: POST /v1/withdraws/coin. 해당 통화 출금 미지원 시 404 가능.)";
                    if (m_lastErrorMessage.IndexOf("invalid_parameter", StringComparison.OrdinalIgnoreCase) >= 0)
                        m_lastErrorMessage += " (가능 원인: 네트워크(net_type) 불일치 예: USDT TRON주소→trc20, 출금 주소 미등록/화이트리스트, 또는 필수 파라미터 형식)";
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                string uuid = jobj["uuid"]?.ToString() ?? jobj["withdraw_id"]?.ToString() ?? "";
                return (true, string.IsNullOrEmpty(uuid) ? "접수됨" : uuid);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
            }
        }

        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((true, "규제에 따라 수취인 정보 필요할 수 있음."));
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
            => throw new NotImplementedException("Bithumb 미구현");

        public override (bool, List<string>) GetMarketSupportCoins()
            => throw new NotImplementedException("Bithumb 미구현");

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
            => throw new NotImplementedException("Bithumb 미구현");

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
            => throw new NotImplementedException("Bithumb 미구현");

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
            => throw new NotImplementedException("Bithumb 미구현");

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
            => throw new NotImplementedException("Bithumb 미구현");

        public override double CalcOrderPrice(string coinCode, double price)
            => throw new NotImplementedException("Bithumb 미구현");

        public override double CalcOrderVolume(string coinCode, double volume)
            => throw new NotImplementedException("Bithumb 미구현");

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
            => throw new NotImplementedException("Bithumb 미구현");
    }
}
