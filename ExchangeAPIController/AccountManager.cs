using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class AccountManager
    {
        private static AccountManager s_instance = new AccountManager();

        public static AccountManager GetInstance()
        {
            return s_instance;
        }

        private EnumExchange m_exchange = EnumExchange.Debug;

        private string m_lastErrorMessage = "";
        public string GetLastErrorMessage()
        {
            return m_lastErrorMessage;
        }

        public void SetLastErrorMessage(string msg)
        {
            m_lastErrorMessage = msg;
        }

        public void SetExchange(EnumExchange exchange)
        {
            m_exchange = exchange;
        }


        public Account GetAccount()
        {
            switch (m_exchange)
            {
                case EnumExchange.Debug:
                    return m_accountDebug;
                case EnumExchange.Binance:
                    return m_accountBinance;
                case EnumExchange.Upbit:
                    return m_accountUpbit;
                case EnumExchange.Bithumb:
                    return m_accountBithumb;
                case EnumExchange.Gate:
                    return m_accountGate;
                case EnumExchange.Bybit:
                    return m_accountBybit;
                case EnumExchange.OKX:
                    return m_accountOKX;
                case EnumExchange.MEXC:
                    return m_accountMexc;
                case EnumExchange.Bitget:
                    return m_accountBitget;
                default:
                    return m_accountDebug;
            }
        }

        private Account m_accountDebug = new Account(EnumExchange.Debug);
        private Account m_accountBinance = new Account(EnumExchange.Binance);
        private Account m_accountUpbit = new Account(EnumExchange.Upbit);
        private Account m_accountBithumb = new Account(EnumExchange.Bithumb);
        private Account m_accountGate = new Account(EnumExchange.Gate);
        private Account m_accountBybit = new Account(EnumExchange.Bybit);
        private Account m_accountOKX = new Account(EnumExchange.OKX);
        private Account m_accountMexc = new Account(EnumExchange.MEXC);
        private Account m_accountBitget = new Account(EnumExchange.Bitget);
    }

    public class Account
    {
        private EnumExchange m_exchange = EnumExchange.Debug;


        public Account(EnumExchange exchange)
        {
            m_exchange = exchange;
        }

        public List<Currency> Currencies { get; set; } = new List<Currency>();

        public Currency GetCurrency(string currencyCode)
        {
            return Currencies.Where(currency => currency.CurrencyCode == currencyCode).FirstOrDefault();
        }

        public async Task<bool> RefreshAccount()
        {
            (bool, List<Currency>) accountResult = await ExchangeApiManager.GetInstance().GetExchangeAPIController(m_exchange).GetCoinHoldingForMyAccount();

            if (accountResult.Item1)
            {
                Currencies = accountResult.Item2;
            }
            else
            {
                AccountManager.GetInstance().SetLastErrorMessage(ExchangeApiManager.GetInstance().GetExchangeAPIController(m_exchange).GetLastErrorMessage());
            }

            return accountResult.Item1;
        }

        public Currency GetBaseCurrency()
        {
            return GetCurrency("USDT");
        }
    }

    public class Currency
    {

        public Currency(string currencyCode, double balance, double locked, double avgBuyPrice, bool avgBuyPriceModified)
        {
            CurrencyCode = currencyCode;
            Balance = balance;
            Locked = locked;
            AvgBuyPrice = avgBuyPrice;
            AvgBuyPriceModified = avgBuyPriceModified;

        }

        /// <summary>
        /// 화폐 코드
        /// </summary>
        public string CurrencyCode { get; set; } = "";

        /// <summary>
        /// 주문가능 금액/수량
        /// </summary>
        public double Balance { get; set; } = 0;

        /// <summary>
        /// 주문 중 묶여있는 금액/수량
        /// </summary>
        public double Locked { get; set; } = 0;

        /// <summary>
        /// 매수평균가
        /// </summary>
        public double AvgBuyPrice { get; set; } = 0;

        /// <summary>
        /// 매수평균가 수정 여부
        /// </summary>
        public bool AvgBuyPriceModified { get; set; } = false;

    }
}
