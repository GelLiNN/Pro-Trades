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
                    case "OBV":
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
            decimal compositeScore = 0;
            JObject data = JObject.Parse(alphaVantageResponse);
            JObject resultSet;

            //different processing for each indicator
            switch (function.ToUpper())
            {
                case "ADX":
                    //When the +DMI is above the -DMI, prices are moving up, and ADX measures the strength of the uptrend.
                    //When the -DMI is above the +DMI, prices are moving down, and ADX measures the strength of the downtrend.
                    //Many traders will use ADX readings above 25 to suggest that the trend is strong enough for trend-trading strategies.
                    //Conversely, when ADX is below 25, many will avoid trend-trading strategies.
                    resultSet = (JObject)data.GetValue("Technical Analysis: ADX");
                    compositeScore = GetADXComposite(resultSet, daysToCalculate);
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
                    compositeScore = GetAROONComposite(resultSet, daysToCalculate);
                    break;

                case "MACD":
                    //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                    //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                    //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                    resultSet = (JObject)data.GetValue("Technical Analysis: MACD");
                    compositeScore = GetMACDComposite(resultSet, daysToCalculate);
                    break;

                case "BBANDS":
                    //Bollinger Bands consist of three lines. The middle band is a simple moving average (generally 20 periods)
                    //of the typical price (TP). The upper and lower bands are F standard deviations (generally 2) above and below the middle band.
                    //The bands widen and narrow when the volatility of the price is higher or lower, respectively.
                    //Bollinger Bands do not, in themselves, generate buy or sell signals; they are an indicator of overbought or oversold conditions.
                    //When the price is near the upper or lower band it indicates that a reversal may be imminent.
                    //The middle band becomes a support or resistance level.The upper and lower bands can also be interpreted as price targets.
                    //When the price bounces off of the lower band and crosses the middle band, then the upper band becomes the price target.
                    //See also Bollinger Width, Envelope, Price Channels and Projection Bands.
                    //https://www.investopedia.com/articles/technical/04/030304.asp
                    //https://www.fmlabs.com/reference/default.htm?url=Bollinger.htm
                    //https://www.alphavantage.co/query?function=BBANDS&symbol=MSFT&interval=weekly&time_period=5&series_type=close&nbdevup=3&nbdevdn=3&apikey=demo
                    break;


                case "STOCH":
                    //The Stochastic Oscillator measures where the close is in relation to the recent trading range.
                    //The values range from zero to 100. D values over 75 indicate an overbought condition; values under 25 indicate an oversold condition.
                    //When the Fast D crosses above the Slow D, it is a buy signal; when it crosses below, it is a sell signal.
                    //The Raw K is generally considered too erratic to use for crossover signals.
                    //https://www.fmlabs.com/reference/default.htm?url=StochasticOscillator.htm
                    //https://www.investopedia.com/articles/technical/073001.asp
                    //https://www.alphavantage.co/query?function=STOCH&symbol=MSFT&interval=daily&apikey=
                        break;

                case "RSI":
                    //The Relative Strength Index (RSI) calculates a ratio of the recent upward price movements to the absolute price movement.
                    //The RSI ranges from 0 to 100. The RSI is interpreted as an overbought/oversold indicator when the value is over 70/below 30.
                    //You can also look for divergence with price. If the price is making new highs/lows, and the RSI is not, it indicates a reversal.
                    //https://www.investopedia.com/articles/active-trading/042114/overbought-or-oversold-use-relative-strength-index-find-out.asp
                    //https://www.alphavantage.co/query?function=RSI&symbol=MSFT&interval=weekly&time_period=10&series_type=open&apikey=demo
                    break;

                case "OBV":
                    //The On Balance Volume (OBV) is a cumulative total of the up and down volume.
                    //When the close is higher than the previous close, the volume is added to the running total,
                    //and when the close is lower than the previous close, the volume is subtracted from the running total.
                    //To interpret the OBV, look for the OBV to move with the price or precede price moves.
                    //If the price moves before the OBV, then it is a non - confirmed move. A series of rising peaks, or falling troughs
                    //in the OBV indicates a strong trend. If the OBV is flat, then the market is not trending.
                    //https://www.investopedia.com/articles/technical/100801.asp
                    //https://www.alphavantage.co/query?function=OBV&symbol=MSFT&interval=daily&apikey=
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
                    //https://www.investopedia.com/investing/timing-trades-with-commodity-channel-index/
                    //https://www.alphavantage.co/query?function=CCI&symbol=MSFT&interval=daily&time_period=10&apikey=
                    break;

            }
            return compositeScore;
        }

        public static decimal GetADXComposite(JObject resultSet, int daysToCalculate)
        {
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> adxValueYList = new Stack<decimal>();
            decimal adxTotal = 0;
            foreach (var result in resultSet)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal adxVal = decimal.Parse(result.Value.Value<string>("ADX"));
                    adxValueYList.Push(adxVal);
                    adxTotal += adxVal;
                    numberOfResults++;

                    string adxKey = result.Key;
                    string adxDate = DateTime.Parse(adxKey).ToString("yyyy-MM-dd");
                    if (!dates.Contains(adxDate))
                    {
                        dates.Add(adxDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> adxXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                adxXList.Add(i);

            List<decimal> adxYList = adxValueYList.ToList();
            decimal adxSlope = GetSlope(adxXList, adxYList);
            decimal adxSlopeMultiplier = GetSlopeMultiplier(adxSlope);
            decimal adxAvg = adxTotal / numberOfResults;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += ((adxAvg) / 25) * 100; //get average trend strength as a percentage of 25
            composite += (adxSlope > -0.1M) ? (adxSlope * adxSlopeMultiplier) + 10 : 0;

            return composite;
        }

        public static decimal GetAROONComposite(JObject resultSet, int daysToCalculate)
        {
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> aroonUpYList = new Stack<decimal>();
            Stack<decimal> aroonDownYList = new Stack<decimal>();
            Stack<decimal> aroonOscillatorYList = new Stack<decimal>();
            decimal aroonUpTotal = 0;
            decimal aroonDownTotal = 0;

            foreach (var result in resultSet)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal aroonUpVal = decimal.Parse(result.Value.Value<string>("Aroon Up"));
                    decimal aroonDownVal = decimal.Parse(result.Value.Value<string>("Aroon Down"));
                    aroonUpYList.Push(aroonUpVal);
                    aroonDownYList.Push(aroonDownVal);
                    aroonOscillatorYList.Push(aroonUpVal - aroonDownVal);
                    aroonUpTotal += aroonUpVal;
                    aroonDownTotal += aroonDownVal;
                    numberOfResults++;

                    string aroonKey = result.Key;
                    string aroonDate = DateTime.Parse(aroonKey).ToString("yyyy-MM-dd");
                    if (!dates.Contains(aroonDate))
                    {
                        dates.Add(aroonDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> aroonXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                aroonXList.Add(i);

            List<decimal> upYList = aroonUpYList.ToList();
            decimal upSlope = GetSlope(aroonXList, upYList);
            List<decimal> downYList = aroonDownYList.ToList();
            decimal downSlope = GetSlope(aroonXList, downYList);
            List<decimal> oscillatorYList = aroonOscillatorYList.ToList();
            decimal oscillatorSlope = GetSlope(aroonXList, oscillatorYList);

            decimal upSlopeMultiplier = GetSlopeMultiplier(upSlope);
            decimal downSlopeMultiplier = GetSlopeMultiplier(downSlope);
            decimal oscillatorSlopeMultiplier = GetSlopeMultiplier(oscillatorSlope);

            //look for buy and sell signals
            bool aroonHasBuySignal = false;
            bool aroonHasSellSignal = false;
            decimal previousOscillatorValue = oscillatorYList[0];
            bool previousIsNegative = previousOscillatorValue <= 0;
            for (int i = 1; i < oscillatorYList.Count(); i++)
            {
                decimal currentOscillatorValue = oscillatorYList[i];
                bool currentIsNegative = currentOscillatorValue <= 0;
                if (!currentIsNegative && previousIsNegative && (upYList[i] >= 20 && downYList[i] <= 80))
                {
                    aroonHasBuySignal = true;
                }
                else if (currentIsNegative && !previousIsNegative && (upYList[i] <= 30 && downYList[i] >= 70))
                {
                    aroonHasSellSignal = true;
                }
                previousOscillatorValue = currentOscillatorValue;
                previousIsNegative = previousOscillatorValue <= 0;
            }

            decimal aroonAvgUp = aroonUpTotal / numberOfResults;
            decimal aroonAvgDown = aroonDownTotal / numberOfResults;
            decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
            decimal percentDiffUp = (aroonAvgUp / aroonAvgDown) * 100;
            decimal bullResult = 100 - percentDiffDown;
            decimal bearResult = 100 - percentDiffUp;

            //Add bull bonus if AROON avg up >= 70 per investopedia recommendation
            //Cap bullBonus at 20
            decimal bullBonus = (aroonAvgUp > aroonAvgDown && aroonAvgUp >= 70) ? Math.Min((aroonAvgUp / 2) + 10, 20) : 0;

            //calculate composite score based on the following values and weighted multipliers
            //if AROON avg up > AROON avg down, start score with 100 - (down as % of up) +10
            //if AROON avg up < AROON avg down, start score with 100 - (up as % of down)
            decimal composite = 0;
            composite += (aroonAvgUp > aroonAvgDown) ? bullResult : bearResult;
            composite += bullBonus;
            composite += (upSlope > -0.1M) ? (upSlope * upSlopeMultiplier) + 10 : 0;
            composite += (downSlope < 0.1M) ? (downSlope * downSlopeMultiplier) : -(downSlope * downSlopeMultiplier);
            composite += (oscillatorSlope > -0.1M) ? (oscillatorSlope * oscillatorSlopeMultiplier) + 10 : -10;
            composite += (aroonHasBuySignal) ? 40 : 0;
            composite += (aroonHasSellSignal) ? -40 : 0;

            return composite;
        }

        public static decimal GetMACDComposite(JObject resultSet, int daysToCalculate)
        {
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> macdHistYList = new Stack<decimal>();
            Stack<decimal> macdBaseYList = new Stack<decimal>();
            Stack<decimal> macdSignalYList = new Stack<decimal>();
            decimal macdTotalHist = 0;

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
                    macdTotalHist += macdHistogramValue;
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

            List<decimal> macdXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                macdXList.Add(i);

            List<decimal> baseYList = macdBaseYList.ToList();
            decimal baseSlope = GetSlope(macdXList, baseYList);
            List<decimal> signalYList = macdSignalYList.ToList();
            decimal signalSlope = GetSlope(macdXList, signalYList);
            List<decimal> histYList = macdHistYList.ToList();
            decimal histSlope = GetSlope(macdXList, histYList);

            //look for buy and sell signals
            bool macdHasBuySignal = false;
            bool macdHasSellSignal = false;

            decimal macdPrev = histYList[0];
            bool macdPrevIsNegative = macdPrev < 0;
            for (int i = 1; i < histYList.Count(); i++)
            {
                decimal current = histYList[i];
                bool currentIsNegative = current < 0;
                if (!currentIsNegative && macdPrevIsNegative)
                {
                    macdHasBuySignal = true;
                }
                else if (currentIsNegative && !macdPrevIsNegative)
                {
                    macdHasSellSignal = true;
                }
                macdPrev = current;
                macdPrevIsNegative = macdPrev < 0;
            }
            decimal histSlopeMultiplier = GetSlopeMultiplier(histSlope);
            decimal baseSlopeMultiplier = GetSlopeMultiplier(baseSlope);
            decimal signalSlopeMultiplier = GetSlopeMultiplier(signalSlope);

            decimal histBonus = Math.Min(macdTotalHist * 4, 20); //cap histBonus at 20

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += (histSlope > -0.1M) ? (histSlope * histSlopeMultiplier) + 30 : 0;
            composite += (baseSlope > -0.1M) ? (baseSlope * baseSlopeMultiplier) + 10 : 0;
            composite += (signalSlope > -0.1M) ? (signalSlope * signalSlopeMultiplier) + 10 : 0;
            composite += (histBonus > 0) ? histBonus : 0;
            composite += (macdHasBuySignal) ? 40 : 0;
            composite += (macdHasSellSignal) ? -40 : 0;

            return composite;
        }

        public static decimal GetSlope(List<decimal> xList, List<decimal> yList)
        {
            //"zip" xs and ys to make the sum of products easier
            var xys = Enumerable.Zip(xList, yList, (x, y) => new { x = x, y = y });
            decimal xbar = xList.Average();
            decimal ybar = yList.Average();
            decimal slope = xys.Sum(xy => (xy.x - xbar) * (xy.y - ybar)) / xList.Sum(x => (x - xbar) * (x - xbar));
            return slope;
        }

        public static decimal GetSlopeMultiplier(decimal slope)
        {
            //Positive cases
            if (slope > 0 && slope < 0.5M)
                return 50.0M;
            else if (slope > 0.5M && slope < 1)
                return 25.0M;
            else if (slope > 1 && slope < 5)
                return 5.0M;
            else if (slope > 5 && slope < 10)
                return 2.5M;
            else if (slope > 10 && slope < 20)
                return 1.5M;
            else if (slope > 20)
                return 1.0M;

            //Negative cases
            else if (slope < 0 && slope > -0.5M)
                return -50.0M;
            else if (slope < -0.5M && slope > -1)
                return -25.0M;
            else if (slope < -1 && slope > -5)
                return -5.0M;
            else if (slope < -5 && slope > -10)
                return -2.5M;
            else if (slope < -10 && slope > -20)
                return -1.5M;
            else if (slope < -20)
                return -1.0M;
            else
                return 0;
        }
    }
}
