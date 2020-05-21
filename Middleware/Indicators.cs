using PT.Models;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace PT.Middleware
{
    //https://github.com/DaveSkender/Stock.Indicators
    public static class Indicators
    {
        public static decimal GetIndicatorComposite(string symbol, string function, IEnumerable<Skender.Stock.Indicators.Quote> history, int daysToCalculate)
        {
            decimal compositeScore = 0;
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
                        //resultSet = (JArray)data.GetValue("values");
                        //compositeScore = GetADXComposite(resultSet, daysToCalculate);
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
                        //resultSet = (JArray)data.GetValue("values");
                        //compositeScore = GetAROONComposite(resultSet, daysToCalculate);
                        break;

                    case "macd":
                        //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                        //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                        //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                        int fastPeriod = 12;
                        int slowPeriod = 26;
                        int signalPeriod = 9;
                        IEnumerable<MacdResult> results = Indicator.GetMacd(history, fastPeriod, slowPeriod, signalPeriod);
                        compositeScore = GetMACDComposite(results, daysToCalculate);
                        break;

                    case "obv":
                        //The On Balance Volume (OBV) is a cumulative total of the up and down volume.
                        //When the close is higher than the previous close, the volume is added to the running total,
                        //and when the close is lower than the previous close, the volume is subtracted from the running total.
                        //To interpret the OBV, look for the OBV to move with the price or precede price moves.
                        //If the price moves before the OBV, then it is a non-confirmed move. A series of rising peaks, or falling troughs
                        //in the OBV indicates a strong trend. If the OBV is flat, then the market is not trending.
                        //https://www.investopedia.com/articles/technical/100801.asp
                        //resultSet = (JArray)data.GetValue("values");
                        //compositeScore = GetOBVComposite(resultSet, daysToCalculate);
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
            //string adxResponse = CompleteTwelveDataRequest("ADX", symbol).Result;
            //decimal adxCompositeScore = GetCompositeScore(symbol, "ADX", adxResponse, 7);
            //string obvResponse = CompleteTwelveDataRequest("OBV", symbol).Result;
            //decimal obvCompositeScore = GetCompositeScore("OBV", obvResponse, 7);
            //string aroonResponse = CompleteTwelveDataRequest("AROON", symbol).Result;
            //decimal aroonCompositeScore = GetCompositeScore(symbol, "AROON", aroonResponse, 7);
            //string macdResponse = CompleteTwelveDataRequest("MACD", symbol).Result;
            //decimal macdCompositeScore = GetCompositeScore(symbol, "MACD", macdResponse, 7);

            IReadOnlyList<Candle> yahooHistory = YahooFinance.GetHistoryAsync(symbol, 7).Result;
            List<Skender.Stock.Indicators.Quote> historyList = new List<Skender.Stock.Indicators.Quote>();
            foreach (Candle data in yahooHistory)
            {
                Skender.Stock.Indicators.Quote curData = new Skender.Stock.Indicators.Quote();
                curData.Open = data.Open;
                curData.Close = data.AdjustedClose;
                curData.High = data.High;
                curData.Low = data.Low;
                curData.Volume = data.Volume;
                curData.Date = data.DateTime;
                historyList.Add(curData);
            }
            IEnumerable<Skender.Stock.Indicators.Quote> history = historyList.AsEnumerable();
            Cleaners.PrepareHistory(history);

            decimal adxCompositeScore = 0;
            decimal macdCompositeScore = GetIndicatorComposite(symbol, "MACD", history, 7);

            ShortInterestResult shortResult = FINRA.GetShortInterest(symbol, 7);

            FundamentalsResult fundResult = TwelveData.GetFundamentals(symbol, quote);

            TipRanksResult trResult = TipRanks.GetTipRanksResult(symbol);

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                DataProviders = "YahooFinance, FINRA, TipRanks",
                ADXComposite = adxCompositeScore,
                //OBVComposite = obvCompositeScore,
                //AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                RatingsComposite = trResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestCompositeScore,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                CompositeScoreValue = (adxCompositeScore + /*obvCompositeScore + aroonCompositeScore +*/ macdCompositeScore +
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

        public static decimal GetMACDComposite(IEnumerable<MacdResult> resultSet, int daysToCalculate)
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
                    decimal macdBaseValue = result.Macd != null ? (decimal) result.Macd : 0.0M;
                    decimal macdSignalValue = result.Signal != null ? (decimal) result.Signal : 0.0M;
                    decimal macdHistogramValue = result.Histogram != null ? (decimal) result.Histogram : 0.0M;

                    macdHistYList.Push(macdHistogramValue);
                    macdBaseYList.Push(macdBaseValue);
                    macdSignalYList.Push(macdSignalValue);
                    macdTotalHist += macdHistogramValue;
                    numberOfResults++;

                    string macdDate = result.Date.ToString("yyyy-MM-dd");
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
            decimal baseSlope = TwelveData.GetSlope(macdXList, baseYList);
            List<decimal> signalYList = macdSignalYList.ToList();
            decimal signalSlope = TwelveData.GetSlope(macdXList, signalYList);
            List<decimal> histYList = macdHistYList.ToList();
            decimal histSlope = TwelveData.GetSlope(macdXList, histYList);

            //look for buy and sell signals
            bool macdHasBuySignal = false;
            bool macdHasSellSignal = false;

            decimal macdPrev = histYList[0];
            bool macdPrevIsNegative = macdPrev < 0;
            for (int i = 1; i < histYList.Count; i++)
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
            decimal histSlopeMultiplier = TwelveData.GetSlopeMultiplier(histSlope);
            decimal baseSlopeMultiplier = TwelveData.GetSlopeMultiplier(baseSlope);
            decimal signalSlopeMultiplier = TwelveData.GetSlopeMultiplier(signalSlope);

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
    }
}
