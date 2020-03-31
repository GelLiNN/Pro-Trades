using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Sociosearch.NET.Models;
using VSLee.IEXSharp;
using VSLee.IEXSharp.Model.Stock.Request;
using VSLee.IEXSharp.Model.Stock.Response;
using YahooFinanceApi;

namespace Sociosearch.NET.Middleware
{
    //API Documentation https://www.alphavantage.co/documentation/
    public static class AV
    {
        private static readonly string AVURI = "https://www.alphavantage.co/query?";
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<string> CompleteAlphaVantageRequest(string function, string symbol)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                //different GET params for each indicator
                switch (function.ToUpper())
                {
                    case "ADX":
                        string adxInterval = "30min";
                        string adxPeriod = "100";
                        response = await HttpClient
                            .GetAsync(String.Format(AVURI + "function={0}&symbol={1}&interval={2}&time_period={3}&apikey={4}",
                            function, symbol, adxInterval, adxPeriod, Program.Config.GetValue<string>("AlphaVantageApiKey")));
                        break;
                    case "AROON":
                        string aroonInterval = "daily";
                        string aroonPeriod = "300";
                        response = await HttpClient
                            .GetAsync(String.Format(AVURI + "function={0}&symbol={1}&interval={2}&time_period={3}&apikey={4}",
                            function, symbol, aroonInterval, aroonPeriod, Program.Config.GetValue<string>("AlphaVantageApiKey")));
                        break;
                    case "BBANDS":
                        break;
                    case "RSI":
                        break;
                    case "MACD":
                        break;
                    case "CCI":
                        break;
                    case "STOCH":
                        break;

                }
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return await Task.FromResult(responseBody);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("EXCEPTION Message: {0}, StackTrace: {1}", e.Message, e.StackTrace);
                return string.Empty;
            }
        }

        public static decimal GetCompositeScore(string function, string alphaVantageResponse, int daysToCalculate)
        {
            decimal compositeScore = -1;
            JObject data = JObject.Parse(alphaVantageResponse);
            JObject resultSet;
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            //different processing for each indicator
            switch (function.ToUpper())
            {
                case "ADX":
                    //When the +DMI is above the -DMI, prices are moving up, and ADX measures the strength of the uptrend.
                    //When the -DMI is above the +DMI, prices are moving down, and ADX measures the strength of the downtrend.
                    //Many traders will use ADX readings above 25 to suggest that the trend is strong enough for trend-trading strategies.
                    //Conversely, when ADX is below 25, many will avoid trend-trading strategies.
                    resultSet = (JObject) data.GetValue("Technical Analysis: ADX");
                    decimal adxTotal = 0;
                    foreach (var result in resultSet)
                    {
                        string adxKey = result.Key;
                        string adxDate = DateTime.Parse(adxKey).ToString("yyyy-MM-dd");
                        if (!dates.Contains(adxDate))
                        {
                            dates.Add(adxDate);
                            daysCalulated++;
                        }

                        if (daysCalulated < daysToCalculate)
                        {
                            string adxVal = result.Value.Value<string>("ADX");
                            adxTotal += decimal.Parse(adxVal);
                            numberOfResults++;
                        }
                        else
                            break;
                    }
                    decimal adxAvg = adxTotal / numberOfResults;
                    compositeScore = ((adxAvg) / 25) * 100; //get trend strength as a percentage of 25
                    break;
                case "AROON":
                    //Indicator Movements Around the Key Levels, 30 and 70 - Movements above 70 indicate a strong trend,
                    //while movements below 30 indicate low trend strength. Movements between 30 and 70 indicate indecision.
                    //For example, if the bullish indicator remains above 70 while the bearish indicator remains below 30,
                    //the trend is definitively bullish.
                    //Crossovers Between the Bullish and Bearish Indicators - Crossovers indicate confirmations if they occur
                    //between 30 and 70.For example, if the bullish indicator crosses above the bearish indicator, it confirms a bullish trend.
                    //The two Aroon indicators(bullish and bearish) can also be made into a single oscillator by
                    //making the bullish indicator 100 to 0 and the bearish indicator 0 to - 100 and finding the
                    //difference between the two values. This oscillator then varies between 100 and - 100, with 0 indicating no trend.
                    resultSet = (JObject)data.GetValue("Technical Analysis: AROON");
                    decimal aroonUpTotal = 0;
                    decimal aroonDownTotal = 0;
                    foreach (var result in resultSet)
                    {
                        string adxKey = result.Key;
                        string adxDate = DateTime.Parse(adxKey).ToString("yyyy-MM-dd");
                        if (!dates.Contains(adxDate))
                        {
                            dates.Add(adxDate);
                            daysCalulated++;
                        }

                        if (daysCalulated < daysToCalculate)
                        {
                            string aroonUpVal = result.Value.Value<string>("Aroon Up");
                            string aroonDownVal = result.Value.Value<string>("Aroon Down");
                            aroonUpTotal += decimal.Parse(aroonUpVal);
                            aroonDownTotal += decimal.Parse(aroonDownVal);
                            numberOfResults++;
                        }
                        else
                            break;
                    }
                    decimal aroonAvgUp = aroonUpTotal / numberOfResults;
                    decimal aroonAvgDown = aroonDownTotal / numberOfResults;
                    decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
                    decimal diff = 100 - percentDiffDown;
                    if (diff < 0)
                        compositeScore = 0;
                    else //score as a percentage of 50 means if avgDown is half of avgUp you will get 100 (perfect)
                        compositeScore = (diff / 50) * 100;
                    break;
                case "BBANDS":
                    break;
                case "RSI":
                    break;
                case "OBV":
                    break;
                case "MACD":
                    break;
                case "CCI":
                    //Possible sell signals:
                    //The CCI crosses above 100 and has started to curve downward.
                    //There is bearish divergence between the CCI and the actual price movement, characterized by downward movement
                    //in the CCI while the price of the asset continues to move higher or moves sideways.
                    //Possible buy signals:
                    //The CCI crosses below -100 and has started to curve upward.
                    //There is a bullish divergence between the CCI and the actual price movement, characterized by upward movement
                    //in the CCI while the price of the asset continues to move downward or sideways.
                    break;
                case "STOCH":
                    break;

            }
            return compositeScore;
        }
    }

    //API Documentation https://financialmodelingprep.com/developer/docs/
    //No API key needed for this one?
    public class FMP
    {
        private static readonly string FMPURI = @"https://financialmodelingprep.com/api/v3/";
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<string> CompleteFMPRequestAsync(string path, string function = null, string symbol = null)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response = await HttpClient.GetAsync(FMPURI + path);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return await Task.FromResult(responseBody);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("EXCEPTION Message: {0}, StackTrace: {1}", e.Message, e.StackTrace);
                return string.Empty;
            }
        }


        public static async Task<CompanyStatsFMP> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsFMP companyStat = new CompanyStatsFMP();
            try
            {
                //FMP quote endpoint
                string quoteResponse = CompleteFMPRequestAsync(String.Format("quote/{0}", symbol)).Result;
                JArray quoteData = JArray.Parse(quoteResponse); //JArray since quote endpoint accepts multiple symbols
                foreach (JObject quote in quoteData)
                {
                    companyStat.Symbol = symbol;
                    companyStat.CompanyName = quote.GetValue("name").ToString();
                    companyStat.MarketCap = decimal.Parse(quote.GetValue("marketCap").ToString());
                    companyStat.SharesOutstanding = decimal.Parse(quote.GetValue("sharesOutstanding").ToString());
                    companyStat.PeRatio = decimal.Parse(quote.GetValue("pe").ToString());
                    companyStat.EarningsPerShare = decimal.Parse(quote.GetValue("eps").ToString());
                    companyStat.EarningsReportDate = DateTime.Parse(quote.GetValue("earningsAnnouncement").ToString());

                    companyStat.Price = decimal.Parse(quote.GetValue("price").ToString());
                    companyStat.PriceOpenToday = decimal.Parse(quote.GetValue("open").ToString());
                    companyStat.PricePreviousClose = decimal.Parse(quote.GetValue("previousClose").ToString());

                    companyStat.PriceHighToday = decimal.Parse(quote.GetValue("dayHigh").ToString());
                    companyStat.PriceLowToday = decimal.Parse(quote.GetValue("dayLow").ToString());
                    companyStat.PriceAverageToday = (companyStat.PriceHighToday + companyStat.PriceLowToday) / 2;

                    companyStat.PriceChangeTodayUSD = decimal.Parse(quote.GetValue("change").ToString());
                    companyStat.PriceChangeTodayPercent = decimal.Parse(quote.GetValue("changesPercentage").ToString());
                    companyStat.PriceAverage50Day = decimal.Parse(quote.GetValue("priceAvg50").ToString());

                    companyStat.PriceHighYTD = decimal.Parse(quote.GetValue("yearHigh").ToString());
                    companyStat.PriceLowYTD = decimal.Parse(quote.GetValue("yearLow").ToString());
                    companyStat.PriceAverageEstimateYTD = (companyStat.PriceHighYTD + companyStat.PriceLowYTD) / 2;

                    companyStat.VolumeToday = decimal.Parse(quote.GetValue("volume").ToString());
                    companyStat.VolumeAverage = decimal.Parse(quote.GetValue("avgVolume").ToString());
                    companyStat.VolumeAverageUSD = (companyStat.VolumeAverage * companyStat.PriceAverageToday);
                }

                //FMP company profile endpoint
                string companyProfileResponse = CompleteFMPRequestAsync(String.Format("company/profile/{0}", symbol)).Result;
                JObject profileData = JObject.Parse(companyProfileResponse);
                if (profileData != null)
                {
                    JObject profile = (JObject) profileData.GetValue("profile");
                    companyStat.Exchange = profile.GetValue("exchange").ToString();
                    companyStat.CompanyDescription = profile.GetValue("description").ToString();
                    companyStat.CompanyCEO = profile.GetValue("ceo").ToString();
                    companyStat.CompanyIndustry = profile.GetValue("industry").ToString();
                    companyStat.CompanySector = profile.GetValue("sector").ToString();
                    companyStat.CompanyImageLink = profile.GetValue("image").ToString();

                    companyStat.BetaValue = decimal.Parse(profile.GetValue("beta").ToString());
                }

                //FMP income statement endpoint
                string financialsResponse = CompleteFMPRequestAsync(String.Format("financials/income-statement/{0}?period=quarter", symbol)).Result;
                JObject financialsData = JObject.Parse(financialsResponse);
                if (financialsData != null)
                {
                    JArray financials = (JArray)financialsData.GetValue("financials");
                    foreach (JObject financial in financials)
                    {
                        FinancialsFMP fin = new FinancialsFMP
                        {
                            RevenueTotal = decimal.Parse(financial.GetValue("Revenue").ToString()),
                            RevenueGrowth = decimal.Parse(financial.GetValue("Revenue Growth").ToString()),
                            ExpensesRD = decimal.Parse(financial.GetValue("R&D Expenses").ToString()),
                            ExpensesSGA = decimal.Parse(financial.GetValue("SG&A Expense").ToString()),
                            ExpensesOperating = decimal.Parse(financial.GetValue("Operating Expenses").ToString()),
                            IncomeOperating = decimal.Parse(financial.GetValue("Operating Income").ToString()),
                            IncomeNet = decimal.Parse(financial.GetValue("Net Income").ToString()),
                            IncomeConsolidated = decimal.Parse(financial.GetValue("Consolidated Income").ToString()),
                            MarginGross = decimal.Parse(financial.GetValue("Gross Margin").ToString()),
                            MarginEBITDA = decimal.Parse(financial.GetValue("EBITDA Margin").ToString()),
                            MarginEBIT = decimal.Parse(financial.GetValue("EBIT Margin").ToString()),
                            MarginCashFlow = decimal.Parse(financial.GetValue("Free Cash Flow margin").ToString()),
                            MarginProfit = decimal.Parse(financial.GetValue("Profit Margin").ToString()),
                            LastFinancialReportDate = DateTime.Parse(financial.GetValue("date").ToString())
                        };
                        companyStat.Financials = fin;
                        break; //only get most recent quarterly sumbission or 10Q
                    }
                }

                /*if (iexTrades != null) //data from intraday-prices
                {
                    TradeData td = new TradeData();
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
                }*/
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR Utility.cs FMP.GetComanyStatsAsync: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        public static string GetQuote(string symbol)
        {
            return CompleteFMPRequestAsync(String.Format("quote/{0}", symbol)).Result;
        }
    }

    //API Documentation https://github.com/lppkarl/YahooFinanceApi
    //No API key needed for this one?
    public class Y
    {
        public static async Task<CompanyStatsYahoo> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsYahoo companyStat = new CompanyStatsYahoo();
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

                    companyStat.Earnings = new EarningsYahoo
                    {
                        EpsForward = decimal.Parse(quote.EpsForward.ToString()),
                        EpsTrailingYTD = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()),
                        EarningsStartDate = quote.EarningsTimestampStart.ToString(),
                        EarningsEndDate = quote.EarningsTimestampEnd.ToString(),
                        EarningsReportDate = quote.EarningsTimestamp.ToString()
                    };

                    //Could get more trade data from IEX possibly
                    companyStat.TradeData = new TradeDataYahoo
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
                Debug.WriteLine("ERROR Utility.cs FMP.GetComanyStatsAsync: " + e.Message + ", StackTrace: " + e.StackTrace);
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
    }

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
                //var earningResponse = await IEXClient.Stock.EarningAsync(symbol); earnings endpoint kills my limits
                var tradesResponse = await IEXClient.Stock.IntradayPriceAsync(symbol);
                KeyStatsResponse iexStat = keyStatsResponse.Data;
                //EarningResponse iexFinancial = earningResponse.Data;
                IEnumerable<IntradayPriceResponse> iexTrades = tradesResponse.Data;

                if (iexStat != null)
                {
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

                    companyStat.CompanyName = iexStat.companyName;
                    companyStat.NumberOfEmployees = iexStat.employees;
                    companyStat.SharesOutstanding = iexStat.sharesOutstanding;
                    companyStat.MarketCap = iexStat.marketcap;
                    companyStat.MovingAvg50d = iexStat.day50MovingAvg;
                    companyStat.PeRatio = iexStat.peRatio;

                    //market capitalization per employee (capita)
                    if (companyStat.NumberOfEmployees > 0)
                        companyStat.MarketCapPerCapita = companyStat.MarketCap / companyStat.NumberOfEmployees;


                    //dividends
                    Dividends dividends = new Dividends
                    {
                        DividendRate = iexStat.ttmDividendRate,
                        DividendYield = iexStat.dividendYield,
                        LastDividendDate = iexStat.exDividendDate
                    };
                    companyStat.Dividends = dividends;
                }

                //if (iexFinancial != null)
                //{
                //List<VSLee.IEXSharp.Model.Shared.Response.Earning> fins = iexFinancial.earnings;
                //if (fins != null)
                //{
                //VSLee.IEXSharp.Model.Shared.Response.Earning fin = fins.FirstOrDefault();

                //re-enable if I can get financials from somewhere
                /*Financials financials = new Financials
                {
                    //Get TotalRevenue somewhere?
                    RetainedEarnings = fin.,
                    ShareHolderEquity = fin.shareholderEquity,
                    ShortTermInvestments = fin.shortTermInvestments,
                    CapitalSurplus = fin.capitalSurplus,
                    TotalAssets = fin.totalAssets,
                    TotalCash = fin.currentCash,
                    TotalDebt = fin.longTermDebt,
                    TotalLiabilities = fin.totalLiabilities,
                    LastFinancialReportDate = fin.reportDate
                };*/
                //companyStat.Earnings = fin;
                //}
                //}

                if (iexTrades != null) //data from intraday-prices
                {
                    TradeData td = new TradeData();
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
    }

    //Module for getting all or most company symbols and names from all exchanges
    //Nasdaq FTP data dump files for loading large datasets
    //ftp://ftp.nasdaqtrader.com and ftp://ftp.nasdaqtrader.com/SymbolDirectory and
    //OTCMarkets raw securities download from https://www.otcmarkets.com/research/stock-screener/api/downloadCSV
    public class Companies
    {
        private static readonly string nasdaqSymbolsUri = @"ftp://ftp.nasdaqtrader.com/SymbolDirectory/nasdaqtraded.txt";
        private static readonly string otcSymbolsUri = @"ftp://ftp.nasdaqtrader.com/SymbolDirectory/otclist.txt";
        private static readonly string otcMarketsUri = @"https://www.otcmarkets.com/research/stock-screener/api/downloadCSV";

        public static CompaniesListIEX GetScreenedCompaniesIEX(CompaniesListIEX allCompanies, string screenId)
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
                    && stats.PeRatio > 1 && stats.NumberOfEmployees > 10 && stats.VolumeAvgUSD > 500000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return screened;
        }

        public static async Task<CompaniesListIEX> GetAllCompaniesIEXAsync()
        {
            CompaniesListIEX companies = new CompaniesListIEX()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyIEX>()
            };

            string nasdaqData = GetFromFtpUri(nasdaqSymbolsUri);
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

            string otcData = GetFromFtpUri(otcSymbolsUri);
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

            string otcMarketsData = GetFromUri(otcMarketsUri);
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

        public static CompaniesListFMP GetScreenedCompaniesFMP(CompaniesListFMP allCompanies, string screenId)
        {
            CompaniesListFMP screened = new CompaniesListFMP
            {
                SymbolsToCompanies = new Dictionary<string, CompanyFMP>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyFMP companyObject = company.Value;
                CompanyStatsFMP stats = companyObject.Stats;
                if (stats.PriceAverageToday < 20 && stats.PriceAverageToday > .01M && stats.VolumeAverageUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return screened;
        }

        public static async Task<CompaniesListFMP> GetAllCompaniesFMPAsync()
        {
            CompaniesListFMP companies = new CompaniesListFMP()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyFMP>()
            };

            string nasdaqData = GetFromFtpUri(nasdaqSymbolsUri);
            string[] nasdaqDataLines = nasdaqData.Split( new[] { Environment.NewLine }, StringSplitOptions.None);
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
                            CompanyStatsFMP stats = FMP.GetCompanyStatsAsync(symbol).Result;
                            CompanyFMP company = new CompanyFMP
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

            string otcData = GetFromFtpUri(otcSymbolsUri);
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
                        CompanyStatsFMP stats = FMP.GetCompanyStatsAsync(symbol).Result;
                        CompanyFMP company = new CompanyFMP
                        {
                            Symbol = symbol,
                            Exchange = "OTC",
                            Stats = stats
                        };
                        companies.SymbolsToCompanies.Add(symbol, company);
                    }
                }
            }

            string otcMarketsData = GetFromUri(otcMarketsUri);
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
                        CompanyStatsFMP stats = FMP.GetCompanyStatsAsync(symbol).Result;
                        CompanyFMP company = new CompanyFMP
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

        public static CompaniesListYahoo GetScreenedCompaniesYahoo(CompaniesListYahoo allCompanies, string screenId)
        {
            CompaniesListYahoo screened = new CompaniesListYahoo
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYahoo>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyYahoo companyObject = company.Value;
                CompanyStatsYahoo stats = companyObject.Stats;
                if (stats.PriceAverageToday < 20 && stats.PriceAverageToday > .01M && stats.VolumeAverage10dUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return screened;
        }

        public static async Task<CompaniesListYahoo> GetAllCompaniesYahooAsync()
        {
            CompaniesListYahoo companies = new CompaniesListYahoo()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYahoo>()
            };

            string nasdaqData = GetFromFtpUri(nasdaqSymbolsUri);
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
                            CompanyStatsYahoo stats = Y.GetCompanyStatsAsync(symbol).Result;
                            CompanyYahoo company = new CompanyYahoo
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

            string otcData = GetFromFtpUri(otcSymbolsUri);
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
                        CompanyStatsYahoo stats = Y.GetCompanyStatsAsync(symbol).Result;
                        CompanyYahoo company = new CompanyYahoo
                        {
                            Symbol = symbol,
                            Exchange = "OTC",
                            Stats = stats
                        };
                        companies.SymbolsToCompanies.Add(symbol, company);
                    }
                }
            }

            string otcMarketsData = GetFromUri(otcMarketsUri);
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
                        CompanyStatsYahoo stats = Y.GetCompanyStatsAsync(symbol).Result;
                        CompanyYahoo company = new CompanyYahoo
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

        public static string GetFromFtpUri(string uri)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.UseBinary = true;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            string responseStr = string.Empty;

            //Read the file from the server & write to destination                
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse()) // Error here
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                responseStr = reader.ReadToEnd();
            }
            return responseStr;
        }

        public static string GetFromUri(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            string responseStr = string.Empty;

            //Read the file from the server & write to destination                
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) // Error here
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                responseStr = reader.ReadToEnd();
            }
            return responseStr;
        }
    }
}
