using Newtonsoft.Json;
using PT.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PT.Middleware
{
    public static class TipRanks
    {
        private static readonly string TipRanksBaseUrl = @"https://www.tipranks.com/api/stocks/";

        //I obtained the context for these IDs by comparing the TipRanks API data to the listed insider trades on their site
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
            TipRanksResponse tipRanksResponse = JsonConvert.DeserializeObject<TipRanksResponse>(responseString);
            return tipRanksResponse.portfolioHoldingData.bestPriceTarget.ToString();
        }

        /*public static string GetSentiment(string symbol)
        {
            //Return a data object that contains:
            //total bullish sentiment
            //total bearish sentiment
            //Composite score of sentiment
        }

        public static string GetTopStocks()
        {
            //Return a data object that contains:
            //all recent top stocks from TipRanks
            //other stuff maybe?
        }*/
    }
}
