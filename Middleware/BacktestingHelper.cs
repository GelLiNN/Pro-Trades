using PT.Models.CoreModels;
using PT.Services;

namespace PT.Middleware
{
    public static class BacktestingHelper
    {
        public static BacktestingResult BacktestSingleComposite(string composite, int days, RequestManager rm)
        {
            BacktestingResult result = new BacktestingResult
            {
                Hits = new List<Hit>(),
                CompositeTested = composite,
                totalPrimes = 0,
                totalNeutralTurnouts = 0,
                totalWinningTurnouts = 0,
                totalLosingTurnouts = 0,
                totalPassingTurnouts = 0,
                totalFailingTurnouts = 0
            };

            // Do backtesting for N days

            return result;
        }
    }
}
