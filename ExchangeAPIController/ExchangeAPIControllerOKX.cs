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

        /// <summary>rcvrInfo용: tr에 sourceKey(또는 targetKey)가 있으면 rcvrInfo[targetKey]에 설정.</summary>
        private static void AddIfPresent(JObject rcvrInfo, JObject tr, string sourceKey, string targetKey)
        {
            var v = tr[sourceKey]?.ToString()?.Trim() ?? tr[targetKey]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(v)) rcvrInfo[targetKey] = v;
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
                    // 58213: OKX가 주소를 내부 주소로 해석 또는 주소 형식 오류 (웹 검색: 주소 끝에 ':' 붙어 전송되는 버그 사례 있음)
                    if (code == "58213" || msg.IndexOf("internal address", StringComparison.OrdinalIgnoreCase) >= 0)
                        result += " (해결: 1) 주소 끝에 콜론(:) 등 여분 문자가 붙지 않았는지 확인 2) 해당 코인/체인 주소 형식에 맞는지 확인 3) OKX [출금 허용 목록] 사용 시 해당 주소 등록 후 24시간 경과 4) 로그의 [API 응답] 본문 확인)";
                    // 58237: rcvrInfo(수취인 정보) 필수. 거래소 출금 시 거래소명·수취인 영문명 입력 안내
                    if (code == "58237" || (msg.IndexOf("rcvrInfo", StringComparison.OrdinalIgnoreCase) >= 0 && msg.IndexOf("recipient", StringComparison.OrdinalIgnoreCase) >= 0))
                        result += " (안내: 수취처 지갑이 [거래소 지갑]이면 [출금 거래소명(영문)]에 수취 거래소명(예: Binance, Bybit)과 [수취인 영문명]에 해당 거래소 계정에 등록된 수취인 영문 이름(예: John Smith)을 입력한 뒤 다시 시도하세요. 개인 지갑으로 보내는 경우 [수취처 지갑]을 [개인 지갑]으로 선택하세요.)";
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

                // 수량 = 출금 요청 금액(수수료 포함). API가 수수료 차감 후 전송하므로 volume 그대로 전달.
                // 공식 예시: on-chain = dest "4", internal = dest "3". toAddr 정규화(OKX/CCXT에서 주소 끝에 ':' 붙는 버그 사례 있음).
                string toAddr = (address ?? "").Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();
                toAddr = toAddr.TrimEnd(':', ',', ' ');
                if (toAddr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && toAddr.Length == 42)
                    toAddr = "0x" + toAddr.Substring(2).ToLowerInvariant(); // ETH 등: 소문자 통일
                if (string.IsNullOrEmpty(toAddr))
                {
                    m_lastErrorMessage = "출금 주소를 입력해 주세요.";
                    return (false, m_lastErrorMessage);
                }
                string path = "/api/v5/asset/withdrawal";
                // 공식 예시: on-chain body = amt, dest, ccy, chain, toAddr (dest="4"). amt = 수수료 포함 출금 수량(그대로 전달)
                var body = new JObject
                {
                    ["amt"] = volume.ToString("F8", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'),
                    ["dest"] = "4",
                    ["ccy"] = ccy,
                    ["toAddr"] = toAddr
                };
                if (!string.IsNullOrWhiteSpace(chain))
                    body["chain"] = chain;
                // 문서: dest=4 시 주소가 'address:tag' 형식일 수 있음(예: ARDOR-...:123456). memo 있으면 toAddr에 :tag 붙이거나 memo 필드 사용.
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    string tagVal = tag.Trim();
                    if (toAddr.IndexOf(':') < 0)
                        toAddr = toAddr + ":" + tagVal;
                    else
                        body["memo"] = tagVal;
                }
                body["toAddr"] = toAddr;

                // rcvrInfo: 문서 기준 walletType = exchange | private, exchId(거래소 미등록 시 '0'), rcvrFirstName/rcvrLastName, 선택: rcvrCountry, rcvrCountrySubDivision, rcvrTownName, rcvrStreetName
                if (!string.IsNullOrWhiteSpace(kycName))
                {
                    try
                    {
                        var tr = JObject.Parse(kycName.Trim());
                        string receiverType = tr["receiver_type"]?.ToString()?.Trim();
                        string receiverEnName = tr["receiver_en_name"]?.ToString()?.Trim();
                        string exchangeName = tr["exchange_name"]?.ToString()?.Trim();
                        bool isCorp = string.Equals(receiverType, "corporation", StringComparison.OrdinalIgnoreCase);
                        if (isCorp)
                            receiverEnName = tr["receiver_corp_en_name"]?.ToString()?.Trim() ?? receiverEnName;
                        string exchId = !string.IsNullOrWhiteSpace(exchangeEntity) ? exchangeEntity.Trim() : (exchangeName ?? "");
                        // walletType: 문서는 exchange | private. UI에서 수취처 지갑(거래소/개인) 선택값 우선, 없으면 거래소명 유무로 추론.
                        string walletType = (tr["wallet_type"]?.ToString() ?? tr["walletType"]?.ToString() ?? "").Trim();
                        if (walletType != "exchange" && walletType != "private")
                        {
                            bool toExchange = !string.IsNullOrWhiteSpace(exchangeName) || !string.IsNullOrWhiteSpace(exchangeEntity);
                            walletType = toExchange ? "exchange" : "private";
                        }
                        if (walletType == "exchange" && string.IsNullOrWhiteSpace(exchId)) exchId = "0";

                        if (!string.IsNullOrWhiteSpace(receiverEnName) || (walletType == "exchange" && !string.IsNullOrWhiteSpace(exchId)) || walletType == "private")
                        {
                            var rcvrInfo = new JObject();
                            rcvrInfo["walletType"] = walletType;
                            if (walletType == "exchange") rcvrInfo["exchId"] = exchId;
                            if (!string.IsNullOrWhiteSpace(receiverEnName))
                            {
                                if (isCorp)
                                {
                                    rcvrInfo["rcvrFirstName"] = (tr["receiver_corp_ko_name"]?.ToString()?.Trim() ?? tr["receiver_corp_en_name"]?.ToString()?.Trim() ?? receiverEnName);
                                    rcvrInfo["rcvrLastName"] = "N/A";
                                }
                                else
                                {
                                    int firstSpace = receiverEnName.IndexOf(' ');
                                    if (firstSpace > 0)
                                    {
                                        rcvrInfo["rcvrFirstName"] = receiverEnName.Substring(0, firstSpace).Trim();
                                        rcvrInfo["rcvrLastName"] = receiverEnName.Substring(firstSpace + 1).Trim();
                                    }
                                    else
                                    {
                                        rcvrInfo["rcvrFirstName"] = receiverEnName;
                                        rcvrInfo["rcvrLastName"] = "";
                                    }
                                }
                            }
                            AddIfPresent(rcvrInfo, tr, "rcvr_country", "rcvrCountry");
                            AddIfPresent(rcvrInfo, tr, "rcvr_country_sub_division", "rcvrCountrySubDivision");
                            AddIfPresent(rcvrInfo, tr, "rcvr_town_name", "rcvrTownName");
                            AddIfPresent(rcvrInfo, tr, "rcvr_street_name", "rcvrStreetName");
                            body["rcvrInfo"] = rcvrInfo;
                        }
                    }
                    catch { /* JSON 아님 또는 파싱 실패 시 rcvrInfo 생략 */ }
                }

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
                // OKX는 HTTP 200이어도 body의 code가 "0"이 아니면 실패(이메일/2FA 확인 필요 등)
                string apiCode = jobj["code"]?.ToString();
                if (apiCode != "0")
                {
                    m_lastErrorMessage = ParseError(response.Content ?? "");
                    return (false, m_lastErrorMessage);
                }

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
                {
                    msg += " (상태:" + state + ")";
                    // state -2: 출금 대기(이메일/2FA 확인 필요). 거래소에서 확인 완료해야 실제 출금 처리됨
                    if (state == "-2")
                        msg += " ※ OKX 앱/이메일에서 출금 확인을 완료해 주세요.";
                }
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
