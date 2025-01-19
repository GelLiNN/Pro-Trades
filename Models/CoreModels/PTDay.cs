namespace PT.Models.CoreModels
{
    public class PTDay
    {
        public decimal PriceOpen { get; set; }
        public decimal PriceClose { get; set; }
        public decimal PriceHigh { get; set; }
        public decimal PriceLow { get; set; }
        public decimal PriceVwap { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePercent { get; set; }
        public decimal PriceCandleMean { get; set; }
        public decimal TradedForwardChange { get; set; }
        public decimal TradedForwardChangePercent { get; set; }
        public decimal Volume { get; set; }
        public decimal DollarVolume { get; set; }
        public bool? PassVolumeFilter { get; set; }
        public bool TradedForward { get; set; }
        public bool ClosedGreen { get; set; }

        public DateTime RecordDate { get; set; }
    }
}
