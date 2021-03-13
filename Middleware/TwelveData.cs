using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PT.Models;
using YahooFinanceApi;

namespace PT.Middleware
{
    //Twelve Data https://twelvedata.com/docs#getting-started
    //API docs https://twelvedata.com/docs#technical-indicators
    public class TwelveData
    {
        private static readonly string APIKey = Program.Config.GetValue<string>("TwelveDataApiKey");
        private static readonly string TDURI = @"https://api.twelvedata.com/";
        private static readonly HttpClient Client = new HttpClient();

        public static async Task<string> CompleteTwelveDataRequest(string function, string symbol)
        {
            try
            {
                function = function.ToLower();
                HttpResponseMessage response = new HttpResponseMessage();

                //different GET params for each indicator
                switch (function)
                {
                    case "time_series":
                        //Gets OCHL data going back 14 trading days for a symbol
                        string tsInterval = "1day";
                        string tsOutputSize = "14";
                        string tsRequest = String.Format(TDURI + "{0}?symbol={1}&interval={2}&outputsize={3}&apikey={4}",
                            function, symbol, tsInterval, tsOutputSize, APIKey);
                        response = await Client.GetAsync(tsRequest);
                        break;

                    case "adx":
                        string adxInterval = "1day";
                        string adxPeriod = "28";
                        string adxOutputSize = "100";
                        string adxRequest = String.Format(TDURI + "{0}?symbol={1}&interval={2}&time_period={3}&outputsize={4}&apikey={5}",
                            function, symbol, adxInterval, adxPeriod, adxOutputSize, APIKey);
                        response = await Client.GetAsync(adxRequest);
                        break;

                    case "aroon":
                        string aroonInterval = "1day";
                        string aroonPeriod = "14"; //14 is used by default on Yahoo Finance, Investopedia calls for 25
                        string aroonRequest = String.Format(TDURI + "{0}?symbol={1}&interval={2}&time_period={3}&apikey={4}",
                            function, symbol, aroonInterval, aroonPeriod, APIKey);
                        response = await Client.GetAsync(aroonRequest);
                        break;

                    case "macd":
                        string macdInterval = "1day";
                        string macdSeriesType = "1"; //0 => close, 1 => open, 2 => high, 3 => low, 4 => volume
                        string macdRequest = String.Format(TDURI + "{0}?symbol={1}&interval={2}&series_type={3}&apikey={4}",
                            function, symbol, macdInterval, macdSeriesType, APIKey);
                        response = await Client.GetAsync(macdRequest);
                        break;

                    case "obv":
                        string obvInterval = "1day";
                        string obvSeriesType = "0"; //0 => close, 1 => open, 2 => high, 3 => low, 4 => volume
                        string obvOutputSize = "100";
                        string obvRequest = String.Format(TDURI + "{0}?symbol={1}&interval={2}&series_type={3}&outputsize={4}&apikey={5}",
                            function, symbol, obvInterval, obvSeriesType, obvOutputSize, APIKey);
                        response = await Client.GetAsync(obvRequest);
                        break;

                    //Below cases need to be migrated to use TD's conventions
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
                Debug.WriteLine("TD EXCEPTION Message: {0}, StackTrace: {1}", e.Message, e.StackTrace);
                return string.Empty;
            }
        }

        public static decimal GetCompositeScore(string symbol, string function, string twelveDataResponse, int daysToCalculate)
        {
            decimal compositeScore = 0;
            JObject data = JObject.Parse(twelveDataResponse);
            JArray resultSet;
            function = function.ToLower();

            try
            {
                //different processing for each indicator
                switch (function)
                {
                    case "adx":
                        //When the +DMI is above the -DMI, prices are moving up, and ADX measures the strength of the uptrend.
                        //When the -DMI is above the +DMI, prices are moving down, and ADX measures the strength of the downtrend.
                        //Many traders will use ADX readings above 25 to suggest that the trend is strong enough for trend-trading strategies.
                        //Conversely, when ADX is below 25, many will avoid trend-trading strategies.
                        resultSet = (JArray)data.GetValue("values");
                        compositeScore = GetADXComposite(resultSet, daysToCalculate);
                        break;

                    case "aroon":
                        //Indicator Movements Around the Key Levels, 30 and 70 - Movements above 70 indicate a strong trend,
                        //while movements below 30 indicate low trend strength. Movements between 30 and 70 indicate indecision.
                        //For example, if the bullish indicator remains above 70 while the bearish indicator remains below 30,
                        //the trend is definitively bullish.
                        //Crossovers Between the Bullish and Bearish Indicators - Crossovers indicate confirmations if they occur
                        //between 30 and 70.For example, if the bullish indicator crosses above the bearish indicator, it confirms a bullish trend.
                        //The two Aroon indicators(bullish and bearish) can also be made into a single oscillator by
                        //making the bullish indicator 100 to 0 and the bearish indicator 0 to - 100 and finding the
                        //difference between the two values. This oscillator then varies between 100 and - 100, with 0 indicating no trend.
                        resultSet = (JArray)data.GetValue("values");
                        compositeScore = GetAROONComposite(resultSet, daysToCalculate);
                        break;

                    case "macd":
                        //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                        //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                        //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                        resultSet = (JArray)data.GetValue("values");
                        compositeScore = GetMACDComposite(resultSet, daysToCalculate);
                        break;

                    case "obv":
                        //The On Balance Volume (OBV) is a cumulative total of the up and down volume.
                        //When the close is higher than the previous close, the volume is added to the running total,
                        //and when the close is lower than the previous close, the volume is subtracted from the running total.
                        //To interpret the OBV, look for the OBV to move with the price or precede price moves.
                        //If the price moves before the OBV, then it is a non-confirmed move. A series of rising peaks, or falling troughs
                        //in the OBV indicates a strong trend. If the OBV is flat, then the market is not trending.
                        //https://www.investopedia.com/articles/technical/100801.asp
                        resultSet = (JArray)data.GetValue("values");
                        compositeScore = GetOBVComposite(resultSet, daysToCalculate);
                        break;

                    //Below cases need to be migrated to use TD's conventions
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
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION CAUGHT: TwelveData.cs GetCompositeScore for symbol " + symbol + ", function " + function + ", message: " + e.Message);
            }
            return compositeScore;
        }

        public static CompositeScoreResult GetCompositeScoreResult(string symbol, Security quote)
        {
            string adxResponse = CompleteTwelveDataRequest("ADX", symbol).Result;
            decimal adxCompositeScore = GetCompositeScore(symbol, "ADX", adxResponse, 7);
            //string obvResponse = CompleteTwelveDataRequest("OBV", symbol).Result;
            //decimal obvCompositeScore = GetCompositeScore("OBV", obvResponse, 7);
            string aroonResponse = CompleteTwelveDataRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = GetCompositeScore(symbol, "AROON", aroonResponse, 7);
            string macdResponse = CompleteTwelveDataRequest("MACD", symbol).Result;
            decimal macdCompositeScore = GetCompositeScore(symbol, "MACD", macdResponse, 7);

            ShortInterestResult shortResult = new ShortInterestResult(); //FINRA.GetShortInterest(symbol, 7);

            FundamentalsResult fundResult = Indicators.GetFundamentals(symbol, quote);

            TipRanksResult trResult = TipRanks.GetTipRanksResult(symbol);

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                DataProviders = "TwelveData, FINRA, YahooFinance, TipRanks",
                ADXComposite = adxCompositeScore,
                //OBVComposite = obvCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                RatingsComposite = trResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestCompositeScore,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                CompositeScoreValue = (adxCompositeScore + /*obvCompositeScore +*/ aroonCompositeScore + macdCompositeScore +
                    shortResult.ShortInterestCompositeScore + fundResult.FundamentalsComposite + trResult.RatingsComposite) / 6,
                ShortInterest = shortResult,
                Fundamentals = fundResult,
                TipRanks = trResult
            };

            string rank = string.Empty;
            if (scoreResult.Fundamentals.IsBlacklisted)
                rank = "DISQUALIFIED";
            else if (scoreResult.CompositeScoreValue > 0 && scoreResult.CompositeScoreValue < 60)
                rank = "BAD";
            else if (scoreResult.CompositeScoreValue >= 60 && scoreResult.CompositeScoreValue < 70)
                rank = "FAIR";
            else if (scoreResult.CompositeScoreValue >= 70 && scoreResult.CompositeScoreValue < 80)
                rank = "GOOD";
            else if (scoreResult.CompositeScoreValue >= 80)
                rank = "PRIME";
            scoreResult.CompositeRank = rank;

            return scoreResult;
        }


        public static decimal GetADXComposite(JArray resultSet, int daysToCalculate)
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
                    decimal adxVal = decimal.Parse(result.Value<string>("adx"));
                    adxValueYList.Push(adxVal);
                    adxTotal += adxVal;
                    numberOfResults++;

                    string resultDate = result.Value<string>("datetime");
                    string adxDate = DateTime.Parse(resultDate).ToString("yyyy-MM-dd");
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
            decimal adxSlope = Indicators.GetSlope(adxXList, adxYList);
            decimal adxSlopeMultiplier = Indicators.GetSlopeMultiplier(adxSlope);
            decimal adxAvg = adxTotal / numberOfResults;

            List<decimal> adxZScores = Indicators.GetZScores(adxYList);
            decimal zScoreSlope = Indicators.GetSlope(adxXList, adxZScores);
            decimal zScoreSlopeMultiplier = Indicators.GetSlopeMultiplier(zScoreSlope);

            //Start with the average of the 2 most recent ADX values
            decimal baseValue = (adxYList[adxYList.Count - 1] + adxYList[adxYList.Count - 2]) / 2;

            //Add based on the most recent Z Score * 100 if it is positive
            decimal recentZScore = adxZScores[adxZScores.Count - 1];
            if (recentZScore > 0)
                baseValue += (recentZScore * 100) / 2; //to even it out
            else
                baseValue += 10; //pity points

            //Add bonus for ADX average above 25 per investopedia recommendation
            decimal averageBonus = adxAvg > 25 ? 25 : 0;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += averageBonus;
            composite += (adxSlope > 0.1M) ? (adxSlope * adxSlopeMultiplier) + 10 : -10; //penalty
            composite += (zScoreSlope > 0.1M) ? (zScoreSlope * zScoreSlopeMultiplier) + 10 : 0;

            return Math.Min(composite, 100); //cap ADX composite at 100, no extra weight
        }

        public static decimal GetAROONComposite(JArray resultSet, int daysToCalculate)
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
                    decimal aroonUpVal = decimal.Parse(result.Value<string>("aroon_up"));
                    decimal aroonDownVal = decimal.Parse(result.Value<string>("aroon_down"));
                    aroonUpYList.Push(aroonUpVal);
                    aroonDownYList.Push(aroonDownVal);
                    aroonOscillatorYList.Push(aroonUpVal - aroonDownVal);
                    aroonUpTotal += aroonUpVal;
                    aroonDownTotal += aroonDownVal;
                    numberOfResults++;

                    string resultDate = result.Value<string>("datetime");
                    string aroonDate = DateTime.Parse(resultDate).ToString("yyyy-MM-dd");
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
            decimal upSlope = Indicators.GetSlope(aroonXList, upYList);
            List<decimal> downYList = aroonDownYList.ToList();
            decimal downSlope = Indicators.GetSlope(aroonXList, downYList);
            List<decimal> oscillatorYList = aroonOscillatorYList.ToList();
            //decimal oscillatorSlope = GetSlope(aroonXList, oscillatorYList);

            decimal upSlopeMultiplier = Indicators.GetSlopeMultiplier(upSlope);
            decimal downSlopeMultiplier = Indicators.GetSlopeMultiplier(downSlope);
            //decimal oscillatorSlopeMultiplier = GetSlopeMultiplier(oscillatorSlope);

            //look for buy and sell signals
            bool aroonHasBuySignal = false;
            bool aroonHasSellSignal = false;
            decimal previousOscillatorValue = oscillatorYList[0];
            bool previousIsNegative = previousOscillatorValue <= 0;
            for (int i = 1; i < oscillatorYList.Count(); i++)
            {
                decimal currentOscillatorValue = oscillatorYList[i];
                bool currentIsNegative = currentOscillatorValue <= 0;
                if (!currentIsNegative && previousIsNegative && (upYList[i] >= 30 && downYList[i] <= 70))
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

            decimal aroonAvgUp = Math.Max(aroonUpTotal / numberOfResults, 1.0M);
            decimal aroonAvgDown = Math.Max(aroonDownTotal / numberOfResults, 1.0M);
            decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
            decimal percentDiffUp = (aroonAvgUp / aroonAvgDown) * 100;

            decimal bullResult = Math.Min(100 - percentDiffDown, 50); //bull result caps at 50
            decimal bearResult = Math.Min(percentDiffUp + 15, 20); //bear result caps at 20

            //Add bull bonus if last AROON UP >= 70 per investopedia recommendation
            //This is the same as when last AROON OSC >= 50
            //Cap bullBonus at 15
            decimal bullBonus = (aroonAvgUp > aroonAvgDown && previousOscillatorValue >= 50) ? Math.Min(previousOscillatorValue / 5, 15) : 0;

            //Add bear bonus if last AROON UP > last AROON DOWN per investopedia recommendation
            //This is the same as when last AROON OSC > 0
            //Cap bearBonus at 10
            decimal bearBonus = (aroonAvgDown > aroonAvgUp && previousOscillatorValue > 0) ? Math.Min(previousOscillatorValue / 5, 10) : 0;

            //calculate composite score based on the following values and weighted multipliers
            //if AROON avg up > AROON avg down, start score with 100 - (down as % of up)
            //if AROON avg up < AROON avg down, start score with 100 - (up as % of down)
            decimal composite = 0;
            composite += (aroonAvgUp > aroonAvgDown) ? bullResult : bearResult;
            composite += (bullBonus > bearBonus) ? bullBonus : bearBonus;
            composite += (upSlope > -0.05M) ? (upSlope * upSlopeMultiplier) + 5 : 0;
            composite += (downSlope < 0.05M) ? (downSlope * downSlopeMultiplier) + 5 : -(downSlope * downSlopeMultiplier);
            composite += (aroonHasBuySignal) ? 25 : 0;
            composite += (aroonHasSellSignal) ? -25 : 0;

            return composite;
        }

        public static decimal GetMACDComposite(JArray resultSet, int daysToCalculate)
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
                    decimal macdBaseValue = decimal.Parse(result.Value<string>("macd"));
                    decimal macdSignalValue = decimal.Parse(result.Value<string>("macd_signal"));
                    decimal macdHistogramValue = decimal.Parse(result.Value<string>("macd_hist"));

                    macdHistYList.Push(macdHistogramValue);
                    macdBaseYList.Push(macdBaseValue);
                    macdSignalYList.Push(macdSignalValue);
                    macdTotalHist += macdHistogramValue;
                    numberOfResults++;

                    string resultDate = result.Value<string>("datetime");
                    string macdDate = DateTime.Parse(resultDate).ToString("yyyy-MM-dd");
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
            decimal baseSlope = Indicators.GetSlope(macdXList, baseYList);
            List<decimal> signalYList = macdSignalYList.ToList();
            decimal signalSlope = Indicators.GetSlope(macdXList, signalYList);
            List<decimal> histYList = macdHistYList.ToList();
            decimal histSlope = Indicators.GetSlope(macdXList, histYList);

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
            decimal histSlopeMultiplier = Indicators.GetSlopeMultiplier(histSlope);
            decimal baseSlopeMultiplier = Indicators.GetSlopeMultiplier(baseSlope);
            decimal signalSlopeMultiplier = Indicators.GetSlopeMultiplier(signalSlope);

            decimal histBonus = Math.Min((macdTotalHist * 5) + 5, 20); //cap histBonus at 20

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += (histSlope > -0.05M) ? (histSlope * histSlopeMultiplier) + 25 : 0;
            composite += (baseSlope > -0.05M) ? (baseSlope * baseSlopeMultiplier) + 10 : 0;
            composite += (signalSlope > -0.05M) ? (signalSlope * signalSlopeMultiplier) + 10 : 0;
            composite += (histBonus > 0) ? histBonus : 0; //Add histBonus if macdTotalHist is not negative
            composite += (macdHasBuySignal) ? 40 : 0;
            composite += (macdHasSellSignal) ? -40 : 0;

            return composite;
        }

        //unused for now
        public static decimal GetOBVComposite(JArray resultSet, int daysToCalculate)
        {
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> obvValueYList = new Stack<decimal>();
            decimal obvSum = 0;

            foreach (var result in resultSet)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal obvValue = decimal.Parse(result.Value<string>("obv"));

                    obvValueYList.Push(obvValue);
                    obvSum += obvValue;
                    numberOfResults++;

                    string resultDate = result.Value<string>("datetime");
                    string obvDate = DateTime.Parse(resultDate).ToString("yyyy-MM-dd");
                    if (!dates.Contains(obvDate))
                    {
                        dates.Add(obvDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> obvXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                obvXList.Add(i);

            List<decimal> obvYList = obvValueYList.ToList();
            decimal obvSlope = Indicators.GetSlope(obvXList, obvYList);
            decimal obvSlopeMultiplier = Indicators.GetSlopeMultiplier(obvSlope);

            List<decimal> zScores = Indicators.GetZScores(obvYList);
            decimal zScoreSlope = Indicators.GetSlope(zScores, obvXList);
            decimal zScoreSlopeMultiplier = Indicators.GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedScores = Indicators.GetNormalizedData(obvYList);
            decimal normalizedSlope = Indicators.GetSlope(normalizedScores, obvXList);
            decimal normalizedSlopeMultiplier = Indicators.GetSlopeMultiplier(normalizedSlope);

            decimal obvAverage = obvSum / numberOfResults;

            //look for buy and sell signals
            bool obvHasBuySignal = false;
            bool obvHasSellSignal = false;

            decimal obvPrev = obvYList[0];
            bool obvPrevIsNegative = obvPrev < 0;
            for (int i = 1; i < obvYList.Count(); i++)
            {
                decimal current = obvYList[i];
                bool currentIsNegative = current < 0;
                if (!currentIsNegative && obvPrevIsNegative)
                {
                    obvHasBuySignal = true;
                    if (obvHasSellSignal)
                        obvHasSellSignal = false; //cancel the previous sell signal if buy signal is most recent
                }
                else if (currentIsNegative && !obvPrevIsNegative)
                {
                    obvHasSellSignal = true;
                    if (obvHasBuySignal)
                        obvHasBuySignal = false; //cancel the previous buy signal if sell signal is most recent
                }
                obvPrev = current;
                obvPrevIsNegative = obvPrev < 0;
            }

            //Start with the average of the 2 most recent OBV Normalized Scores
            //Only use the normalized scores if average OBV is greater than 0
            decimal baseValue;
            if (obvAverage > 0)
                baseValue = ((normalizedScores[normalizedScores.Count - 1] + normalizedScores[normalizedScores.Count - 2]) / 2) * 100;
            else
            {
                //ZScore bonus helps us score based on derivatives
                //Only add ZScoreBonus if it is positive, divide by 4 instead of 2 (which would be classic mean)
                decimal zScoreBonus = ((zScores[zScores.Count - 1] + zScores[zScores.Count - 2]) / 4) * 100;
                if (zScoreBonus < 0)
                    zScoreBonus = 15; //pity points

                baseValue = zScoreBonus;
            }

            //Add bonus if average OBV is greater than 0
            decimal obvAverageBonus = obvAverage > 0 ? 15 : 0;

            //Add bonus if OBV slope positive
            decimal obvSlopeBonus = (obvSlope > 0) ? 10 : 0;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += obvAverageBonus;
            composite += obvSlopeBonus;
            composite += (zScoreSlope > 0) ? (zScoreSlope * zScoreSlopeMultiplier) : 0;
            composite += (normalizedSlope > 0) ? (normalizedSlope * normalizedSlopeMultiplier) : 0;
            composite += (obvHasBuySignal) ? 15 : 0;
            composite += (obvHasSellSignal) ? -15 : 0;

            return Math.Min(composite, 100); //cap OBV composite at 100, no extra weight
        }

        public static string result = "";
        public static string GenerateLowestNumber(string number, int n)
        {
            Helper(number, n);
            string retval = result;
            result = string.Empty;
            return retval;
        }
        private static void Helper(string str, int n)
        {
	        if (n == 0)
	        {
		        result += str;
		        return;
	        }
	        if (str.Length <= n)
		        return;
	        // Find smallest characters
	        int minIndex = 0;
	        for (int i = 1; i <= n; i++)
		        if (str[i] < str[minIndex])
			        minIndex = i;
	        result += str[minIndex];
	        string newStr = str.Substring(minIndex + 1);
            Helper(newStr, n - minIndex);
        }
    }
}
