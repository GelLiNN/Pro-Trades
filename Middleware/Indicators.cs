using PT.Models.CoreModels;
using PT.Models.RequestModels;
using PT.Services;
using static PT.Core.Maths;
using Skender.Stock.Indicators;
using System.Diagnostics;
using YahooQuotesApi;

namespace PT.Middleware
{
    //https://github.com/DaveSkender/Stock.Indicators
    //https://dotnet.stockindicators.dev/examples/#content
    //https://www.codeproject.com/Articles/15047/Creating-a-Mechanical-Trading-System-Part-1-Techni
    public static class Indicators
    {
        //TODO: move to Core/Predictor.cs, Core/Maths.cs, and Core/GRU.cs
        //TODO: add version numbers 1.0 in comments to each Indicator Composite Function
        public static CompositeScoreResult GetCompositeScoreResult(string symbol, RequestManager rm)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Alpaca API price history
            PTHistory ptHistory = HistoryHelper.GetHistoryAsync(rm, symbol, Constants.DEFAULT_HISTORY_DAYS).GetAwaiter().GetResult();

            // Always fail the prediction if we fail to get history data
            if (ptHistory.SkenderHistory.Count() == 0) return new CompositeScoreResult();

            //AlpacaHistory alpacaHistory = Alpaca.GetHistoryAsyncOld(rm, symbol, Constants.DEFAULT_HISTORY_DAYS).GetAwaiter().GetResult();
            List<Skender.Stock.Indicators.Quote> history = ptHistory.SkenderHistory.ToList();

            // Only used for the obv composite
            List<Skender.Stock.Indicators.Quote> obvHistory = ptHistory.SkenderHistory.TakeLast(Constants.OBV_LOOKBACK_DAYS).ToList();

            // Only used for the bbands composite
            List<Skender.Stock.Indicators.Quote> supplement = ptHistory.SkenderHistory.TakeLast(Constants.DEFAULT_LOOKBACK_DAYS).ToList();

            long alpacaStopMs = sw.ElapsedMilliseconds;
            long alpacaMs = alpacaStopMs;

            // YahooQuotesApi get quote
            Snapshot? quote = YahooFinance.GetQuoteAsync(symbol).GetAwaiter().GetResult();

            long yahooStopMs = sw.ElapsedMilliseconds;
            long yahooMs = yahooStopMs - alpacaStopMs;

            // get fundamentals with Alpaca price history and YahooQuotesApi quote
            FundamentalsResult fundResult = GetFundamentalsResult(symbol, quote, ptHistory);

            decimal adxCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_ADX, history, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal obvCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_OBV, obvHistory, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal macdCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_MACD, history, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal bbandsCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_BBANDS, history, Constants.DEFAULT_LOOKBACK_DAYS, supplement);
            decimal aroonCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_AROON, history, Constants.DEFAULT_LOOKBACK_DAYS);

            long indicatorStopMs = sw.ElapsedMilliseconds;
            long coreMs1 = indicatorStopMs - yahooStopMs;

            ShortInterestResult shortResult = FINRA.GetShortInterest(symbol, supplement, 7, rm);

            long finraStopMs = sw.ElapsedMilliseconds;
            long finraMs = finraStopMs - indicatorStopMs;

            HedgeFundsResult hfResult = TipRanks.GetTipRanksResult(symbol, rm);

            long tipRanksStopMs = sw.ElapsedMilliseconds;
            long tipRanksMs = tipRanksStopMs - finraStopMs;

            var finalResult = CalcParametrizedComposites(fundResult, hfResult, shortResult, adxCompositeScore,
                obvCompositeScore, macdCompositeScore, bbandsCompositeScore, aroonCompositeScore);

            var paramType = GetParameterType(finalResult.hs);

            // Price targets for buy, sell, and short
            decimal buyTarget = HistoryHelper.GetPriceBuyTarget(ptHistory);
            HistoryHelper.ComputePriceSellTargets(fundResult, ptHistory);
            List<PTPriceTarget> priceTargets = ptHistory.PriceTargets.OrderByDescending(x => x.TargetPrice).ToList();

            string compositeScoreNotes = GetCompositeScoreNotes(fundResult, hfResult, shortResult,
                adxCompositeScore, obvCompositeScore, macdCompositeScore, bbandsCompositeScore, aroonCompositeScore,
                ptHistory.PriceTargetAvgLong, ptHistory.PriceTargetAvgShort);

            long coreStopMs = sw.ElapsedMilliseconds;
            long coreMs2 = coreStopMs - tipRanksStopMs;

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                Name = quote?.LongName,
                Exchange = quote?.FullExchangeName,
                CompositeScoreValue = finalResult.cs,
                CompositeScoreNotes = compositeScoreNotes,
                PriceOpen = ptHistory.TodayOpen,
                PriceLast = quote?.RegularMarketPrice ?? ptHistory.TodayClose,
                PriceVwap = ptHistory.PriceHistory[0].PriceVwap,
                PriceBuyTarget = buyTarget,
                PriceSellTarget = ptHistory.PriceTargetProLong,
                PriceSellTargetShort = ptHistory.PriceTargetProShort,
                PriceTargetHedgeFunds = hfResult.PriceTarget,
                PriceHistoryDays = history.Count(),
                ADXComposite = adxCompositeScore,
                OBVComposite = obvCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                BBANDSComposite = bbandsCompositeScore,
                RatingsComposite = hfResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestComposite,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                TotalTimeMS = coreStopMs,
                AlpacaTimeMS = alpacaMs,
                YahooTimeMS = yahooMs,
                FinraTimeMS = finraMs,
                TipRanksTimeMS = tipRanksMs,
                CoreTimeMS = coreMs1 + coreMs2,
                ScoreDate = DateTime.Now,
                ParameterSet = paramType,
                PriceTargets = priceTargets,
                ShortInterest = shortResult,
                Fundamentals = fundResult,
                HedgeFunds = hfResult,
                DataProviders = "YahooFinance, Alpaca, FINRA, TipRanks"
            };
            scoreResult.PriceRedGreen = scoreResult.PriceLast >= scoreResult.PriceOpen ?
                Constants.DEFAULT_GREEN : Constants.DEFAULT_RED;
            scoreResult.CompositeScoreRank = GetCompositeScoreRank(scoreResult);

            sw.Reset();
            return scoreResult;
        }

        // Get Composite Rank for the prediction depending on boundary conditions
        private static string GetCompositeScoreRank(CompositeScoreResult scoreResult)
        {
            DateTime today = DateTime.Today;
            DateTime nextFriday = Enumerable.Range(1, 7)
                .Select(days => today.AddDays(days))
                .First(date => date.DayOfWeek == DayOfWeek.Friday);

            bool earningsDuringAttrition = scoreResult.Fundamentals.NextEarningsDate > today &&
                scoreResult.Fundamentals.NextEarningsDate < nextFriday;

            // This is where blacklisting happens, right now only from bad dollar volume throughput
            string rank = string.Empty;
            if (IsDisqualifiedPrediction(scoreResult))
                rank = Constants.RANK_DISQUALIFIED;
            else if (IsShortPrediction(scoreResult))
                rank = Constants.RANK_SHORT;
            else if (scoreResult.CompositeScoreValue < 50)
                rank = Constants.RANK_BAD;
            else if (scoreResult.CompositeScoreValue >= 50 && scoreResult.CompositeScoreValue < 60)
                rank = Constants.RANK_NEUTRAL;
            else if (scoreResult.CompositeScoreValue >= 60 && scoreResult.CompositeScoreValue < 70)
                rank = Constants.RANK_FAIR;
            else if (scoreResult.CompositeScoreValue >= 70 && scoreResult.CompositeScoreValue < Constants.CORE_PRIME_GATE)
                rank = Constants.RANK_GOOD;
            else if (scoreResult.CompositeScoreValue >= Constants.CORE_PRIME_GATE)
                rank = Constants.RANK_PRIME;

            rank += earningsDuringAttrition ? Constants.RANK_E : string.Empty;
            return rank;
        }

        private static bool IsDisqualifiedPrediction(CompositeScoreResult scoreResult)
        {
            return
                (scoreResult.Fundamentals.IsBlacklisted ||
                (scoreResult.PriceLast < Constants.DEFAULT_PENNY_PRICE_D_LIMIT || scoreResult.PriceVwap < Constants.DEFAULT_PENNY_PRICE_D_LIMIT));
        }

        private static bool IsShortPrediction(CompositeScoreResult scoreResult)
        {
            return
                (scoreResult.CompositeScoreValue < 40 &&
                (scoreResult.ShortInterestComposite <= 50 && scoreResult.FundamentalsComposite <= 60) &&
                !(scoreResult.RatingsComposite == Constants.CORE_INVALID_COMP && scoreResult.FundamentalsComposite == Constants.CORE_INVALID_COMP));
        }

        // Get final prediction composite score decimal, and prediction parameter set HS type string
        private static (decimal cs, string hs) CalcParametrizedComposites(FundamentalsResult fr,
            HedgeFundsResult hr, ShortInterestResult sr, decimal adxComposite, decimal obvComposite,
            decimal macdComposite, decimal bbandsComposite, decimal aroonComposite)
        {
            decimal compositeScoreFinal = 0;
            if (hr.RatingsComposite == Constants.CORE_INVALID_COMP)
            {
                //HS5 - FINANCIAL INSTRUMENTS
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + bbandsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS5_MOD, Constants.HS5);
            }
            else if (fr.FundamentalsComposite == Constants.CORE_INVALID_COMP)
            {
                //HS4 - FUNDAMENTALS NOT FOUND
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + bbandsComposite + hr.RatingsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS4_MOD, Constants.HS4);
            }
            else if (bbandsComposite > aroonComposite && aroonComposite < obvComposite)
            {
                //HS3 - BBANDS AROON SWAP
                compositeScoreFinal = (adxComposite + bbandsComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS3_MOD, Constants.HS3); 
            }
            else if (bbandsComposite > obvComposite && obvComposite < aroonComposite)
            {
                //HS2 - BBANDS OBV SWAP
                compositeScoreFinal = (adxComposite + aroonComposite + bbandsComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS2_MOD, Constants.HS2);
            }
            else
            {
                //HS1 - PURE FORM
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS1_MOD, Constants.HS1);
            }
        }

        /// <summary>
        /// Get string of notes with buy and sell signals (++ major buy signal, + minor buy signal, - sell signal)
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="hr"></param>
        /// <param name="sr"></param>
        /// <param name="adxComposite"></param>
        /// <param name="obvComposite"></param>
        /// <param name="macdComposite"></param>
        /// <param name="bbandsComposite"></param>
        /// <param name="aroonComposite"></param>
        /// <returns></returns>
        public static string GetCompositeScoreNotes(FundamentalsResult fr,
            HedgeFundsResult hr, ShortInterestResult sr, decimal adxComposite, decimal obvComposite,
            decimal macdComposite, decimal bbandsComposite, decimal aroonComposite, decimal targetL, decimal targetS)
        {
            string notes = "";
            notes += (fr != null && fr.FundamentalsComposite != Constants.CORE_INVALID_COMP && fr.FundamentalsComposite >= 90) ? "fund+, " : "";
            notes += (fr != null && fr.FundamentalsComposite != Constants.CORE_INVALID_COMP && fr.FundamentalsComposite <= 33) ? "fund-, " : "";
            notes += (hr != null && hr.RatingsComposite != Constants.CORE_INVALID_COMP && hr.RatingsComposite >= 90) ? "hedge+, " : "";
            notes += (hr != null && hr.RatingsComposite != Constants.CORE_INVALID_COMP && hr.RatingsComposite <= 33) ? "hedge-, " : "";
            notes += (sr != null && sr.ShortInterestComposite != Constants.CORE_INVALID_COMP && sr.ShortInterestComposite >= 95) ? "long+, " : "";
            notes += (sr != null && sr.ShortInterestComposite != Constants.CORE_INVALID_COMP && sr.ShortInterestComposite <= 33) ? "long-, " : "";

            notes += (macdComposite >= 95) ? "macd++, " : (macdComposite >= Constants.CORE_PRIME_GATE) ? "macd+, " : (macdComposite <= 33) ? "macd-, " : "";
            notes += (adxComposite == 100) ? "adx++, " : (adxComposite >= Constants.CORE_PRIME_GATE) ? "adx+, " : (adxComposite <= 33) ? "adx-, " : "";
            notes += (obvComposite == 100) ? "obv++, " : (obvComposite >= Constants.CORE_PRIME_GATE) ? "obv+, " : (obvComposite <= 33) ? "obv-, " : "";
            notes += (aroonComposite >= 95) ? "aroon++, " : (aroonComposite >= Constants.CORE_PRIME_GATE) ? "aroon+, " : (aroonComposite <= 33) ? "aroon-, " : "";
            notes += (bbandsComposite >= 90) ? "bbands++, " : (bbandsComposite >= Constants.CORE_PRIME_GATE) ? "bbands+, " : (bbandsComposite <= 33) ? "bbands-, " : "";
            notes += $"TL: ${Math.Round(targetL, 2)}, TS: ${Math.Round(targetS, 2)}";
            return notes;
        }

        // Get ParameterType object using HS Type name constant as identifier
        public static ParameterSetType GetParameterType(string typeName)
        {
            if (typeName == Constants.HS1)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS1,
                    ShortDescription = Constants.HS1_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS1_LONG_DESCRIPTION,
                    Set = Constants.HS1_SET
                };
            }
            else if (typeName == Constants.HS2)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS2,
                    ShortDescription = Constants.HS2_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS2_LONG_DESCRIPTION,
                    Set = Constants.HS2_SET
                };
            }
            else if (typeName == Constants.HS3)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS3,
                    ShortDescription = Constants.HS3_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS3_LONG_DESCRIPTION,
                    Set = Constants.HS3_SET
                };
            }
            else if (typeName == Constants.HS4)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS4,
                    ShortDescription = Constants.HS4_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS4_LONG_DESCRIPTION,
                    Set = Constants.HS4_SET
                };
            }
            else if (typeName == Constants.HS5)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS5,
                    ShortDescription = Constants.HS5_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS5_LONG_DESCRIPTION,
                    Set = Constants.HS5_SET
                };
            }
            return null;
        }

        // Main composite function to separate and organize the AI model's composite elements
        public static decimal GetIndicatorComposite(string symbol, string comp, List<Skender.Stock.Indicators.Quote> history, int daysToCalculate, object supplement = null)
        {
            decimal compositeScore = 0;
            try
            {
                //different processing for each indicator
                switch (comp)
                {
                    case Constants.COMPOSITE_ADX:
                        //When the +DMI is above the -DMI, prices are moving up, and ADX measures the strength of the uptrend.
                        //When the -DMI is above the +DMI, prices are moving down, and ADX measures the strength of the downtrend.
                        //Many traders will use ADX readings above 25 to suggest that the trend is strong enough for trend-trading strategies.
                        //Conversely, when ADX is below 25, many will avoid trend-trading strategies.
                        int adxPeriod = 15;
                        IEnumerable<AdxResult> adxResults = Indicator.GetAdx(history, adxPeriod);
                        compositeScore = GetADXComposite(adxResults, daysToCalculate);
                        break;

                    case Constants.COMPOSITE_AROON:
                        //Indicator Movements Around the Key Levels, 30 and 70 - Movements above 70 indicate a strong trend,
                        //while movements below 30 indicate low trend strength. Movements between 30 and 70 indicate indecision.
                        //For example, if the bullish indicator remains above 70 while the bearish indicator remains below 30,
                        //the trend is definitively bullish.
                        //Crossovers Between the Bullish and Bearish Indicators - Crossovers indicate confirmations if they occur
                        //between 30 and 70. For example, if the bullish indicator crosses above the bearish indicator, it confirms a bullish trend.
                        //The two Aroon indicators(bullish and bearish) can also be made into a single oscillator by
                        //making the bullish indicator 100 to 0 and the bearish indicator 0 to - 100 and finding the
                        //difference between the two values. This oscillator then varies between 100 and - 100, with 0 indicating no trend.
                        int aroonPeriod = 16;
                        IEnumerable<AroonResult> aroonResults = Indicator.GetAroon(history, aroonPeriod);
                        compositeScore = GetAROONComposite(aroonResults, daysToCalculate);
                        break;

                    case Constants.COMPOSITE_MACD:
                        //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                        //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                        //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                        int fastPeriod = 12;
                        int slowPeriod = 26;
                        int signalPeriod = 9;
                        IEnumerable<MacdResult> macdResults = Indicator.GetMacd(history, fastPeriod, slowPeriod, signalPeriod);
                        compositeScore = GetMACDComposite(macdResults, daysToCalculate);
                        break;

                    case Constants.COMPOSITE_OBV:
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
                    case Constants.COMPOSITE_BBANDS:
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
                Debug.WriteLine("ERROR Indicators.cs GetCompositeScore for symbol " + symbol + ", comosite " + comp + ", message: " + e.Message);
            }
            return compositeScore;
        }

        // Fundamentals (advanced stats, volume, price, earnings and filings up-to-date)
        // RELIES completely on unofficial yahoo finance API for now
        public static FundamentalsResult GetFundamentalsResult(string symbol, Snapshot? quote, PTHistory history)
        {
            try
            {
                //Get last 10 VWAPs, Volume, and Date data from PTHistory
                List<decimal> normalizedPrice = GetNormalizedData(history.Price10YList);
                decimal priceSlope = GetSlope(history.Price10XList, history.Price10YList);

                decimal normalizedPriceSlope = GetSlope(history.Price10XList, normalizedPrice);
                decimal normalizedPriceSlopeMultiplier = GetSlopeMultiplier(normalizedPriceSlope);

                List<decimal> normalizedVolume = GetNormalizedData(history.Volume10YList);
                decimal volumeSlope = GetSlope(history.Volume10XList, history.Volume10YList);
                decimal normalizedVolumeSlope = GetSlope(history.Volume10XList, normalizedVolume);
                decimal normalizedVolumeSlopeMultiplier = GetSlopeMultiplier(normalizedVolumeSlope);

                //Get avg vwap slope for price target projection
                decimal vwapSlope = GetSlope(history.HistoricalVwapXList, history.HistoricalVwapYList);
                decimal avgVolumeSlope = GetSlope(history.HistoricalVolAvgXList, history.HistoricalVolAvgYList);

                // Collect values from YahooFinance
                decimal peTrailing = 0.0M;
                decimal peForward = 0.0M;
                decimal epsTrailing = 0.0M;
                decimal epsCurrentYear = 0.0M;
                decimal epsForward = 0.0M;
                decimal priceToBook = 1.0M;
                decimal sharesOutstanding = -1.0M;
                decimal divRate = 0.0M;
                decimal divYield = 0.0M;
                decimal netAssets = -1.0M; // TODO: use to boost HS5
                decimal netExpenseRatio = 1.0M;// TODO: use to boost HS5
                DateTime? nextEarningsDate = null;
                DateTime? prevEarningsDate = null;

                try
                {
                    if (quote != null)
                    {
                        peTrailing = Convert.ToDecimal(quote.TrailingPE);
                        peTrailing = peTrailing == 0 ? Convert.ToDecimal(quote.PriceEpsCurrentYear) : peTrailing;
                        peForward = Convert.ToDecimal(quote.ForwardPE);
                        epsTrailing = quote.EpsTrailingTwelveMonths;
                        epsCurrentYear = quote.EpsCurrentYear;
                        epsForward = quote.EpsForward;
                        priceToBook = Convert.ToDecimal(quote.PriceToBook);
                        sharesOutstanding = Convert.ToDecimal(quote.SharesOutstanding);
                        divRate = quote.DividendRate;
                        divYield = Convert.ToDecimal(quote.DividendYield);
                        netAssets = Convert.ToDecimal(quote.NetAssets);
                        netExpenseRatio = Convert.ToDecimal(quote.NetExpenseRatio);
                        nextEarningsDate = quote.EarningsTimestampStart.ToDateTimeUtc();
                        prevEarningsDate = quote.EarningsTimestamp.ToDateTimeUtc();
                    }
                }
                catch (Exception e) { /*do nothing*/ }

                // Get base value starting with 1Mil USD bonus, then function of price-to-book percentage
                decimal baseValue = history.TodayVolUsd >= Constants.MILLION ? Constants.CORE_BONUS : 0;
                decimal bookValuePrice = 0;
                if (priceToBook > 0 && history.TodayVwap > 0)
                {
                    bookValuePrice = history.TodayVwap * (1 / priceToBook);
                    decimal bookValuePriceDiffPercent = GetPercentDiff(history.TodayVwap, bookValuePrice);
                    baseValue = bookValuePriceDiffPercent * 100;
                    if (baseValue >= 10)
                    {
                        baseValue += Constants.CORE_BONUS + 1; //more than 10% undervalued bonus
                    }
                    else if (baseValue <= 0)
                    {
                        //pi pity points with vwap slope bonus
                        baseValue = Constants.CORE_BONUS + (vwapSlope > 0.5M ? Constants.CORE_BONUS : 0);
                    }
                    baseValue = Math.Min(baseValue, 30);
                }
                else
                {
                    //pi pity points
                    baseValue = Constants.CORE_BONUS;
                }

                // Calculate net asset value if unavailable
                if (netAssets < 0 || netAssets == 0)
                {
                    netAssets = bookValuePrice * sharesOutstanding;
                }

                // Fair value price bonus
                decimal fairValuePriceBonus = 0;
                decimal fairValuePrice = 0;
                if (netAssets > 0 && sharesOutstanding > 0)
                {
                    fairValuePrice = netAssets / sharesOutstanding;
                    decimal avgPrice30d = history.HistoricalVwapYList[0];
                    if (avgPrice30d < fairValuePrice)
                    {
                        fairValuePriceBonus = 2 * Constants.CORE_BONUS + 1;
                    }
                    else if (avgPrice30d / fairValuePrice <= 3)
                    {
                        fairValuePriceBonus = Constants.CORE_BONUS + 1;
                    }
                    else if (avgPrice30d / fairValuePrice <= 7)
                    {
                        fairValuePriceBonus = Constants.CORE_BONUS * Constants.HALF;
                    }
                }

                // Calculate figures for EPS bonus and PE bonus
                decimal averageEPS = 0.0M, growthEPS = 0.0M, averagePE = 0.0M, growthPE = 0.0M;
                averageEPS = (epsForward + epsTrailing + epsCurrentYear) / 3;
                growthEPS = epsForward - epsTrailing;

                averagePE = (peForward + peTrailing) / 2;
                growthPE = peForward - peTrailing;

                // Get EPS activity modifier
                decimal epsModifier = CalcEPSModifier(averageEPS, growthEPS);

                // Get PE ratio activity modifier
                decimal peModifier = CalcPEModifier(averagePE, growthPE);

                // Get volume trending modifier
                decimal volumeTrendingModifier = CalcVolumeTrendingModifier(history);

                // Get modifier for interactions with 30d SMA and 100d SMA
                decimal smaModifier = CalcSmaModifier(history);

                // Get dividend bonus
                decimal divBonus = CalcDividendBonus(divRate, divYield);

                // Get net expense ratio bonus to supplement HS5 financial instruments
                decimal netExpenseRatioBonus = CalcNetExpenseRatioBonus(netExpenseRatio);

                // Get golden path bonus if todays dollar is volume geater than the 10d avg
                // dollar volume, and if 10d avg dollar volume is greater than the 30d avg dollar volume
                decimal goldenPathBonus = 0;
                bool hasGoldenPath = history.TodayVolUsd > (history.AverageVolUsd10Day + Constants.THIRTY_THOUSAND)
                    && history.AverageVolUsd10Day > (history.AverageVolUsd30Day + Constants.THIRTY_THOUSAND);
                if (hasGoldenPath)
                {
                    goldenPathBonus += Constants.CORE_BONUS * 2 + 1;
                }

                // Get normalized price slope and volume slope bonus
                decimal normalizedPriceVolBonus = normalizedPriceSlope > 0.025M ? Constants.CORE_BONUS * Constants.HALF : 0;
                normalizedPriceVolBonus += normalizedVolumeSlope > 0.025M ? Constants.CORE_BONUS * Constants.HALF : 0;

                // calculate composite score based on the following values and weighted multipliers
                // Base value should be calculated based on EPS and PE data
                // Bonuses added for positive v:wq
                // olume and price slopes, PE Growth, and dividends
                decimal composite = 0;
                composite += baseValue;
                composite += normalizedPriceVolBonus;
                composite += fairValuePriceBonus;
                composite += netExpenseRatioBonus;
                composite += peModifier;
                composite = Math.Min(60, composite);
                composite += goldenPathBonus;
                composite += epsModifier;
                composite += divBonus;
                composite += composite >= 60 && smaModifier < 0 ? smaModifier : 0;
                composite += smaModifier > 0 ? smaModifier : 0;
                composite += composite >= 60 && volumeTrendingModifier < 0 ? volumeTrendingModifier : 0;
                composite += volumeTrendingModifier > 0 ? volumeTrendingModifier : 0;
                // Give back half the PE penalty if composite is below fair
                composite += composite < 60 && peModifier < 0 ? (-0.5M * peModifier) : 0;

                composite = Math.Min(composite, 100); // cap composite at 100, no extra weight
                composite = Math.Max(composite, 0); // limit composite at 0, no negatives

                // disqualify if less than USD volume multiplicative from constants
                var disqualifyingLimit = Constants.DEFAULT_VOLUME_USD_1D_LIMIT;

                bool volumeDisqualified = !(history.Has1DayQualifiedVolume && history.Has10DayQualifiedVolume && history.Has30DayQualifiedVolume);
                decimal volUsdAvg = (history.TodayVolUsd + history.AverageVolUsd10Day + history.AverageVolUsd30Day) / 3.0M;

                bool hasDivs = divRate > 0 && divYield > 0;

                return new FundamentalsResult
                {
                    FundamentalsComposite = composite,
                    HasBullishSMA = history.HasBullishSMA,
                    HasBearishSMA = history.HasBearishSMA,
                    HasDividends = hasDivs,
                    HasGoldenPath = hasGoldenPath,
                    IsBlacklisted = volumeDisqualified,
                    NextEarningsDate = nextEarningsDate,
                    PrevEarningsDate = prevEarningsDate,
                    Message = string.Empty,
                    AveragePrice100Day = history.AveragePrice100Day,
                    AveragePrice50Day = history.AveragePrice50Day,
                    AveragePrice30Day = history.AveragePrice30Day,
                    AveragePrice20Day = history.AveragePrice20Day,
                    DollarVolumeToday = history.TodayVolUsd,
                    DollarVolume10Day = history.AverageVolUsd10Day,
                    DollarVolume30Day = history.AverageVolUsd30Day,
                    DollarVolumeAverage = volUsdAvg,
                    VolumeSlope = volumeSlope,
                    PriceSlope = priceSlope,
                    VwapSlope = vwapSlope,
                    FairValuePrice = fairValuePrice,
                    BookValuePrice = bookValuePrice,
                    AverageEPS = averageEPS,
                    AveragePE = averagePE,
                    GrowthEPS = growthEPS,
                    GrowthPE = growthPE,
                    DivRate = divRate,
                    DivYield = divYield
                };
            }
            catch (Exception e)
            {
                string msg = $"ERROR: Indicators.cs GetFundamentals for symbol {symbol}, message: {e.Message}";
                Debug.WriteLine(msg);
                return new FundamentalsResult
                {
                    FundamentalsComposite = Constants.CORE_INVALID_COMP,
                    HasBullishSMA = false,
                    HasBearishSMA = false,
                    HasDividends = false,
                    HasGoldenPath = false,
                    IsBlacklisted = false,
                    DollarVolumeToday = 0.0M,
                    DollarVolume10Day = 0.0M,
                    DollarVolume30Day = 0.0M,
                    DollarVolumeAverage = 0.0M,
                    VolumeSlope = 0.0M,
                    PriceSlope = 0.0M,
                    AverageEPS = 0.0M,
                    AveragePE = 0.0M,
                    GrowthEPS = 0.0M,
                    GrowthPE = 0.0M,
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

            bool averageDmiTrendingPositive = pDmiAvg > nDmiAvg;
            bool recentDmiTrendingPositive = pDmiYList[pDmiYList.Count - 1] > nDmiYList[nDmiYList.Count - 1];

            //base value average of the 2 most recent +DMI values, and adx average if trending
            //also add the most recent Z Score as an average percentage if it is positive
            decimal baseValue = (pDmiYList[pDmiYList.Count - 1] + pDmiYList[pDmiYList.Count - 2]) / 2;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add bonus for recent trending and avg trending
            decimal recentTrendingBonus = recentDmiTrendingPositive ? Constants.CORE_BONUS * 2 : 0;
            decimal averageTrendingBonus = averageDmiTrendingPositive ? Constants.CORE_BONUS * 2 : 0;

            //Add time-scaled bonus and penalty for buy and sell signals
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(hasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(hasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

            //Add bonus for ADX average above 25 per investopedia recommendation
            decimal averageBuySignalBonus = adxAvg > 25 && hasBuySignal ? Constants.CORE_BONUS * 2 : 0;

            //Only add zscore slope bonus if +DMI > -DMI
            decimal zScoreSlopeBonus = (zScoreSlope > 0.1m) && averageDmiTrendingPositive ?
                (zScoreSlope * zScoreSlopeMultiplier) + Constants.CORE_BONUS : 0;

            decimal pDmiSlopeBonus = (pDmiSlope > 0.1m) ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;
            decimal nDmiSlopeBonus = (nDmiSlope < -0.1m) ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;

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

            decimal baseValue = 0;

            //Start with the average of the 2 most recent OBV Normalized Scores
            //Only allow positive normalizedScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal normalizedScoreBase =
                ((normalizedScores[normalizedScores.Count - 1] + normalizedScores[normalizedScores.Count - 2]) / 4) * 100;
            normalizedScoreBase = normalizedScoreBase < 0 ? Constants.CORE_BONUS * 5 : normalizedScoreBase;

            //ZScore base helps us get the base value for composite from derivatives
            //Only allow positive zScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal zScoreBase = ((zScores[zScores.Count - 1] + zScores[zScores.Count - 2]) / 4) * 100;
            zScoreBase = zScoreBase < 0 ? Constants.CORE_BONUS * 5 : zScoreBase;

            baseValue = (normalizedScoreBase + zScoreBase) / 2.0M;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add bonus if average OBV is greater than 0
            decimal obvAverageBonus = obvAverage > 0 ? Constants.CORE_BONUS * 2 : 0;

            //Add bonus if OBV slope positive
            decimal obvSlopeBonus = obvSlope > 0 ? Constants.CORE_BONUS * 3 : 0;

            //Add Zscore slope bonus
            decimal zScoreSlopeBonus = 0;
            if (zScoreSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                zScoreSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0.05m)
                zScoreSlopeBonus += Constants.CORE_BONUS;

            //Add Normalized slope bonus
            decimal normalizedSlopeBonus = 0;
            if (normalizedSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                normalizedSlopeBonus += (normalizedSlope * normalizedSlopeMultiplier);
            if (normalizedSlope > 0.05m)
                normalizedSlopeBonus += Constants.CORE_BONUS;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(obvHasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(obvHasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

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

            //Use total base and signal diffs, along with macd total hist to get macd base value
            decimal baseValue = 0;
            decimal macdBaseSignalDiff = macdTotalBase - macdTotalSignal;
            if (macdBaseSignalDiff > 0)
            {
                baseValue = (macdBaseSignalDiff * Constants.CORE_BONUS) + 5;
            }
            if (macdTotalHist > 0)
            {
                baseValue += (macdTotalHist * Constants.CORE_BONUS) + 5;
            }
            if (baseValue == 0)
            {
                baseValue += (3 * Constants.CORE_BONUS); //3-pi pity points
            }
            baseValue = Math.Min(30, baseValue);
            baseValue += positiveHistDays;

            decimal histSlopeBonus = (histSlope > 0) ? histSlope + (Constants.CORE_BONUS * 3) : 0;
            decimal baseSlopeBonus = (baseSlope > 0) ? baseSlope + (Constants.CORE_BONUS * 2) : 0;
            decimal signalSlopeBonus = (signalSlope > 0) ? signalSlope + Constants.CORE_BONUS : 0;

            //Add histogram zscore slope bonus
            decimal zScoreHistSlopeBonus = 0;
            if (zScoreSlope > 0.1m)
                zScoreHistSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0)
                zScoreHistSlopeBonus += (2 * Constants.CORE_BONUS);

            //Add normalized histogram slope bonus
            decimal normalizedHistSlopeBonus = 0;
            if (normalizedHistSlope >= 0.5m)
                normalizedHistSlopeBonus += normalizedHistSlope;
            if (normalizedHistSlope > 0)
                normalizedHistSlopeBonus += (2 * Constants.CORE_BONUS);

            //Get previous 2 base above signal bonus
            bool prevTwoBaseAboveSignal = baseYList[baseYList.Count - 1] > signalYList[signalYList.Count - 1]
                && baseYList[baseYList.Count - 2] > signalYList[signalYList.Count - 2];
            decimal baseAboveSignalBonus = prevTwoBaseAboveSignal ? Constants.CORE_BONUS * 3 : 0;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(macdHasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(macdHasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

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
            int aroonNegativeDays = 0;
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
                        else
                        {
                            aroonNegativeDays++;
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

            //Use percent diffs to get aroon base value
            decimal aroonAvgUp = Math.Max(aroonUpTotal / daysCalculated, 1.0M);
            decimal aroonAvgDown = Math.Max(aroonDownTotal / daysCalculated, 1.0M);
            decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
            decimal percentDiffUp = (aroonAvgUp / aroonAvgDown) * 100;

            //Whether aroonAvgUp or aroonAvgDown is higher, that one will be more than 100 percent of the other
            decimal baseBullResult = Math.Min(100 - percentDiffDown, 42); //base bull result caps at 42
            decimal baseBearResult = Math.Min(percentDiffUp, 30); //base bear result caps at 30
            decimal baseValue = (aroonAvgUp > aroonAvgDown) ? baseBullResult : baseBearResult;
            baseValue += aroonPositiveDays * Constants.HALF;
            baseValue += aroonPositiveDays > aroonNegativeDays ? Constants.CORE_BONUS : 0;

            //Get recent positivity modifiers
            decimal recentPositivityMinorModifier = aroonHasRecentPositivityMinor ? Constants.CORE_BONUS : 0;
            decimal recentPositivityMajorModifier = aroonHasRecentPositivityMajor ? 2 * Constants.CORE_BONUS : 0;

            //Get aroon average modifier
            decimal aroonAvgModifier = aroonAvgUp > aroonAvgDown ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;

            //Get slope modifiers
            decimal oscilatorSlopeModifier = (oscillatorSlope > 1.0M) ? oscillatorSlope + (2 * Constants.CORE_BONUS) : Constants.CORE_PENALTY * 3;
            oscilatorSlopeModifier = oscilatorSlopeModifier > 20 ? Math.Max(20, oscilatorSlopeModifier) : oscilatorSlopeModifier;

            decimal downSlopeModifier = (downSlope < 0) ? (-1 * downSlope) + (2 * Constants.CORE_BONUS) : (-1 * downSlope) + (Constants.CORE_PENALTY * 2);
            downSlopeModifier = downSlopeModifier > 20 ? Math.Max(20, downSlopeModifier) : downSlopeModifier;
            downSlopeModifier = downSlopeModifier < -20 ? Math.Max(-20, downSlopeModifier) : downSlopeModifier;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(aroonHasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(aroonHasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

            //Get other bonuses
            decimal lastOscValue = oscillatorYList[oscillatorYList.Count - 1];

            //Add bull major bonus if last AROON UP >= 70 per investopedia recommendation
            //This is the same as when last AROON OSC >= 50
            decimal bullMajorBonus = (lastOscValue >= 50) ? Constants.CORE_BONUS * 2: 0;

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
            composite = Math.Min(composite, 70 + (Constants.CORE_BONUS * 2));
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

            return (aroonCrossingPoint >= 30 && aroonCrossingPoint <= 70);
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

                if (hasPrevPrice && curPrice >= lowerYList[i] && prices[i - 1] < lowerYList[i - 1])
                {
                    crossLowerBand = true;
                    crossUpperBand = false;
                }

                if (hasPrevPrice && curPrice >= middleYList[i] && prices[i - 1] < middleYList[i - 1])
                {
                    crossMiddleBand = true;
                    crossUpperBand = false;
                }

                if (hasPrevPrice && curPrice >= upperYList[i] && prices[i - 1] < upperYList[i - 1])
                {
                    crossUpperBand = true;
                    crossLowerBand = false;
                }
            }

            decimal priceSlope = GetSlope(bbandsXList, prices);
            bool recentPositivity = prices[prices.Count - 1] > prices[prices.Count - 3];

            // Apply minor and major ranking to bbands sell sinals
            // Different from the other time-scaled buy and sell signals
            decimal bbandsBonus = 0;
            // Cross lower band and have positive breakout, buy signal, max weight
            if (crossLowerBand && recentPositivity && hasBreakout)
            {
                bbandsBonus += Constants.CORE_BONUS * 8;
                bbandsHasMaxBuySignal = true;
            }
            // Cross middle band and have positive breakout, buy signal, medium weight
            else if (crossMiddleBand && recentPositivity && hasBreakout)
            {
                bbandsBonus += Constants.CORE_BONUS * 4;
                bbandsHasMedBuySignal = true;
            }

            // Cross upper band and have positive breakout, sell signal, medium weight
            if (crossUpperBand && recentPositivity && hasBreakout)
            {
                bbandsBonus -= Constants.CORE_BONUS * 4;
                bbandsHasMedSellSignal = true;
            }
            // Cross upper band and have negative breakout, sell signal, max weight
            else if (crossUpperBand && !recentPositivity && hasBreakout)
            {
                bbandsBonus -= Constants.CORE_BONUS * 8;
                bbandsHasMaxSellSignal = true;
            }

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
                baseValue = percentageDiffRebound > 0 ? (100 - percentageDiffRebound) / 2 + Constants.CORE_BONUS : Constants.CORE_BONUS * 5; // Reward for price being below 2.5 std devs
            }
            // Cap base value at 40 with small bonus for max
            baseValue = Math.Min(baseValue, 40);
            baseValue += baseValue == 40 ? Constants.CORE_BONUS : 0;

            // Bonus for bullish consolidation of the bands
            decimal consolidationBonus = lowerSlope > 0 && upperSlope < 0 && priceSlope > 0.05M ? Constants.CORE_BONUS * 3 : 0;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += consolidationBonus;
            composite += (lowerSlope > 0) ? (lowerSlope * lowerSlopeMultiplier) + (Constants.CORE_BONUS * 3) : 0;
            composite += (middleSlope > 0) ? (middleSlope * middleSlopeMultiplier) + (Constants.CORE_BONUS * 4) : (Constants.CORE_PENALTY * 2);
            composite += (upperSlope > 0 && recentPositivity) ? (Constants.CORE_BONUS * 3) : 0;
            composite = Math.Min(composite, 70 + (Constants.HALF * Constants.CORE_BONUS));
            composite += composite > 50 && bbandsBonus < 0 ? bbandsBonus : 0;
            composite += bbandsBonus > 0 ? bbandsBonus : 0;

            composite = Math.Min(composite, 100); // cap BBANDS composite at 100, no extra weight
            return Math.Max(0, composite); // limit BBANDS composite at 0, no negatives
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }

        public static decimal CalcEPSModifier(decimal averageEPS, decimal growthEPS)
        {
            decimal epsModifier = 0;

            //If everything is negative return base
            if (averageEPS <= 0 && growthEPS <= 0)
            {
                return epsModifier;
            }

            // averageEPS score formulation
            // Reward cases
            if (0 < averageEPS && averageEPS <= 0.5M)
            {
                epsModifier += averageEPS * 10 + Constants.CORE_BONUS;
            }
            else if (0.5M < averageEPS && averageEPS <= 1)
            {
                epsModifier += averageEPS * 5 + (2 * Constants.CORE_BONUS);
            }
            else if (1 < averageEPS && averageEPS <= 2)
            {
                epsModifier += averageEPS * 2 + (3 * Constants.CORE_BONUS);
            }
            else if (2 < averageEPS && averageEPS <= 5)
            {
                epsModifier += averageEPS * 2 + (4 * Constants.CORE_BONUS);
            }
            else if (5 < averageEPS)
            {
                epsModifier += Constants.HALF * averageEPS + (4 * Constants.CORE_BONUS);
            }

            // growthEPS score formulation
            // Reward cases
            if (0 < growthEPS && growthEPS <= 1)
            {
                epsModifier += growthEPS * 5 + Constants.CORE_BONUS;
            }
            else if (1 < growthEPS && growthEPS <= 3)
            {
                epsModifier += growthEPS * 3 + (2 * Constants.CORE_BONUS);
            }
            else if (3 < growthEPS)
            {
                epsModifier += growthEPS + (3 * Constants.CORE_BONUS);
            }
            /* Penalty cases
            else if (-5 <= growthEPS && growthEPS < 0)
            {
                epsModifier += growthEPS * 3 - 3;
            }
            else if (growthEPS < -5)
            {
                epsModifier += growthEPS * 3 - 6;
            }*/
            return Math.Min(epsModifier, Constants.FUND_EPS_MOD_UPPER_LIMIT);
        }

        public static decimal CalcPEModifier(decimal averagePE, decimal growthPE)
        {
            //If average PE significantly negative return penalty
            if (averagePE < -10)
            {
                return Constants.CORE_PENALTY;
            }
            //If everything is 0 return 0
            if (averagePE <= 0 && growthPE <= 0)
            {
                return 0;
            }
            decimal peModifier = 0;

            // averagePE score fomulation
            // Reward cases
            if (0 < averagePE && averagePE <= 25)
            {
                peModifier += (averagePE / 5) + (3 * Constants.CORE_BONUS);
            }
            else if (25 < averagePE && averagePE <= 50)
            {
                peModifier += (averagePE / 10) + Constants.CORE_BONUS;
            }
            else if (50 < averagePE && averagePE <= 100)
            {
                peModifier += (averagePE / 15);
            }
            // Penalty cases
            else if (averagePE > 100)
            {
                peModifier += (-1 * (averagePE / 15));
            }

            // growthPE score fomulation
            // Reward cases
            if (-50 <= growthPE && growthPE < 0)
            {
                peModifier += (-1 * (growthPE / 10));
            }
            else if (-100 <= growthPE && growthPE < -50)
            {
                peModifier += (-1 * (growthPE / 10)) + Constants.CORE_BONUS;
            }
            else if (-100 > growthPE)
            {
                peModifier += (-1 * (growthPE / 100)) + (Constants.CORE_BONUS * 3);
            }
            else if (0 < growthPE && growthPE <= 50)
            {
                peModifier += (-1 * (growthPE / 10));
            }
            // Penalty cases
            /*else if (50 < growthPE && growthPE <= 100)
            {
                peModifier += (-1 * (growthPE / 10)) - 3;
            }
            else if (100 < growthPE)
            {
                peModifier += (-1 * (growthPE / 100)) - 10;
            }
            return Math.Max(-10, peModifier);*/
            return Math.Min(peModifier, Constants.FUND_PE_MOD_UPPER_LIMIT);
        }

        private static decimal CalcDividendBonus(decimal divRate, decimal divYield)
        {
            decimal divBonus = 0;
            bool hasDivs = divRate > 0;
            bool hasYield = divYield > 0;
            if (hasDivs)
            {
                //if div rate above 0, add 1
                if (divRate > 0)
                {
                    divBonus += 1;
                }
                //if div rate between 0 and 1.5, add divRate * 2 + 2
                if (0.5M < divRate && divRate <= 1.5M)
                {
                    divBonus += divRate * 2 + 1;
                }
                //else if div rate above 1.5, add divRate * 2 + bonus
                else if (divRate > 1.5M)
                {
                    divBonus += divRate * (Constants.CORE_BONUS - 1) + Constants.CORE_BONUS;
                }
            }
            if (hasYield)
            {
                //if div yield is between 0 and 5, add reduced bonus
                if (1 < divYield && divYield <= 5)
                {
                    divBonus += (Constants.HALF * divYield) + (Constants.CORE_BONUS / Constants.THREE);
                }
                //if div yield is between 5 and 10, add divYield + bonus
                else if (5 < divYield && divYield <= 10)
                {
                    divBonus += (Constants.HALF * divYield) + (Constants.CORE_BONUS / Constants.TWO);
                }
                //if div yield is above 3, add divYield + 5 bonus
                if (divYield >= 10)
                {
                    divBonus += (Constants.HALF * divYield) + Constants.CORE_BONUS;
                }
            }
            return Math.Min(divBonus, 25);
        }

        private static decimal CalcSmaModifier(PTHistory history)
        {
            decimal smaModifier = 0;

            // General bullish or bearish
            if (history.HasBullishSMA)
            {
                smaModifier += Constants.CORE_BONUS * 2 + 1;
            }
            else if (history.HasBearishSMA)
            {
                smaModifier += Constants.CORE_PENALTY * 2;
            }

            // Bonus if current price is close enough to 100d SMA for likely rebound
            var percentDiff = GetPercentDiff(history.AveragePrice100Day, history.TodayVwap);
            if (-7 <= Math.Abs(percentDiff) && Math.Abs(percentDiff) <= 7)
            {
                smaModifier += Constants.CORE_BONUS + (7 - Math.Abs(percentDiff));
            }

            // Bonus if 30d SMA is above 100d SMA
            if (history.AveragePrice30Day > history.AveragePrice100Day)
            {
                smaModifier += Constants.CORE_BONUS / 2;
            }

            // Bonus if current price is above 20d SMA
            if (history.TodayVwap > history.AveragePrice20Day + (history.AveragePrice20Day * .01M))
            {
                smaModifier += Constants.CORE_BONUS / 2;
            }
            return smaModifier;
        }

        private static decimal CalcNetExpenseRatioBonus(decimal netExpenseRatio)
        {
            if (netExpenseRatio <= 0) return 0;
            decimal nerBonus = 0;
            nerBonus += netExpenseRatio <= 1 ? 1 : 0;
            if (0 < netExpenseRatio && netExpenseRatio <= Constants.FUND_NER_MAJOR_LIMIT_PERCENT)
            {
                nerBonus += (1.0M / netExpenseRatio) * Constants.FUND_NER_INVERSE_MULTIPLIER_PERCENT + 1;
                nerBonus += netExpenseRatio <= Constants.FUND_NER_MINOR_LIMIT_PERCENT ?
                    Constants.CORE_BONUS / Constants.TWO : 0;
            }
            return nerBonus;
        }

        private static decimal CalcVolumeTrendingModifier(PTHistory history)
        {
            // Add positive fractional bonus if today dollar volume is greater than 30d average dollar volume, negative otherwise
            decimal percentChange = GetPercentDiff(history.AverageVolUsd30Day, history.TodayVolUsd);
            decimal volumeTrendingModifier = 0;
            if (0.5M < percentChange && percentChange <= 100)
            {
                volumeTrendingModifier += percentChange < 25 ? (percentChange / 5) + Constants.CORE_BONUS :
                    (percentChange / 20) + (Constants.CORE_BONUS * 2);
            }
            else if (100 < percentChange)
            {
                volumeTrendingModifier += Constants.CORE_BONUS * 2 + 1;
            }
            // Penalty cases
            if (history.TodayVolUsd < history.AverageVolUsd10Day - Constants.FIFTY_THOUSAND &&
                history.AverageVolUsd10Day < history.AverageVolUsd30Day - Constants.FIFTY_THOUSAND)
            {
                volumeTrendingModifier += Constants.CORE_PENALTY * 2;
            }
            else if (-0.5M > percentChange && percentChange >= -100)
            {
                volumeTrendingModifier += percentChange > -25 ? (percentChange / 5) + Constants.CORE_PENALTY :
                    (percentChange / 20) + (Constants.CORE_PENALTY * 2);
            }
            else if (-100 > percentChange)
            {
                volumeTrendingModifier += Constants.CORE_PENALTY * 3;
            }
            return volumeTrendingModifier;
        }

        private static decimal CalcTimeScaledBuySignalBonus(bool hasBuySignal, decimal bonus, int daysSinceSignal)
        {
            decimal timeScaledBonus = 0;
            if (hasBuySignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledBonus = bonus * 8 + Constants.CORE_SIGNAL_MOD + 1;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledBonus = bonus * 7 + Constants.CORE_SIGNAL_MOD + 1;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledBonus = bonus * 5 + Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledBonus = bonus * 3 + Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledBonus = bonus * 2 + 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledBonus = bonus + 1;
                }
            }
            return timeScaledBonus;
        }

        private static decimal CalcTimeScaledSellSignalPenalty(bool hasSellSignal, decimal penalty, int daysSinceSignal)
        {
            decimal timeScaledPenalty = 0;
            if (hasSellSignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledPenalty = penalty * 7 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledPenalty = penalty * 6 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledPenalty = penalty * 5 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledPenalty = penalty * 3 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledPenalty = penalty + 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledPenalty = 0;
                }
            }
            return (-1 * timeScaledPenalty);
        }
    }
}
