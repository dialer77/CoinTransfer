using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public abstract class ExchangeAPIControllerBase
    {
        public abstract string GetLastErrorMessage();

        public abstract (bool, List<string>) GetMarketCoinsAndPairs();

        /// <summary>
        /// 해당 거래소의 지원 코인 리스트 확인
        /// </summary>
        /// <returns></returns>
        public abstract (bool, List<string>) GetMarketSupportCoins();

        /// <summary>
        /// 나의 지갑의 coinName 의 보유량을 읽어온다
        /// </summary>
        /// <param name="coinName"></param>
        /// <returns></returns>
        public abstract Task<(bool, List<Currency>)> GetCoinHoldingForMyAccount();

        /// <summary>
        /// marketCode 의 현재가격을 읽어온다
        /// </summary>
        /// <param name="marketCode"></param>
        /// <returns></returns>
        public abstract Task<(bool, PriceInfo)> GetCurrentPriceInfo(string marketCode);

        /// <summary>
        /// marketCode List의 각 현재가격을 읽어온다
        /// </summary>
        /// <param name="listMarketCode"></param>
        /// <returns></returns>
        public abstract Task<(bool, Dictionary<string, PriceInfo>)> GetCurrentPriceInfos(List<string> listMarketCode);


        public virtual (bool, List<NetworkInfo>) GetCoinNetworksDetail(string coinName)
        {
            return (false, null);
        }

        /// <summary>
        ///  코인 출금
        /// </summary>
        /// <param name="coinName"></param>
        /// <param name="volume"></param>
        /// <param name="address"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public abstract Task<(bool, string)> WithdrawCoin(string coinName, string chainName, double volume, string address, string exchangeEntity = "", string kycName = "", string tag = null);

        /// <summary>
        /// 코인 주문
        /// </summary>
        /// <param name="coinName"></param>
        /// <param name="pair"></param>
        /// <param name="volume"></param>
        /// <param name="price"></param>
        /// <param name="tradeType"></param>
        /// <returns></returns>
        public abstract Task<(bool, string)> OrderCoin(string coinCode, double volume, double price, EnumOrderSide orderSide, EnumTradeType tradeType, string identifier);

        /// <summary>
        /// 주문 취소
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public abstract Task<(bool, string)> CancelOrder(string coinCode,  string identifier);

        /// <summary>
        /// 현재 거래가격에 맞는 최소 주문단위에 맞게 가격 계산
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public abstract double CalcOrderPrice(string coinCode,  double price);

        /// <summary>
        /// 현재 거래가격에 맞는 최소 주문단위에 맞게 거래 수량 계산
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public abstract double CalcOrderVolume(string coinCode, double volume);


        public abstract Task<(bool, OrderInfo)> CheckOrderInfo(string coinCode, string identifier);

        // @abc.abstractmethod
        // def GetWithdrawFee(self, coinName):
        //     pass

        // @abc.abstractmethod
        // def CheckWithdrawStaust(self, id, coinName):
        //     pass

    }
}
