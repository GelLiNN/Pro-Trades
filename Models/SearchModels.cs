using System;
using System.Collections.Generic;
using YahooFinanceApi;

namespace Sociosearch.NET.Models
{
    public class CompositeScoreResult
    {
        public decimal ADXComposite { get; set; }
        public decimal AROONComposite { get; set; }
        public decimal CompositeScore { get; set; }
    }

    /*
     * IEX Models
     */
    public class CompanyStatsIEX
    {
        public string CompanyName { get; set; }
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

        public TradeData TradeData { get; set; }
        //public Financials Financials { get; set; } re-enable if I can get from somewhere
        public VSLee.IEXSharp.Model.Shared.Response.Earning Earnings { get; set; }
        public Dividends Dividends { get; set; }
    }

    public class TradeData
    {
        public string Source { get; set; }
        public long RealVolume { get; set; }
        public decimal RealVolumeUSD { get; set; }
        public long TotalTrades { get; set; }
        public decimal AvgTradeSize { get; set; }
        public decimal AvgTradeSizeUSD { get; set; }
        public decimal NotionalValueTotal { get; set; }
        public decimal NotionalValuePerShare { get; set; }

        public TradeData()
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

    public class Dividends
    {
        public string LastDividendDate { get; set; }
        public decimal DividendYield { get; set; }
        public decimal DividendRate { get; set; }
    }

    public class Financials
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
        public string Symbol{ get; set; }
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

    /*
     * Yahoo Models
     */
    public class CompanyStatsYahoo
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }

        public string CompanyName { get; set; }
        public bool CompanyTrading { get; set; }
        public string CompanyQuoteType { get; set; }
        public string CompanyMarket { get; set; }

        public decimal MarketCap { get; set; }
        public decimal SharesOutstanding { get; set; }
        public decimal PeRatioForward { get; set; }
        public decimal PeRatioTrailing { get; set; }
        public decimal BookValue { get; set; }

        public decimal Price { get; set; }
        public decimal PriceOpenToday { get; set; }
        public decimal PricePreviousClose { get; set; }

        public decimal PriceHighToday { get; set; }
        public decimal PriceLowToday { get; set; }
        public decimal PriceAverageToday { get; set; }

        public decimal PriceChangeTodayUSD { get; set; }
        public decimal PriceChangeTodayPercent { get; set; }
        public decimal PriceAverage50DayUSD { get; set; }
        public decimal PriceAverage50DayPercent { get; set; }

        public decimal PriceHigh52w { get; set; }
        public decimal PriceLow52w { get; set; }
        public decimal PriceAverageEstimate52w { get; set; }
        public decimal PriceToBook { get; set; }

        public decimal VolumeToday { get; set; }
        public decimal VolumeTodayUSD { get; set; }
        public decimal VolumeAverage10d { get; set; }
        public decimal VolumeAverage10dUSD { get; set; }
        public decimal VolumeAverage3m { get; set; }
        public decimal VolumeAverage3mUSD { get; set; }

        public TradeDataYahoo TradeData { get; set; }
        public EarningsYahoo Earnings { get; set; }
        public List<DividendTick> Dividends { get; set; }
        public List<SplitTick> Splits { get; set; }

        public CompanyStatsYahoo()
        {
            Dividends = new List<DividendTick>();
            Splits = new List<SplitTick>();
        }
    }

    public class TradeDataYahoo
    {
        public decimal BidPrice { get; set; }
        public decimal BidSize { get; set; }
        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
    }

    public class EarningsYahoo
    {
        public decimal EpsForward { get; set; }
        public decimal EpsTrailingYTD { get; set; }

        //should use DateTime below instead
        public String EarningsStartDate { get; set; }
        public String EarningsEndDate { get; set; }
        public String EarningsReportDate { get; set; }
    }

    public class CompaniesListYahoo
    {
        public Dictionary<string, CompanyYahoo> SymbolsToCompanies { get; set; }
    }

    //Can add more fields for searching, screening, sorting
    public class CompanyYahoo
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public CompanyStatsYahoo Stats { get; set; }
    }
}
