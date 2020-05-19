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

        public static TipRanksResult GetData(string symbol)
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

            //filter results to the last 3 months
            DateTime startDate = DateTime.Now.AddMonths(-3);
            List<Insider> insiders = trResponse.insiders
                .Where(x => DateTime.Compare(x.rDate, startDate) > 0)
                .ToList();
            List<ConsensusOverTime> bsns = trResponse.consensusOverTime
                .Where(x => DateTime.Compare(x.date, startDate) > 0)
                .ToList();
            List<Expert> ratings = trResponse.experts
                .Where(x => DateTime.Compare(x.ratings.FirstOrDefault().date, startDate) > 0 && x.rankings.FirstOrDefault().stars > 0)
                .ToList();


            //find the slope of the buy and sell consensuses (net)
            List<decimal> bsnYList = bsns
                .Select(x => Convert.ToDecimal(x.consensus - 3 + x.buy - x.sell))
                .ToList();

            List<decimal> bsnXList = new List<decimal>();
            int counter = 1;
            foreach (decimal bsn in bsnYList)
            {
                bsnXList.Add(counter);
                counter++;
            }

            decimal bsnSlope = TwelveData.GetSlope(bsnXList, bsnYList);
            decimal bsnSlopeMultiplier = 10.0M; //TwelveData.GetSlopeMultiplier(bsnSlope);

            //find the average rating
            List<decimal> ratingNums = ratings
                .Select(x => Convert.ToDecimal(x.rankings.FirstOrDefault().stars))
                .ToList();

            decimal averageRating = ratingNums.Sum() / ratingNums.Count;

            decimal priceTarget = Convert.ToDecimal(trResponse.portfolioHoldingData.priceTarget);
            decimal lastPrice = Convert.ToDecimal(trResponse.prices[trResponse.prices.Length - 1].p);

            decimal ratingsComposite = 0;
            ratingsComposite += (averageRating / 6.0M) * 100; //Get score using the average rating as a percentage of (max rating + 1)
            ratingsComposite += bsnSlope > 0 ? (bsnSlope * bsnSlopeMultiplier) + 5 : -(bsnSlope * bsnSlopeMultiplier) - 5;
            ratingsComposite += priceTarget > lastPrice ? 10 : -10; //Adjust with price target
            ratingsComposite = Math.Min(ratingsComposite, 100);

            return new TipRanksResult
            {
                Insiders = insiders,
                Institutions = trResponse.hedgeFundData,
                ThirdPartyRatings = ratings,
                ConsensusOverTime = bsns,
                InsiderComposite = 0.0M,
                InstitutionalComposite = 0.0M,
                RatingsComposite = ratingsComposite,
                PriceTarget = priceTarget
            };
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
            string score = "TipRanks Score: " + trResponse.score;

            //TODO: formulate into Sentiment Analysis Composite Score
            //TODO: do stuff with the word clouds possibly
            //TODO: see if I can adjust the params on the API call to get better data back

            return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                bsn, bullish, bearish, sectorBullish, sectorAverageNews, score);
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
