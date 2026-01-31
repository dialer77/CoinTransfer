using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class PriceInfo
    {
        public double TradePrice { get; set; } = 0;

        public double AccTradeVolume_24h { get; set; } = 0;

        public double AccTradeVolume { get; set; } = 0;

    }
}
