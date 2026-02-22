using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// 빗썸: 출금 가능 정보 조회. 공식 문서 apidocs.bithumb.com v2.1.5 "출금 가능 정보"
        /// 빗썸은 업비트와 달리 GET /v1/withdraws/chance 호출 시 currency + net_type 둘 다 필수.
        /// net_type=default 로 조회하여 해당 코인 기본 네트워크 수수료/최소출금량 반환. (멀티체인 목록은 별도 API 확인 필요)
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

                string currency = (coinName ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(currency))
                {
                    m_lastErrorMessage = "코인명을 입력해 주세요.";
                    return (false, null);
                }

                // 빗썸: chance API는 net_type 필수. 코인·체인 모두 대문자로 전달
                const string netType = "DEFAULT";
                string queryStringForHash = $"currency={Uri.EscapeDataString(currency)}&net_type={Uri.EscapeDataString(netType)}";
                string jwt = CreateBithumbJwt(accessKey, secretKey, queryStringForHash);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/chance", Method.Get);
                request.AddQueryParameter("currency", currency);
                request.AddQueryParameter("net_type", netType);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Accept", "application/json");

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/chance", "currency=" + currency + ", net_type=" + netType);
                RestResponse response = client.Execute(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    var fallback = new List<NetworkInfo>
                    {
                        new NetworkInfo { ChainName = "DEFAULT", WithdrawFee = 0m, WithdrawMin = 0m, WithdrawEnabled = true, DepositEnabled = true }
                    };
                    return (true, fallback);
                }

                // 문서: currency.withdraw_fee, withdraw_limit.minimum, withdraw_limit.can_withdraw
                var jobj = JObject.Parse(response.Content ?? "{}");
                var currencyObj = jobj["currency"];
                var withdrawLimitObj = jobj["withdraw_limit"];
                decimal fee = ParseDecimal(currencyObj?["withdraw_fee"]?.ToString());
                decimal min = ParseDecimal(withdrawLimitObj?["minimum"]?.ToString());
                bool canWithdraw = withdrawLimitObj?["can_withdraw"]?.Value<bool?>() ?? true;
                var list = new List<NetworkInfo>
                {
                    new NetworkInfo
                    {
                        ChainName = netType,
                        WithdrawFee = fee,
                        WithdrawMin = min,
                        WithdrawEnabled = canWithdraw,
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

        private static decimal ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v) ? v : 0m;
        }

        /// <summary>
        /// 빗썸: GET /v1/withdraws/chance 로 코인(currency)·네트워크(net_type) 출금 지원 여부만 검증.
        /// 문서 응답: currency.withdraw_fee, withdraw_limit.minimum, withdraw_limit.can_withdraw
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

                string currency = (coinName ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(currency))
                {
                    m_lastErrorMessage = "코인을 입력해 주세요.";
                    return (false, m_lastErrorMessage);
                }

                string netType = string.IsNullOrWhiteSpace(chainName) ? "DEFAULT" : chainName.Trim().ToUpperInvariant();
                string queryStringForHash = $"currency={Uri.EscapeDataString(currency)}&net_type={Uri.EscapeDataString(netType)}";
                string jwt = CreateBithumbJwt(accessKey, secretKey, queryStringForHash);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/chance", Method.Get);
                request.AddQueryParameter("currency", currency);
                request.AddQueryParameter("net_type", netType);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Accept", "application/json");

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/chance", "currency=" + currency + ", net_type=" + netType);
                RestResponse response = client.Execute(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, m_lastErrorMessage);
                }

                var jobj = JObject.Parse(response.Content ?? "{}");
                var withdrawLimit = jobj["withdraw_limit"];
                bool canWithdraw = withdrawLimit?["can_withdraw"]?.Value<bool?>() ?? true;
                if (!canWithdraw)
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

        /// <summary>
        /// 빗썸: GET /v1/withdraws/coin_addresses 로 출금 허용(화이트리스트) 주소 리스트 조회.
        /// </summary>
        public override (bool success, List<WithdrawAddressItem> list) GetWithdrawAllowedAddresses()
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

                string jwt = CreateBithumbJwt(accessKey, secretKey, null);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v1/withdraws/coin_addresses", Method.Get);
                request.AddHeader("Authorization", "Bearer " + jwt);
                request.AddHeader("Accept", "application/json");

                ApiRequestLogger.LogRequest("GET", BASE_URL + "/v1/withdraws/coin_addresses", "(출금 허용 주소 리스트)");
                RestResponse response = client.Execute(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, null);
                }

                var list = new List<WithdrawAddressItem>();
                var token = JToken.Parse(response.Content ?? "[]");

                void ParseItem(JObject jobj, string defaultCurrency = "")
                {
                    string currency = jobj["currency"]?.ToString()?.Trim().ToUpperInvariant()
                        ?? jobj["ccy"]?.ToString()?.Trim().ToUpperInvariant()
                        ?? jobj["coin"]?.ToString()?.Trim().ToUpperInvariant()
                        ?? defaultCurrency;
                    string netType = jobj["net_type"]?.ToString()?.Trim().ToUpperInvariant()
                        ?? jobj["netType"]?.ToString()?.Trim().ToUpperInvariant()
                        ?? jobj["chain"]?.ToString()?.Trim().ToUpperInvariant() ?? "";
                    string address = jobj["address"]?.ToString()?.Trim()
                        ?? jobj["to_address"]?.ToString()?.Trim()
                        ?? jobj["wallet_address"]?.ToString()?.Trim()
                        ?? jobj["withdraw_address"]?.ToString()?.Trim()
                        ?? jobj["toAddress"]?.ToString()?.Trim() ?? "";
                    string label = jobj["label"]?.ToString()?.Trim() ?? jobj["name"]?.ToString()?.Trim() ?? jobj["memo"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(address)) return;
                    list.Add(new WithdrawAddressItem
                    {
                        Currency = currency,
                        NetType = string.IsNullOrEmpty(netType) ? "DEFAULT" : netType,
                        Address = address,
                        Label = label
                    });
                }

                if (token is JArray arrTop)
                {
                    foreach (var item in arrTop)
                        if (item is JObject jobj) ParseItem(jobj);
                }
                else if (token is JObject obj)
                {
                    JArray arr = null;
                    if (obj["data"] is JArray dataArr) arr = dataArr;
                    else if (obj["list"] is JArray listArr) arr = listArr;
                    else if (obj["info"]?["list"] is JArray infoListArr) arr = infoListArr;
                    else if (obj["coin_addresses"] is JArray caArr) arr = caArr;
                    else if (obj["addresses"] is JArray addrArr) arr = addrArr;

                    if (arr != null)
                    {
                        foreach (var item in arr)
                            if (item is JObject jobj) ParseItem(jobj);
                    }
                    else
                    {
                        // 통화별 객체 형태: { "BTC": [ {...}, ... ], "ETH": [ ... ] }
                        foreach (var prop in obj.Properties())
                        {
                            if (prop.Value is JArray perCoinArr)
                            {
                                string ccy = prop.Name?.Trim().ToUpperInvariant() ?? "";
                                foreach (var item in perCoinArr)
                                {
                                    if (item is JObject jobj) ParseItem(jobj, ccy);
                                }
                            }
                        }
                    }
                }

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

                // 출금 수수료 포함 금액
                var (netOk, netList) = GetCoinNetworksDetail(coinName);
                string netType = string.IsNullOrWhiteSpace(chainName) ? "DEFAULT" : chainName.Trim().ToUpperInvariant();
                NetworkInfo network = (netOk && netList != null && netList.Count > 0)
                    ? (netList.FirstOrDefault(n => string.Equals(n.ChainName, netType, StringComparison.OrdinalIgnoreCase)) ?? netList[0])
                    : null;
                double totalAmount = network != null ? CalcWithdrawTotalAmount(volume, network.WithdrawFee, network.WithdrawPercentageFee) : volume;

                // 문서: POST body는 JSON, query_hash는 querystring(key=value&...)을 SHA512 해싱. 서버는 JSON 파싱 후 동일 규칙으로 해시 검증.
                // Upbit와 동일하게 net_type(네트워크) 필수. 미지정 시 default. (USDT: trc20 등 체인별 값 필요할 수 있음)
                string amountStr = totalAmount.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
                string currency = (coinName ?? "").Trim().ToUpperInvariant();
                string addr = (address ?? "").Trim();
                var formPairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("address", addr),
                    new KeyValuePair<string, string>("amount", amountStr),
                    new KeyValuePair<string, string>("currency", currency),
                    new KeyValuePair<string, string>("net_type", netType)
                };
                if (!string.IsNullOrWhiteSpace(tag))
                    formPairs.Add(new KeyValuePair<string, string>("destination_tag", tag.Trim()));
                // 트래블룰(수취인 정보): 빗썸 API 명세 필드만 전달 (exchange_name, receiver_type, receiver_ko_name, receiver_en_name, receiver_corp_ko_name, receiver_corp_en_name)
                if (!string.IsNullOrWhiteSpace(kycName))
                {
                    try
                    {
                        var tr = JObject.Parse(kycName.Trim());
                        string exchangeName = tr["exchange_name"]?.ToString()?.Trim();
                        string receiverType = tr["receiver_type"]?.ToString()?.Trim();
                        string receiverKoName = tr["receiver_ko_name"]?.ToString()?.Trim();
                        string receiverEnName = tr["receiver_en_name"]?.ToString()?.Trim();
                        string receiverCorpKoName = tr["receiver_corp_ko_name"]?.ToString()?.Trim();
                        string receiverCorpEnName = tr["receiver_corp_en_name"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(exchangeName))
                            formPairs.Add(new KeyValuePair<string, string>("exchange_name", exchangeName));
                        if (!string.IsNullOrEmpty(receiverType))
                            formPairs.Add(new KeyValuePair<string, string>("receiver_type", receiverType));
                        if (!string.IsNullOrEmpty(receiverKoName))
                            formPairs.Add(new KeyValuePair<string, string>("receiver_ko_name", receiverKoName));
                        if (!string.IsNullOrEmpty(receiverEnName))
                            formPairs.Add(new KeyValuePair<string, string>("receiver_en_name", receiverEnName));
                        if (!string.IsNullOrEmpty(receiverCorpKoName))
                            formPairs.Add(new KeyValuePair<string, string>("receiver_corp_ko_name", receiverCorpKoName));
                        if (!string.IsNullOrEmpty(receiverCorpEnName))
                            formPairs.Add(new KeyValuePair<string, string>("receiver_corp_en_name", receiverCorpEnName));
                    }
                    catch { /* JSON 아님 또는 파싱 실패 시 트래블룰 파라미터 생략 */ }
                }
                // 빗썸 공식 예시(Python): query_str = "&".join(f"{k}={v}" for k, v in requestBody.items()) → 인코딩 없이 키=값 연결, body와 동일한 키 순서로 SHA512.
                var bodyObj = new JObject();
                foreach (var p in formPairs)
                    bodyObj[p.Key] = p.Value;
                string bodyJson = bodyObj.ToString(Formatting.None);

                string queryStringForHash = string.Join("&", formPairs.Select(p => $"{p.Key}={p.Value}"));
                string jwt = CreateBithumbJwt(accessKey, secretKey, queryStringForHash);

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
