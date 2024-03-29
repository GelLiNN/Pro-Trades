﻿using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace PT.Services
{
    //Request Manager class
    public class RequestManager
    {
        //Settings
        private static readonly int MaxConcurrentRequests = 10;
        private static readonly int SleepInterval = 100; //MS
        private static readonly string InRiverUrl = @"https://apiuse.productmarketingcloud.com/api/v1.0.0/";

        //Globals
        public HashSet<Guid> _concurrentRequests;
        public HttpClient _client { get; private set; }

        public RequestManager()
        {
            _concurrentRequests = new HashSet<Guid>();
            _client = new HttpClient();

            int timeoutMins = Program.Config.GetValue<int>("Custom:WebRequestTimeoutMinutes");
            _client.Timeout = TimeSpan.FromMinutes(timeoutMins);
        }

        // Helper to get response string via normal http "GET" request
        /*public string GetFromUri(string uri)
        {
            using HttpResponseMessage response = _client.GetAsync(uri).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return responseBody;
        }*/

        // Helper to get response string via normal http "GET" request with optional headers
        public string GetFromUri(string uri, Dictionary<string, string>? headers = null)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    request.Headers.Add(key, headers[key]);
                }
            }
            using (var response = _client.SendAsync(request).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return responseBody;
            }
        }

        //Helper to complete web request and return response as string with appropriate throttling
        public string CompletePimRequest(string path, string method, object content = null)
        {
            string responseString = "";
            try
            {
                //Wait if there are already max concurrent requests
                while (_concurrentRequests.Count >= MaxConcurrentRequests)
                {
                    Thread.Sleep(SleepInterval);
                }

                Guid requestId = Guid.NewGuid();
                lock (_concurrentRequests) { _concurrentRequests.Add(requestId); }

                string key = Program.Config.GetValue<string>("InRiverApiKey");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(InRiverUrl + path);
                request.Accept = "application/json";
                request.ContentType = "application/json";
                method = method.ToUpper();
                request.Method = method;
                request.Headers.Add("X-inRiver-APIKey", key);

                if ((method == "POST" || method == "PUT" || method == "DELETE") && content != null)
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        string requestStr = JsonConvert.SerializeObject(content);
                        streamWriter.Write(requestStr);
                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                    response.Close();
                }

                lock (_concurrentRequests) { _concurrentRequests.Remove(requestId); }
            }
            catch (WebException we)
            {
                responseString += "{ Exception: " + we.Message + ", StackTrace: " + we.StackTrace + "}";
            }
            return responseString;
        }
    }
}
