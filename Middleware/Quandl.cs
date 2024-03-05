using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using PT.Models.RequestModels;

namespace PT.Middleware
{
    //QUANDL module useful for getting short interest data for a company
    //Mayble I should get more data from Q?

    //API Key https://www.quandl.com/account/profile
    //API docs https://docs.quandl.com/docs/in-depth-usage
    //FINRA Short Interest docs https://www.quandl.com/data/FINRA-Financial-Industry-Regulatory-Authority/usage/quickstart/api
    public class Quandl
    {
        private static readonly string APIKey = Program.Config.GetValue<string>("QuandlApiKey");
        private static readonly string QURI = "https://www.quandl.com/api/v3/datasets/";
        //private static readonly HttpClient Client = new HttpClient();

        //quandl codes for FINRA
        //FNSQ - Nasdaq
        //FNYX - NYSE
        //FORF - Other?
        public static readonly List<string> FinraCodes = new List<string> { "FNSQ", "FNYX", "FORF" };

        public static async Task<string> CompleteQuandlRequest(string function, string path)
        {
            try
            {
                //HttpWebRequest Implementation
                HttpWebRequest request = null;
                switch (function.ToUpper())
                {
                    case "SHORT":
                        request = (HttpWebRequest)HttpWebRequest.Create(String.Format(QURI + "{0}?apikey={1}", path, APIKey));
                        request.Accept = "application/json";
                        request.ContentType = "application/json";
                        request.Method = "GET";
                        break;
                }

                string responseStr = String.Empty;
                if (request != null)
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        responseStr = reader.ReadToEnd();
                        response.Close();
                    }
                }
                return await Task.FromResult(responseStr);

                //HttpClient Implementation, could not use because of headers...
                /*HttpResponseMessage response = new HttpResponseMessage();
                switch (function.ToUpper())
                {
                    case "SHORT":
                        response = await Client.GetAsync(String.Format(QURI + "{0}?apikey={1}", path, APIKey));
                        break;
                }
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return await Task.FromResult(responseBody);*/
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION Message: {0}, StackTrace: {1}", e.Message, e.StackTrace);
                return string.Empty;
            }
        }

        public static ShortInterestResult GetShortInterest(string quandlResponse, int daysToCalculate)
        {
            decimal compositeScore = 0;
            JObject response = JObject.Parse(quandlResponse);
            JObject dataset = (JObject)response.GetValue("dataset");
            JArray data = (JArray)dataset.GetValue("data");

            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();
            Stack<decimal> shortInterestYList = new Stack<decimal>();

            decimal shortInterestToday = 0;
            decimal totalVolume = 0;
            decimal totalVolumeShort = 0;
            foreach (JArray result in data)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal shortVal = decimal.Parse(result[1].Value<string>());
                    decimal shortExemptVal = decimal.Parse(result[2].Value<string>());
                    decimal shortVolume = shortVal + shortExemptVal;
                    decimal volume = decimal.Parse(result[3].Value<string>());
                    decimal shortInterest = (shortVolume / volume) * 100;

                    if (numberOfResults == 0)
                        shortInterestToday = shortInterest;

                    shortInterestYList.Push(shortInterest);
                    totalVolume += volume;
                    totalVolumeShort += shortVolume;
                    numberOfResults++;

                    string shortDate = DateTime.Parse(result[0].Value<string>()).ToString("yyyy-MM-dd");
                    if (!dates.Contains(shortDate))
                    {
                        dates.Add(shortDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> shortXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                shortXList.Add(i);

            List<decimal> shortYList = shortInterestYList.ToList();
            decimal shortSlope = Indicators.GetSlope(shortXList, shortYList);
            decimal shortSlopeMultiplier = Indicators.GetSlopeMultiplier(shortSlope);
            decimal shortInterestAverage = (totalVolumeShort / totalVolume) * 100;

            //Add these bonuses to account for normal short interest fluctuations
            bool slightlyBearish = (0.0M <= shortSlope && shortSlope <= 0.5M);
            bool moderatelyBearish = (0.5M <= shortSlope && shortSlope <= 1.0M);

            //calculate composite score based on the following values and weighted multipliers
            compositeScore += 100 - shortInterestAverage; //get score as 100 - short interest
            compositeScore += (shortSlope < 0) ? (shortSlope * shortSlopeMultiplier) + 20 : -5;
            compositeScore += (shortSlope > 0 && slightlyBearish) ? 15 : 0;
            compositeScore += (shortSlope > 0 && moderatelyBearish) ? 10 : 0;

            //Return ShortInterestResult
            return new ShortInterestResult
            {
                TotalVolume = totalVolume,
                TotalVolumeShort = totalVolumeShort,
                ShortInterestPercentToday = shortInterestToday,
                ShortInterestPercentAverage = shortInterestAverage,
                ShortInterestSlope = shortSlope,
                ShortInterestCompositeScore = compositeScore
            };
        }
    }
}
