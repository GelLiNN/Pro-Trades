using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sociosearch.NET.Models;
using VSLee.IEXSharp;
using VSLee.IEXSharp.Model.Stock.Request;
using VSLee.IEXSharp.Model.Stock.Response;

namespace Sociosearch.NET.Middleware
{
    //API Key console https://iexcloud.io/console
    //API docs https://iexcloud.io/docs/api/#testing-sandbox
    //Client docs https://github.com/vslee/IEXSharp
    public class IEX
    {
        private static readonly string PToken = Program.Config.GetValue<string>("IexPublishableToken");
        private static readonly string SToken = Program.Config.GetValue<string>("IexSecretToken");
        public static IEXCloudClient IEXClient = new IEXCloudClient(PToken, SToken, signRequest: false, useSandBox: false);

        public static async Task<string> GetHistoricalPricesAsync(string symbol, ChartRange range)
        {
            var response = await IEXClient.Stock.HistoricalPriceAsync(symbol, range);
            IEnumerable<HistoricalPriceResponse> prices = response.Data;
            string result = "";
            foreach (HistoricalPriceResponse price in prices)
            {
                decimal high = price.high;
                decimal low = price.low;
                long changePercent = price.changePercent;
                long changeOverTime = price.changeOverTime;
                decimal open = price.open;
                decimal close = price.close;
                long volume = price.volume;
                result += "{date: " + price.date + ", price: " + close + "},";
            }
            return await Task.FromResult(result);
        }

        public static async Task<CompanyStatsIEX> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsIEX companyStat = new CompanyStatsIEX();
            try
            {
                var keyStatsResponse = await IEXClient.Stock.KeyStatsAsync(symbol);
                var tradesResponse = await IEXClient.Stock.IntradayPriceAsync(symbol);
                KeyStatsResponse iexStat = keyStatsResponse.Data;
                IEnumerable<IntradayPriceResponse> iexTrades = tradesResponse.Data;

                //Key Stats
                if (iexStat != null)
                {
                    companyStat.CompanyName = iexStat.companyName;
                    companyStat.Symbol = symbol;
                    companyStat.NumberOfEmployees = iexStat.employees;
                    companyStat.SharesOutstanding = iexStat.sharesOutstanding;
                    companyStat.MarketCap = iexStat.marketcap;
                    companyStat.PeRatio = iexStat.peRatio;

                    decimal high = iexStat.week52high;
                    decimal low = iexStat.week52low;
                    decimal medianPrice52w = (high + low) / 2;
                    companyStat.PriceHigh52w = high;
                    companyStat.PriceLow52w = low;
                    companyStat.PriceMedian52w = medianPrice52w;

                    decimal change1m = iexStat.month1ChangePercent;
                    decimal change3m = iexStat.month3ChangePercent;
                    decimal change6m = iexStat.month6ChangePercent;
                    companyStat.PercentChangeAvg = (change1m + change3m + change6m) / 3;
                    companyStat.PercentChange1m = change1m;

                    decimal volume10d = iexStat.avg10Volume;
                    decimal volume30d = iexStat.avg30Volume;
                    decimal avgVolume = (volume10d + volume30d) / 2;
                    companyStat.Volume10d = volume10d;

                    //VERY APPROXIMATE volume estimates for this symbol
                    companyStat.VolumeAvg = avgVolume;
                    companyStat.VolumeAvgUSD = avgVolume * companyStat.PriceMedian52w;

                    companyStat.MovingAvg50d = iexStat.day50MovingAvg;

                    //market capitalization per employee (capita)
                    if (companyStat.NumberOfEmployees > 0)
                        companyStat.MarketCapPerCapita = companyStat.MarketCap / companyStat.NumberOfEmployees;


                    //dividends
                    DividendsIEX dividends = new DividendsIEX
                    {
                        DividendRate = iexStat.ttmDividendRate,
                        DividendYield = iexStat.dividendYield,
                        LastDividendDate = iexStat.exDividendDate
                    };
                    companyStat.Dividends = dividends;
                }

                //Trade Data from intraday-prices endpoint
                if (iexTrades != null)
                {
                    TradeDataIEX td = new TradeDataIEX();
                    foreach (var item in iexTrades)
                    {
                        td.RealVolume += item.volume;
                        td.RealVolumeUSD += item.volume * item.average;
                        td.TotalTrades += item.numberOfTrades;
                        td.NotionalValueTotal += item.notional;
                    }
                    if (td.RealVolume > 0)
                    {
                        //https://www.investopedia.com/terms/n/notionalvalue.asp
                        td.NotionalValuePerShare = td.NotionalValueTotal / td.RealVolume; //idk
                        td.AvgTradeSize = td.RealVolume / td.TotalTrades;
                        td.AvgTradeSizeUSD = td.RealVolumeUSD / td.TotalTrades;
                    }
                    td.Source = "IEX";
                    companyStat.TradeData = td;
                }

                //Composite Score
                var score = Controllers.SearchController.GetCompositeScoreTD(symbol);
                if (score.CompositeScoreValue > 0)
                    companyStat.CompositeScore = score;
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR Utility.cs IEX.GetComanyStatsAsync: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        public static async Task<VSLee.IEXSharp.Model.Shared.Response.Quote> GetQuoteAsync(string symbol)
        {
            var response = await IEXClient.Stock.QuoteAsync(symbol);
            VSLee.IEXSharp.Model.Shared.Response.Quote quote = response.Data;
            return quote;
        }

        public static async Task<CompaniesListIEX> GetScreenedCompaniesAsync(CompaniesListIEX allCompanies, string screenId)
        {
            CompaniesListIEX screened = new CompaniesListIEX
            {
                SymbolsToCompanies = new Dictionary<string, CompanyIEX>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyIEX companyObject = company.Value;
                CompanyStatsIEX stats = companyObject.Stats;
                if (stats.MovingAvg50d > 5 && stats.PeRatio > 1 && stats.PriceMedian52w < 20 && stats.PriceMedian52w > .01M
                    && stats.PeRatio > 1 && stats.NumberOfEmployees > 10 && stats.VolumeAvgUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return await Task.FromResult(screened);
        }

        public static async Task<CompaniesListIEX> GetAllCompaniesAsync()
        {
            CompaniesListIEX companies = new CompaniesListIEX()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyIEX>()
            };

            string nasdaqData = Companies.GetFromFtpUri(Companies.NasdaqSymbolsUri);
            string[] nasdaqDataLines = nasdaqData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 1; i < nasdaqDataLines.Length - 1; i++) //trim first and last row
            {
                string line = nasdaqDataLines[i];
                string[] data = line.Split('|');
                if (data.Count() > 3)
                {
                    string symbol = data[1];
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol) && !String.IsNullOrEmpty(symbol))
                    {
                        bool isNasdaq = data[0] == "Y";
                        if (isNasdaq)
                        {
                            CompanyStatsIEX stats = IEX.GetCompanyStatsAsync(symbol).Result;
                            CompanyIEX company = new CompanyIEX
                            {
                                Symbol = symbol,
                                Exchange = "NASDAQ",
                                Stats = stats
                            };
                            companies.SymbolsToCompanies.Add(symbol, company);
                        }
                    }
                }
            }

            string otcData = Companies.GetFromFtpUri(Companies.OtcSymbolsUri);
            string[] otcDataLines = otcData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int j = 1; j < otcDataLines.Length - 1; j++) //trim first and last row
            {
                string line = otcDataLines[j];
                string[] data = line.Split('|');
                if (data.Count() > 3)
                {
                    string symbol = data[0];
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol) && !String.IsNullOrEmpty(symbol))
                    {
                        CompanyStatsIEX stats = IEX.GetCompanyStatsAsync(symbol).Result;
                        CompanyIEX company = new CompanyIEX
                        {
                            Symbol = symbol,
                            Exchange = "OTC",
                            Stats = stats
                        };
                        companies.SymbolsToCompanies.Add(symbol, company);
                    }
                }
            }

            string otcMarketsData = Companies.GetFromUri(Companies.OtcMarketsUri);
            string[] otcMarketsDataLines = otcMarketsData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int k = 1; k < otcMarketsDataLines.Length; k++) //trim first row
            {
                string line = otcMarketsDataLines[k];
                string[] data = line.Split(',');
                if (data.Count() > 3)
                {
                    string symbol = data[0];
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol) && !String.IsNullOrEmpty(symbol))
                    {
                        CompanyStatsIEX stats = IEX.GetCompanyStatsAsync(symbol).Result;
                        CompanyIEX company = new CompanyIEX
                        {
                            Symbol = symbol,
                            Exchange = data[2],
                            Stats = stats
                        };
                        companies.SymbolsToCompanies.Add(symbol, company);
                    }
                }
            }
            return await Task.FromResult(companies);
        }
    }
}
