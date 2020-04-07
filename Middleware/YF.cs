using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sociosearch.NET.Models;
using YahooFinanceApi;

namespace Sociosearch.NET.Middleware
{
    //Yahoo Finance API Documentation https://github.com/lppkarl/YahooFinanceApi
    //No API key needed for this one?
    public class YF
    {
        public static async Task<CompanyStatsYF> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsYF companyStat = new CompanyStatsYF();
            try
            {
                //Yahoo Quote
                Security quote = GetQuoteAsync(symbol).Result;
                if (quote != null)
                {
                    //parse quote data into company stat
                    companyStat.Symbol = symbol;
                    companyStat.Exchange = quote.FullExchangeName;
                    companyStat.CompanyName = quote.LongName;
                    companyStat.CompanyTrading = quote.Tradeable;
                    companyStat.CompanyQuoteType = quote.QuoteType;
                    companyStat.CompanyMarket = quote.Market;

                    companyStat.MarketCap = quote.MarketCap;
                    companyStat.SharesOutstanding = quote.SharesOutstanding;
                    companyStat.PeRatioForward = decimal.Parse(quote.ForwardPE.ToString());
                    companyStat.PeRatioTrailing = decimal.Parse(quote.TrailingPE.ToString());
                    companyStat.BookValue = decimal.Parse(quote.BookValue.ToString());

                    companyStat.Price = decimal.Parse(quote.RegularMarketPrice.ToString());
                    companyStat.PriceOpenToday = decimal.Parse(quote.RegularMarketOpen.ToString());
                    companyStat.PricePreviousClose = decimal.Parse(quote.RegularMarketPreviousClose.ToString());

                    companyStat.PriceHighToday = decimal.Parse(quote.RegularMarketDayHigh.ToString());
                    companyStat.PriceLowToday = decimal.Parse(quote.RegularMarketDayLow.ToString());
                    companyStat.PriceAverageToday = (companyStat.PriceHighToday + companyStat.PriceLowToday) / 2;

                    companyStat.PriceChangeTodayUSD = decimal.Parse(quote.RegularMarketChange.ToString());
                    companyStat.PriceChangeTodayPercent = decimal.Parse(quote.RegularMarketChangePercent.ToString());
                    companyStat.PriceAverage50DayUSD = decimal.Parse(quote.FiftyDayAverage.ToString());
                    companyStat.PriceAverage50DayPercent = decimal.Parse(quote.FiftyDayAverageChangePercent.ToString());

                    companyStat.PriceHigh52w = decimal.Parse(quote.FiftyTwoWeekHigh.ToString());
                    companyStat.PriceLow52w = decimal.Parse(quote.FiftyTwoWeekLow.ToString());
                    companyStat.PriceAverageEstimate52w = (companyStat.PriceHigh52w + companyStat.PriceLow52w) / 2;
                    companyStat.PriceToBook = decimal.Parse(quote.PriceToBook.ToString());

                    companyStat.VolumeToday = decimal.Parse(quote.RegularMarketVolume.ToString());
                    companyStat.VolumeTodayUSD = (companyStat.VolumeToday * companyStat.PriceAverageToday);
                    companyStat.VolumeAverage10d = decimal.Parse(quote.AverageDailyVolume10Day.ToString());
                    companyStat.VolumeAverage10dUSD = (companyStat.VolumeAverage10d * companyStat.PriceAverageToday);
                    companyStat.VolumeAverage3m = decimal.Parse(quote.AverageDailyVolume3Month.ToString());
                    companyStat.VolumeAverage3mUSD = (companyStat.VolumeAverage3m * companyStat.PriceAverageToday);

                    companyStat.Earnings = new EarningsYF
                    {
                        EpsForward = decimal.Parse(quote.EpsForward.ToString()),
                        EpsTrailingYTD = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()),
                        EarningsStartDate = quote.EarningsTimestampStart.ToString(),
                        EarningsEndDate = quote.EarningsTimestampEnd.ToString(),
                        EarningsReportDate = quote.EarningsTimestamp.ToString()
                    };

                    //Could get more trade data from IEX possibly
                    companyStat.TradeData = new TradeDataYF
                    {
                        BidPrice = decimal.Parse(quote.Bid.ToString()),
                        BidSize = decimal.Parse(quote.BidSize.ToString()),
                        AskPrice = decimal.Parse(quote.Ask.ToString()),
                        AskSize = decimal.Parse(quote.AskSize.ToString())
                    };
                }

                //Yahoo Dividends for the last year
                //You should be able to query data from various markets including US, HK, TW
                var dividends = await Yahoo.GetDividendsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                foreach (DividendTick div in dividends)
                    companyStat.Dividends.Add(div);

                //Yahoo historical trade data and splits data
                var splits = await Yahoo.GetSplitsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                foreach (var split in splits)
                    companyStat.Splits.Add(split);
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR YF.cs YF.GetComanyStatsAsync: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        public static async Task<Security> GetQuoteAsync(string symbol)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            var securities = await Yahoo.Symbols(symbol).QueryAsync();
            var quote = securities[symbol];
            return quote;
        }

        public static async Task<CompaniesListYF> GetScreenedCompaniesAsync(CompaniesListYF allCompanies, string screenId)
        {
            CompaniesListYF screened = new CompaniesListYF
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyYF companyObject = company.Value;
                CompanyStatsYF stats = companyObject.Stats;
                if (stats.PriceAverageToday < 20 && stats.PriceAverageToday > .01M && stats.VolumeAverage10dUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return await Task.FromResult(screened);
        }

        public static async Task<CompaniesListYF> GetAllCompaniesAsync()
        {
            CompaniesListYF companies = new CompaniesListYF()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
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
                            CompanyStatsYF stats = YF.GetCompanyStatsAsync(symbol).Result;
                            CompanyYF company = new CompanyYF
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
                        CompanyStatsYF stats = YF.GetCompanyStatsAsync(symbol).Result;
                        CompanyYF company = new CompanyYF
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
                        CompanyStatsYF stats = YF.GetCompanyStatsAsync(symbol).Result;
                        CompanyYF company = new CompanyYF
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
