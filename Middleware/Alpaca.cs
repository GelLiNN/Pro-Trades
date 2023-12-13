using Newtonsoft.Json.Linq;
using PT.Services;
using Skender.Stock.Indicators;
using System.Xml;

namespace PT.Middleware
{
    public class Alpaca
    {
        // TODO: get price history from alpaca API https://docs.alpaca.markets/reference/stockbars
        // It has volume weighted prices for each day which is useful
        public static async Task<Models.RequestModels.AlpacaHistory> GetHistoryAsync(RequestManager rm, string symbol, int days)
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
            Models.RequestModels.AlpacaHistory alpacaHistory = new Models.RequestModels.AlpacaHistory();
            List<Quote> historyList = new List<Quote>();
            JObject responseObj = JObject.Parse(response);
            JToken pathResult = responseObj.SelectToken($"bars.{symbol}");
            JArray historyArr = pathResult as JArray;

            // Averages we must compute
            decimal avgPrice30d = 0;
            decimal avgPrice10d = 0;
            decimal lastPrice = 0;
            decimal avgVol30d = 0;
            decimal avgVol10d = 0;
            decimal lastVol = 0;

            for (int i = 0; i < historyArr.Count; i++)
            {
                Quote curHistoryObj = new Quote();
                var curData = historyArr[i];

                bool isLast30 = (historyArr.Count - (i + 1) <= 30);
                bool isLast10 = (historyArr.Count - (i + 1) <= 10);
                bool isLast = (historyArr.Count - (i + 1) == 0);

                if (isLast30)
                {
                    avgPrice30d += Convert.ToDecimal(curData["vw"].ToString());
                    avgVol30d += Convert.ToDecimal(curData["v"].ToString());
                }
                if (isLast10)
                {
                    avgPrice10d += Convert.ToDecimal(curData["vw"].ToString());
                    avgVol10d += Convert.ToDecimal(curData["v"].ToString());
                }
                if (isLast)
                {
                    lastPrice += Convert.ToDecimal(curData["vw"].ToString());
                    lastVol += Convert.ToDecimal(curData["v"].ToString());
                }

                curHistoryObj.Open = Convert.ToDecimal(curData["o"].ToString());
                curHistoryObj.Close = Convert.ToDecimal(curData["c"].ToString());
                curHistoryObj.High = Convert.ToDecimal(curData["h"].ToString());
                curHistoryObj.Low = Convert.ToDecimal(curData["l"].ToString());
                curHistoryObj.Volume = Convert.ToDecimal(curData["v"].ToString());
                curHistoryObj.Date = DateTime.Parse(curData["t"].ToString());
                historyList.Add(curHistoryObj);
            }
            // Compute final averages
            avgPrice30d = avgPrice30d / 30.0M;
            avgVol30d = avgVol30d / 30.0M;
            avgPrice10d = avgPrice10d / 10.0M;
            avgVol10d = avgVol10d / 10.0M;
            alpacaHistory.VolumeUSD = lastPrice * lastVol;
            alpacaHistory.AverageVolumeUSD = avgPrice30d * avgVol30d;

            // Make X and Y Lists
            alpacaHistory.PriceAvgYList.Add(avgPrice30d);
            alpacaHistory.PriceAvgYList.Add(avgPrice10d);
            alpacaHistory.PriceAvgYList.Add(lastPrice);
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
}
