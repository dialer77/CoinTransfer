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
    public class ExchangeAPIControllerBinance : ExchangeAPIControllerBase
    {
        private EnumExchange m_exchange = EnumExchange.Binance;
        private const string BASE_URL = "https://api.binance.com";
        private const string SAPI_BASE_URL = "https://api.binance.com";

        private string m_lastErrorMessage = "";

        private static string GetBinanceErrorDescription(int code, string msg)
        {
            return code switch
            {
                -1000 => "알 수 없는 오류가 발생했습니다.",
                -1001 => "내부 오류. 잠시 후 다시 시도해 주세요.",
                -1002 => "권한이 없습니다. API 키 설정을 확인하세요.",
                -1003 => "요청 한도를 초과했습니다. 잠시 후 다시 시도하세요.",
                -1006 => "예기치 않은 응답입니다.",
                -1007 => "서버 응답 대기 시간 초과.",
                -1008 => "서버가 바쁩니다. 잠시 후 다시 시도하세요.",
                -1013 => "API가 요청을 거부했습니다.",
                -1020 => "이 작업은 지원되지 않습니다.",
                -1021 => "시스템 시간이 맞지 않습니다. PC 시간 동기화 후 다시 시도하세요.",
                -1022 => "서명이 잘못되었습니다. API Secret 키를 확인하세요.",
                -2014 => "API 키 형식이 올바르지 않습니다.",
                -2015 => "API 키, IP 제한 또는 권한 오류. 출금 권한이 있는지 확인하세요.",
                -1100 => "파라미터에 잘못된 문자가 포함되어 있습니다.",
                -1102 => "필수 파라미터가 비어있거나 형식이 잘못되었습니다.",
                -1105 => "필수 파라미터가 비어있습니다.",
                -1111 => "수량 소수점 자릿수가 너무 많습니다.",
                -1130 => "파라미터 값이 올바르지 않습니다.",
                -20124 => "출금 화이트리스트가 활성화되어 있습니다. Binance 웹사이트에서 해당 주소를 화이트리스트에 추가한 후 출금해 주세요.",
                -20175 => "출금 주소가 화이트리스트에 등록되어 있지 않습니다. Binance에서 먼저 해당 주소로 출금하여 인증해 주세요.",
                _ => msg
            };
        }

        private string ParseBinanceError(string responseContent, int? httpStatus = null)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return httpStatus.HasValue ? $"HTTP {(int)httpStatus}: 응답이 비어있습니다." : "알 수 없는 오류가 발생했습니다.";

            try
            {
                var obj = JObject.Parse(responseContent);
                var codeToken = obj["code"];
                var msgToken = obj["msg"];

                int code = codeToken != null ? (int)codeToken : -1;
                string msg = msgToken?.ToString() ?? "";

                var desc = GetBinanceErrorDescription(code, msg);
                return string.IsNullOrEmpty(desc) || desc == msg
                    ? $"Code {code}: {msg}"
                    : $"[{code}] {desc}";
            }
            catch
            {
                return responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent;
            }
        }

        public override string GetLastErrorMessage()
        {
            return m_lastErrorMessage;
        }

        private string CreateSignature(string queryString, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        public override async Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            bool bResult = false;
            List<Currency> currencies = new List<Currency>();

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string queryString = $"timestamp={timestamp}";
                string signature = CreateSignature(queryString, secretKey);

                var url = $"{BASE_URL}/api/v3/account";
                ApiRequestLogger.LogRequest("GET", url, $"timestamp={timestamp}, signature=***");

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/api/v3/account", Method.Get);
                request.AddQueryParameter("timestamp", timestamp.ToString());
                request.AddQueryParameter("signature", signature);
                request.AddHeader("X-MBX-APIKEY", accessKey);

                RestResponse response = await client.ExecuteAsync(request);

                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 1500);

                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);
                    var balances = responseObj["balances"]?.ToObject<JArray>();

                    if (balances != null)
                    {
                        foreach (JObject balance in balances)
                        {
                            string asset = balance["asset"]?.ToString() ?? "";
                            double free = double.TryParse(balance["free"]?.ToString(), out double f) ? f : 0;
                            double locked = double.TryParse(balance["locked"]?.ToString(), out double l) ? l : 0;

                            Currency currency = new Currency(asset, free, locked, 0, false);
                            currencies.Add(currency);
                        }
                    }
                    bResult = true;
                }
                else
                {
                    m_lastErrorMessage = ParseBinanceError(response.Content ?? "", (int?)response.StatusCode);
                    if (string.IsNullOrEmpty(m_lastErrorMessage))
                        m_lastErrorMessage = response.ErrorMessage ?? $"HTTP {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = $"{ex.Message}";
            }

            return (bResult, currencies);
        }

        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            bool bResult = false;
            List<NetworkInfo> networkInfos = new List<NetworkInfo>();

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string queryString = $"timestamp={timestamp}";
                string signature = CreateSignature(queryString, secretKey);

                var url = $"{SAPI_BASE_URL}/sapi/v1/capital/config/getall";
                ApiRequestLogger.LogRequest("GET", url, $"timestamp={timestamp}, signature=*** (coin={coinName})");

                var client = new RestClient(SAPI_BASE_URL);
                var request = new RestRequest("/sapi/v1/capital/config/getall", Method.Get);
                request.AddQueryParameter("timestamp", timestamp.ToString());
                request.AddQueryParameter("signature", signature);
                request.AddHeader("X-MBX-APIKEY", accessKey);

                RestResponse response = client.Execute(request);

                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 3000);

                if (response.IsSuccessful)
                {
                    var configs = JArray.Parse(response.Content);
                    var coinConfig = configs.FirstOrDefault(x =>
                        string.Equals(x["coin"]?.ToString(), coinName, StringComparison.OrdinalIgnoreCase));

                    if (coinConfig != null)
                    {
                        var networkList = coinConfig["networkList"]?.ToObject<JArray>();
                        if (networkList != null)
                        {
                            foreach (JObject network in networkList)
                            {
                                bool withdrawEnabled = network["withdrawEnable"]?.ToString() == "True" ||
                                    string.Equals(network["withdrawEnable"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

                                NetworkInfo networkInfo = new NetworkInfo
                                {
                                    ChainName = network["network"]?.ToString() ?? "",
                                    WithdrawFee = decimal.TryParse(network["withdrawFee"]?.ToString(), out decimal wf) ? wf : 0m,
                                    DepositMin = decimal.TryParse(network["depositMin"]?.ToString(), out decimal dmin) ? dmin : 0m,
                                    WithdrawMin = decimal.TryParse(network["withdrawMin"]?.ToString(), out decimal wmin) ? wmin : 0m,
                                    DepositEnabled = network["depositEnable"]?.ToString() == "True" ||
                                        string.Equals(network["depositEnable"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase),
                                    WithdrawEnabled = withdrawEnabled,
                                    ContractAddress = network["contractAddress"]?.ToString() ?? "",
                                    Confirmation = int.TryParse(network["confirmTimes"]?.ToString(), out int ct) ? ct : 0
                                };

                                networkInfos.Add(networkInfo);
                            }
                        }
                        bResult = true;
                    }
                    else
                    {
                        m_lastErrorMessage = $"Coin {coinName} not found";
                    }
                }
                else
                {
                    m_lastErrorMessage = ParseBinanceError(response.Content ?? "", (int?)response.StatusCode);
                    if (string.IsNullOrEmpty(m_lastErrorMessage))
                        m_lastErrorMessage = response.ErrorMessage ?? $"HTTP {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
            }

            return (bResult, networkInfos);
        }

        /// <summary>
        /// Travel Rule 준수 필요 여부 확인. NIL이면 일반 출금, 아니면 localentity/withdraw/apply 사용 필요.
        /// https://developers.binance.com/docs/wallet/travel-rule
        /// </summary>
        public override async Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string queryString = $"timestamp={timestamp}";
                string signature = CreateSignature(queryString, secretKey);

                var client = new RestClient(SAPI_BASE_URL);
                var request = new RestRequest("/sapi/v1/localentity/questionnaire-requirements", Method.Get);
                request.AddQueryParameter("timestamp", timestamp.ToString());
                request.AddQueryParameter("signature", signature);
                request.AddHeader("X-MBX-APIKEY", accessKey);

                ApiRequestLogger.Log("[Travel Rule] questionnaire-requirements 확인 중...");
                RestResponse response = await client.ExecuteAsync(request);
                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "", 500);

                var content = (response.Content ?? "").Trim();
                if (string.IsNullOrEmpty(content) || string.Equals(content, "NIL", StringComparison.OrdinalIgnoreCase))
                    return (false, "");

                try
                {
                    var obj = JObject.Parse(content);
                    var countryCode = obj["questionnaireCountryCode"]?.ToString()?.Trim();
                    // questionnaireCountryCode가 NIL이면 Travel Rule 불필요 → 일반 출금 사용
                    if (string.IsNullOrEmpty(countryCode) || string.Equals(countryCode, "NIL", StringComparison.OrdinalIgnoreCase))
                        return (false, "");
                    return (true, $"국가코드: {countryCode}");
                }
                catch
                {
                    return (true, content);
                }
            }
            catch (Exception ex)
            {
                ApiRequestLogger.Log($"[Travel Rule] 확인 실패: {ex.Message}");
                return (false, "");
            }
        }

        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity = "", string kycName = "", string tag = null)
        {
            bool bResult = false;
            string errorMessage = "";

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                var (travelRuleRequired, travelRuleInfo) = await CheckTravelRuleRequiredAsync();
                if (travelRuleRequired)
                {
                    ApiRequestLogger.Log($"[Travel Rule] 준수 필요 ({travelRuleInfo}). localentity/withdraw/apply 사용");
                }

                // 출금 수수료 포함 금액
                var (netOk, netList) = GetCoinNetworksDetail(coinName);
                NetworkInfo network = (netOk && netList != null && netList.Count > 0)
                    ? (netList.FirstOrDefault(n => !string.IsNullOrEmpty(chainName) && string.Equals(n.ChainName, chainName, StringComparison.OrdinalIgnoreCase))
                        ?? netList.FirstOrDefault(n => n.WithdrawEnabled) ?? netList[0])
                    : null;
                double totalAmount = network != null ? CalcWithdrawTotalAmount(volume, network.WithdrawFee, network.WithdrawPercentageFee) : volume;

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var parameters = new Dictionary<string, string>
                {
                    { "coin", coinName },
                    { "address", address },
                    { "amount", totalAmount.ToString("0.##############", System.Globalization.CultureInfo.InvariantCulture) },
                    { "timestamp", timestamp.ToString() }
                };

                if (!string.IsNullOrEmpty(chainName))
                {
                    parameters.Add("network", chainName);
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    parameters.Add("addressTag", tag);
                }

                if (travelRuleRequired)
                {
                    parameters.Add("questionnaire", string.IsNullOrEmpty(kycName) || !kycName.TrimStart().StartsWith("{")
                        ? "{}"
                        : kycName.Trim());
                }

                // Binance: timestamp, recvWindow는 마지막에. 서명할 문자열은 signature 제외.
                parameters.Add("recvWindow", "60000"); // 시계 오차 허용

                string stringToSign = string.Join("&", parameters.OrderBy(x => x.Key)
                    .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
                string signature = CreateSignature(stringToSign, secretKey);
                parameters.Add("signature", signature);

                // Binance 문서: "signature Must be the last parameter"
                var bodyItems = parameters.Where(kv => kv.Key != "signature")
                    .OrderBy(x => x.Key)
                    .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}")
                    .ToList();
                bodyItems.Add($"signature={Uri.EscapeDataString(signature)}");
                string bodyString = string.Join("&", bodyItems);

                var logParams = string.Join(", ", parameters.Where(p => p.Key != "signature")
                    .Select(p => $"{p.Key}={p.Value}"));

                RestResponse response;
                if (travelRuleRequired)
                {
                    ApiRequestLogger.LogRequest("POST", $"{SAPI_BASE_URL}/sapi/v1/localentity/withdraw/apply", logParams + " [Travel Rule]");
                    ApiRequestLogger.LogRequestBody(bodyString);

                    var clientTr = new RestClient(SAPI_BASE_URL);
                    var requestTr = new RestRequest("/sapi/v1/localentity/withdraw/apply", Method.Post);
                    requestTr.AddHeader("X-MBX-APIKEY", accessKey);
                    requestTr.AddStringBody(bodyString, "application/x-www-form-urlencoded");
                    response = await clientTr.ExecuteAsync(requestTr);
                }
                else
                {
                    ApiRequestLogger.LogRequest("POST", $"{SAPI_BASE_URL}/sapi/v1/capital/withdraw/apply", logParams);
                    ApiRequestLogger.LogRequestBody(bodyString);

                    var client = new RestClient(SAPI_BASE_URL);
                    var request = new RestRequest("/sapi/v1/capital/withdraw/apply", Method.Post);
                    request.AddHeader("X-MBX-APIKEY", accessKey);
                    request.AddStringBody(bodyString, "application/x-www-form-urlencoded");
                    response = await client.ExecuteAsync(request);
                }

                ApiRequestLogger.LogResponse((int)response.StatusCode, response.Content ?? "");

                if (response.IsSuccessful)
                {
                    var content = response.Content ?? "";
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        errorMessage = "서버 응답이 비어있습니다.";
                        m_lastErrorMessage = errorMessage;
                    }
                    else
                    {
                        try
                        {
                            JObject responseObj = JObject.Parse(content);
                            if (responseObj["id"] != null)
                            {
                                bResult = true;
                                return (true, responseObj["id"]?.ToString() ?? "Success");
                            }
                            if (responseObj["trId"] != null && string.Equals(responseObj["accpted"]?.ToString(), "True", StringComparison.OrdinalIgnoreCase))
                            {
                                bResult = true;
                                return (true, responseObj["trId"]?.ToString() ?? responseObj["info"]?.ToString() ?? "Success");
                            }
                            if (responseObj["accpted"]?.ToString() != "True" && responseObj["info"] != null)
                            {
                                errorMessage = responseObj["info"]?.ToString() ?? ParseBinanceError(content);
                                m_lastErrorMessage = errorMessage;
                            }
                            else
                            {
                                errorMessage = ParseBinanceError(content);
                                m_lastErrorMessage = errorMessage;
                            }
                        }
                        catch
                        {
                            errorMessage = content.Length > 300 ? content.Substring(0, 300) + "..." : content;
                            m_lastErrorMessage = errorMessage;
                        }
                    }
                }
                else
                {
                    errorMessage = ParseBinanceError(response.Content ?? "", (int?)response.StatusCode);
                    if (string.IsNullOrEmpty(errorMessage))
                        errorMessage = response.ErrorMessage ?? $"HTTP {(int)response.StatusCode}";
                    if (travelRuleRequired && (errorMessage.Contains("Questionnaire") || errorMessage.Contains("questionnaire")))
                    {
                        errorMessage += " (Travel Rule: questionnaire 형식이 필요합니다. Binance 문서 참고)";
                    }
                    m_lastErrorMessage = errorMessage;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{ex.Message}";
                m_lastErrorMessage = errorMessage;
            }

            return (bResult, errorMessage);
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
        {
            throw new NotImplementedException();
        }

        public override (bool, List<string>) GetMarketSupportCoins()
        {
            throw new NotImplementedException();
        }

        public override Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
        {
            throw new NotImplementedException();
        }

        public override Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
        {
            throw new NotImplementedException();
        }

        public override Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier)
        {
            throw new NotImplementedException();
        }

        public override Task<(bool, string)> CancelOrder(string coinCode, string identifier)
        {
            throw new NotImplementedException();
        }

        public override double CalcOrderPrice(string coinCode, double price)
        {
            throw new NotImplementedException();
        }

        public override double CalcOrderVolume(string coinCode, double volume)
        {
            throw new NotImplementedException();
        }

        public override Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
        {
            throw new NotImplementedException();
        }
    }
}
