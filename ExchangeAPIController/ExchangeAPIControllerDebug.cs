using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class ExchangeAPIControllerDebug : ExchangeAPIControllerBase
    {
        private string m_lastErrorMessage = "";
        public override string GetLastErrorMessage()
        {
            return m_lastErrorMessage;
        }

        public override async Task<(bool, List<Currency>) > GetCoinHoldingForMyAccount()
        {
            return (true, new List<Currency>());
        }

        public override async Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode)
        {
            PriceInfo price = new PriceInfo();
            return (true, price);
        }

        public override async Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode)
        {
            Dictionary<string, PriceInfo> dicCurrentPrice = new Dictionary<string, PriceInfo>();
            foreach (string marketCode in listMarketCode)
            {
                dicCurrentPrice.Add(marketCode, new PriceInfo());
            }

            return (true, dicCurrentPrice);
        }

        public override (bool, List<string>) GetMarketCoinsAndPairs()
        {
            List<string> listDebugCoinPairs = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                listDebugCoinPairs.Add($"Coin{i}/KRW");
            }

            return (true, listDebugCoinPairs);
        }

        public override (bool, List<string>) GetMarketSupportCoins()
        {
            return (true, new List<string>());
        }

        public override async Task<(bool, string)> OrderCoin(string coinNmae, double volume, double price, EnumOrderSide orderSide,  EnumTradeType tradeType, string identifier)
        {
            return (true, "");
        }

        public override async Task<(bool, string)> CancelOrder(string market, string identifier)
        {
            return (true, "");
        }
        public override async Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity, string kycName, string tag)
        {
            return (true, "");
        }

        public override double CalcOrderPrice(string coinCode, double price)
        {
            double calcPrice = 0;

            if (price >= 2000000) // 1000
            {
                calcPrice = Math.Floor((price + 500) / 1000) * 1000;
            }
            else if (price >= 1000000) // 500
            {
                calcPrice = Math.Floor((price * 2 + 500) / 1000) * 500;
            }
            else if (price >= 500000) // 100
            {
                calcPrice = Math.Floor((price + 50) / 100) * 100;
            }
            else if (price >= 100000) // 50
            {
                calcPrice = Math.Floor((price * 2 + 50) / 100) * 50;
            }
            else if (price >= 10000) // 10
            {
                calcPrice = Math.Floor((price + 5) / 10) * 10;
            }
            else if (price >= 1000) // 5
            {
                calcPrice = Math.Floor((price * 2 + 5) / 10) * 5;
            }
            else if (price >= 100) // 1
            {
                calcPrice = Math.Floor(price + 0.5);
            }
            else if (price >= 10) // 0.1
            {
                calcPrice = Math.Floor(price * 10 + 0.5) / 10;
            }
            else if (price >= 1)
            {
                calcPrice = Math.Floor(price * 100 + 0.5) / 100;
            }
            else if (price >= 0.1)
            {
                calcPrice = Math.Floor(price * 1000 + 0.5) / 1000;
            }
            else
            {
                calcPrice = Math.Floor(price * 10000 + 0.5) / 10000;
            }
            return calcPrice;
        }

        public override double CalcOrderVolume(string coinCode, double volume)
        {
            return volume;
        }

        public override async Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier)
        {
            return (true, new OrderInfo());
        }
    }
}