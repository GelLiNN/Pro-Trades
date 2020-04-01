using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

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
                    case "MACD":
                        string macdInterval = "daily";
                        string macdSeriesType = "open";
                        response = await HttpClient
                            .GetAsync(String.Format(AVURI + "function={0}&symbol={1}&interval={2}&series_type={3}&apikey={4}",
                            function, symbol, macdInterval, macdSeriesType, Program.Config.GetValue<string>("AlphaVantageApiKey")));
                        break;
                    case "BBANDS":
                        break;
                    case "STOCH":
                        break;
                    case "RSI":
                        break;
                    case "CCI":
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
                    resultSet = (JObject)data.GetValue("Technical Analysis: ADX");
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
                case "MACD":
                    //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                    //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                    //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                    resultSet = (JObject)data.GetValue("Technical Analysis: MACD");
                    List<decimal> xList = new List<decimal>();
                    for (int i = 1; i <= daysToCalculate; i++)
                        xList.Add(i);

                    Stack<decimal> macdHistYList = new Stack<decimal>();
                    Stack<decimal> macdBaseYList = new Stack<decimal>();
                    Stack<decimal> macdSignalYList = new Stack<decimal>();

                    foreach (var result in resultSet)
                    {
                        if (daysCalulated < daysToCalculate)
                        {
                            decimal macdBaseValue = decimal.Parse(result.Value.Value<string>("MACD"));
                            decimal macdSignalValue = decimal.Parse(result.Value.Value<string>("MACD_Signal"));
                            decimal macdHistogramValue = decimal.Parse(result.Value.Value<string>("MACD_Hist"));
                            macdHistYList.Push(macdHistogramValue);
                            macdBaseYList.Push(macdBaseValue);
                            macdSignalYList.Push(macdSignalValue);
                            numberOfResults++;

                            string macdKey = result.Key;
                            string macdDate = DateTime.Parse(macdKey).ToString("yyyy-MM-dd");
                            if (!dates.Contains(macdDate))
                            {
                                dates.Add(macdDate);
                                daysCalulated++;
                            }
                        }
                        else
                            break;
                    }
                    List<decimal> baseYList = macdBaseYList.ToList();
                    decimal baseSlope = GetSlope(xList, baseYList);
                    List<decimal> signalYList = macdSignalYList.ToList();
                    decimal signalSlope = GetSlope(xList, signalYList);
                    List<decimal> histYList = macdHistYList.ToList();
                    decimal histSlope = GetSlope(xList, histYList);
                    //decimal multiplier = GetScoreMultiplier(histXList, histYList);

                    //look for buy and sell signals
                    bool hasBuySignal = false;
                    bool hasSellSignal = false;

                    decimal previous = histYList[0];
                    bool previousIsNegative = previous < 0;
                    for (int i = 1; i < histYList.Count(); i++)
                    {
                        decimal current = histYList[i];
                        bool currentIsNegative = current < 0;
                        if (!currentIsNegative && previousIsNegative)
                        {
                            hasBuySignal = true;
                        }
                        else if (currentIsNegative && !previousIsNegative)
                        {
                            hasSellSignal = true;
                        }
                        previous = current;
                        previousIsNegative = currentIsNegative;
                    }
                    if (histSlope > 0)
                    {
                        compositeScore = 0;// (histSlope / maxHistSlope);
                    }
                    break;
                case "BBANDS":
                    break;
                case "RSI":
                    break;
                case "OBV":
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

        public static decimal GetSlope(List<decimal> xList, List<decimal> yList)
        {
            //get list of x's and y's with potential skipping
            //var xs = xList.Skip(skip).Take(months);
            //var ys = yList.Skip(skip).Take(months);

            //"zip" xs and ys to make the sum of products easier
            var xys = Enumerable.Zip(xList, yList, (x, y) => new { x = x, y = y });

            decimal xbar = xList.Average();
            decimal ybar = yList.Average();

            decimal slope = xys.Sum(xy => (xy.x - xbar) * (xy.y - ybar)) / xList.Sum(x => (x - xbar) * (x - xbar));

            return slope;
        }
    }
}
