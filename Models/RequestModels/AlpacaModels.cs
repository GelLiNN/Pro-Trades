namespace PT.Models.RequestModels
{
    /*
     * Alpaca Models
     */
    public class AlpacaHistory
    {
        // Data for 30d, 10d, today
        public List<decimal> PriceAvgXList { get; set; }
        public List<decimal> PriceAvgYList { get; set; }
        public List<decimal> VolAvgXList { get; set; }
        public List<decimal> VolAvgYList { get; set; }

        // 30 day average volume in USD
        public decimal AverageVolumeUSD { get; set; }

        // Volume today in USD
        public decimal VolumeUSD { get; set; }

        // Price history object for internal indicators library
        public IEnumerable<Skender.Stock.Indicators.Quote> PriceHistory;

        // Constructor
        public AlpacaHistory()
        {
            PriceHistory = new List<Skender.Stock.Indicators.Quote>();
            PriceAvgXList = new List<decimal>();
            PriceAvgYList = new List<decimal>();
            VolAvgXList = new List<decimal>();
            VolAvgYList = new List<decimal>();
        }
    }


    public class CompanyStatsA
    {
        public string Symbol { get; set; }
        public string CompanyName { get; set; }
        public CompositeScoreResult CompositeScoreResult { get; set; }

        public string Exchange { get; set; }
        public bool CompanyTrading { get; set; }

        public string CompanyQuoteType { get; set; }
        public string CompanyMarket { get; set; }
        public decimal? MarketCap { get; set; }
        public decimal? SharesOutstanding { get; set; }

        public decimal PeRatioForward { get; set; }
        public decimal PeRatioTrailing { get; set; }
        public decimal EpsForward { get; set; }
        public decimal EpsTrailing { get; set; }

        public decimal? BookValue { get; set; }

        public decimal Price { get; set; }

        public decimal PriceAverage50DayUSD { get; set; }
        public decimal PriceAverage200DayUSD { get; set; }

        public decimal PriceHigh52w { get; set; }
        public decimal PriceLow52w { get; set; }
        public decimal PriceAverageEstimate52w { get; set; }

        public decimal VolumeToday { get; set; }
        public decimal VolumeTodayUSD { get; set; }
        public decimal VolumeAverage10d { get; set; }
        public decimal VolumeAverage10dUSD { get; set; }
        public decimal VolumeAverage3m { get; set; }
        public decimal VolumeAverage3mUSD { get; set; }

        public TradeDataA TradeData { get; set; }

        //public decimal PriceToBook { get; set; }
        //public List<DividendTick> Dividends { get; set; }
        //public List<SplitTick> Splits { get; set; }

        public CompanyStatsA()
        {
            //Dividends = new List<DividendTick>();
            //Splits = new List<SplitTick>();
        }
    }

    public class TradeDataA
    {
        public decimal BidPrice { get; set; }
        public decimal BidSize { get; set; }
        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
    }

    public class CompaniesListA
    {
        public Dictionary<string, CompanyA> SymbolsToCompanies { get; set; }
    }

    // Can add more fields for searching, screening, sorting
    public class CompanyA
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public CompanyStatsA Stats { get; set; }
    }
}
