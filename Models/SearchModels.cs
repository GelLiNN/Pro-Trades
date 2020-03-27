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
        public decimal High52w { get; set; }
        public decimal Low52w { get; set; }
        public decimal MedianPrice { get; set; }
        public decimal PercentChange1m { get; set; }
        public decimal PercentChangeAvg { get; set; }
        public decimal Volume10d { get; set; }
        public decimal VolumeAvg { get; set; }
        public decimal VolumeAvgUSD { get; set; }
        public decimal MovingAvg50d { get; set; }
        public decimal MarketCapPerCapita { get; set; }
        public long PeRatio { get; set; }
        public long GrossProfit { get; set; }
        public long ShareHolderEquity { get; set; }
        public long TotalAssets { get; set; }
        public long TotalCash { get; set; }
        public long TotalDebt { get; set; }
        public long TotalLiabilities { get; set; }
        public long TotalRevenue { get; set; }
        public DateTime LastFinancialReportDate { get; set; }
        public Dividends Dividends { get; set; }
    }

    public class Dividends
    {
        public string LastDividendDate { get; set; }
        public decimal DividendYield { get; set; }
        public decimal DividendRate { get; set; }
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
