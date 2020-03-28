using System;
using System.Collections.Generic;

namespace Sociosearch.NET.Models
{
    public class CompositeScoreResult
    {
        public decimal ADXComposite { get; set; }
        public decimal AROONComposite { get; set; }
        public decimal CompositeScore { get; set; }
    }

    public class CompanyStats
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

    public class AllCompanies
    {
        public Dictionary<string, Company> SymbolsToCompanies { get; set; }
    }

    //Can add more fields for searching, screening, sorting
    public class Company
    {
        public string Name { get; set; }
        public string Exchange { get; set; }
    }
}
