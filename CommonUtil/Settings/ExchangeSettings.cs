using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtil.Settings
{
    public class ExchangeSettings
    {
        public string AutoTradingMarket { get; set; } = "";

        public double AutoTradingStartMoney { get; set; } = -1;

        public DateTime StartMoneyResetDate { get; set; } = DateTime.Now;

        public double AutoTradingSellCondition { get; set; } = 0.005;

        public int AutoTradingBidOrderTime { get; set; } = 600000;

        public double AutoTradingStartBuyPercent { get; set; } = 0.1;
        public double AutoTradingFinishBuyPercent { get; set; } = 3.5;
        public double AutoTradingStepBuyPercent { get; set; } = 0.05;

        public double AutoTradingFirstBuyMoney { get; set; } = 50;

        public int AutoTradingLeverage { get; set; } = 100;

        public List<string> BidOrderMarketList { get; set; } = new List<string>();



        //[NonSerialized]
        //public Dictionary<string, List<long>> BidOrderIdentifyList = new Dictionary<string, List<long>>();
        
        //[NonSerialized]
        //public Dictionary<string, List<long>> AskOrderIdentifyList = new Dictionary<string, List<long>>();

    }
}
