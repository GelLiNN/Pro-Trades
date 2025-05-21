namespace PT.Models.CoreModels
{
    public class PTHistory
    {
        /// <summary>
        /// Pro Trades price history data
        /// </summary>
        public List<PTDay> PriceHistory { get; set; }

        /// <summary>
        /// Price history object for internal indicators library
        /// </summary>
        public IEnumerable<Skender.Stock.Indicators.Quote> SkenderHistory;

        /// <summary>
        /// List with day unit count (1 == today) looking back
        /// </summary>
        public List<decimal> HistoricalVwapXList { get; set; }

        /// <summary>
        /// List with 30d average price (0), 10d average price (1), 1d average price (2)
        /// </summary>
        public List<decimal> HistoricalVwapYList { get; set; }

        /// <summary>
        /// List with day unit count (1 == today) looking back
        /// </summary>
        public List<decimal> HistoricalVolAvgXList { get; set; }

        /// <summary>
        /// List with 30d average volume (0), 10d average volume (1), 1d volume (2)
        /// </summary>
        public List<decimal> HistoricalVolAvgYList { get; set; }

        /// <summary>
        /// List with day unit count (1 == today) looking back
        /// </summary>
        public List<decimal> Price10XList { get; set; }

        /// <summary>
        /// List with volume weighted average price with INDEX 0 earliest
        /// </summary>
        public List<decimal> Price10YList { get; set; }

        /// <summary>
        /// List with day unit count (1 == today) looking back
        /// </summary>
        public List<decimal> Volume10XList { get; set; }

        /// <summary>
        /// List with daily trading volume with INDEX 0 earliest
        /// </summary>
        public List<decimal> Volume10YList { get; set; }

        /// <summary>
        /// List with price targets
        /// </summary>
        public List<PTPriceTarget> PriceTargets { get; set; }

        /// <summary>
        /// Pro Trades Price Target (tm)
        /// </summary>
        public decimal PriceTargetProLong { get; set; }
        /// <summary>
        /// Pro Trades Price Target (tm)
        /// </summary>
        public decimal PriceTargetProShort { get; set; }

        // Average shares traded volume USD for different periods
        public decimal DollarVolume30Day { get; set; }
        public decimal DollarVolume10Day { get; set; }
        public decimal DollarVolumeToday { get; set; }

        // Average shares traded volume USD for different periods
        public decimal HighestHigh30Day { get; set; }
        public decimal AverageHigh10Day { get; set; }
        public decimal LowestLow30Day { get; set; }
        public decimal AverageLow10Day { get; set; }
        public decimal TodayOpen { get; set; }
        public decimal TodayClose { get; set; }
        public decimal TodayLow { get; set; }
        public decimal TodayHigh { get; set; }
        public decimal TodayVwap { get; set; }

        // Track specific counts of volume USD going below threshold
        public bool Has30DayQualifiedVolume { get; set; }
        public bool Has10DayQualifiedVolume { get; set; }
        public bool Has1DayQualifiedVolume { get; set; }
        public bool TradedForwardToday { get; set; }

        /// <summary>
        /// Contructor Required
        /// </summary>
        public PTHistory()
        {
            PriceHistory = new List<PTDay>();
            SkenderHistory = new List<Skender.Stock.Indicators.Quote>();
            HistoricalVwapXList = new List<decimal>();
            HistoricalVwapYList = new List<decimal>();
            HistoricalVolAvgXList = new List<decimal>();
            HistoricalVolAvgYList = new List<decimal>();
            Price10XList = new List<decimal>();
            Price10YList = new List<decimal>();
            Volume10XList = new List<decimal>();
            Volume10YList = new List<decimal>();
            PriceTargets = new List<PTPriceTarget>();

            HighestHigh30Day = 0;
            LowestLow30Day = 0;
        }

        public void AddPriceTarget(string targetDesc, decimal target)
        {
            PriceTargets.Add(new PTPriceTarget
            {
                TargetDescription = targetDesc,
                TargetPrice = target,
            });
        }
    }

    public class PTPriceTarget
    {
        public string TargetDescription { get; set; }
        public decimal TargetPrice { get; set; }
    }
}
