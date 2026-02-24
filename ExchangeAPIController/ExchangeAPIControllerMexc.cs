using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace ExchangeAPIController
{
    public class ExchangeAPIControllerMexc : ExchangeAPIControllerBase
    {


        private EnumExchange m_exchange = EnumExchange.MEXC;
        private const string BASE_URL = "https://api.mexc.com";

        private string m_lastErrorMessage = "";
        public override string GetLastErrorMessage()
        {
            return m_lastErrorMessage;
        }

        public async override Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount()
        {
            bool bResult = false;
            List<Currency> currencies = new List<Currency>();
            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                // 타임스탬프 생성 (밀리초)
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // 서명 생성을 위한 파라미터 설정
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "timestamp", timestamp.ToString() }
                };

                // 파라미터를 정렬하고 쿼리 문자열 생성
                string queryString = string.Join("&", parameters.OrderBy(x => x.Key)
                          .Select(x => $"{x.Key}={x.Value}"));
                
                // HMAC SHA256 서명 생성
                string signature = CreateSignature(queryString, secretKey);
                parameters.Add("signature", signature);

                var client = new RestClient("https://api.mexc.com");
                var request = new RestRequest("/api/v3/account", Method.Get);
                
                foreach (var param in parameters)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }

                request.AddHeader("X-MEXC-APIKEY", accessKey);
                request.AddHeader("Content-Type", "application/json");
                
                RestResponse response = client.Execute(request);
                bResult = response.IsSuccessful;

                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);
                    var balances = responseObj["balances"].ToObject<JArray>();

                    foreach (JObject balance in balances)
                    {
                        string currencyCode = balance["asset"].ToString();
                        double free = double.Parse(balance["free"].ToString());
                        double locked = double.Parse(balance["locked"].ToString());

                        Currency currency = new Currency(currencyCode, free, locked, 0, false);
                        currencies.Add(currency);
                    }
                    bResult = true;
                }
                else
                {
                    m_lastErrorMessage = response.ErrorMessage;
                }
            }
            catch(Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                bResult = false;
            }
            
            return (bResult, currencies);
        }

        private string CreateSignature(string queryString, string secretKey)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

   
        


        public override (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            bool bResult = false;
            List<NetworkInfo> networkInfos = new List<NetworkInfo>();

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                // 타임스탬프 생성 (밀리초)
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // 서명 생성을 위한 파라미터 설정
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "timestamp", timestamp.ToString() }
                };

                // 파라미터를 정렬하고 쿼리 문자열 생성
                string queryString = string.Join("&", parameters.OrderBy(x => x.Key)
                          .Select(x => $"{x.Key}={x.Value}"));

                // HMAC SHA256 서명 생성
                string signature = CreateSignature(queryString, secretKey);
                parameters.Add("signature", signature);

                var client = new RestClient("https://api.mexc.com");
                var request = new RestRequest("/api/v3/capital/config/getall", Method.Get);

                foreach (var param in parameters)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }

                request.AddHeader("X-MEXC-APIKEY", accessKey);
                request.AddHeader("Content-Type", "application/json");

                RestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    JArray responseArray = JArray.Parse(response.Content);
                    var coinInfo = responseArray.FirstOrDefault(x => x["coin"].ToString().Equals(coinName, StringComparison.OrdinalIgnoreCase));

                    if (coinInfo != null)
                    {
                        var networks = coinInfo["networkList"].ToObject<JArray>();
                        foreach (JObject network in networks)
                        {
                            NetworkInfo networkInfo = new NetworkInfo
                            {
                                ChainName = network["network"]?.ToString() ?? "",
                                WithdrawFee = decimal.TryParse(network["withdrawFee"]?.ToString(), out decimal withdrawFee) ? withdrawFee : 0m,
                                DepositMin = decimal.TryParse(network["minDeposit"]?.ToString(), out decimal depositMin) ? depositMin : 0m,
                                WithdrawMin = decimal.TryParse(network["minWithdraw"]?.ToString(), out decimal withdrawMin) ? withdrawMin : 0m,
                                DepositEnabled = bool.TryParse(network["depositEnabled"]?.ToString(), out bool depositEnabled) && depositEnabled,
                                WithdrawEnabled = bool.TryParse(network["withdrawEnabled"]?.ToString(), out bool withdrawEnabled) && withdrawEnabled,
                                ContractAddress = network["contractAddress"]?.ToString() ?? "",
                                Confirmation = int.TryParse(network["depositConfirm"]?.ToString(), out int confirm) ? confirm : 0
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
                    JObject errorObj = JObject.Parse(response.Content);
                    m_lastErrorMessage = $"Error {errorObj["code"]}: {errorObj["msg"]}";
                }
            }
            catch (Exception ex)
            {
                m_lastErrorMessage = ex.Message;
                bResult = false;
            }

            return (bResult, networkInfos);
        }

        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity, string kycName, string tag)
        {
            bool bResult = false;
            string errorMessage = "";

            try
            {
                string accessKey = ApikeySetting.GetInstance().GetExchangeAccessKey(m_exchange);
                string secretKey = ApikeySetting.GetInstance().GetExchangeSecretKey(m_exchange);

                // 수량 = 출금 요청 금액(수수료 포함). API가 수수료 차감 후 전송.
                // 서버 시간 가져오기
                var client = new RestClient(BASE_URL);
                var timeRequest = new RestRequest("/api/v3/time", Method.Get);
                var timeResponse = client.Execute(timeRequest);
                var serverTime = JObject.Parse(timeResponse.Content)["serverTime"].ToString();

                // 파라미터 설정
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "coin", coinName },
                    { "network", chainName },
                    { "address", address },
                    { "amount", volume.ToString() },
                    { "memo", string.IsNullOrEmpty(tag) ? "" : tag }
                };

                // 쿼리 문자열 생성 (정렬 없이)
                string requestBody = string.Join("&", parameters
                    .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

                // timestamp와 함께 서명 생성
                string toSign = $"{requestBody}&timestamp={serverTime}";
                string signature = CreateSignature(toSign, secretKey);

                // 최종 파라미터에 timestamp와 signature 추가
                parameters.Add("timestamp", serverTime);
                parameters.Add("signature", signature);

                var request = new RestRequest("/api/v3/capital/withdraw/apply", Method.Post);
                request.AddHeader("X-MEXC-APIKEY", accessKey);
                request.AddHeader("Content-Type", "application/json");

                // 파라미터 추가
                foreach (var param in parameters)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }

                RestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);
                    if (responseObj["id"] != null)
                    {
                        bResult = true;
                    }
                    else
                    {
                        errorMessage = responseObj["msg"]?.ToString() ?? "Unknown error";
                        m_lastErrorMessage = errorMessage;
                    }
                }
                else
                {
                    errorMessage = response.Content ?? response.ErrorMessage ?? "Request failed";
                    m_lastErrorMessage = errorMessage;
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

        /// <summary>MEXC: Travel Rule 조회 API 없음. 규정은 거래소 안내 참고.</summary>
        public override Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((false, "MEXC: 조회 API 없음. 규정 확인 권장."));
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
