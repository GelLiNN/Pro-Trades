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
    //https://www.codeproject.com/Articles/15047/Creating-a-Mechanical-Trading-System-Part-1-Techni
    public static class Indicators
    {
        public static decimal GetIndicatorComposite(string symbol, string function, IEnumerable<Skender.Stock.Indicators.Quote> history, int daysToCalculate, object supplement = null)
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
                        int adxPeriod = 14;
                        IEnumerable<AdxResult> adxResults = Indicator.GetAdx(history, adxPeriod);
                        compositeScore = GetADXComposite(adxResults, daysToCalculate);
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
                        int aroonPeriod = 14;
                        IEnumerable<AroonResult> aroonResults = Indicator.GetAroon(history, aroonPeriod);
                        compositeScore = GetAROONComposite(aroonResults, daysToCalculate);
                        break;

                    case "macd":
                        //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                        //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                        //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                        int fastPeriod = 12;
                        int slowPeriod = 26;
                        int signalPeriod = 9;
                        IEnumerable<MacdResult> macdResults = Indicator.GetMacd(history, fastPeriod, slowPeriod, signalPeriod);
                        compositeScore = GetMACDComposite(macdResults, daysToCalculate);
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
                        int obvPeriod = 14;
                        IEnumerable<ObvResult> obvResults = Indicator.GetObv(history);
                        compositeScore = GetOBVComposite(obvResults, daysToCalculate);
                        break;

                    //Below cases need to be migrated to use TD's conventions
                    case "bbands":
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
                        int bbandsPeriod = 20;
                        int standardDeviations = 2;
                        IEnumerable<BollingerBandsResult> bbandsResults = Indicator.GetBollingerBands(history, bbandsPeriod, standardDeviations);
                        compositeScore = GetBBANDSComposite(bbandsResults, (List<Skender.Stock.Indicators.Quote>) supplement, daysToCalculate);
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
            IReadOnlyList<Candle> yahooHistory = YahooFinance.GetHistoryAsync(symbol, 200).Result;
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

            List<Skender.Stock.Indicators.Quote> supplement = historyList.TakeLast(7).ToList();

            decimal adxCompositeScore = GetIndicatorComposite(symbol, "ADX", history, 7);
            decimal obvCompositeScore = GetIndicatorComposite(symbol, "OBV", history, 7);
            decimal macdCompositeScore = GetIndicatorComposite(symbol, "MACD", history, 7);
            //decimal bbandsCompositeScore = GetIndicatorComposite(symbol, "BBANDS", history, 7, supplement);
            decimal aroonCompositeScore = GetIndicatorComposite(symbol, "AROON", history, 7);

            ShortInterestResult shortResult = FINRA.GetShortInterest(symbol, history, 7);

            FundamentalsResult fundResult = TwelveData.GetFundamentals(symbol, quote);

            TipRanksResult trResult = TipRanks.GetTipRanksResult(symbol);

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                DataProviders = "YahooFinance, FINRA, TipRanks",
                ADXComposite = adxCompositeScore,
                OBVComposite = obvCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                //BBANDSComposite = bbandsCompositeScore,
                RatingsComposite = trResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestCompositeScore,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                CompositeScoreValue = (adxCompositeScore + aroonCompositeScore + obvCompositeScore + macdCompositeScore +
                    shortResult.ShortInterestCompositeScore + fundResult.FundamentalsComposite + trResult.RatingsComposite) / 7,
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

        public static decimal GetADXComposite(IEnumerable<AdxResult> resultSet, int daysToCalculate)
        {
            List<AdxResult> results = resultSet.ToList();
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> adxValueYList = new Stack<decimal>();
            decimal adxTotal = 0;

            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (daysCalulated < daysToCalculate)
                {
                    AdxResult result = results[i];
                    decimal adxVal = result.Adx != null ? (decimal)result.Adx : 0.0M;
                    adxValueYList.Push(adxVal);
                    adxTotal += adxVal;
                    numberOfResults++;

                    string adxDate = result.Date.ToString("yyyy-MM-dd");
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
            decimal adxSlope = TwelveData.GetSlope(adxXList, adxYList);
            decimal adxSlopeMultiplier = TwelveData.GetSlopeMultiplier(adxSlope);
            decimal adxAvg = adxTotal / numberOfResults;

            List<decimal> adxZScores = TwelveData.GetZScores(adxYList);
            decimal zScoreSlope = TwelveData.GetSlope(adxXList, adxZScores);
            decimal zScoreSlopeMultiplier = TwelveData.GetSlopeMultiplier(zScoreSlope);

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

        public static decimal GetOBVComposite(IEnumerable<ObvResult> resultSet, int daysToCalculate)
        {
            List<ObvResult> results = resultSet.ToList();
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> obvValueYList = new Stack<decimal>();
            decimal obvSum = 0;

            foreach (var result in resultSet)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal obvValue = Convert.ToDecimal(result.Obv);

                    obvValueYList.Push(obvValue);
                    obvSum += obvValue;
                    numberOfResults++;

                    string obvDate = result.Date.ToString("yyyy-MM-dd");
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
            decimal obvSlope = TwelveData.GetSlope(obvXList, obvYList);
            decimal obvSlopeMultiplier = TwelveData.GetSlopeMultiplier(obvSlope);

            List<decimal> zScores = TwelveData.GetZScores(obvYList);
            decimal zScoreSlope = TwelveData.GetSlope(zScores, obvXList);
            decimal zScoreSlopeMultiplier = TwelveData.GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedScores = TwelveData.GetNormalizedData(obvYList);
            decimal normalizedSlope = TwelveData.GetSlope(normalizedScores, obvXList);
            decimal normalizedSlopeMultiplier = TwelveData.GetSlopeMultiplier(normalizedSlope);

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

        public static decimal GetMACDComposite(IEnumerable<MacdResult> resultSet, int daysToCalculate)
        {
            List<MacdResult> results = resultSet.ToList();
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> macdHistYList = new Stack<decimal>();
            Stack<decimal> macdBaseYList = new Stack<decimal>();
            Stack<decimal> macdSignalYList = new Stack<decimal>();
            decimal macdTotalHist = 0;

            for (int i = results.Count - 1; i >= 0; i--)
            {
                MacdResult result = results[i];
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

            return Math.Max(0, composite);
        }

        public static decimal GetBBANDSComposite(IEnumerable<BollingerBandsResult> resultSet, List<Skender.Stock.Indicators.Quote> supplement, int daysToCalculate)
        {
            List<BollingerBandsResult> results = resultSet.ToList();
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> lowerBandYList = new Stack<decimal>();
            Stack<decimal> middleBandYList = new Stack<decimal>();
            Stack<decimal> upperBandYList = new Stack<decimal>();
            Stack<decimal> differenceValueYList = new Stack<decimal>();
            Stack<bool> diverging = new Stack<bool>();

            for (int i = results.Count - 1; i >= 0; i--)
            {
                BollingerBandsResult result = results[i];
                if (daysCalulated < daysToCalculate)
                {
                    decimal lowerBandValue = result.LowerBand != null ? (decimal) result.LowerBand : 0.0M;
                    decimal middleBandValue = result.Sma != null ? (decimal) result.Sma : 0.0M;
                    decimal upperBandValue = result.UpperBand != null ? (decimal) result.UpperBand : 0.0M;
                    decimal difference = upperBandValue - lowerBandValue;
                    bool isDiverging = result.IsDiverging != null ? (bool) result.IsDiverging : false;

                    lowerBandYList.Push(lowerBandValue);
                    middleBandYList.Push(middleBandValue);
                    upperBandYList.Push(upperBandValue);
                    differenceValueYList.Push(difference);
                    diverging.Push(isDiverging);
                    numberOfResults++;

                    string bbandsDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(bbandsDate))
                    {
                        dates.Add(bbandsDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> bbandsXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                bbandsXList.Add(i);

            List<decimal> lowerYList = lowerBandYList.ToList();
            decimal lowerSlope = TwelveData.GetSlope(bbandsXList, lowerYList);
            List<decimal> middleYList = middleBandYList.ToList();
            decimal middleSlope = TwelveData.GetSlope(bbandsXList, middleYList);
            List<decimal> upperYList = upperBandYList.ToList();
            decimal upperSlope = TwelveData.GetSlope(bbandsXList, upperYList);
            List<decimal> differenceYList = differenceValueYList.ToList();
            decimal differenceSlope = TwelveData.GetSlope(bbandsXList, differenceYList);
            List<bool> divergingList = diverging.ToList();

            decimal lowerSlopeMultiplier = TwelveData.GetSlopeMultiplier(lowerSlope);
            decimal middleSlopeMultiplier = TwelveData.GetSlopeMultiplier(middleSlope);
            decimal upperSlopeMultiplier = TwelveData.GetSlopeMultiplier(upperSlope);

            //look for buy and sell signals
            bool bbandsHasBuySignal = false;
            bool bbandsHasSellSignal = false;

            //if there is more divergence than convergence in the last N days and there's positive price movement
            //if the current price is approaching the lower band and there's positive prive volume action
            //measure arbitrary base value minus the percentage difference between the current price and the upper band

            //if there's more convergence than divergence we have low volatility, we don't want to subtract from the score
            //1 negative day when the price is approaching the upper band would probably generate a good enough sell signal
            //measure arbitrary base value minus the percentage difference between the current price and the lower band

            List<Skender.Stock.Indicators.Quote> ochlvList = supplement.ToList();
            decimal breakoutSlopeCutoff = 0.3M;
            List<decimal> prices = new List<decimal>();
            for (int i = 0; i < ochlvList.Count; i++)
            {
                var ochlv = ochlvList[i];
                prices.Add(ochlv.Close);
                if ((i - 1 >= 0 && prices[i - 1] < prices[i]) && differenceSlope > breakoutSlopeCutoff)
                {
                    //positive bbands breakout
                    bbandsHasBuySignal = true;
                    bbandsHasSellSignal = false;
                }
                else if ((i - 1 >= 0 && prices[i - 1] > prices[i]) && differenceSlope > breakoutSlopeCutoff)
                {
                    //negative bbands breakout
                    bbandsHasBuySignal = false;
                    bbandsHasSellSignal = true;
                }
            }
            decimal priceSlope = TwelveData.GetSlope(bbandsXList, prices);

            //To calculate the percentage increase:
            //First: work out the difference between the two numbers you are comparing.
            //Diff = New Number - Original Number
            //Then: divide the difference by the original number and multiply the answer by 100.
            //% increase = Increase ÷ Original Number × 100.
            decimal baseValue = 40 - Math.Abs((prices[prices.Count - 1] - lowerYList[lowerYList.Count - 1]) / lowerYList[lowerYList.Count - 1] * 100);
            baseValue = baseValue < 0 ? 0 : baseValue;
            baseValue = baseValue > 40 ? 40 : baseValue;
            decimal priceSlopeBonus = priceSlope > 0.1M ? 10 : 0;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += priceSlopeBonus;
            composite += (lowerSlope > -0.05M) ? (lowerSlope * lowerSlopeMultiplier) + 10 : 0;
            composite += (middleSlope > 0.0M) ? (middleSlope * middleSlopeMultiplier) + 10 : -10;
            composite += (upperSlope > -0.05M) ? (upperSlope * upperSlopeMultiplier) + 10 : 0;
            composite += (bbandsHasBuySignal) ? 30 : 0;
            composite += (bbandsHasSellSignal) ? -30 : 0;
            composite = composite > 100 ? 100 : composite;
            return Math.Max(0, composite);
        }

        public static decimal GetAROONComposite(IEnumerable<AroonResult> resultSet, int daysToCalculate)
        {
            List<AroonResult> results = resultSet.ToList();
            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();

            Stack<decimal> aroonUpYList = new Stack<decimal>();
            Stack<decimal> aroonDownYList = new Stack<decimal>();
            Stack<decimal> aroonOscillatorYList = new Stack<decimal>();
            decimal aroonUpTotal = 0;
            decimal aroonDownTotal = 0;

            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (daysCalulated < daysToCalculate)
                {
                    AroonResult result = results[i];
                    decimal aroonUpVal = result.AroonUp != null ? (decimal) result.AroonUp : 0.0M;
                    decimal aroonDownVal = result.AroonDown != null ? (decimal)result.AroonDown : 0.0M;
                    aroonUpYList.Push(aroonUpVal);
                    aroonDownYList.Push(aroonDownVal);
                    aroonOscillatorYList.Push(aroonUpVal - aroonDownVal);
                    aroonUpTotal += aroonUpVal;
                    aroonDownTotal += aroonDownVal;
                    numberOfResults++;

                    string aroonDate = result.Date.ToString("yyyy-MM-dd");
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
            decimal upSlope = TwelveData.GetSlope(aroonXList, upYList);
            List<decimal> downYList = aroonDownYList.ToList();
            decimal downSlope = TwelveData.GetSlope(aroonXList, downYList);
            List<decimal> oscillatorYList = aroonOscillatorYList.ToList();

            decimal upSlopeMultiplier = TwelveData.GetSlopeMultiplier(upSlope);
            decimal downSlopeMultiplier = TwelveData.GetSlopeMultiplier(downSlope);

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

            return Math.Max(0, composite);
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
