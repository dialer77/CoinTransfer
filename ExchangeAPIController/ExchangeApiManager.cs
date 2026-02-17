using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class ExchangeApiManager
    {
        private static ExchangeApiManager s_instance = null;

        public static ExchangeApiManager GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new ExchangeApiManager();
            }

            return s_instance;
        }

        public ExchangeAPIControllerBase GetExchangeAPIController(EnumExchange exchange)
        {
            ExchangeAPIControllerBase controller = null;

            switch (exchange)
            {
                case EnumExchange.Debug:
                    controller = new ExchangeAPIControllerDebug();
                    break;
                case EnumExchange.Binance:
                    controller = new ExchangeAPIControllerBinance();
                    break;
                case EnumExchange.Upbit:
                    controller = new ExchangeAPIControllerUpbit();
                    break;
                case EnumExchange.Bithumb:
                    controller = new ExchangeAPIControllerBithumb();
                    break;
                case EnumExchange.Gate:
                    controller = new ExchangeAPIControllerGate();
                    break;
                case EnumExchange.Bybit:
                    controller = new ExchangeAPIControllerBybit();
                    break;
                case EnumExchange.OKX:
                    controller = new ExchangeAPIControllerOKX();
                    break;
                case EnumExchange.MEXC:
                    controller = new ExchangeAPIControllerMexc();
                    break;
                case EnumExchange.Bitget:
                    controller = new ExchangeAPIControllerBitget();
                    break;
            }

            return controller;
        }

    }
}
