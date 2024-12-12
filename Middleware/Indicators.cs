using PT.Models.RequestModels;
using PT.Services;
using Skender.Stock.Indicators;
using System.Diagnostics;
using YahooQuotesApi;

namespace PT.Middleware
{
    //https://github.com/DaveSkender/Stock.Indicators
    //https://www.codeproject.com/Articles/15047/Creating-a-Mechanical-Trading-System-Part-1-Techni
    public static class Indicators
    {
        //TODO: add version numbers 1.0 in comments to each Indicator Composite Function
        public static CompositeScoreResult GetCompositeScoreResult(string symbol, Snapshot quote, RequestManager rm)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Alpaca API price history
            AlpacaHistory alpacaHistory = Alpaca.GetHistoryAsync(rm, symbol, Constants.DEFAULT_HISTORY_DAYS).Result;
            IEnumerable<Skender.Stock.Indicators.Quote> history = alpacaHistory.PriceHistory;
            IEnumerable<Skender.Stock.Indicators.Quote> obvHistory = alpacaHistory.PriceHistory.TakeLast(42);

            // This was only used for the bbands composite
            List<Skender.Stock.Indicators.Quote> supplement = alpacaHistory.PriceHistory.TakeLast(7).ToList();

            // get fundamentals with Alpaca price history
            FundamentalsResult fundResult = GetFundamentalsResult(symbol, quote, alpacaHistory);

            decimal adxCompositeScore = GetIndicatorComposite(symbol, "ADX", history, 7);
            decimal obvCompositeScore = GetIndicatorComposite(symbol, "OBV", obvHistory, 7);
            decimal macdCompositeScore = GetIndicatorComposite(symbol, "MACD", history, 7);
            decimal bbandsCompositeScore = GetIndicatorComposite(symbol, "BBANDS", history, 7, supplement);
            decimal aroonCompositeScore = GetIndicatorComposite(symbol, "AROON", history, 7);

            ShortInterestResult shortResult = FINRA.GetShortInterest(symbol, history, 7, rm);
            HedgeFundsResult hfResult = TipRanks.GetTipRanksResult(symbol, rm);

            decimal compositeScoreFinal = GetCompositeScoreFinalValue(fundResult, hfResult, shortResult,
                adxCompositeScore, obvCompositeScore, macdCompositeScore, bbandsCompositeScore, aroonCompositeScore);

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                Name = quote.LongName,
                Exchange = quote.FullExchangeName,
                DataProviders = "YahooFinance, Alpaca, FINRA, TipRanks",
                PriceL = quote.RegularMarketPrice,
                PriceVW = alpacaHistory.PriceAvgYList[alpacaHistory.PriceAvgYList.Count - 3],
                PriceHistoryDays = history.Count(),
                ADXComposite = adxCompositeScore,
                OBVComposite = obvCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                BBANDSComposite = bbandsCompositeScore,
                RatingsComposite = hfResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestCompositeScore,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                CompositeScoreValue = compositeScoreFinal,
                ScoreTimeMS = sw.ElapsedMilliseconds,
                ScoreDate = DateTime.Now,
                ShortInterest = shortResult,
                Fundamentals = fundResult,
                HedgeFunds = hfResult
            };

            // This is where blacklisting happens, right now only from bad volume
            string rank = string.Empty;
            if (scoreResult.Fundamentals.IsBlacklisted)
                rank = "DISQUALIFIED";
            else if (scoreResult.CompositeScoreValue > 0 && scoreResult.CompositeScoreValue < 60)
                rank = "BAD";
            else if (scoreResult.CompositeScoreValue >= 60 && scoreResult.CompositeScoreValue < 70)
                rank = "FAIR";
            else if (scoreResult.CompositeScoreValue >= 70 && scoreResult.CompositeScoreValue < 83)
                rank = "GOOD";
            else if (scoreResult.CompositeScoreValue >= 83)
                rank = "PRIME";
            scoreResult.CompositeRank = rank;

            return scoreResult;
        }

        //TODO: add hotswap type result, use constants/enums, and return a CompositeScoreWithHotSwaps object
        private static decimal GetCompositeScoreFinalValue(FundamentalsResult fr, HedgeFundsResult hr, ShortInterestResult sr,
            decimal adxComposite, decimal obvComposite, decimal macdComposite, decimal bbandsComposite, decimal aroonComposite)
        {
            decimal compositeScoreFinal = 0;
            if (hr.RatingsComposite == Constants.INVALID_COMPOSITE)
            {
                //HS4 - HEDGES NOT FOUND
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestCompositeScore + fr.FundamentalsComposite + bbandsComposite) / 7;
            }
            else if (fr.FundamentalsComposite == Constants.INVALID_COMPOSITE)
            {
                //HS3 - FUNDAMENTALS NOT FOUND
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestCompositeScore + bbandsComposite + hr.RatingsComposite) / 7;
            }
            else if (bbandsComposite > obvComposite)
            {
                //HS2 - BBANDS SWAP
                compositeScoreFinal = (adxComposite + aroonComposite + bbandsComposite + macdComposite +
                    sr.ShortInterestCompositeScore + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            }
            else
            {
                //HS1 - PURE FORM
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestCompositeScore + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            }
            return compositeScoreFinal;
        }

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
                        //between 30 and 70. For example, if the bullish indicator crosses above the bearish indicator, it confirms a bullish trend.
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
                        int obvSmaPeriod = 20;
                        IEnumerable<ObvResult> obvResults = Indicator.GetObv(history, obvSmaPeriod);
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
                        int bbandsPeriod = 25;
                        double standardDeviations = 2.5;
                        IEnumerable<BollingerBandsResult> bbandsResults = Indicator.GetBollingerBands(history, bbandsPeriod, standardDeviations);
                        compositeScore = GetBBANDSComposite(bbandsResults, (List<Skender.Stock.Indicators.Quote>)supplement, daysToCalculate);
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
                Debug.WriteLine("EXCEPTION CAUGHT: Indicators.cs GetCompositeScore for symbol " + symbol + ", function " + function + ", message: " + e.Message);
            }
            return compositeScore;
        }

        // Fundamentals (advanced stats, volume, price, earnings and filings up-to-date)
        // RELIES completely on unofficial yahoo finance API for now
        public static FundamentalsResult GetFundamentalsResult(string symbol, Snapshot quote, AlpacaHistory history)
        {
            try
            {
                List<decimal> normalizedPrice = GetNormalizedData(history.PriceAvgYList);
                decimal priceSlope = GetSlope(history.PriceAvgXList, history.PriceAvgYList);
                decimal normalizedPriceSlope = GetSlope(history.PriceAvgXList, normalizedPrice);
                decimal normalizedPriceSlopeMultiplier = GetSlopeMultiplier(normalizedPriceSlope);

                List<decimal> normalizedVolume = GetNormalizedData(history.VolAvgYList);
                decimal volumeSlope = GetSlope(history.VolAvgXList, history.VolAvgYList);
                decimal normalizedVolumeSlope = GetSlope(history.VolAvgXList, normalizedVolume);
                decimal normalizedVolumeSlopeMultiplier = GetSlopeMultiplier(normalizedVolumeSlope);

                // Do stuff with PE and EPS data
                decimal peTrailing = 0.0M;
                try { peTrailing = decimal.Parse(quote.TrailingPE.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal peForward = 0.0M;
                try { peForward = decimal.Parse(quote.ForwardPE.ToString()); }
                catch (Exception e) { /*set to trailing*/ peForward = peTrailing; }

                decimal epsTrailing = 0.0M;
                try { epsTrailing = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal epsCurrentYear = 0.0M;
                try { epsCurrentYear = decimal.Parse(quote.EpsCurrentYear.ToString()); }
                catch (Exception e) { /*set to trailing*/ epsCurrentYear = epsTrailing; }

                decimal epsForward = 0.0M;
                try { epsForward = decimal.Parse(quote.EpsForward.ToString()); }
                catch (Exception e) { /*set to trailing*/ epsForward = epsTrailing; }

                decimal priceToBook = 1.0M;
                try { priceToBook = decimal.Parse(quote.PriceToBook.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal netExpenseRatio = 1.0M;
                try { netExpenseRatio = decimal.Parse(quote.NetExpenseRatio.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal netAssets = -1.0M;
                try { netAssets = decimal.Parse(quote.NetAssets.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal sharesOutstanding = -1.0M;
                try { sharesOutstanding = decimal.Parse(quote.SharesOutstanding.ToString()); }
                catch (Exception e) { /*do nothing*/ }

                decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
                decimal bonus = Convert.ToDecimal(Math.PI);

                // Get base value as a function of price-to-book percentage
                decimal baseValue = 100 - (priceToBook * 100);
                if (baseValue >= 10)
                {
                    baseValue += (bonus * 2); // more than 10% undervalued bonus
                }
                else if (baseValue <= 0)
                {
                    baseValue = 2 * bonus; // 2-pi pity points
                }
                baseValue = Math.Min(baseValue, 30);

                // Net expense ratio bonus
                decimal netExpenseRatioBonus = 0;
                if ( 0 <= netExpenseRatio && netExpenseRatio < 0.25M)
                {
                    netExpenseRatioBonus = 6 * bonus;
                }
                else if (0.25M <= netExpenseRatio && netExpenseRatio < 0.6M)
                {
                    netExpenseRatioBonus = 3 * bonus;
                }

                // Fair value price bonus
                decimal fairValuePriceBonus = 0;
                decimal fairValuePrice = 0;
                if (netAssets > 0 && sharesOutstanding > 0)
                {
                    fairValuePrice = netAssets / sharesOutstanding;
                    decimal lastPrice = history.PriceAvgYList[history.PriceAvgYList.Count - 1];
                    if (lastPrice < fairValuePrice)
                    {
                        fairValuePriceBonus = 3 * bonus;
                    }
                }

                // Calculate figures for EPS bonus and PE bonus
                decimal averageEPS = 0.0M, growthEPS = 0.0M, averagePE = 0.0M, growthPE = 0.0M;
                averageEPS = (epsForward + epsTrailing + epsCurrentYear) / 3;
                growthEPS = epsForward - epsTrailing;

                averagePE = (peForward + peTrailing) / 2;
                growthPE = peForward - peTrailing;

                // Add EPS activity bonus
                decimal epsBonus = GetEPSBonus(averageEPS, growthEPS, bonus);

                // Add PE ratio activity bonus
                decimal peBonus = GetPEBonus(averagePE, growthPE, bonus);

                // Add dividend bonus
                decimal divBonus = GetDividendBonus(quote);

                // Add positive fractional bonus if current volume is greater than average volume, negative otherwise
                decimal diff = history.VolumeUSD - history.AverageVolumeUSD;
                decimal percentChange = (diff / Math.Abs(history.AverageVolumeUSD)) * 100;
                decimal volumeTrendingModifier = 0;
                // Reward cases
                if (0 < percentChange && percentChange <= 100)
                {
                    volumeTrendingModifier += (percentChange / 20) + bonus;
                }
                else if (100 < percentChange)
                {
                    volumeTrendingModifier += bonus * 4;
                }
                // Penalty cases
                else if (0 > percentChange && percentChange >= -100)
                {
                    volumeTrendingModifier += (percentChange / 20) + penalty;
                }
                else if (-100 > percentChange)
                {
                    volumeTrendingModifier += penalty * 4;
                }

                // Get normalized price slope and volume slope bonuses
                decimal normalizedPriceSlopeBonus = (normalizedPriceSlope > 0.05M) ?
                    normalizedPriceSlope * normalizedPriceSlopeMultiplier : 0;
                normalizedPriceSlopeBonus = Math.Min(normalizedPriceSlopeBonus, 10);

                decimal normalizedVolumelopeBonus = (normalizedVolumeSlope > 0.05M) ?
                    normalizedVolumeSlope * normalizedVolumeSlopeMultiplier : 0;
                normalizedVolumelopeBonus = Math.Min(normalizedVolumelopeBonus, 10);

                // calculate composite score based on the following values and weighted multipliers
                // Base value should be calculated based on EPS and PE data
                // Bonuses added for positive volume and price slopes, PE Growth, and dividends
                decimal composite = 0;
                composite += baseValue;
                composite += normalizedVolumelopeBonus;
                composite += normalizedPriceSlopeBonus;
                composite += fairValuePriceBonus;
                composite += netExpenseRatioBonus;
                composite += epsBonus;
                composite = Math.Min(70, composite);
                composite += peBonus;
                composite += divBonus;
                composite += composite >= 60 && volumeTrendingModifier < 0 ? volumeTrendingModifier : 0;
                composite += volumeTrendingModifier > 0 ? volumeTrendingModifier : 0;

                composite = Math.Min(composite, 100); // cap composite at 100, no extra weight
                composite = Math.Max(composite, 0); // limit composite at 0, no negatives

                // disqualify if less than USD volume multiplicative from constants
                var disqualifyingLimit = Constants.DEFAULT_VOLUME_USD_DISQUALIFYING_LIMIT;
                bool volumeDisqualified = (history.VolumeUSD < disqualifyingLimit || history.AverageVolumeUSD < disqualifyingLimit);
                bool hasDivs = quote.DividendRate > 0 && quote.DividendYield > 0;

                return new FundamentalsResult
                {
                    FundamentalsComposite = composite,
                    VolumeUSD = history.VolumeUSD,
                    AverageVolumeUSD = history.AverageVolumeUSD,
                    VolumeSlope = volumeSlope,
                    PriceSlope = priceSlope,
                    AverageEPS = averageEPS,
                    AveragePE = averagePE,
                    GrowthEPS = growthEPS,
                    GrowthPE = growthPE,
                    HasDividends = hasDivs,
                    IsBlacklisted = volumeDisqualified,
                    Message = string.Empty
                };
            }
            catch (Exception e)
            {
                string msg = $"Indicators.cs GetFundamentals for symbol {symbol}, message: {e.Message}";
                Debug.WriteLine($"EXCEPTION CAUGHT: {msg}");
                return new FundamentalsResult
                {
                    FundamentalsComposite = Constants.INVALID_COMPOSITE,
                    VolumeUSD = 0.0M,
                    AverageVolumeUSD = 0.0M,
                    VolumeSlope = 0.0M,
                    PriceSlope = 0.0M,
                    AverageEPS = 0.0M,
                    AveragePE = 0.0M,
                    GrowthEPS = 0.0M,
                    GrowthPE = 0.0M,
                    HasDividends = false,
                    IsBlacklisted = false,
                    Message = msg
                };
            }
        }

        public static decimal GetADXComposite(IEnumerable<AdxResult> resultSet, int daysToCalculate)
        {
            List<AdxResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            int daysCalculated = 0;

            Queue<decimal> adxValueYList = new Queue<decimal>();
            Queue<decimal> pDmiValueYList = new Queue<decimal>();
            Queue<decimal> nDmiValueYList = new Queue<decimal>();

            decimal adxTotal = 0;
            decimal pDmiTotal = 0;
            decimal nDmiTotal = 0;
            bool hasBuySignal = false;
            bool hasSellSignal = false;
            int daysSinceSignal = -1;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    AdxResult result = results[i];

                    string adxDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(adxDate))
                    {
                        decimal adxVal = result.Adx != null ? (decimal)result.Adx : 0.0M;
                        adxValueYList.Enqueue(adxVal);
                        decimal plusDmiVal = result.Pdi != null ? (decimal)result.Pdi : 0.0M;
                        pDmiValueYList.Enqueue(plusDmiVal);
                        decimal negDmiVal = result.Mdi != null ? (decimal)result.Mdi : 0.0M;
                        nDmiValueYList.Enqueue(negDmiVal);
                        adxTotal += adxVal;
                        pDmiTotal += plusDmiVal;
                        nDmiTotal += negDmiVal;

                        //Get buy and sell signals
                        if (adxVal > 25 && plusDmiVal > negDmiVal)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            hasBuySignal = true;
                            hasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (adxVal > 25 && plusDmiVal < negDmiVal)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            hasSellSignal = true;
                            hasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        daysCalculated++;
                        dates.Add(adxDate);
                    }
                }
                else
                    break;
            }

            List<decimal> adxXList = new List<decimal>();
            for (int i = 1; i <= daysToCalculate; i++)
                adxXList.Add(i);

            List<decimal> adxYList = adxValueYList.ToList();
            List<decimal> pDmiYList = pDmiValueYList.ToList();
            List<decimal> nDmiYList = nDmiValueYList.ToList();

            decimal pDmiSlope = GetSlope(adxXList, pDmiYList);
            decimal pDmiSlopeMultiplier = GetSlopeMultiplier(pDmiSlope);
            decimal pDmiAvg = pDmiTotal / daysToCalculate;

            decimal nDmiSlope = GetSlope(adxXList, nDmiYList);
            decimal nDmiSlopeMultiplier = GetSlopeMultiplier(nDmiSlope);
            decimal nDmiAvg = nDmiTotal / daysToCalculate;

            decimal adxSlope = GetSlope(adxXList, adxYList);
            decimal adxSlopeMultiplier = GetSlopeMultiplier(adxSlope);
            decimal adxAvg = adxTotal / daysToCalculate;

            List<decimal> adxZScores = GetZScores(adxYList);
            decimal zScoreSlope = GetSlope(adxXList, adxZScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
            decimal bonus = Convert.ToDecimal(Math.PI);

            bool averageDmiTrendingPositive = pDmiAvg > nDmiAvg;
            bool recentDmiTrendingPositive = pDmiYList[pDmiYList.Count - 1] > nDmiYList[nDmiYList.Count - 1];

            //base value average of the 2 most recent +DMI values, and adx average if trending
            //also add the most recent Z Score as an average percentage if it is positive
            decimal baseValue = (pDmiYList[pDmiYList.Count - 1] + pDmiYList[pDmiYList.Count - 2]) / 2;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add bonus for recent trending and avg trending
            decimal recentTrendingBonus = recentDmiTrendingPositive ? bonus * 2 : 0;
            decimal averageTrendingBonus = averageDmiTrendingPositive ? bonus * 2 : 0;

            //Add time-scaled bonus and penalty for buy and sell signals
            decimal buySignalBonus = GetTimeScaledBuySignalBonus(hasBuySignal, bonus, daysSinceSignal);
            decimal sellSignalPenalty = GetTimeScaledSellSignalPenalty(hasSellSignal, bonus, daysSinceSignal);

            //Add bonus for ADX average above 25 per investopedia recommendation
            decimal averageBuySignalBonus = adxAvg > 25 && hasBuySignal ? bonus * 2 : 0;

            //Only add zscore slope bonus if +DMI > -DMI
            decimal zScoreSlopeBonus = (zScoreSlope > 0.1m) && averageDmiTrendingPositive ?
                (zScoreSlope * zScoreSlopeMultiplier) + bonus : 0;

            decimal pDmiSlopeBonus = (pDmiSlope > 0.1m) ? bonus * 2 : penalty * 2;
            decimal nDmiSlopeBonus = (nDmiSlope < -0.1m) ? bonus * 2 : penalty * 2;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += recentTrendingBonus;
            composite += averageTrendingBonus;
            composite += zScoreSlopeBonus;
            composite += pDmiSlopeBonus;
            composite += nDmiSlopeBonus;
            composite = Math.Min(composite, 75);
            composite += averageBuySignalBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit ADX composite to 0, no negatives
            return Math.Min(composite, 100); //cap ADX composite at 100, no extra weight
        }

        public static decimal GetOBVComposite(IEnumerable<ObvResult> resultSet, int daysToCalculate)
        {
            List<ObvResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> obvValueYList = new Queue<decimal>();

            int daysCalculated = 0;
            decimal obvSum = 0;
            bool obvHasBuySignal = false;
            bool obvHasSellSignal = false;
            int daysSinceSignal = -1;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    ObvResult result = results[i];
                    ObvResult prevResult = results[i - 1];

                    string obvDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(obvDate))
                    {
                        decimal obvValue = Convert.ToDecimal(result.Obv);
                        decimal prevObvValue = Convert.ToDecimal(prevResult.Obv);

                        obvValueYList.Enqueue(obvValue);
                        obvSum += obvValue;

                        //Get buy and sell signals
                        bool obvCurrentIsNegative = obvValue < 0;
                        bool obvPrevIsNegative = prevObvValue < 0;
                        if (!obvCurrentIsNegative && obvPrevIsNegative)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            obvHasBuySignal = true;
                            obvHasSellSignal = false; 
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (obvCurrentIsNegative && !obvPrevIsNegative)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            obvHasSellSignal = true;
                            obvHasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }

                        dates.Add(obvDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> obvXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
                obvXList.Add(i);

            List<decimal> obvYList = obvValueYList.ToList();
            decimal obvSlope = GetSlope(obvXList, obvYList);

            List<decimal> zScores = GetZScores(obvYList);
            decimal zScoreSlope = GetSlope(obvXList, zScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedScores = GetNormalizedData(obvYList);
            decimal normalizedSlope = GetSlope(obvXList, normalizedScores);
            decimal normalizedSlopeMultiplier = GetSlopeMultiplier(normalizedSlope);

            decimal obvAverage = obvSum / daysCalculated;

            decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
            decimal bonus = Convert.ToDecimal(Math.PI);

            decimal baseValue = 0;

            //Start with the average of the 2 most recent OBV Normalized Scores
            //Only allow positive normalizedScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal normalizedScoreBase =
                ((normalizedScores[normalizedScores.Count - 1] + normalizedScores[normalizedScores.Count - 2]) / 4) * 100;
            normalizedScoreBase = normalizedScoreBase < 0 ? bonus * 5 : normalizedScoreBase;

            //ZScore base helps us get the base value for composite from derivatives
            //Only allow positive zScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal zScoreBase = ((zScores[zScores.Count - 1] + zScores[zScores.Count - 2]) / 4) * 100;
            zScoreBase = zScoreBase < 0 ? bonus * 5 : zScoreBase;

            baseValue = (normalizedScoreBase + zScoreBase) / 2.0M;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add bonus if average OBV is greater than 0
            decimal obvAverageBonus = obvAverage > 0 ? bonus * 2 : 0;

            //Add bonus if OBV slope positive
            decimal obvSlopeBonus = obvSlope > 0 ? bonus * 3 : 0;

            //Add Zscore slope bonus
            decimal zScoreSlopeBonus = 0;
            if (zScoreSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                zScoreSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0.05m)
                zScoreSlopeBonus += bonus;

            //Add Normalized slope bonus
            decimal normalizedSlopeBonus = 0;
            if (normalizedSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                normalizedSlopeBonus += (normalizedSlope * normalizedSlopeMultiplier);
            if (normalizedSlope > 0.05m)
                normalizedSlopeBonus += bonus;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = GetTimeScaledBuySignalBonus(obvHasBuySignal, bonus, daysSinceSignal);
            decimal sellSignalPenalty = GetTimeScaledSellSignalPenalty(obvHasSellSignal, bonus, daysSinceSignal);

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += obvAverageBonus;
            composite += obvSlopeBonus;
            composite += zScoreSlopeBonus;
            composite += normalizedSlopeBonus;
            composite = Math.Min(composite, 75);
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit OBV composite at 0, no negatives
            return Math.Min(composite, 100); //cap OBV composite at 100, no extra weight
        }

        public static decimal GetMACDComposite(IEnumerable<MacdResult> resultSet, int daysToCalculate)
        {
            List<MacdResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> macdHistYList = new Queue<decimal>();
            Queue<decimal> macdBaseYList = new Queue<decimal>();
            Queue<decimal> macdSignalYList = new Queue<decimal>();

            int daysCalculated = 0;
            decimal macdTotalHist = 0;
            decimal macdTotalBase = 0;
            decimal macdTotalSignal = 0;
            int positiveHistDays = 0;
            int daysSinceSignal = -1;
            bool macdHasBuySignal = false;
            bool macdHasSellSignal = false;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    MacdResult result = results[i];
                    MacdResult prevResult = results[i - 1];

                    string macdDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(macdDate))
                    {
                        decimal macdBaseValue = result.Macd != null ? (decimal)result.Macd : 0.0M;
                        decimal macdSignalValue = result.Signal != null ? (decimal)result.Signal : 0.0M;
                        decimal macdHistogramValue = result.Histogram != null ? (decimal)result.Histogram : 0.0M;

                        decimal prevMacdBaseValue = prevResult.Macd != null ? (decimal)prevResult.Macd : 0.0M;
                        decimal prevMacdSignalValue = prevResult.Signal != null ? (decimal)prevResult.Signal : 0.0M;
                        decimal prevMacdHistogramValue = prevResult.Histogram != null ? (decimal)prevResult.Histogram : 0.0M;

                        macdHistYList.Enqueue(macdHistogramValue);
                        macdBaseYList.Enqueue(macdBaseValue);
                        macdSignalYList.Enqueue(macdSignalValue);
                        macdTotalHist += macdHistogramValue;
                        macdTotalBase += macdBaseValue;
                        macdTotalSignal += macdSignalValue;

                        //Look for buy and sell signals
                        bool macdCurrentIsNegative = macdBaseValue < macdSignalValue;
                        bool macdPrevIsNegative = prevMacdBaseValue < prevMacdSignalValue;
                        if (!macdCurrentIsNegative && macdPrevIsNegative)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            macdHasBuySignal = true;
                            macdHasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (macdCurrentIsNegative && !macdPrevIsNegative)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            macdHasSellSignal = true;
                            macdHasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }

                        //Positive hist days
                        if (!macdCurrentIsNegative)
                        {
                            positiveHistDays++;
                        }

                        dates.Add(macdDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> macdXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
                macdXList.Add(i);

            List<decimal> baseYList = macdBaseYList.ToList();
            decimal baseSlope = GetSlope(macdXList, baseYList);
            List<decimal> signalYList = macdSignalYList.ToList();
            decimal signalSlope = GetSlope(macdXList, signalYList);
            List<decimal> histYList = macdHistYList.ToList();
            decimal histSlope = GetSlope(macdXList, histYList);

            List<decimal> zScores = GetZScores(histYList);
            decimal zScoreSlope = GetSlope(macdXList, zScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedHist = GetNormalizedData(histYList);
            decimal normalizedHistSlope = GetSlope(macdXList, normalizedHist);
            decimal normalizedSlopeMultiplier = GetSlopeMultiplier(normalizedHistSlope);

            decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
            decimal bonus = Convert.ToDecimal(Math.PI);

            //Use total base and signal diffs, along with macd total hist to get macd base value
            decimal baseValue = 0;
            decimal macdBaseSignalDiff = macdTotalBase - macdTotalSignal;
            if (macdBaseSignalDiff > 0)
            {
                baseValue = (macdBaseSignalDiff * bonus) + 5;
            }
            if (macdTotalHist > 0)
            {
                baseValue += (macdTotalHist * bonus) + 5;
            }
            if (baseValue == 0)
            {
                baseValue += (3 * bonus); //3-pi pity points
            }
            baseValue = Math.Min(30, baseValue);
            baseValue += positiveHistDays;

            decimal histSlopeBonus = (histSlope > 0) ? histSlope + (bonus * 3) : 0;
            decimal baseSlopeBonus = (baseSlope > 0) ? baseSlope + (bonus * 2) : 0;
            decimal signalSlopeBonus = (signalSlope > 0) ? signalSlope + bonus : 0;

            //Add histogram zscore slope bonus
            decimal zScoreHistSlopeBonus = 0;
            if (zScoreSlope > 0.1m)
                zScoreHistSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0)
                zScoreHistSlopeBonus += (2 * bonus);

            //Add normalized histogram slope bonus
            decimal normalizedHistSlopeBonus = 0;
            if (normalizedHistSlope >= 0.5m)
                normalizedHistSlopeBonus += normalizedHistSlope;
            if (normalizedHistSlope > 0)
                normalizedHistSlopeBonus += (2 * bonus);

            //Get previous 2 base above signal bonus
            bool prevTwoBaseAboveSignal = baseYList[baseYList.Count - 1] > signalYList[signalYList.Count - 1]
                && baseYList[baseYList.Count - 2] > signalYList[signalYList.Count - 2];
            decimal baseAboveSignalBonus = prevTwoBaseAboveSignal ? bonus * 3 : 0;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = GetTimeScaledBuySignalBonus(macdHasBuySignal, bonus, daysSinceSignal);
            decimal sellSignalPenalty = GetTimeScaledSellSignalPenalty(macdHasSellSignal, bonus, daysSinceSignal);

            //Calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += histSlopeBonus;
            composite += baseSlopeBonus;
            composite += signalSlopeBonus;
            composite += zScoreHistSlopeBonus;
            composite += normalizedHistSlopeBonus;
            composite = Math.Min(80, composite);
            composite += baseAboveSignalBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit MACD composite at 0, no negatives
            return Math.Min(composite, 115); //cap MACD composite at 115, extra weight
        }

        public static decimal GetAROONComposite(IEnumerable<AroonResult> resultSet, int daysToCalculate)
        {
            List<AroonResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> aroonUpYList = new Queue<decimal>();
            Queue<decimal> aroonDownYList = new Queue<decimal>();
            Queue<decimal> aroonOscillatorYList = new Queue<decimal>();

            int daysCalculated = 0;
            int aroonPositiveDays = 0;
            decimal aroonUpTotal = 0;
            decimal aroonDownTotal = 0;
            int daysSinceSignal = -1;
            bool aroonHasBuySignal = false;
            bool aroonHasSellSignal = false;
            bool aroonHasRecentPositivityMinor = false;
            bool aroonHasRecentPositivityMajor = false;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    AroonResult result = results[i];
                    AroonResult prevResult = results[i - 1];
                    AroonResult prevPrevResult = results[i - 2];

                    string aroonDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(aroonDate))
                    {
                        decimal curAroonUpVal = result.AroonUp != null ? (decimal)result.AroonUp : 0.0M;
                        decimal curAroonDownVal = result.AroonDown != null ? (decimal)result.AroonDown : 0.0M;
                        decimal curAroonOsc = curAroonUpVal - curAroonDownVal;

                        aroonUpYList.Enqueue(curAroonUpVal);
                        aroonDownYList.Enqueue(curAroonDownVal);
                        aroonOscillatorYList.Enqueue(curAroonOsc);
                        aroonUpTotal += curAroonUpVal;
                        aroonDownTotal += curAroonDownVal;

                        decimal prevAroonUpVal = prevResult.AroonUp != null ? (decimal)prevResult.AroonUp : 0.0M;
                        decimal prevAroonDownVal = prevResult.AroonDown != null ? (decimal)prevResult.AroonDown : 0.0M;
                        decimal prevAroonOsc = prevAroonUpVal - prevAroonDownVal;

                        decimal prevPrevAroonUpVal = prevPrevResult.AroonUp != null ? (decimal)prevPrevResult.AroonUp : 0.0M;
                        decimal prevPrevAroonDownVal = prevPrevResult.AroonDown != null ? (decimal)prevPrevResult.AroonDown : 0.0M;
                        decimal prevPrevAroonOsc = prevPrevAroonUpVal - prevPrevAroonDownVal;

                        //Look for buy and sell signals
                        bool aroonCurrentIsNegative = curAroonOsc < 0;
                        bool aroonPrevIsNegative = prevAroonOsc < 0;
                        bool aroonPrevPrevIsNegative = prevPrevAroonOsc < 0;
                        bool aroonWithinBounds = AroonUpDownWithinBounds(curAroonUpVal, prevAroonUpVal,
                            curAroonDownVal, prevAroonDownVal);

                        if (!aroonCurrentIsNegative && aroonPrevIsNegative && aroonWithinBounds)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            aroonHasBuySignal = true;
                            aroonHasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (aroonCurrentIsNegative && !aroonPrevIsNegative && aroonWithinBounds)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            aroonHasSellSignal = true;
                            aroonHasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }

                        //Look for recent positivity minor
                        if (!aroonCurrentIsNegative && !aroonPrevIsNegative)
                        {
                            aroonHasRecentPositivityMinor = true;
                        }
                        else if (aroonCurrentIsNegative && aroonPrevIsNegative)
                        {
                            aroonHasRecentPositivityMinor = false;
                        }

                        //Look for recent positivity major
                        if (!aroonCurrentIsNegative && !aroonPrevIsNegative && !aroonPrevPrevIsNegative)
                        {
                            aroonHasRecentPositivityMajor = true;
                        }
                        else if (aroonCurrentIsNegative && aroonPrevIsNegative && aroonPrevPrevIsNegative)
                        {
                            aroonHasRecentPositivityMajor = false;
                        }

                        //Count positive days
                        if (!aroonCurrentIsNegative)
                        {
                            aroonPositiveDays++;
                        }

                        dates.Add(aroonDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> aroonXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
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

            decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
            decimal bonus = Convert.ToDecimal(Math.PI);

            //Use percent diffs to get aroon base value
            decimal aroonAvgUp = Math.Max(aroonUpTotal / daysCalculated, 1.0M);
            decimal aroonAvgDown = Math.Max(aroonDownTotal / daysCalculated, 1.0M);
            decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
            decimal percentDiffUp = (aroonAvgUp / aroonAvgDown) * 100;

            //Whether aroonAvgUp or aroonAvgDown is higher, that one will be more than 100 percent of the other
            decimal baseBullResult = Math.Min(100 - percentDiffDown, 45); //base bull result caps at 45
            decimal baseBearResult = Math.Min(percentDiffUp, 30); //base bear result caps at 30
            decimal baseValue = (aroonAvgUp > aroonAvgDown) ? baseBullResult : baseBearResult;
            baseValue += aroonPositiveDays + bonus;

            //Get recent positivity modifiers
            decimal recentPositivityMinorModifier = aroonHasRecentPositivityMinor ? bonus : 0;
            decimal recentPositivityMajorModifier = aroonHasRecentPositivityMajor ? 2 * bonus : 0;

            //Get aroon average modifier
            decimal aroonAvgModifier = aroonAvgUp > aroonAvgDown ? bonus * 2 : penalty * 2;

            //Get slope modifiers
            decimal oscilatorSlopeModifier = (oscillatorSlope > 1.0M) ? oscillatorSlope + (2 * bonus) : penalty * 3;
            oscilatorSlopeModifier = oscilatorSlopeModifier > 20 ? Math.Max(20, oscilatorSlopeModifier) : oscilatorSlopeModifier;

            decimal downSlopeModifier = (downSlope < 0) ? (-1 * downSlope) + (2 * bonus) : (-1 * downSlope) + (penalty * 2);
            downSlopeModifier = downSlopeModifier > 20 ? Math.Max(20, downSlopeModifier) : downSlopeModifier;
            downSlopeModifier = downSlopeModifier < -20 ? Math.Max(-20, downSlopeModifier) : downSlopeModifier;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = GetTimeScaledBuySignalBonus(aroonHasBuySignal, bonus, daysSinceSignal);
            decimal sellSignalPenalty = GetTimeScaledSellSignalPenalty(aroonHasSellSignal, bonus, daysSinceSignal);

            //Get other bonuses
            decimal lastOscValue = oscillatorYList[oscillatorYList.Count - 1];

            //Add bull major bonus if last AROON UP >= 70 per investopedia recommendation
            //This is the same as when last AROON OSC >= 50
            decimal bullMajorBonus = (lastOscValue >= 50) ? bonus * 2: 0;

            //calculate composite score based on the following values and weighted multipliers
            //if AROON avg up > AROON avg down, start score with 100 - (down as % of up)
            //if AROON avg up < AROON avg down, start score with 100 - (up as % of down)
            decimal composite = 0;
            composite += baseValue;
            composite += recentPositivityMinorModifier;
            composite += recentPositivityMajorModifier;
            composite += aroonAvgModifier;
            composite += oscilatorSlopeModifier;
            composite += downSlopeModifier;
            composite = Math.Min(composite, 80);
            composite += bullMajorBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit AROON composite at 0, no negatives
            return Math.Min(composite, 115); //cap AROON composite at 115, extra weight
        }

        private static bool AroonUpDownWithinBounds(decimal curAroonUpVal, decimal prevAroonUpVal, decimal curAroonDownVal, decimal prevAroonDownVal)
        {
            decimal aroonUpMidpoint = (curAroonUpVal + prevAroonUpVal) / 2.0M;
            decimal aroonDownMidpoint = (curAroonDownVal + prevAroonDownVal) / 2.0M;
            decimal aroonCrossingPoint = (aroonUpMidpoint + aroonDownMidpoint) / 2.0M;

            return (aroonCrossingPoint >= 25 && aroonCrossingPoint <= 75);
        }

        public static decimal GetBBANDSComposite(IEnumerable<BollingerBandsResult> resultSet, List<Skender.Stock.Indicators.Quote> supplement, int daysToCalculate)
        {
            List<BollingerBandsResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> lowerBandYList = new Queue<decimal>();
            Queue<decimal> middleBandYList = new Queue<decimal>();
            Queue<decimal> upperBandYList = new Queue<decimal>();
            Queue<decimal> differenceValueYList = new Queue<decimal>();

            int daysCalulated = 0;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalulated < daysToCalculate)
                {
                    BollingerBandsResult result = results[i];

                    string bbandsDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(bbandsDate))
                    {
                        decimal lowerBandValue = result.LowerBand != null ? (decimal)result.LowerBand : 0.0M;
                        decimal middleBandValue = result.Sma != null ? (decimal)result.Sma : 0.0M;
                        decimal upperBandValue = result.UpperBand != null ? (decimal)result.UpperBand : 0.0M;
                        decimal difference = upperBandValue - lowerBandValue;

                        lowerBandYList.Enqueue(lowerBandValue);
                        middleBandYList.Enqueue(middleBandValue);
                        upperBandYList.Enqueue(upperBandValue);
                        differenceValueYList.Enqueue(difference);

                        dates.Add(bbandsDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> bbandsXList = new List<decimal>();
            for (int i = 1; i <= daysCalulated; i++)
                bbandsXList.Add(i);

            List<decimal> lowerYList = lowerBandYList.ToList();
            decimal lowerSlope = GetSlope(bbandsXList, lowerYList);
            List<decimal> middleYList = middleBandYList.ToList();
            decimal middleSlope = GetSlope(bbandsXList, middleYList);
            List<decimal> upperYList = upperBandYList.ToList();
            decimal upperSlope = GetSlope(bbandsXList, upperYList);
            List<decimal> differenceYList = differenceValueYList.ToList();
            decimal differenceSlope = GetSlope(bbandsXList, differenceYList);

            decimal lowerSlopeMultiplier = GetSlopeMultiplier(lowerSlope);
            decimal middleSlopeMultiplier = GetSlopeMultiplier(middleSlope);
            decimal upperSlopeMultiplier = GetSlopeMultiplier(upperSlope);

            //if there is more divergence than convergence in the last N days and there's positive price movement
            //if the current price is approaching the lower band and there's positive prive volume action
            //measure arbitrary base value as the percentage difference between the current price and the upper band

            //if there's more convergence than divergence we have low volatility, we don't want to subtract from the score
            //1 negative day when the price is approaching the upper band would probably generate a good enough sell signal
            //measure arbitrary base value minus the percentage difference between the current price and the lower band
            decimal breakoutSlopeCutoff = 0.15M;
            bool hasBreakout = differenceSlope >= breakoutSlopeCutoff;

            // look for buy and sell signals
            bool bbandsHasMedBuySignal = false;
            bool bbandsHasMaxBuySignal = false;
            bool bbandsHasMedSellSignal = false;
            bool bbandsHasMaxSellSignal = false;

            List<Skender.Stock.Indicators.Quote> ochlvList = supplement.ToList();
            List<decimal> prices = new List<decimal>();
            bool crossUpperBand = false;
            bool crossLowerBand = false;
            bool crossMiddleBand = false;
            for (int i = 0; i < ochlvList.Count; i++)
            {
                var ochlv = ochlvList[i];
                decimal curPrice = ochlv.Close;
                prices.Add(curPrice);
                bool hasPrevPrice = i - 1 >= 0;

                if (hasPrevPrice && curPrice <= lowerYList[i] && prices[i - 1] > lowerYList[i - 1])
                {
                    crossLowerBand = true;
                }

                if (hasPrevPrice && curPrice >= middleYList[i] && prices[i - 1] < middleYList[i - 1])
                {
                    crossMiddleBand = true;
                }

                if (hasPrevPrice && curPrice >= upperYList[i] && prices[i - 1] < upperYList[i - 1])
                {
                    crossUpperBand = true;
                }
            }
            decimal priceSlope = GetSlope(bbandsXList, prices);

            bool recentPositivity = prices[prices.Count - 1] > prices[prices.Count - 2]
                && prices[prices.Count - 2] > prices[prices.Count - 3];

            // Apply minor and major ranking to bbands sell sinals
            // Different from the other time-scaled buy and sell signals
            decimal bbandsBonus = 0;

            // Cross lower band and have positive breakout, buy signal, max weight
            if (crossLowerBand && recentPositivity && hasBreakout)
            {
                bbandsBonus += (decimal)Math.PI * 7;
                bbandsHasMaxBuySignal = true;
            }
            // Cross middle band and have positive breakout, buy signal, medium weight
            else if (crossMiddleBand && recentPositivity && hasBreakout)
            {
                bbandsBonus += (decimal)Math.PI * 4;
                bbandsHasMedBuySignal = true;
            }

            // Cross upper band and have positive breakout, sell signal, medium weight
            if (crossUpperBand && recentPositivity && hasBreakout)
            {
                bbandsBonus -= (decimal)Math.PI * 4;
                bbandsHasMedSellSignal = true;
            }
            // Cross upper band and have negative breakout, sell signal, max weight
            else if (crossUpperBand && !recentPositivity && hasBreakout)
            {
                bbandsBonus -= (decimal)Math.PI * 7;
                bbandsHasMaxSellSignal = true;
            }
            decimal penalty = -1.0m * Convert.ToDecimal(Math.PI);
            decimal bonus = Convert.ToDecimal(Math.PI);

            // Base value from percentage diff from the lower band if below middle band (rebound conditions)
            // Base value from percentage diff from the upper band if above middle band (bullish conditions)
            decimal baseValue = 0;
            if (prices[prices.Count - 1] > middleYList[middleYList.Count - 1])
            {
                decimal percentageDiffBullish = (upperYList[upperYList.Count - 1] - prices[prices.Count - 1]) / prices[prices.Count - 1] * 100;
                baseValue = percentageDiffBullish > 0 ? (100 - percentageDiffBullish) / 2 : 0;
            }
            else
            {
                decimal percentageDiffRebound = (prices[prices.Count - 1] - lowerYList[lowerYList.Count - 1]) / lowerYList[lowerYList.Count - 1] * 100;
                baseValue = percentageDiffRebound > 0 ? (100 - percentageDiffRebound) / 2 + bonus : bonus * 5; // Reward for price being below 2.5 std devs
            }
            // Cap base value at 42 with small bonus for max
            baseValue = Math.Min(baseValue, 42);
            baseValue += baseValue == 42 ? bonus : 0;

            // Bonus for bullish consolidation of the bands
            decimal consolidationBonus = lowerSlope > 0 && upperSlope < 0 && priceSlope > 0.05M ? bonus * 3 : 0;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += consolidationBonus;
            composite += (lowerSlope > 0) ? (lowerSlope * lowerSlopeMultiplier) + (bonus * 3) : 0;
            composite += (middleSlope > 0) ? (middleSlope * middleSlopeMultiplier) + (bonus * 4) : (penalty * 2);
            composite += (upperSlope > 0 && recentPositivity) ? (bonus * 3) : 0;
            composite = Math.Min(composite, 80);
            composite += composite > 50 && bbandsBonus < 0 ? bbandsBonus : 0;
            composite += bbandsBonus > 0 ? bbandsBonus : 0;

            composite = Math.Min(composite, 100); // cap BBANDS composite at 100, no extra weight
            return Math.Max(0, composite); // limit BBANDS composite at 0, no negatives
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
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
            if (slope > 0 && slope < 0.25M)
                return 40.0M;
            else if (slope >= 0.25M && slope < 0.5M)
                return 30.0M;
            else if (slope >= 0.5M && slope < 1)
                return 20.0M;
            else if (slope >= 1 && slope < 5)
                return 2.0M;
            else if (slope >= 5 && slope < 10)
                return 1.5M;
            else if (slope >= 10 && slope < 20)
                return 1.0M;
            else if (slope >= 20)
                return 1.0M;

            //Negative cases
            else if (slope < 0 && slope > -0.25M)
                return -40.0M;
            else if (slope <= -0.25M && slope > -0.5M)
                return -30.0M;
            else if (slope <= -0.5M && slope > -1)
                return -20.0M;
            else if (slope <= -1 && slope > -5)
                return -2.0M;
            else if (slope <= -5 && slope > -10)
                return -1.5M;
            else if (slope <= -10 && slope > -20)
                return -1.0M;
            else if (slope <= -20)
                return -1.0M;
            else
                return 0;
        }

        // Transform input data into normalized (or scaled) data
        public static List<decimal> GetNormalizedData(List<decimal> input)
        {
            // Estimate min and max from the input values using standard deviation
            decimal mean = input.Sum() / input.Count;

            decimal stdDev = GetStandardDeviation(input, true);

            decimal setMax = input.Max();
            decimal setMin = input.Min();
            decimal range = setMax - setMin;
            decimal stdDevScalar = GetStdDevScalar(range, stdDev);

            decimal estMax = mean + (stdDev * stdDevScalar);
            decimal estMin = mean - (stdDev * stdDevScalar);

            List<decimal> normalized = new List<decimal>();

            //below is using a difference quotient to get results for normalization
            for (int i = 0; i < input.Count; i++)
            {
                decimal curScore = (input[i] - estMin) / (estMax - estMin);
                normalized.Add(curScore);
            }

            return normalized;
        }

        public static List<decimal> GetZScores(List<decimal> input)
        {
            // Find standard deviation and compute Z Scores
            decimal mean = input.Sum() / input.Count;

            decimal stdDev = GetStandardDeviation(input, true);

            decimal estMin = mean - (stdDev);
            decimal estMax = mean + (stdDev);

            decimal setMax = input.Max();
            decimal setMin = input.Min();
            decimal range = setMax - setMin;
            decimal zScoreScalar = GetStdDevScalar(range, stdDev);

            List<decimal> zScores = new List<decimal>();

            // Normally z-score tells you how many stdDev away from the mean this value is
            // In this case, we're finding how many (stdDev * zScoreScalar) away from the mean this value is
            for (int i = 0; i < input.Count; i++)
            {
                //OR compare the range to the stdDev to decide on our Z Score multiplier
                decimal curZ = (input[i] - mean) / (stdDev * zScoreScalar);
                zScores.Add(curZ);
            }

            return zScores;
        }

        // May not be needed anymore now that I can use Standard Deviation
        public static decimal GetEstimatedBound(List<decimal> set, decimal setMin, decimal setMax, bool isMax)
        {
            decimal sum = 0;
            for (int i = 0; i < set.Count; i++)
                sum += set[i];

            decimal mean = sum / set.Count;

            decimal stdDev = GetStandardDeviation(set, true);

            //decimal range = setMax - setMin;
            //decimal expandedRange = range * 1.5M; //like an estimated std deviation
            //decimal expander = expandedRange / 2.0M;
            return (isMax) ? mean + stdDev : mean - stdDev;
        }

        public static decimal GetStdDevScalar(decimal range, decimal stdDev)
        {
            //how many stdDevs do you need to cover the entire range?
            return range / stdDev;
        }

        // Return the standard deviation of an array of decimals
        // If the second argument is True, evaluate as a sample
        // If the second argument is False, evaluate as a population
        public static decimal GetStandardDeviation(List<decimal> values, bool isSample)
        {
            // Get the mean
            decimal sum = 0;
            for (int i = 0; i < values.Count; i++)
                sum += values[i];
            decimal mean = sum / values.Count;

            // Get the sum of the squares of the differences between each value and the mean
            decimal sumOfSquares = 0;
            for (int i = 0; i < values.Count; i++)
                sumOfSquares += (values[i] - mean) * (values[i] - mean);

            if (isSample)
                return DecimalSqrt(sumOfSquares / (values.Count() - 1));
            else
                return DecimalSqrt(sumOfSquares / values.Count());
        }

        // https://stackoverflow.com/questions/4124189/performing-math-operations-on-decimal-datatype-in-c
        // x - a number, from which we need to calculate the square root
        // epsilon - an accuracy of calculation of the root from our number.
        // The result of the calculations will differ from an actual value
        // of the root on less than epslion.
        public static decimal DecimalSqrt(decimal x, decimal epsilon = 0.0M)
        {
            if (x < 0) throw new Exception("EXCEPTION: Cannot calculate square root from a negative number");
            decimal current = (decimal)Math.Sqrt((double)x), previous;
            do
            {
                previous = current;
                if (previous == 0.0M) return 0;
                current = (previous + x / previous) / 2;
            }
            while (Math.Abs(previous - current) > epsilon);
            return current;
        }

        public static decimal GetEPSBonus(decimal averageEPS, decimal growthEPS, decimal bonus)
        {
            decimal epsBonus = 0;

            //If everything is negative return base
            if (averageEPS <= 0 && growthEPS <= 0)
            {
                return epsBonus;
            }

            // averageEPS score formulation
            // Reward cases
            if (0 < averageEPS && averageEPS <= 0.5M)
            {
                epsBonus += averageEPS * 10 + bonus;
            }
            else if (0.5M < averageEPS && averageEPS <= 1)
            {
                epsBonus += averageEPS * 5 + (2 * bonus);
            }
            else if (1 < averageEPS && averageEPS <= 2)
            {
                epsBonus += averageEPS * 2 + (3 * bonus);
            }
            else if (2 < averageEPS && averageEPS <= 3)
            {
                epsBonus += averageEPS * 2 + (4 * bonus);
            }
            else if (3 < averageEPS)
            {
                epsBonus += averageEPS + (5 * bonus);
            }

            // growthEPS score formulation
            // Reward cases
            if (0 < growthEPS && growthEPS <= 1)
            {
                epsBonus += growthEPS * 5 + bonus;
            }
            else if (1 < growthEPS && growthEPS <= 2)
            {
                epsBonus += growthEPS * 3 + (2 * bonus);
            }
            else if (2 < growthEPS)
            {
                epsBonus += growthEPS + (5 * bonus);
            }
            /* Penalty cases
            else if (-1 <= growthEPS && growthEPS <0)
            {
                epsBonus += growthEPS * 3 - 3;
            }
            else if (-1 >= growthEPS)
            {
                epsBonus += growthEPS * 3 - 6;
            }*/
            return epsBonus;
        }

        public static decimal GetPEBonus(decimal averagePE, decimal growthPE, decimal bonus)
        {
            decimal peBonus = 0;

            //If everything is negative return 0
            if (averagePE <= 0 && growthPE <= 0)
            {
                return peBonus;
            }

            // averagePE score fomulation
            // Reward cases
            if (0 < averagePE && averagePE <=25)
            {
                peBonus += (averagePE / 5) + (3 * bonus);
            }
            else if (25 < averagePE && averagePE <= 50)
            {
                peBonus += (averagePE / 10) + bonus;
            }
            else if (50 < averagePE && averagePE <= 100)
            {
                peBonus += (averagePE / 15);
            }
            // Penalty cases
            else if (averagePE > 100)
            {
                peBonus += (-1 * (averagePE / 15));
            }

            // growthPE score fomulation
            // Reward cases
            if (-50 <= growthPE && growthPE < 0)
            {
                peBonus += (-1 * (growthPE / 10));
            }
            else if (-100 <= growthPE && growthPE < -50)
            {
                peBonus += (-1 * (growthPE / 10)) + bonus;
            }
            else if (-100 > growthPE)
            {
                peBonus += (-1 * (growthPE / 100)) + (bonus * 3);
            }
            else if (0 < growthPE && growthPE <= 50)
            {
                peBonus += (-1 * (growthPE / 10));
            }
            // Penalty cases
            /*else if (50 < growthPE && growthPE <= 100)
            {
                peBonus += (-1 * (growthPE / 10)) - 3;
            }
            else if (100 < growthPE)
            {
                peBonus += (-1 * (growthPE / 100)) - 10;
            }
            return Math.Max(-10, peBonus);*/
            return peBonus;
        }

        private static decimal GetDividendBonus(Snapshot quote)
        {
            decimal divBonus = 0;
            bool hasDivs = quote.DividendRate > 0;
            bool hasYield = quote.DividendYield > 0;
            if (hasDivs)
            {
                decimal divRate = (decimal)quote.DividendRate;
                //if div rate between 0 and 0.5, add divRate * 2 + 1 bonus
                if (0 < divRate && divRate <= 0.5M)
                {
                    divBonus += divRate * 2 + 1;
                }
                //if div rate above 0.5, add divRate * 3 + 2 bonus
                else if (divRate > 0.5M)
                {
                    divBonus += divRate * 3 + 2;
                }
            }
            if (hasYield)
            {
                decimal divYield = (decimal)quote.DividendYield;
                //if div yield is between 0 and 1, add 2 * divYield bonus
                if (0 < divYield && divYield <= 1)
                {
                    divBonus += divYield * 2;
                }
                //if div yield is between 1 and 3, add divYield + 2 bonus
                else if (1 < divYield && divYield <= 3)
                {
                    divBonus += divYield + 2;
                }
                //if div yield is above 3, add divYield + 5 bonus
                if (divYield >= 3)
                {
                    divBonus += divYield + 5;
                }
            }
            return divBonus;
        }

        private static decimal GetTimeScaledBuySignalBonus(bool hasBuySignal, decimal bonus, int daysSinceSignal)
        {
            decimal timeScaledBonus = 0;
            if (hasBuySignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledBonus = bonus * 8 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledBonus = bonus * 7 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledBonus = bonus * 5 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledBonus = bonus * 4 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledBonus = bonus * 3 + 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledBonus = bonus * 2 + 1;
                }
            }
            return timeScaledBonus;
        }

        private static decimal GetTimeScaledSellSignalPenalty(bool hasSellSignal, decimal penalty, int daysSinceSignal)
        {
            decimal timeScaledPenalty = 0;
            if (hasSellSignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledPenalty = penalty * 7 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledPenalty = penalty * 6 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledPenalty = penalty * 5 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledPenalty = penalty * 4 + Constants.SIGNAL_CONSTANT;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledPenalty = penalty * 3 + 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledPenalty = penalty * 2 + 1;
                }
            }
            return (-1 * timeScaledPenalty);
        }
    }
}
