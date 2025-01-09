using Newtonsoft.Json.Linq;
using PT.Models.RequestModels;
using PT.Services;
using Skender.Stock.Indicators;
using System.Xml;

namespace PT.Middleware
{
    public class Alpaca
    {
        // TODO: get price history from alpaca API https://docs.alpaca.markets/reference/stockbars
        // It has volume weighted prices for each day which is useful
        public static async Task<AlpacaHistory> GetHistoryAsync(RequestManager rm, string symbol, int days)
        {
            // You should be able to query data from various markets including US, HK, TW
            // The timezone here may or may not impact accuracy
            days *= -1;
            var historyStartTime = DateTime.Now.AddDays(days);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "accept", "application/json" },
                { Constants.ALPACA_KEY_ID, Program.Config.GetValue<string>(Constants.ALPACA_KEY_ID) },
                { Constants.ALPACA_SECRET_KEY, Program.Config.GetValue<string>(Constants.ALPACA_SECRET_KEY) },
            };

            // Format and make Alpaca history request
            string formattedStartDate = XmlConvert.ToString(historyStartTime, XmlDateTimeSerializationMode.Local);
            string uri = $"https://data.alpaca.markets/v2/stocks/bars?symbols={symbol}&timeframe=1Day&start={formattedStartDate}&limit=1000&adjustment=raw&feed=sip&sort=asc";
            string response = rm.GetFromUri(uri, headers);

            // Convert into AlpacaHistory with Stock.Indicators.Quote inside
            AlpacaHistory alpacaHistory = new();
            List<Skender.Stock.Indicators.Quote> historyList = new();
            JObject responseObj = JObject.Parse(response);
            JToken pathResult = responseObj.SelectToken($"bars.{symbol}");
            JArray historyArr = pathResult as JArray;

            // Averages we must compute
            decimal avgPrice30d = 0;
            decimal avgPrice10d = 0;
            decimal lastPriceVw = 0;
            decimal avgVol30d = 0;
            decimal avgVol10d = 0;
            decimal lastVol = 0;

            // This is where volume USD throughput filtering happens now
            bool usdVolumeQualified1d = false;
            int last10PassCount = 0;
            int last30PassCount = 0;

            for (int i = 0; i < historyArr.Count; i++)
            {
                // Make history object for processing indicators with Skender's lib
                Skender.Stock.Indicators.Quote curHistoryObj = new();
                var curData = historyArr[i];

                curHistoryObj.Open = Convert.ToDecimal(curData["o"].ToString());
                // Can swap this with VWAP for Pro-Trades: Experimental Mode
                curHistoryObj.Close = Convert.ToDecimal(curData["c"].ToString());
                curHistoryObj.High = Convert.ToDecimal(curData["h"].ToString());
                curHistoryObj.Low = Convert.ToDecimal(curData["l"].ToString());
                curHistoryObj.Volume = Convert.ToDecimal(curData["v"].ToString());
                curHistoryObj.Date = DateTime.Parse(curData["t"].ToString());
                historyList.Add(curHistoryObj);

                // History object added, the rest is custom
                var curVwap = Convert.ToDecimal(curData["vw"].ToString());
                var curVol = Convert.ToDecimal(curData["v"].ToString());
                var curVolUsd = curVwap * curVol;

                bool isLast30 = (historyArr.Count - (i + 1) < 30);
                bool isLast10 = (historyArr.Count - (i + 1) < 10);
                bool isLast = (historyArr.Count - (i + 1) == 0);

                if (isLast30)
                {
                    avgPrice30d += curVwap;
                    avgVol30d += curVol;
                    bool result30d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_30D_LIMIT;

                    if (result30d)
                    {
                        last30PassCount++;
                    }
                }
                if (isLast10)
                {
                    avgPrice10d += curVwap;
                    avgVol10d += curVol;
                    bool result10d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_10D_LIMIT;
                    if (result10d)
                    {
                        last10PassCount++;
                    }
                }
                if (isLast)
                {
                    lastPriceVw += curVwap;
                    lastVol += curVol;
                    usdVolumeQualified1d = curVolUsd >= Constants.DEFAULT_VOLUME_USD_1D_LIMIT;
                }
            }

            // Get Volume USD qualifying results
            alpacaHistory.Has30DayQualifiedVolume = last10PassCount >= Constants.DEFAULT_MIN_PASS_10D_LIMIT;
            alpacaHistory.Has10DayQualifiedVolume = last30PassCount >= Constants.DEFAULT_MIN_PASS_30D_LIMIT;
            alpacaHistory.Has1DayQualifiedVolume = usdVolumeQualified1d;

            // Compute final averages for 30d and 10d
            avgPrice30d = avgPrice30d / 30.0M;
            avgVol30d = avgVol30d / 30.0M;
            avgPrice10d = avgPrice10d / 10.0M;
            avgVol10d = avgVol10d / 10.0M;
            alpacaHistory.DollarVolumeToday = lastPriceVw * lastVol;
            alpacaHistory.DollarVolume10Day = avgPrice10d * avgVol10d;
            alpacaHistory.DollarVolume30Day = avgPrice30d * avgVol30d;

            // Make X and Y Lists
            alpacaHistory.PriceAvgYList.Add(avgPrice30d);
            alpacaHistory.PriceAvgYList.Add(avgPrice10d);
            alpacaHistory.PriceAvgYList.Add(lastPriceVw);
            for (int i = 1; i <= alpacaHistory.PriceAvgYList.Count; i++)
                alpacaHistory.PriceAvgXList.Add(i);

            alpacaHistory.VolAvgYList.Add(avgVol30d);
            alpacaHistory.VolAvgYList.Add(avgVol10d);
            alpacaHistory.VolAvgYList.Add(lastVol);
            for (int i = 1; i <= alpacaHistory.VolAvgYList.Count; i++)
                alpacaHistory.VolAvgXList.Add(i);

            alpacaHistory.PriceHistory = historyList.AsEnumerable();
            return alpacaHistory;
        }
    }

    // TODO: https://docs.alpaca.markets/docs/historical-option-data
    // Implement functions for accessing options data
}
