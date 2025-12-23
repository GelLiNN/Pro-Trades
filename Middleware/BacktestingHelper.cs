using PT.Models.CoreModels;
using PT.Services;
using Skender.Stock.Indicators;
using System.Collections.Generic;
using System.Diagnostics;

namespace PT.Middleware
{
    public static class BacktestingHelper
    {
        public static BacktestingResult BacktestSingleComposite(string composite, int daysFromHistStart, RequestManager rm)
        {
            Stopwatch sw = Stopwatch.StartNew();
            composite = composite.ToUpper();
            BacktestingResult result = new BacktestingResult
            {
                CompositeTested = composite,
                TotalPrimes = 0,
                TotalNeutralTurnouts = 0,
                TotalWinningTurnouts = 0,
                TotalLosingTurnouts = 0,
                TotalPassingTurnouts = 0,
                TotalFailingTurnouts = 0,
                AveragePriceMovePercent = 0,
                Hits = new List<Hit>(),
            };

            // Get randomized set of companies to backtest with
            var scrapedSymbols = AssetAggregator.GetRandomizedCompanySymbols(rm, 300);
            foreach (var symbol in scrapedSymbols)
            {
                // Alpaca API price history
                PTHistory ptHistory = HistoryHelper.GetHistoryAsync(rm, symbol, Constants.DEFAULT_HISTORY_DAYS).GetAwaiter().GetResult();

                List<Quote> history = ptHistory.SkenderHistory.ToList();
                if (history.Count > 250)
                {
                    // Test with ADX first
                    if (composite == Constants.COMPOSITE_ADX)
                    {
                        BacktestADX(history, result, daysFromHistStart, symbol);
                    }
                }
            }
            result.AveragePriceMovePercent = result.TotalPrimes > 0 ?
                result.AveragePriceMovePercent / Convert.ToDecimal(result.TotalPrimes) : 0;
            result.CompositePassingPercent = result.TotalPrimes > 0 ?
                Convert.ToDecimal(result.TotalPassingTurnouts) / Convert.ToDecimal(result.TotalPrimes) : 0;
            result.TimeMS = sw.ElapsedMilliseconds;
            return result;
        }

        private static void BacktestADX(List<Quote> history, BacktestingResult result, int days, string symbol)
        {
            // Make full calcs to train indicator before adjusting
            int adxPeriod = 15;
            IEnumerable<AdxResult> adxResults = Indicator.GetAdx(history, adxPeriod);

            // Get strategic subsets of full calcs history to backtest for N days
            int startTrainingDays = days;
            for (int i = startTrainingDays; i < history.Count - 5; i++)
            {
                Quote curHist = history[i];
                decimal dollarVolumeStart = curHist.Volume * curHist.Close;
                bool disqualified = dollarVolumeStart < Constants.DEFAULT_VOLUME_USD_1D_LIMIT;

                // Set earliest and latest backtesting timestamps
                if (result.EarliestDate == null)
                {
                    result.EarliestDate = curHist.Date;
                }
                if (i == history.Count - 6)
                {
                    result.LatestDate = curHist.Date;
                }

                var adxAdjusted = adxResults.Take(i);
                decimal compScore = Indicators.GetADXComposite(adxAdjusted, Constants.DEFAULT_LOOKBACK_DAYS);
                if (compScore >= 85 && !disqualified)
                {
                    result.TotalPrimes++;
                    Quote turnoutHist5 = history[i + 5];

                    Hit hit = new Hit
                    {
                        HitSymbol = symbol,
                        HitScore = compScore,
                        HitDate = curHist.Date,
                        HitPriceClose = curHist.Close,
                        TurnoutDate = turnoutHist5.Date,
                        TurnoutPrice = turnoutHist5.High,
                        TurnoutPercentChange = (turnoutHist5.High - curHist.Close) / curHist.Close * Constants.ONE_HUNDRED,
                    };
                    if (hit.TurnoutPercentChange >= 0)
                    {
                        // Passing composite
                        hit.Passing = true;
                        result.TotalPassingTurnouts++;
                    }
                    else
                    {
                        // Failing composite
                        hit.Passing = false;
                        result.TotalFailingTurnouts++;
                    }
                    result.AveragePriceMovePercent += hit.TurnoutPercentChange;
                    result.Hits.Add(hit);
                }
            }
        }
    }
}
