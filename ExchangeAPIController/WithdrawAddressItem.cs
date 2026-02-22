namespace ExchangeAPIController
{
    /// <summary>
    /// 출금 허용(화이트리스트) 주소 한 건. 빗썸 등 거래소 출금 허용 주소 조회 시 사용.
    /// </summary>
    public class WithdrawAddressItem
    {
        public string Currency { get; set; }
        public string NetType { get; set; }
        public string Address { get; set; }
        public string Label { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Label))
                return $"[{Currency}/{NetType}] {Address} ({Label})";
            return $"[{Currency}/{NetType}] {Address}";
        }
    }
}
