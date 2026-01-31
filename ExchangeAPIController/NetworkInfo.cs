using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    // 네트워크 상세 정보를 포함하는 클래스
    public class NetworkInfo
    {
        public string ChainName { get; set; }        // chain
        public int Confirmation { get; set; }        // confirmation
        public decimal WithdrawFee { get; set; }     // withdrawFee
        public decimal DepositMin { get; set; }      // depositMin
        public decimal WithdrawMin { get; set; }     // withdrawMin
        public bool DepositEnabled { get; set; }     // chainDeposit == "1"
        public bool WithdrawEnabled { get; set; }    // chainWithdraw == "1"
        public int MinAccuracy { get; set; }         // minAccuracy
        public decimal WithdrawPercentageFee { get; set; }  // withdrawPercentageFee
        public string ContractAddress { get; set; }   // contractAddress

        public override string ToString()
        {
            return ChainName;
        }
    }
}

