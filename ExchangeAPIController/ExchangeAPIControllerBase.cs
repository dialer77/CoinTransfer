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
        /// 출금 허용(화이트리스트) 주소 리스트 조회. (빗썸 등에서 구현)
        /// </summary>
        /// <returns>(성공 여부, 주소 목록. 실패 시 null)</returns>
        public virtual (bool success, System.Collections.Generic.List<WithdrawAddressItem> list) GetWithdrawAllowedAddresses()
        {
            return (false, null);
        }

        /// <summary>
        /// 해당 코인·네트워크 출금 지원 여부 검증. (빗썸 등에서 구현)
        /// </summary>
        /// <param name="coinName">코인 심볼</param>
        /// <param name="chainName">출금 네트워크(net_type, ex.BTC, DASH)</param>
        /// <returns>(지원 여부, 실패 시 사유 메시지)</returns>
        public virtual (bool supported, string message) ValidateWithdrawSupport(string coinName, string chainName)
        {
            return (true, "");
        }

        /// <summary>
        /// 출금 지원 확인 등으로 조회한 마지막 수수료 정보. UI 수수료 라벨 갱신용.
        /// </summary>
        /// <returns>(정보 있음, 고정수수료, 최소출금, 비율수수료%)</returns>
        public virtual (bool hasInfo, decimal withdrawFee, decimal withdrawMin, decimal withdrawPctFee) GetLastWithdrawFeeFromValidation()
        {
            return (false, 0m, 0m, 0m);
        }

        /// <summary>
        /// Travel Rule 준수 필요 여부 확인 (Binance 등 일부 거래소만 해당)
        /// </summary>
        public virtual Task<(bool required, string info)> CheckTravelRuleRequiredAsync()
        {
            return Task.FromResult((false, ""));
        }

        /// <summary>
        /// 출금 수수료(고정+비율) 계산. 수량은 수수료 포함 금액이므로 실제 수령 예상 = volume - 수수료.
        /// (표시용 등. 출금 API에는 수량을 그대로 전달하며, 거래소가 수수료를 차감함.)
        /// </summary>
        protected static double CalcWithdrawTotalFee(double volume, decimal withdrawFee, decimal withdrawPercentageFee)
        {
            return (double)withdrawFee + volume * (double)withdrawPercentageFee / 100.0;
        }

        /// <summary>
        /// 수수료를 포함한 출금 요청 금액 (수취 희망 volume + 수수료). API에서 수수료를 자동 차감하지 않는 거래소(업비트 등)용.
        /// </summary>
        protected static double CalcWithdrawAmountIncludingFee(double volume, decimal withdrawFee, decimal withdrawPercentageFee)
        {
            return volume + CalcWithdrawTotalFee(volume, withdrawFee, withdrawPercentageFee);
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
        public abstract Task<(bool, string)> CancelOrder(string coinCode, string identifier);

        /// <summary>
        /// 현재 거래가격에 맞는 최소 주문단위에 맞게 가격 계산
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public abstract double CalcOrderPrice(string coinCode, double price);

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
