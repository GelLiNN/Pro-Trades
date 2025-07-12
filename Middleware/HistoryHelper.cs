using Newtonsoft.Json.Linq;
using PT.Models.CoreModels;
using PT.Models.RequestModels;
using PT.Services;
using YahooQuotesApi;

namespace PT.Middleware
{
    public class HistoryHelper
    {
        /// <summary>
        /// Pro-Trades fetches and processes price history data first.
        /// </summary>
        /// <param name="rm"></param>
        /// <param name="symbol"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static async Task<PTHistory> GetHistoryAsync(RequestManager rm, string symbol, int days)
        {
            var historyData = await Alpaca.GetAlpacaPriceHistory(rm, symbol, days);
            if (historyData != null)
            {
                // Convert into PTHistory with Stock.Indicators.Quote inside
                PTHistory ptHistory = new();
                List<Skender.Stock.Indicators.Quote> skenderHistoryList = new();
                Stack<PTDay> historyStack = new();

                // Averages we must compute
                decimal avgPrice100d = 0;
                decimal avgPrice30d = 0;
                decimal avgPrice10d = 0;
                decimal avgVol30d = 0;
                decimal avgVol10d = 0;
                decimal avgVolUsd30d = 0;
                decimal avgVolUsd10d = 0;
                decimal avgLow10d = 0;
                decimal avgHigh10d = 0;
                decimal lastPriceVw = 0;
                decimal lastVol = 0;

                // This is where volume USD throughput filtering happens
                bool usdVolumeQualified1d = false;
                int last10PassCount = 0;
                int last30PassCount = 0;

                for (int i = 0; i < historyData.Count; i++)
                {
                    var curData = historyData[i];
                    var singleDayResult = ProcessSingleDay(curData);
                    if (singleDayResult.Day == null || singleDayResult.SkenderObject == null)
                        continue;

                    skenderHistoryList.Add(singleDayResult.SkenderObject);

                    // History object added, the rest is custom
                    var ptDay = singleDayResult.Day;
                    var curVwap = ptDay.PriceVwap;
                    var curVol = ptDay.Volume;
                    var curVolUsd = ptDay.DollarVolume;

                    bool isLast100 = (historyData.Count - (i + 1) < 100);
                    bool isLast30 = (historyData.Count - (i + 1) < 30);
                    bool isLast10 = (historyData.Count - (i + 1) < 10);
                    bool isLast = (historyData.Count - (i + 1) == 0);

                    if (isLast100)
                    {
                        avgPrice100d += curVwap;
                    }

                    if (isLast30)
                    {
                        avgPrice30d += curVwap;
                        avgVol30d += curVol;
                        avgVolUsd30d += curVolUsd;

                        ptHistory.HighestHigh30Day = Math.Max(ptDay.PriceHigh, ptHistory.HighestHigh30Day);
                        ptHistory.LowestLow30Day = ptHistory.LowestLow30Day == 0 ? ptDay.PriceLow : ptHistory.LowestLow30Day;
                        ptHistory.LowestLow30Day = Math.Min(ptDay.PriceLow, ptHistory.LowestLow30Day);

                        // Check trading volume disqualifying limit
                        bool result30d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_30D_LIMIT;
                        if (result30d)
                        {
                            last30PassCount++;
                            ptDay.PassVolumeFilter = true;
                        }
                    }
                    if (isLast10)
                    {
                        avgPrice10d += curVwap;
                        avgVol10d += curVol;
                        avgHigh10d += ptDay.PriceHigh;
                        avgLow10d += ptDay.PriceLow;
                        avgVolUsd10d += curVolUsd;

                        ptHistory.Price10YList.Add(curVwap);
                        ptHistory.Volume10YList.Add(curVol);

                        // Check trading volume disqualifying limit
                        bool result10d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_10D_LIMIT;
                        if (result10d)
                        {
                            last10PassCount++;
                            ptDay.PassVolumeFilter = true;
                        }
                    }
                    if (isLast)
                    {
                        lastPriceVw += curVwap;
                        lastVol += curVol;
                        usdVolumeQualified1d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_1D_LIMIT;
                        ptDay.PassVolumeFilter = usdVolumeQualified1d;
                        ptHistory.TodayOpen = ptDay.PriceOpen;
                        ptHistory.TodayClose = ptDay.PriceClose;
                        ptHistory.TodayLow = ptDay.PriceLow;
                        ptHistory.TodayHigh = ptDay.PriceHigh;
                        ptHistory.TodayVwap = curVwap;
                    }
                    historyStack.Push(ptDay);
                }

                // Get Volume USD qualifying results
                ptHistory.Has30DayQualifiedVolume = last10PassCount >= Constants.DEFAULT_MIN_PASS_10D_LIMIT;
                ptHistory.Has10DayQualifiedVolume = last30PassCount >= Constants.DEFAULT_MIN_PASS_30D_LIMIT;
                ptHistory.Has1DayQualifiedVolume = usdVolumeQualified1d;

                // Compute final averages and figures for 100d, 30d, and 10d
                avgPrice100d = avgPrice100d / Constants.HUNDRED;
                avgPrice30d = avgPrice30d / Constants.THIRTY;
                avgVol30d = avgVol30d / Constants.THIRTY;
                avgPrice10d = avgPrice10d / Constants.TEN;
                avgVol10d = avgVol10d / Constants.TEN;

                ptHistory.AveragePrice100Day = avgPrice100d;
                ptHistory.AveragePrice30Day = avgPrice30d;

                ptHistory.AverageHigh10Day = avgHigh10d / Constants.TEN;
                ptHistory.AverageLow10Day = avgLow10d / Constants.TEN;

                ptHistory.AverageVolUsd30Day = avgVolUsd30d / Constants.THIRTY;
                ptHistory.AverageVolUsd10Day = avgVolUsd10d / Constants.TEN;
                ptHistory.TodayVolUsd = lastPriceVw * lastVol;

                // Make X and Y Lists
                ptHistory.HistoricalVwapYList.Add(avgPrice30d);
                ptHistory.HistoricalVwapYList.Add(avgPrice10d);
                ptHistory.HistoricalVwapYList.Add(lastPriceVw);
                for (int i = 1; i <= ptHistory.HistoricalVwapYList.Count; i++)
                    ptHistory.HistoricalVwapXList.Add(i);

                ptHistory.HistoricalVolAvgYList.Add(avgVol30d);
                ptHistory.HistoricalVolAvgYList.Add(avgVol10d);
                ptHistory.HistoricalVolAvgYList.Add(lastVol);
                for (int i = 1; i <= ptHistory.HistoricalVolAvgYList.Count; i++)
                    ptHistory.HistoricalVolAvgXList.Add(i);

                for (int i = 1; i <= ptHistory.Price10YList.Count; i++)
                    ptHistory.Price10XList.Add(i);

                for (int i = 1; i <= ptHistory.Volume10YList.Count; i++)
                    ptHistory.Volume10XList.Add(i);

                ptHistory.PriceHistory = historyStack.ToList();
                ptHistory.SkenderHistory = skenderHistoryList.AsEnumerable();
                return ptHistory;
            }
            else
            {
                return new PTHistory();
            }
        }

        /// <summary>
        /// Private helper for processing a single OHCLV data blob into our objects
        /// </summary>
        /// <param name="curData"></param>
        /// <returns></returns>
        private static (Skender.Stock.Indicators.Quote? SkenderObject, PTDay? Day) ProcessSingleDay(JToken curData)
        {
            // Exception protection
            bool canParse = curData["o"] != null && curData["c"] != null && curData["v"] != null;
            if (!canParse)
                return (null, null);

            // Parse OHCLV data into decimals
            decimal open = Convert.ToDecimal(curData["o"].ToString());
            // Can swap below with VWAP for Pro-Trades: Experimental Mode
            decimal close = Convert.ToDecimal(curData["c"].ToString());
            decimal high = Convert.ToDecimal(curData["h"].ToString());
            decimal low = Convert.ToDecimal(curData["l"].ToString());
            decimal volume = Convert.ToDecimal(curData["v"].ToString());
            decimal vwap = Convert.ToDecimal(curData["vw"].ToString());
            DateTime date = DateTime.Parse(curData["t"].ToString());

            // Make history object for processing indicators with Skender's lib
            Skender.Stock.Indicators.Quote curHistoryObj = new();
            curHistoryObj.Open = open;
            curHistoryObj.Close = close;
            curHistoryObj.High = high;
            curHistoryObj.Low = low;
            curHistoryObj.Volume = volume;
            curHistoryObj.Date = date;

            // Make PTDay history object for other model data processing
            PTDay ptDay = new()
            {
                PriceOpen = open,
                PriceClose = close,
                PriceLow = low,
                PriceHigh = high,
                PriceVwap = vwap,
                Volume = volume,
                PassVolumeFilter = false,
                RecordDate = date
            };
            ptDay.PriceCandleMean = (open + close + high + low + vwap) / 5.0M;
            ptDay.DollarVolume = vwap * volume;

            ptDay.TradedForward = vwap > open;
            ptDay.TradedForwardChange = vwap - open;
            ptDay.TradedForwardChangePercent = ((vwap - open) / open) * Constants.HUNDRED;

            ptDay.ClosedGreen = close >= open;
            ptDay.PriceChange = close - open;
            ptDay.PriceChangePercent = ((close - open) / open) * Constants.HUNDRED;

            return (curHistoryObj, ptDay);
        }

        public static decimal GetFibExtPriceTarget(decimal hh30d, decimal ll30d, decimal al10d, bool isShort)
        {
            decimal priceDiff = isShort ? (hh30d - ll30d) * -1 : hh30d - ll30d;
            decimal fibExtTarget = al10d + (priceDiff * Constants.FIB);
            if (isShort && fibExtTarget < 0)
            {
                priceDiff = (hh30d - al10d) * -1;
                fibExtTarget = al10d + (priceDiff * Constants.FIB);
            }
            return fibExtTarget;
        }

        public static decimal GetBasicPriceTarget(decimal curPrice, bool isShort)
        {
            decimal priceDiff = isShort ? curPrice * Constants.TARGET_AVG_WEEK_DIFF_PERCENT * -1 :
                curPrice * Constants.TARGET_AVG_WEEK_DIFF_PERCENT;
            decimal backtestingSupportedTarget = curPrice + priceDiff;
            return backtestingSupportedTarget;
        }

        public static decimal GetPriceBuyTarget(PTHistory history)
        {
            PTDay yDay = history.PriceHistory[1];
            PTDay tDay = history.PriceHistory[0];
            return (yDay.PriceVwap + yDay.PriceCandleMean + tDay.PriceLow + yDay.PriceLow) / 4.0M;
        }

        public static void ComputePriceSellTargets(FundamentalsResult fundResult, PTHistory history)
        {
            // Long sell targets
            decimal priceSlopeProjection5d = history.TodayVwap + (fundResult.PriceSlope * Constants.FIVE);
            history.AddPriceTarget("Standard price slope projection target", priceSlopeProjection5d);

            decimal vwapSlopeProjection = history.TodayVwap + (fundResult.VwapSlope * (1 + Constants.FIB));
            history.AddPriceTarget("Average VWAP slope projection target", vwapSlopeProjection);

            decimal basicTarget = GetBasicPriceTarget(history.TodayVwap, false);
            history.AddPriceTarget("Pro-Trades basic price target", basicTarget);
            history.AddPriceTarget("Recent highest high price target", history.HighestHigh30Day);

            decimal fibExtPriceTarget =
                GetFibExtPriceTarget(history.HighestHigh30Day, history.LowestLow30Day, history.AverageLow10Day, false);
            history.AddPriceTarget("Fibonacci extension price target", fibExtPriceTarget);

            decimal avgPriceTarget = 0;
            foreach (var target in history.PriceTargets) { avgPriceTarget += target.TargetPrice; }
            avgPriceTarget = avgPriceTarget / Convert.ToDecimal(history.PriceTargets.Count);
            history.AddPriceTarget("Pro-Trades average price target", avgPriceTarget);

            // Short sell targets
            history.AddPriceTarget("Recent lowest low price target (short)", history.LowestLow30Day);

            decimal fibExtShort =
                GetFibExtPriceTarget(history.HighestHigh30Day, history.LowestLow30Day, history.AverageLow10Day, true);

            decimal fibExtPriceTargetShort = (fibExtShort + history.LowestLow30Day) / 2.0M;
            
            history.AddPriceTarget("Fibonacci extension modified price target (short)", fibExtPriceTargetShort);

            decimal compositeShortTarget = (history.HistoricalVwapYList[0] + history.LowestLow30Day + history.AverageLow10Day) / 3.0M;
            history.AddPriceTarget("Composite 30d VWAP, 30d low, 10d avg low price target (short)", compositeShortTarget);

            decimal basicTargetShort = GetBasicPriceTarget(history.TodayVwap, true);
            history.AddPriceTarget("Pro-Trades basic price target (short)", basicTargetShort);

            decimal avgShortTarget = (fibExtPriceTargetShort + compositeShortTarget + basicTargetShort) / 3.0M;
            history.AddPriceTarget("Pro-Trades average price target (short)", compositeShortTarget);

            history.PriceTargets = history.PriceTargets.OrderByDescending(x => x.TargetPrice).ToList();

            int len = history.PriceTargets.Count;
            history.PriceTargetProLong = 
                (history.PriceTargets[0].TargetPrice + history.PriceTargets[1].TargetPrice + history.PriceTargets[2].TargetPrice) / 3.0M;
            history.PriceTargetAvgLong = avgPriceTarget;
            history.PriceTargetProShort =
                (history.PriceTargets[len - 1].TargetPrice + history.PriceTargets[len - 2].TargetPrice + history.PriceTargets[len - 3].TargetPrice) / 3.0M;
            history.PriceTargetAvgShort = avgShortTarget;
        }
    }
}
