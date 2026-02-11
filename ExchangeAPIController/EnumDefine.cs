using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public enum EnumExchange
    {
        Debug,
        Binance,
        //Upbit,
        //Bithumb,
        //Huobi,
        //Binance_Future,
        //Coinone,
        Bybit,
        MEXC,
    }

    public enum EnumTradeType
    {
        /// <summary>
        /// 시장가 주문(매도)
        /// </summary>
        market,
        /// <summary>
        /// 시장가 주문(매수)
        /// </summary>
        price,
        /// <summary>
        /// 지정가 주문
        /// </summary>
        limit,
    }

    public enum EnumOrderSide
    {
        /// <summary>
        /// 매수
        /// </summary>
        bid,
        /// <summary>
        /// 매도
        /// </summary>
        ask,
    }

    public enum EnumOrderStatus
    {
        NONE,
        CANCELED,
        FILLED,
    }

}
