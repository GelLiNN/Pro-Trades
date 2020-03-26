using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using VSLee.IEXSharp;

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
                        string adxInterval = "60min";
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
        public string GetData(string symbol)
        {
            string pToken = Program.Config.GetValue<string>("IexPublishableToken");
            string sToken = Program.Config.GetValue<string>("IexSecretToken");

            IEXCloudClient iexClient = new IEXCloudClient(pToken, sToken, signRequest: false, useSandBox: false);
            //iexClient.
            return string.Empty;
        }
    }

    //From StockSharp - IDK yet if we will use it
    public class SimpleStrategy : Strategy
    {
        [Display(Name = "CandleSeries",
             GroupName = "Base settings")]
        public CandleSeries CandleSeries { get; set; }
        public SimpleStrategy() { }

        protected override void OnStarted()
        {
            var connector = (Connector)Connector;
            connector.WhenCandlesFinished(CandleSeries).Do(CandlesFinished).Apply(this);
            connector.SubscribeCandles(CandleSeries);
            base.OnStarted();
        }

        private void CandlesFinished(Candle candle)
        {
            if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
            {
                RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
            }
            else if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
            {
                RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
            }
        }
    }
}
