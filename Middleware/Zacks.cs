using System.Net;
using System.Text;

namespace PT.Middleware
{
    public static class Zacks
    {
        private static readonly string ZacksBaseUrl = @"https://quote-feed.zacks.com/";

        public static string GetZacksRank(string symbol)
        {
            string responseString = String.Empty;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(ZacksBaseUrl + symbol);
            //request.Accept = "application/json";
            //request.ContentType = "application/json";
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseString = reader.ReadToEnd();
                response.Close();
            }
            return responseString;
        }
    }
}
