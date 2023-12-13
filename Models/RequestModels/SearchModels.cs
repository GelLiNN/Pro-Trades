namespace PT.Models.RequestModels
{
    public class CompositeScoreResult
    {
        public string? Symbol { get; set; }
        public string? Name { get; set; }
        public string? Exchange { get; set; }
        public decimal Price { get; set; }
        public int PriceHistoryDays { get; set; }
        public decimal ADXComposite { get; set; }
        public decimal OBVComposite { get; set; }
        public decimal AROONComposite { get; set; }
        public decimal MACDComposite { get; set; }
        //public decimal BBANDSComposite { get; set; }
        public decimal RatingsComposite { get; set; }
        public decimal ShortInterestComposite { get; set; }
        public decimal FundamentalsComposite { get; set; }
        public string CompositeRank { get; set; }
        public decimal CompositeScoreValue { get; set; }
        public long ScoreTimeMS { get; set; }
        public DateTime ScoreDate { get; set; }
        public ShortInterestResult ShortInterest { get; set; }
        public FundamentalsResult Fundamentals { get; set; }
        public HedgeFundsResult HedgeFunds { get; set; }
        public string DataProviders { get; set; }
    }

    public class CacheViewResult
    {
        public int CacheCount { get; set; }
        public int ScrapeCount { get; set; }
        public int ScrapeAttemptCount { get; set; }
        public HashSet<string> CacheKeys { get; set; }
    }

    public class ShortInterestResult
    {
        public decimal TotalVolume { get; set; }
        public decimal TotalVolumeShort { get; set; }
        public decimal ShortInterestPercentToday { get; set; }
        public decimal ShortInterestPercentAverage { get; set; }
        public decimal ShortInterestSlope { get; set; }
        public decimal ShortInterestCompositeScore { get; set; }
    }

    public class FundamentalsResult
    {
        public decimal FundamentalsComposite { get; set; }
        public decimal VolumeUSD { get; set; }
        public decimal AverageVolumeUSD { get; set; }
        public decimal VolumeSlope { get; set; }
        public decimal PriceSlope { get; set; }
        public decimal AverageEPS { get; set; }
        public decimal AveragePE { get; set; }
        public decimal GrowthEPS { get; set; }
        public decimal GrowthPE { get; set; }
        public bool HasDividends { get; set; }
        public bool IsBlacklisted { get; set; }
        public string Message { get; set; }
    }

    /*
     * Yahoo Models
     */
    public class CompanyStats
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

        public TradeDataYF TradeData { get; set; }

        //public decimal PriceToBook { get; set; }
        //public List<DividendTick> Dividends { get; set; }
        //public List<SplitTick> Splits { get; set; }

        public CompanyStats()
        {
            //Dividends = new List<DividendTick>();
            //Splits = new List<SplitTick>();
        }
    }

    public class TradeDataYF
    {
        public decimal BidPrice { get; set; }
        public decimal BidSize { get; set; }
        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
    }

    public class CompaniesListYF
    {
        public Dictionary<string, CompanyYF> SymbolsToCompanies { get; set; }
    }

    //Can add more fields for searching, screening, sorting
    public class CompanyYF
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public CompanyStats Stats { get; set; }
    }

    /*
     * FINRA Models
     */
    public class FinraRecord
    {
        // URL: http://regsho.finra.org/CNMSshvol20181105.txt
        // STRUCTURE: Date|Symbol|ShortVolume|ShortExemptVolume|TotalVolume

        public DateTime Date { get; set; }
        public string Symbol { get; set; }
        public decimal ShortVolume { get; set; }
        public decimal ShortExemptVolume { get; set; }
        public decimal TotalVolume { get; set; }
        public string Market { get; set; }
    }

    /*
     * IEX Models
     */
    public class CompanyStatsIEX
    {
        public string Symbol { get; set; }
        public string CompanyName { get; set; }
        public CompositeScoreResult CompositeScore { get; set; }

        public long NumberOfEmployees { get; set; }
        public long SharesOutstanding { get; set; }
        public long MarketCap { get; set; }
        public decimal MarketCapPerCapita { get; set; }
        public long PeRatio { get; set; }

        public decimal PriceHigh52w { get; set; }
        public decimal PriceLow52w { get; set; }
        public decimal PriceMedian52w { get; set; }

        public decimal PercentChange1m { get; set; }
        public decimal PercentChangeAvg { get; set; }

        public decimal Volume10d { get; set; }
        public decimal VolumeAvg { get; set; }
        public decimal VolumeAvgUSD { get; set; }
        public decimal MovingAvg50d { get; set; }

        public TradeDataIEX TradeData { get; set; }
        //public Financials Financials { get; set; } re-enable if I can get from somewhere
        //public VSLee.IEXSharp.Model.Shared.Response.Earning Earnings { get; set; }
        public DividendsIEX Dividends { get; set; }
    }

    public class TradeDataIEX
    {
        public string Source { get; set; }
        public long RealVolume { get; set; }
        public decimal RealVolumeUSD { get; set; }
        public long TotalTrades { get; set; }
        public decimal AvgTradeSize { get; set; }
        public decimal AvgTradeSizeUSD { get; set; }
        public decimal NotionalValueTotal { get; set; }
        public decimal NotionalValuePerShare { get; set; }

        public TradeDataIEX()
        {
            Source = string.Empty;
            RealVolume = 0;
            RealVolumeUSD = 0;
            TotalTrades = 0;
            AvgTradeSize = 0;
            AvgTradeSizeUSD = 0;
            NotionalValueTotal = 0;
            NotionalValuePerShare = 0;
        }
    }

    public class DividendsIEX
    {
        public DateTime? LastDividendDate { get; set; }
        public decimal DividendYield { get; set; }
        public decimal DividendRate { get; set; }
    }

    public class FinancialsIEX
    {
        //Get TotalRevenue somewhere?
        public long RetainedEarnings { get; set; }
        public long ShareHolderEquity { get; set; }
        public long ShortTermInvestments { get; set; }
        public long CapitalSurplus { get; set; }
        public long TotalAssets { get; set; }
        public long TotalCash { get; set; }
        public long TotalDebt { get; set; }
        public long TotalLiabilities { get; set; }
        public DateTime LastFinancialReportDate { get; set; }
    }

    public class CompaniesListIEX
    {
        public Dictionary<string, CompanyIEX> SymbolsToCompanies { get; set; }
    }

    //Can add more fields for searching, screening, sorting
    public class CompanyIEX
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public CompanyStatsIEX Stats { get; set; }
    }

    /*
     * FMP Models
     */
    public class CompanyStatsFMP
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }

        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string CompanyCEO { get; set; }
        public string CompanyIndustry { get; set; }
        public string CompanySector { get; set; }
        public string CompanyImageLink { get; set; }

        public decimal MarketCap { get; set; }
        public decimal SharesOutstanding { get; set; }
        public decimal PeRatio { get; set; }
        public decimal BetaValue { get; set; }
        public decimal EarningsPerShare { get; set; }
        public DateTime EarningsReportDate { get; set; }

        public decimal Price { get; set; }
        public decimal PriceOpenToday { get; set; }
        public decimal PricePreviousClose { get; set; }

        public decimal PriceHighToday { get; set; }
        public decimal PriceLowToday { get; set; }
        public decimal PriceAverageToday { get; set; }

        public decimal PriceChangeTodayUSD { get; set; }
        public decimal PriceChangeTodayPercent { get; set; }
        public decimal PriceAverage50Day { get; set; }

        public decimal PriceHighYTD { get; set; }
        public decimal PriceLowYTD { get; set; }
        public decimal PriceAverageEstimateYTD { get; set; }

        public decimal VolumeToday { get; set; }
        public decimal VolumeAverage { get; set; }
        public decimal VolumeAverageUSD { get; set; }

        public TradeDataFMP TradeData { get; set; }
        public FinancialsFMP Financials { get; set; }
        public DividendsFMP Dividends { get; set; }
    }

    public class TradeDataFMP
    {

    }

    public class FinancialsFMP
    {
        public decimal RevenueTotal { get; set; }
        public decimal RevenueGrowth { get; set; }
        public decimal ExpensesRD { get; set; }
        public decimal ExpensesSGA { get; set; }
        public decimal ExpensesOperating { get; set; }
        public decimal IncomeOperating { get; set; }
        public decimal IncomeNet { get; set; }
        public decimal IncomeConsolidated { get; set; }
        public decimal MarginGross { get; set; }
        public decimal MarginEBITDA { get; set; }
        public decimal MarginEBIT { get; set; }
        public decimal MarginProfit { get; set; }
        public decimal MarginCashFlow { get; set; }
        public DateTime LastFinancialReportDate { get; set; }
    }

    public class DividendsFMP
    {

    }

    public class CompaniesListFMP
    {
        public Dictionary<string, CompanyFMP> SymbolsToCompanies { get; set; }
    }

    //Can add more fields for searching, screening, sorting
    public class CompanyFMP
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public CompanyStatsFMP Stats { get; set; }
    }
}
