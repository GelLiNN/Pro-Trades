using Flurl.Util;
using Newtonsoft.Json;
using PT.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PT.Middleware
{
    public static class TipRanks
    {
        private static readonly string TipRanksBaseUrl = @"https://www.tipranks.com/api/stocks/";

        //I obtained the context for these IDs by comparing the TipRanks API data to the listed insider trades on their site
        //Use them in the GetData method to decipher the insider buys and sells
        private static readonly int InsiderInformativeBuyTypeId = 2;
        private static readonly int InsiderInformativeSellTypeId = 7;
        private static readonly int InsiderNonInformativeBuyTypeId = 4;
        private static readonly int InsiderNonInformativeSellTypeId = 51;

        public static string GetData(string symbol)
        {
            //Return a data object that contains:
            //Aggregated ratings data and ratings providers
            //Aggregated insider transactions data, insider names, insider ratings
            //Aggregated insitutional transactions data, hedge fund ratings, hedge fund names
            //Composite score of the above 3 items

            string responseString = String.Empty;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(TipRanksBaseUrl + "getData/" + symbol);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseString = reader.ReadToEnd();
                response.Close();
            }
            TipRanksDataResponse trResponse = JsonConvert.DeserializeObject<TipRanksDataResponse>(responseString);
            return "Price Target: " + trResponse.portfolioHoldingData.priceTarget
                + ", Best Price Target: " + trResponse.portfolioHoldingData.bestPriceTarget.ToString();
        }

        public static string GetSentiment(string symbol)
        {
            //Return a data object that contains:
            //total bullish sentiment
            //total bearish sentiment
            //Composite score of sentiment

            string responseString = String.Empty;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(TipRanksBaseUrl + "getNewsSentiments/?ticker=" + symbol);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseString = reader.ReadToEnd();
                response.Close();
            }
            TipRanksSentimentResponse trResponse = JsonConvert.DeserializeObject<TipRanksSentimentResponse>(responseString);

            int totalBuySentiments = 0;
            int totalSellSentiments = 0;
            int totalNeutralSentiments = 0;

            foreach (TipRanksSentimentResponse.Count c in trResponse.counts)
            {
                totalBuySentiments += c.buy != null ? (int)c.buy : 0;
                totalSellSentiments += c.sell != null ? (int)c.sell : 0;
                totalNeutralSentiments += c.neutral != null ? (int)c.neutral : 0;
            }

            List<string> sectorSiblings = new List<string>();
            
            foreach (TipRanksSentimentResponse.Sector sibling in trResponse.sector)
            {
                string res = string.Format("Sector Sibling: {0}, Symbol: {1}, BullishPercent: {2}, BearishPercent {3}",
                    sibling.companyName, sibling.ticker, sibling.bullishPercent, sibling.bearishPercent);
                sectorSiblings.Add(res);
            }

            string bsn = "Buy Sentiments: " + totalBuySentiments + ", Sell Sentiments: " + totalSellSentiments + ", Neutral Sentiments: " + totalNeutralSentiments;

            //Have to do these annoying null checks apparently
            string bullish = "Average Bullish Sentiment: ";
            string bearish = "Average Bearish Sentiment: ";
            if (trResponse.sentiment != null)
            {
                bullish += trResponse.sentiment.bullishPercent.ToString();
                bearish += trResponse.sentiment.bearishPercent.ToString();
            }
            else
            {
                bullish += "n/a";
                bearish += "n/a";
            }

            string sectorBullish = "Sector Average Bullish Sentiment: " + trResponse.sectorAverageBullishPercent;
            string sectorAverageNews = "Sector Average News Score: " + trResponse.sectorAverageNewsScore;
            string score = "Score: " + trResponse.score;

            return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}",
                bsn, bullish, bearish, sectorBullish, sectorAverageNews, String.Join("\n", sectorSiblings), score);
        }

        public static TipRanksTrendingCompany[] GetTrendingCompanies()
        {
            //Return a data object that contains:
            //all recent top stocks from TipRanks
            //other stuff maybe?

            string responseString = String.Empty;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(TipRanksBaseUrl + "gettrendingstocks/?daysago=30&which=most");
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseString = reader.ReadToEnd();
                response.Close();
            }
            TipRanksTrendingCompany[] trResponse = JsonConvert.DeserializeObject<TipRanksTrendingCompany[]>(responseString);
            return trResponse;
        }
    }
}
