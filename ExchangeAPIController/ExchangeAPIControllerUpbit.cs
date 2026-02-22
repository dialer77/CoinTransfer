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
        /// 업비트: GET /v1/withdraws/chance?currency=XXX&amp;net_type=YYY → 출금 수수료 등.
        /// 멀티체인 코인(USDT 등)은 net_type 필수이므로, currency만으로 400이면 출금 허용 주소 API로 net_type 목록을 구해 각각 조회.
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

                string currency = coinName.Trim();
                var client = new RestClient(BASE_URL);

                // 1) 먼저 currency만으로 시도 (단일 네트워크 코인: BTC, ETH 등)
                var (ok, list) = TryWithdrawsChance(client, accessKey, secretKey, currency, null);
                if (ok && list != null && list.Count > 0)
                    return (true, list);
                if (ok)
                    return (true, list);

                // 2) 400 invalid_parameter → 멀티체인 코인. 출금 허용 주소에서 해당 통화의 net_type 목록 조회
                var netTypes = GetNetTypesFromCoinAddresses(accessKey, secretKey, currency);
                if (netTypes == null)
                    return (false, null);

                if (netTypes.Count == 0)
                {
                    // 등록된 주소 없으면 단일 체인처럼 net_type=통화코드 시도 (BTC, SRN 등)
                    netTypes.Add(currency);
                    // USDT 등 알려진 멀티체인은 흔한 net_type 추가
                    if (string.Equals(currency, "USDT", StringComparison.OrdinalIgnoreCase))
                    {
                        netTypes.Clear();
                        netTypes.Add("ERC20");
                        netTypes.Add("TRC20");
                        netTypes.Add("TRX");
                    }
                }

                list = new List<NetworkInfo>();
                foreach (string netType in netTypes)
                {
                    (ok, var oneList) = TryWithdrawsChance(client, accessKey, secretKey, currency, netType);
                    if (ok && oneList != null)
                    {
                        foreach (var n in oneList)
                        {
                            if (list.All(x => x.ChainName != n.ChainName))
                                list.Add(n);
                        }
                    }
                }
                if (list.Count > 0)
                    return (true, list);
                return (false, null);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, null);
            }
        }

        /// <summary>
        /// GET /v1/withdraws/chance 호출. net_type null이면 currency만 전달. 반환: (성공 여부, 응답 본문).
        /// </summary>
        private (bool ok, string content) ExecuteWithdrawsChance(RestClient client, string accessKey, string secretKey, string currency, string netType)
        {
            string query = "currency=" + Uri.EscapeDataString(currency);
            if (!string.IsNullOrEmpty(netType))
                query += "&net_type=" + Uri.EscapeDataString(netType);
            string jwt = CreateJwtToken(accessKey, secretKey, query);

            var request = new RestRequest("/v1/withdraws/chance", Method.Get);
            request.AddQueryParameter("currency", currency);
            if (!string.IsNullOrEmpty(netType))
                request.AddQueryParameter("net_type", netType);
            request.AddHeader("Authorization", "Bearer " + jwt);

            ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/chance", string.IsNullOrEmpty(netType) ? "currency=" + currency : "currency=" + currency + ", net_type=" + netType);
            RestResponse response = client.Execute(request);
            ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

            if (!response.IsSuccessful)
            {
                m_lastErrorMessage = ParseUpbitError(response.Content, response.StatusCode);
                return (false, null);
            }
            return (true, response.Content ?? "{}");
        }

        /// <summary>
        /// GET /v1/withdraws/chance 호출 후 NetworkInfo 리스트로 파싱.
        /// </summary>
        private (bool ok, List<NetworkInfo> list) TryWithdrawsChance(RestClient client, string accessKey, string secretKey, string currency, string netType)
        {
            var (ok, content) = ExecuteWithdrawsChance(client, accessKey, secretKey, currency, netType);
            if (!ok || string.IsNullOrEmpty(content))
                return (false, null);
            var jobj = JObject.Parse(content);
            var list = new List<NetworkInfo>();
            var currencyObj = jobj["currency"] as JObject;
            var withdrawLimitObj = jobj["withdraw_limit"] as JObject;
            decimal fee = ParseDecimal(currencyObj?["withdraw_fee"]?.ToString());
            decimal min = ParseDecimal(withdrawLimitObj?["minimum"]?.ToString());
            string chainName = netType ?? "default";
            list.Add(new NetworkInfo
            {
                ChainName = chainName,
                WithdrawFee = fee,
                WithdrawMin = min,
                WithdrawEnabled = true,
                DepositEnabled = true
            });
            return (true, list);
        }

        /// <summary>
        /// GET /v1/withdraws/coin_addresses 로 해당 통화의 net_type 목록 수집 (중복 제거).
        /// </summary>
        private List<string> GetNetTypesFromCoinAddresses(string accessKey, string secretKey, string currency)
        {
            try
            {
                string jwt = CreateJwtToken(accessKey, secretKey, null);
                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/coin_addresses", Method.Get);
                request.AddHeader("Authorization", "Bearer " + jwt);
                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/coin_addresses", "(출금 허용 주소)");
                RestResponse response = client.Execute(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 2000);
                if (!response.IsSuccessful)
                    return new List<string>();
                var arr = JArray.Parse(response.Content ?? "[]");
                var netTypes = new List<string>();
                foreach (var item in arr)
                {
                    string c = item["currency"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(c) || !string.Equals(c, currency, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string nt = item["net_type"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(nt) && !netTypes.Contains(nt))
                        netTypes.Add(nt);
                }
                return netTypes;
            }
            catch
            {
                return new List<string>();
            }
        }

        private static decimal ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v) ? v : 0m;
        }

        /// <summary>
        /// 업비트: GET /v1/withdraws/chance?currency=XXX&amp;net_type=YYY 로 출금 지원 여부 검증.
        /// 응답의 withdraw_limit.can_withdraw, currency.wallet_support 확인.
        /// </summary>
        public override (bool supported, string message) ValidateWithdrawSupport(string coinName, string chainName)
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
                string currency = (coinName ?? "").Trim();
                if (string.IsNullOrEmpty(currency))
                {
                    m_lastErrorMessage = "코인을 입력해 주세요.";
                    return (false, m_lastErrorMessage);
                }
                string netType = string.IsNullOrWhiteSpace(chainName) ? "default" : chainName.Trim();
                var client = new RestClient(BASE_URL);
                var (ok, content) = ExecuteWithdrawsChance(client, accessKey, secretKey, currency, netType);
                if (!ok)
                {
                    if (string.IsNullOrWhiteSpace(chainName))
                    {
                        netType = currency;
                        (ok, content) = ExecuteWithdrawsChance(client, accessKey, secretKey, currency, netType);
                    }
                    if (!ok)
                        return (false, m_lastErrorMessage);
                }
                if (string.IsNullOrEmpty(content))
                    return (false, m_lastErrorMessage ?? "출금 가능 정보를 가져오지 못했습니다.");
                var jobj = JObject.Parse(content);
                var withdrawLimitObj = jobj["withdraw_limit"] as JObject;
                bool canWithdraw = withdrawLimitObj?["can_withdraw"]?.Value<bool?>() ?? true;
                var walletSupport = jobj["currency"]?["wallet_support"] as JArray;
                bool hasWithdrawInSupport = walletSupport == null || walletSupport.Any(t => string.Equals(t?.ToString(), "withdraw", StringComparison.OrdinalIgnoreCase));
                if (!canWithdraw || !hasWithdrawInSupport)
                {
                    m_lastErrorMessage = "해당 코인·네트워크는 현재 출금이 지원되지 않습니다.";
                    return (false, m_lastErrorMessage);
                }
                return (true, "");
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                return (false, m_lastErrorMessage);
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
