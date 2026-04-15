using PT.Middleware;
using PT.Models.CoreModels;
using PT.Models.RequestModels;
using PT.Services;
using Skender.Stock.Indicators;
using System.Diagnostics;
using YahooQuotesApi;

namespace PT.Core
{
    /// <summary>
    /// Wrapper for processing and compositing AI GRU modules to produce a final prediction.
    /// </summary>
    public static class Predictor
    {
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
            FundamentalsResult fundResult = GRU.GetFundamentalsResult(symbol, quote, ptHistory);

            decimal adxCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_ADX, history, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal obvCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_OBV, obvHistory, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal macdCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_MACD, history, Constants.DEFAULT_LOOKBACK_DAYS);
            decimal bbandsCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_BBANDS, history, Constants.DEFAULT_LOOKBACK_DAYS, supplement);
            decimal aroonCompositeScore = GetIndicatorComposite(symbol, Constants.COMPOSITE_AROON, history, Constants.DEFAULT_LOOKBACK_DAYS);

            long indicatorStopMs = sw.ElapsedMilliseconds;
            long coreMs1 = indicatorStopMs - yahooStopMs;

            // TODO: move outer function to GRU class
            ShortInterestResult shortResult = FINRA.GetShortInterest(symbol, supplement, Constants.DEFAULT_LOOKBACK_DAYS, rm);

            long finraStopMs = sw.ElapsedMilliseconds;
            long finraMs = finraStopMs - indicatorStopMs;

            // TODO: move outer function to GRU class
            HedgeFundsResult hfResult = TipRanks.GetTipRanksResult(symbol, rm);

            long tipRanksStopMs = sw.ElapsedMilliseconds;
            long tipRanksMs = tipRanksStopMs - finraStopMs;

            var finalResult = GRU.ParametrizeComposites(fundResult, hfResult, shortResult, adxCompositeScore,
                obvCompositeScore, macdCompositeScore, bbandsCompositeScore, aroonCompositeScore);

            var paramType = Predictor.GetParameterType(finalResult.hs);

            // Price targets for buy, sell, and short
            decimal priceLast = quote?.PostMarketPrice ?? quote?.RegularMarketPrice ?? ptHistory.TodayClose;
            decimal buyTarget = HistoryHelper.GetPriceBuyTarget(ptHistory, priceLast);
            HistoryHelper.ComputePriceSellTargets(fundResult, ptHistory);
            List<PTPriceTarget> priceTargets = ptHistory.PriceTargets.OrderByDescending(x => x.TargetPrice).ToList();

            string compositeScoreNotes = Predictor.GetCompositeScoreNotes(fundResult, hfResult, shortResult,
                adxCompositeScore, obvCompositeScore, macdCompositeScore, bbandsCompositeScore, aroonCompositeScore,
                ptHistory.PriceTargetAvgLong, ptHistory.PriceTargetAvgShort);

            bool notFound = hfResult.Description.Equals("Not Found");

            string minDescription = notFound ? hfResult.Description :
                hfResult.Description.Substring(0, Math.Min(300, hfResult.Description.Length));

            if (!notFound)
            {
                int lastSpaceIndex = minDescription.LastIndexOf(' ');
                if (lastSpaceIndex != -1) // if a space was found
                {
                    minDescription = minDescription.Substring(0, lastSpaceIndex);
                }
                minDescription += minDescription.EndsWith('.') ? ".." : "...";
            }

            string? assetName = string.IsNullOrWhiteSpace(quote?.LongName) ?
                hfResult.Name : quote?.LongName;

            CompositeScoreResult scoreResult = new CompositeScoreResult
            {
                Symbol = symbol,
                Name = assetName,
                Exchange = quote?.FullExchangeName,
                AssetType = fundResult.AssetType,
                AssetSector = hfResult.Sector,
                CompositeScoreValue = finalResult.cs,
                CompositeScoreNotes = compositeScoreNotes,
                PriceOpen = ptHistory.TodayOpen.ToString(Constants.FORMAT_CURRENCY),
                PriceClose = ptHistory.TodayClose.ToString(Constants.FORMAT_CURRENCY),
                PriceVwap = ptHistory.PriceHistory[0].PriceVwap.ToString(Constants.FORMAT_CURRENCY),
                PriceLast = priceLast.ToString(Constants.FORMAT_CURRENCY),
                PriceBuyTarget = buyTarget.ToString(Constants.FORMAT_CURRENCY),
                PriceSellTarget = ptHistory.PriceTargetProLong.ToString(Constants.FORMAT_CURRENCY),
                PriceSellTargetShort = ptHistory.PriceTargetProShort.ToString(Constants.FORMAT_CURRENCY),
                PriceTargetHedgeFunds = hfResult.PriceTarget.ToString(Constants.FORMAT_CURRENCY),
                PriceFairValue = fundResult.FairValuePrice.ToString(Constants.FORMAT_CURRENCY),
                RPriceToEarnings = fundResult.AveragePE,
                RPriceToBook = fundResult.PriceToBook,
                GRUHistoryDays = history.Count(),
                ADXComposite = adxCompositeScore,
                OBVComposite = obvCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                BBANDSComposite = bbandsCompositeScore,
                RatingsComposite = hfResult.RatingsComposite,
                ShortInterestComposite = shortResult.ShortInterestComposite,
                FundamentalsComposite = fundResult.FundamentalsComposite,
                ScoreDate = DateTime.Now,
                ParameterSet = paramType.Type,
                AssetDescription = minDescription,
                PriceTargets = priceTargets,
                ShortInterest = shortResult,
                Fundamentals = fundResult,
                HedgeFunds = hfResult,
                DataProviders = Constants.DEFAULT_DATA_PROVIDERS
            };
            scoreResult.PriceRedGreen = priceLast >= ptHistory.TodayOpen ?
                Constants.DEFAULT_GREEN : Constants.DEFAULT_RED;
            scoreResult.MarketCap = (fundResult.MarketCap / Constants.ONE_BILLION)
                .ToString(Constants.FORMAT_ROUND_2) + " Billion";
            scoreResult.IsQualifiedVolume = ptHistory.QualifiedVolume;
            scoreResult.IsBullishLongSMA = ptHistory.TodayVwap >= ptHistory.AveragePrice200Day;
            scoreResult.IsBullishDiffSMA = ptHistory.IsBullishSMA;
            scoreResult.IsBullishBandSMA = ptHistory.IsAboveSMABand;
            scoreResult.PCM = Predictor.GetPostCompositeMod(scoreResult, paramType);
            scoreResult.CompositeScoreValue += scoreResult.PCM;
            scoreResult.CompositeScoreRank = Predictor.GetCompositeScoreRank(scoreResult);

            long coreStopMs = sw.ElapsedMilliseconds;
            long coreMs2 = coreStopMs - tipRanksStopMs;
            scoreResult.AlpacaTimeMS = alpacaMs;
            scoreResult.YahooTimeMS = yahooMs;
            scoreResult.FinraTimeMS = finraMs;
            scoreResult.TipRanksTimeMS = tipRanksMs;
            scoreResult.CoreTimeMS = coreMs1 + coreMs2;
            scoreResult.TotalTimeMS = coreStopMs;

            sw.Reset();
            return scoreResult;
        }

        // Main switch function to organize the AI model's technical indicator GRU composites
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
                        compositeScore = GRU.GetADXComposite(adxResults, daysToCalculate);
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
                        compositeScore = GRU.GetAROONComposite(aroonResults, daysToCalculate);
                        break;

                    case Constants.COMPOSITE_MACD:
                        //Positive rate-of-change for the MACD Histogram values indicate bullish movement
                        //Recent Buy signal measured by MACD base value crossing (becoming greater than) the MACD signal value
                        //Recent Sell signal measured by MACD signal value crossing (becoming greater than) the MACD base value
                        int fastPeriod = 12;
                        int slowPeriod = 26;
                        int signalPeriod = 9;
                        IEnumerable<MacdResult> macdResults = Indicator.GetMacd(history, fastPeriod, slowPeriod, signalPeriod);
                        compositeScore = GRU.GetMACDComposite(macdResults, daysToCalculate);
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
                        compositeScore = GRU.GetOBVComposite(obvResults, daysToCalculate);
                        break;

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
                        int bbandsPeriod = 21;
                        double standardDeviations = 2.5;
                        IEnumerable<BollingerBandsResult> bbandsResults = Indicator.GetBollingerBands(history, bbandsPeriod, standardDeviations);
                        compositeScore = GRU.GetBBANDSComposite(bbandsResults, (List<Skender.Stock.Indicators.Quote>)supplement, daysToCalculate);
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

        /// <summary>
        /// Get Composite Rank constant string for the prediction depending on gated boundaries
        /// </summary>
        /// <param name="scoreResult"></param>
        /// <returns></returns>
        public static string GetCompositeScoreRank(CompositeScoreResult scoreResult)
        {
            DateTime today = DateTime.Today;
            DateTime nextFriday = Enumerable.Range(1, 7)
                .Select(days => today.AddDays(days))
                .First(date => date.DayOfWeek == DayOfWeek.Friday);

            bool earningsDuringAttrition = scoreResult.Fundamentals.NextEarningsDate > today &&
                scoreResult.Fundamentals.NextEarningsDate < nextFriday;

            // Rounding accounding to specific limit
            scoreResult.CompositeScoreValue = scoreResult.CompositeScoreValue >= Constants.CORE_PRIME_RND_LIMIT &&
                scoreResult.CompositeScoreValue < Constants.CORE_PRIME_GATE ?
                Constants.CORE_PRIME_GATE : scoreResult.CompositeScoreValue;

            // This is where blacklisting happens from bad dollar volume throughput or penny price limit
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

            // Earnings scheduled for the period during attrition are ranked parallel
            rank += earningsDuringAttrition ? Constants.RANK_E : string.Empty;
            return rank;
        }

        private static bool IsDisqualifiedPrediction(CompositeScoreResult scoreResult)
        {
            decimal mcap = Convert.ToDecimal(scoreResult.MarketCap.Split(" ")[0]);
            bool qualifiedMcap = (scoreResult.ParameterSet == Constants.HS5 && mcap == 0) || mcap > Constants.DEFAULT_MCAP_D_LIMIT
                || Constants.FUND_HANDICAP_MODE_ENABLED;
            bool qualifiedHistDays = scoreResult.GRUHistoryDays >= Constants.DEFAULT_HISTORY_DAYS_LIMIT;
            return
                (!scoreResult.IsQualifiedVolume || !qualifiedMcap || !qualifiedHistDays ||
                (Convert.ToDecimal(scoreResult.PriceLast.Substring(1)) < Constants.DEFAULT_PENNY_PRICE_D_LIMIT ||
                Convert.ToDecimal(scoreResult.PriceVwap.Substring(1)) < Constants.DEFAULT_PENNY_PRICE_D_LIMIT));
        }

        private static bool IsShortPrediction(CompositeScoreResult scoreResult)
        {
            return
                scoreResult.CompositeScoreValue < 40 && scoreResult.RatingsComposite < 50 &&
                scoreResult.ShortInterestComposite < 50 && scoreResult.FundamentalsComposite < 50 &&
                !(scoreResult.RatingsComposite == Constants.CORE_INVALID_COMP &&
                scoreResult.FundamentalsComposite == Constants.CORE_INVALID_COMP);
        }

        /// <summary>
        /// Get post GRU compositing mod for prediction score smoothing
        /// </summary>
        /// <param name="scoreResult"></param>
        /// <param name="paramType"></param>
        /// <returns></returns>
        public static decimal GetPostCompositeMod(CompositeScoreResult scoreResult, ParameterSetType paramType)
        {
            decimal postCompositeMod = 0; // Only if HS1, HS2, HS3
            if (paramType.Type == Constants.HS1 || paramType.Type == Constants.HS2 || paramType.Type == Constants.HS3)
            {
                if (!Constants.FUND_HANDICAP_MODE_ENABLED)
                {
                    postCompositeMod += scoreResult.RPriceToBook <= 0 ? (Constants.CORE_PENALTY + 2) : 0;
                    postCompositeMod += scoreResult.RPriceToEarnings <= 0 ? (Constants.CORE_PENALTY + 2) : 0;
                    postCompositeMod += scoreResult.RPriceToBook > 12 ? (Constants.CORE_PENALTY + 2) : 0;
                    postCompositeMod += scoreResult.RPriceToEarnings > 37 ? (Constants.CORE_PENALTY + 2) : 0;
                    postCompositeMod += scoreResult.Fundamentals.BookValuePrice > 0 &&
                        (scoreResult.Fundamentals.PriceToFairValue > 0 && scoreResult.Fundamentals.PriceToFairValue < 2.0M) &&
                        (scoreResult.RPriceToBook > 0 && scoreResult.RPriceToBook < 2.5M) &&
                        (scoreResult.RPriceToEarnings > 0 && scoreResult.RPriceToEarnings < 30.0M) ? (Constants.CORE_BONUS - 2.5M) : 0;
                    postCompositeMod += scoreResult.IsBullishLongSMA ? (Constants.CORE_BONUS - 2.75M) : 0;
                    postCompositeMod += scoreResult.IsBullishDiffSMA ? (Constants.CORE_BONUS - 2.75M) : 0;
                    postCompositeMod += scoreResult.IsBullishBandSMA ? (Constants.CORE_BONUS - 2.5M) : 0;
                }
                else
                {
                    postCompositeMod += scoreResult.IsBullishLongSMA ? (Constants.CORE_BONUS - 2.45M) : 0;
                    postCompositeMod += scoreResult.IsBullishDiffSMA ? (Constants.CORE_BONUS - 2.45M) : 0;
                    postCompositeMod += scoreResult.IsBullishBandSMA ? (Constants.CORE_BONUS - 2.45M) : 0;
                }
            } // HS5
            else if (paramType.Type == Constants.HS5)
            {
                postCompositeMod += scoreResult.IsBullishLongSMA ? (Constants.CORE_BONUS - 2.66M) : 0;
                postCompositeMod += scoreResult.IsBullishDiffSMA ? (Constants.CORE_BONUS - 2.25M) : 0;
                postCompositeMod += scoreResult.IsBullishBandSMA ? (Constants.CORE_BONUS - 2.25M) : 0;
            }
            postCompositeMod += !scoreResult.IsBullishDiffSMA && !scoreResult.IsBullishBandSMA ? -.5M : 0;
            postCompositeMod += !Constants.FUND_HANDICAP_MODE_ENABLED &&
                !scoreResult.IsBullishLongSMA && !scoreResult.IsBullishDiffSMA && !scoreResult.IsBullishBandSMA ? -.25M : 0;
            postCompositeMod = Constants.CORE_EXT_MODE_ENABLED && postCompositeMod < 0 ? postCompositeMod * 0.66M : postCompositeMod;
            postCompositeMod = Constants.CORE_EXT_MODE_ENABLED && postCompositeMod > 1 ? postCompositeMod + 0.5M : postCompositeMod;
            return postCompositeMod;
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
            notes += (adxComposite >= 95) ? "adx++, " : (adxComposite >= Constants.CORE_PRIME_GATE) ? "adx+, " : (adxComposite <= 33) ? "adx-, " : "";
            notes += (obvComposite >= 95) ? "obv++, " : (obvComposite >= Constants.CORE_PRIME_GATE) ? "obv+, " : (obvComposite <= 33) ? "obv-, " : "";
            notes += (aroonComposite >= 95) ? "aroon++, " : (aroonComposite >= Constants.CORE_PRIME_GATE) ? "aroon+, " : (aroonComposite <= 33) ? "aroon-, " : "";
            notes += (bbandsComposite >= 90) ? "bbands++, " : (bbandsComposite >= Constants.CORE_PRIME_GATE) ? "bbands+, " : (bbandsComposite <= 33) ? "bbands-, " : "";
            notes += $"TL: ${Math.Round(targetL, 2)}, TS: ${Math.Round(targetS, 2)}";
            return notes;
        }

        /// <summary>
        /// Get GRU compositing parameter set type for passed type name
        /// </summary>
        /// <param name="typeName">HS1, HS2, etc.</param>
        /// <returns></returns>
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
            else if (typeName == Constants.HS6)
            {
                return new ParameterSetType
                {
                    Type = Constants.HS6,
                    ShortDescription = Constants.HS6_SHORT_DESCRIPTION,
                    LongDescription = Constants.HS6_LONG_DESCRIPTION,
                    Set = Constants.HS6_SET
                };
            }
            return null;
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
