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

namespace Sociosearch.NET.Middleware
{
    public static class Utility
    {
        private static readonly string AlphaVantageUri = "https://www.alphavantage.co/query?";
        private static readonly HttpClient HttpClient = new HttpClient();

        //If I want to use defaults
        private static readonly string Interval = "daily";
        private static readonly string Period = "100";

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
                            .GetAsync(String.Format(AlphaVantageUri + "function={0}&symbol={1}&interval={2}&time_period={3}&apikey={4}",
                            function, symbol, adxInterval, adxPeriod, Program.Config.GetValue<string>("AlphaVantageApiKey")));
                        break;
                    case "AROON":
                        string aroonInterval = "daily";
                        string aroonPeriod = "300";
                        response = await HttpClient
                            .GetAsync(String.Format(AlphaVantageUri + "function={0}&symbol={1}&interval={2}&time_period={3}&apikey={4}",
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
            decimal highestHigh = 0;
            decimal lowestLow = 0;
            string result = "";
            foreach (HistoricalPriceResponse price in prices)
            {
                decimal high = price.high;
                decimal low = price.low;
                long changePercent = price.changePercent;
                long changeOverTime = price.changeOverTime;
                decimal open = price.open;
                decimal close = price.close;
                long volumen = price.volume;
                result += "{date: " + price.date + ", price: " + close + "},";
            }
            return await Task.FromResult(result);
        }

        public static async Task<CompanyStats> GetCompanyStatsAsync(string symbol)
        {
            var keyStatsResponse = await IEXClient.Stock.KeyStatsAsync(symbol);
            //var financialResponse = await IEXClient.Stock.FinancialAsync(symbol, 1);
            //var largestTradesResponse = await IEXClient.Stock.LargestTradesAsync(symbol);
            KeyStatsResponse iexStat = keyStatsResponse.Data;
            FinancialResponse iexFinancial = null; //apparently paid service now ...?
            IEnumerable<LargestTradeResponse> iexTrades = null; //apparently paid service now ...?
            CompanyStats companyStat = new CompanyStats();

            if (iexStat != null)
            {
                decimal high = iexStat.week52high;
                decimal low = iexStat.week52low;
                decimal medianPrice52w = (high + low) / 2;
                companyStat.High52w = high;
                companyStat.Low52w = low;
                companyStat.MedianPrice = medianPrice52w;

                decimal change1m = iexStat.month1ChangePercent;
                decimal change3m = iexStat.month3ChangePercent;
                decimal change6m = iexStat.month6ChangePercent;
                companyStat.PercentChangeAvg = (change1m + change3m + change6m) / 3;
                companyStat.PercentChange1m = change1m;

                decimal volume10d = iexStat.avg10Volume;
                decimal volume30d = iexStat.avg30Volume;
                decimal avgVolume = (volume10d + volume30d) / 2;
                companyStat.Volume10d = volume10d;
                companyStat.VolumeAvg = avgVolume;

                //average daily liguid money trading around for this symbol
                companyStat.VolumeAvgUSD = avgVolume * companyStat.MedianPrice;

                decimal movingAvg50d = iexStat.day50MovingAvg;
                long peRatio = iexStat.peRatio;
                string companyName = iexStat.companyName;
                long numEmployees = iexStat.employees;
                long marketCap = iexStat.marketcap;
                long sharesOutstanding = iexStat.sharesOutstanding;
                companyStat.MovingAvg50d = movingAvg50d;
                companyStat.PeRatio = peRatio;
                companyStat.CompanyName = companyName;

                //dividends
                Dividends dividends = new Dividends
                {
                    DividendRate = iexStat.ttmDividendRate,
                    DividendYield = iexStat.dividendYield,
                    LastDividendDate = iexStat.exDividendDate
                };
                companyStat.Dividends = dividends;

                //market capitalization per employee (capita)
                companyStat.MarketCapPerCapita = marketCap / numEmployees;
            }

            if (iexFinancial != null) //apparently paid service now ...?
            {
                var fins = iexFinancial.financials;
                var fin = fins.FirstOrDefault();
                if (fin != null)
                {
                    companyStat.GrossProfit = fin.grossProfit;
                    companyStat.ShareHolderEquity = fin.shareholderEquity;
                    companyStat.TotalAssets = fin.totalAssets;
                    companyStat.TotalCash = fin.totalCash;
                    companyStat.TotalDebt = fin.totalDebt;
                    companyStat.TotalLiabilities = fin.totalLiabilities;
                    companyStat.TotalRevenue = fin.totalRevenue;
                    companyStat.LastFinancialReportDate = fin.reportDate;
                }
            }

            if (iexTrades != null) //apparently paid service now ...?
            {
                foreach (var item in iexTrades)
                {
                    Console.WriteLine(item);
                }
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

        public static async Task<AllCompanies> GetAllCompaniesAsync()
        {
            AllCompanies companies = new AllCompanies()
            {
                SymbolsToCompanies = new Dictionary<string, Company>()
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
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol))
                    {
                        bool isNasdaq = data[0] == "Y";
                        if (isNasdaq)
                        {
                            Company company = new Company
                            {
                                Name = data[2],
                                Exchange = "Nasdaq"
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
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol))
                    {
                        Company company = new Company
                        {
                            Name = data[1],
                            Exchange = "OTC"
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
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol))
                    {
                        Company company = new Company
                        {
                            Name = data[1],
                            Exchange = data[2]
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
