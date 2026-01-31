using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class OrderInfo
    {
        public EnumOrderStatus OrderStatus { get; set; } = EnumOrderStatus.NONE;

        public double TradePrice { get; set; } = 0;

        public double TradeVolume { get; set; } = 0;

        public double ExcutedVolume { get; set; } = 0;

    }
}
