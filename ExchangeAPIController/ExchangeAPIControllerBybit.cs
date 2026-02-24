using bybit.net.api.Models.Account;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using bybit.net.api.ApiServiceImp;
using Newtonsoft.Json.Serialization;

namespace ExchangeAPIController
{
    public class ExchangeAPIControllerBybit : ExchangeAPIControllerBase
    {


        private EnumExchange m_exchange = EnumExchange.Bybit;
        private const string BASE_URL = "https://api.bybit.com";

        private string m_lastErrorMessage = "";
        public override string GetLastErrorMessage()
        {
            return m_lastErrorMessage;
        }

        /// <summary>바이비트 서버 시간(ms). 타임스탬프 오차 보정용.</summary>
        private async Task<long> GetBybitServerTimeMsAsync()
        {
            try
            {
                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/market/time", Method.Get);
                RestResponse response = await client.ExecuteAsync(request);
                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var jo = JObject.Parse(response.Content);
                    if (jo["retCode"]?.Value<int>() == 0 && jo["result"]?["timeSecond"] != null)
                    {
                        long sec = long.Parse(jo["result"]["timeSecond"].ToString());
                        return sec * 1000;
                    }
                }
            }
            catch { /* fallback to client time */ }
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public async override Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            bool bResult = false;
            List<Currency> currencies = new List<Currency>();
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                // 서버 시간 사용 시 타임스탬프 오차로 invalid request 방지 (recv_window 내에 맞춤)
                long timestamp = await GetBybitServerTimeMsAsync();
                const string recvWindow = "60000"; // 60초 허용 (PC 시계 오차 대응)
                string queryString = "accountType=UNIFIED";
                string signature = CreateSignatureV5Get(timestamp, accessKey, recvWindow, queryString, secretKey);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/account/wallet-balance", Method.Get);
                request.AddQueryParameter("accountType", "UNIFIED");
                request.AddHeader("X-BAPI-API-KEY", accessKey);
                request.AddHeader("X-BAPI-SIGN", signature);
                request.AddHeader("X-BAPI-TIMESTAMP", timestamp.ToString());
                request.AddHeader("X-BAPI-RECV-WINDOW", recvWindow);
                request.AddHeader("Accept", "application/json");

                RestResponse response = await client.ExecuteAsync(request);
                string responseResult = response?.Content ?? "";

                if (!response.IsSuccessful)
                {
                    m_lastErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ErrorMessage}";
                    return (false, currencies);
                }

                JObject responseObj = JObject.Parse(responseResult);
                if (responseObj["retCode"]?.Value<int>() != 0)
                {
                    m_lastErrorMessage = responseObj["retMsg"]?.ToString() ?? "Unknown error";
                    return (false, currencies);
                }

                var list = responseObj["result"]?["list"] as JArray;
                if (list == null || list.Count == 0)
                    return (true, currencies);

                var coinList = list[0]["coin"] as JArray;
                if (coinList == null)
                    return (true, currencies);

                foreach (JToken item in coinList)
                {
                    string currencyCode = item["coin"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(currencyCode)) continue;
                    double balance = double.TryParse(item["walletBalance"]?.ToString(), out double wb) ? wb : 0;
                    double locked = double.TryParse(item["locked"]?.ToString(), out double lk) ? lk : 0;
                    currencies.Add(new Currency(currencyCode, balance, locked, 0, false));
                }
                bResult = true;
                return (bResult, currencies);
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
            }
            return (bResult, currencies);
        }

        /// <summary>v3 스타일(쿼리 서명) - v5에서는 사용하지 않음.</summary>
        private string CreateSignature(string queryString, string secretKey)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>v5 GET 서명: timestamp + api_key + recv_window + queryString (쿼리에는 인증 파라미터 제외)</summary>
        private string CreateSignatureV5Get(long timestamp, string apiKey, string recvWindow, string queryString, string secretKey)
        {
            string signPayload = timestamp + apiKey + recvWindow + queryString;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signPayload));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private string GeneratePostSignature(IDictionary<string, object> parameters, string apiKey, string secretKey)
        {
            
            string paramJson = JsonConvert.SerializeObject(parameters);
            string rawData = parameters["timestamp"] + apiKey  + "5000" + paramJson;  // RecvWindow는 5000으로 고정

            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(signature).Replace("-", "").ToLower();
        }

        // 상세 정보를 포함한 버전의 메서드
        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            bool bResult = false;
            List<NetworkInfo> networkInfos = new List<NetworkInfo>();

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                const string recvWindow = "5000";
                // v5 GET: 쿼리에는 요청 파라미터만 (coin), 서명은 timestamp+api_key+recv_window+queryString
                string queryString = string.IsNullOrEmpty(coinName) ? "" : $"coin={coinName}";
                string signature = CreateSignatureV5Get(timestamp, accessKey, recvWindow, queryString, secretKey);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/asset/coin/query-info", Method.Get);
                if (!string.IsNullOrEmpty(coinName))
                    request.AddQueryParameter("coin", coinName);

                request.AddHeader("X-BAPI-API-KEY", accessKey);
                request.AddHeader("X-BAPI-SIGN", signature);
                request.AddHeader("X-BAPI-TIMESTAMP", timestamp.ToString());
                request.AddHeader("X-BAPI-RECV-WINDOW", recvWindow);
                request.AddHeader("Accept", "application/json");
                RestResponse response = client.Execute(request);

                JObject responseObj = JObject.Parse(response.Content);
                if (responseObj["retCode"].ToString() == "0")
                {
                    var rows = responseObj["result"]["rows"].ToObject<JArray>();
                    if (rows.Count > 0)
                    {
                        var coin = rows[0];
                        var chains = coin["chains"].ToObject<JArray>();

                        foreach (JObject chain in chains)
                        {
                            NetworkInfo networkInfo = new NetworkInfo
                            {
                                ChainName = chain["chain"]?.ToString() ?? "",
                                Confirmation = int.TryParse(chain["confirmation"]?.ToString(), out int confirmation) ? confirmation : 0,
                                WithdrawFee = decimal.TryParse(chain["withdrawFee"]?.ToString(), out decimal withdrawFee) ? withdrawFee : 0m,
                                DepositMin = decimal.TryParse(chain["depositMin"]?.ToString(), out decimal depositMin) ? depositMin : 0m,
                                WithdrawMin = decimal.TryParse(chain["withdrawMin"]?.ToString(), out decimal withdrawMin) ? withdrawMin : 0m,
                                DepositEnabled = chain["chainDeposit"]?.ToString() == "1",
                                WithdrawEnabled = chain["chainWithdraw"]?.ToString() == "1",
                                MinAccuracy = int.TryParse(chain["minAccuracy"]?.ToString(), out int minAccuracy) ? minAccuracy : 0,
                                WithdrawPercentageFee = decimal.TryParse(chain["withdrawPercentageFee"]?.ToString(), out decimal percentageFee) ? percentageFee : 0m,
                                ContractAddress = chain["contractAddress"]?.ToString() ?? ""
                            };

                            networkInfos.Add(networkInfo);
                        }
                        bResult = true;
                    }
                    else
                    {
                        m_lastErrorMessage = "Coin not found";
                    }
                }
                else
                {
                    m_lastErrorMessage = responseObj["retMsg"].ToString();
                }
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                bResult = false;
            }

            return (bResult, networkInfos);
        }

        private async Task<(bool, string)> CreateInternalTransfer(string coinName, double volume, string from, string to)
        {
            bool bResult = false;
            string errorMessage = "";

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                Guid myuuid = Guid.NewGuid();
                string myuuidAsString = myuuid.ToString();

                var parameters = new Dictionary<string, object>
                {
                    {"transferId", myuuidAsString},
                    {"coin", coinName.ToUpper()},
                    {"amount", volume.ToString()},
                    {"fromAccountType", from},
                    {"toAccountType", to}
                };

                // 서명 생성
                string paramJson = JsonConvert.SerializeObject(parameters);
                string rawData = timeStamp + accessKey + "20000" + paramJson;  // RecvWindow는 5000으로 고정

                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/asset/transfer/inter-transfer", Method.Post);
                
                request.AddHeader("X-BAPI-API-KEY", accessKey);
                request.AddHeader("X-BAPI-SIGN", signature);
                request.AddHeader("X-BAPI-TIMESTAMP", timeStamp.ToString());
                request.AddHeader("X-BAPI-RECV-WINDOW", "20000");

                request.AddJsonBody(paramJson);

                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);
                    int retCode = responseObj["retCode"].Value<int>();
                    string retMsg = responseObj["retMsg"].Value<string>();

                    if (retCode == 0)
                    {
                        bResult = true;
                        return (bResult, retMsg);
                    }
                    else
                    {
                        errorMessage = $"Error {retCode}: {retMsg}";
                        m_lastErrorMessage = errorMessage;
                        return (false, errorMessage);
                    }
                }
                else
                {
                    errorMessage = $"HTTP Error: {response.StatusCode} - {response.ErrorMessage}";
                    m_lastErrorMessage = errorMessage;
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                m_lastErrorMessage = errorMessage;
                bResult = false;
            }

            return (bResult, errorMessage);
        }

        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity, string kycName, string tag)
        {
            bool bResult = false;
            string errorMessage = "";

            try
            {
                (bool, List<NetworkInfo>) networkResult = GetCoinNetworksDetail(coinName);
                if (networkResult.Item1 == false)
                {
                    return (false, m_lastErrorMessage);
                }
                List<NetworkInfo> networkList = networkResult.Item2;
                NetworkInfo network = networkList.Where(x => x.ChainName == chainName).ToList().FirstOrDefault();
                if (network == null)
                {
                    return (false, "chain 이 잘못되었습니다");
                }

                // 수량 = 출금 요청 금액(수수료 포함). API가 수수료 차감 후 전송.
                (bool, string) transferResult = await CreateInternalTransfer(coinName, volume, "UNIFIED", "FUND");
                if (transferResult.Item1 == false)
                {
                    return transferResult;
                }
                await Task.Delay(100);

                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);
                bybit.net.api.ApiServiceImp.BybitAssetService assetService = new bybit.net.api.ApiServiceImp.BybitAssetService(accessKey, secretKey, url: BASE_URL);
                string result = await assetService.CreateInternalTransfer(AccountType.Spot, AccountType.Fund, coinName, volume.ToString());

                long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // beneficiary 객체 생성
                kycName = exchangeEntity == "others" ? null : kycName;
                var beneficiary = new Dictionary<string, string>
        {
            { "vaspEntityId", exchangeEntity },
            { "beneficiaryName", kycName }
        };

                var parameters = new Dictionary<string, object>
                {
                    {"coin", coinName},
                    {"chain", chainName},
                    {"address", address},
                    {"amount", volume.ToString()},
                    {"timestamp", timeStamp},
                    {"accountType", "FUND"},
                    {"beneficiary", beneficiary}

                };

                if (!string.IsNullOrEmpty(tag))
                {
                    parameters.Add("tag", tag);
                }

                // 서명 생성
                string paramJson = JsonConvert.SerializeObject(parameters);
                string rawData = timeStamp + accessKey + "5000" + paramJson;  // RecvWindow는 5000으로 고정

                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower(); 

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/asset/withdraw/create", Method.Post);
                
                request.AddHeader("X-BAPI-API-KEY", accessKey);
                request.AddHeader("X-BAPI-SIGN", signature);
                request.AddHeader("X-BAPI-TIMESTAMP", timeStamp.ToString());
                request.AddHeader("X-BAPI-RECV-WINDOW", "5000");
                request.AddHeader("Content-Type", "application/json");
                
                request.AddJsonBody(paramJson);

                RestResponse response = await client.ExecuteAsync(request);
                
                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);
                    int retCode = responseObj["retCode"].Value<int>();
                    string retMsg = responseObj["retMsg"].Value<string>();

                    if (retCode == 0)
                    {
                        bResult = true;
                        return (bResult, retMsg);
                    }
                    else
                    {
                        errorMessage = $"Error {retCode}: {retMsg}";
                        m_lastErrorMessage = errorMessage;

                        await CreateInternalTransfer(coinName, volume, "FUND", "UNIFIED");
                        return (false, errorMessage);
                    }
                }
                else
                {
                    errorMessage = $"HTTP Error: {response.StatusCode} - {response.ErrorMessage}";
                    m_lastErrorMessage = errorMessage;

                    await CreateInternalTransfer(coinName, volume, "FUND", "UNIFIED");
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                m_lastErrorMessage = errorMessage;
                bResult = false;
            }

            return (bResult, errorMessage);
        }

        /// <summary>바이비트: 일부 지역/사용자에게 beneficiary(VASP) 정보 필요. 출금 시 입력.</summary>
        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((true, "일부 지역: beneficiary(VASP) 정보 필요. 출금 시 입력."));
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

        public async Task<(bool, Dictionary<string, string>)> GetExchangeEntities()
        {
            bool bResult = false;
            Dictionary<string, string> vaspDictionary = new Dictionary<string, string>();
            
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                const string recvWindow = "5000";
                string queryString = ""; // v5 GET 파라미터 없음
                string signature = CreateSignatureV5Get(timestamp, accessKey, recvWindow, queryString, secretKey);

                var client = new RestClient(BASE_URL);
                var request = new RestRequest("/v5/asset/withdraw/vasp/list", Method.Get);
                request.AddHeader("X-BAPI-API-KEY", accessKey);
                request.AddHeader("X-BAPI-SIGN", signature);
                request.AddHeader("X-BAPI-TIMESTAMP", timestamp.ToString());
                request.AddHeader("X-BAPI-RECV-WINDOW", recvWindow);
                request.AddHeader("Accept", "application/json");
                RestResponse response = await client.ExecuteAsync(request);
                
                if (response.IsSuccessful)
                {
                    var responseObj = JObject.Parse(response.Content);
                    if (responseObj["retCode"].ToString() == "0")
                    {
                        var vaspArray = responseObj["result"]["vasp"] as JArray;
                        if (vaspArray != null)
                        {
                            foreach (var vasp in vaspArray)
                            {
                                string vaspEntityId = vasp["vaspEntityId"].ToString();
                                string vaspName = vasp["vaspName"].ToString();
                                vaspDictionary[vaspEntityId] = vaspName;
                            }
                        }
                        bResult = true;
                    }
                    else
                    {
                        m_lastErrorMessage = responseObj["retMsg"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
            }
            
            return (bResult, vaspDictionary);
        }
    }
}
