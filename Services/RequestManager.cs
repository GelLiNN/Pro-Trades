using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PT.Models.RequestModels;
using System.Net;
using System.Text;

namespace PT.Services
{
    //Request Manager class
    public class RequestManager
    {
        //Settings
        private static readonly int MaxConcurrentRequests = 10;
        private static readonly int SleepInterval = 100; //MS

        //Globals
        public HashSet<Guid> _concurrentRequests;
        public Dictionary<int, string> _errors;
        public HttpClient _client { get; private set; }
        public ChromeDriver Driver { get; private set; }

        public RequestManager()
        {
            // Custom stuff
            _errors = new Dictionary<int, string>();
            _concurrentRequests = new HashSet<Guid>();

            // HttpClient initialization, optional handler with a cookie container
            //var handler = new HttpClientHandler
            //{
            //    UseCookies = false
            //};
            _client = new HttpClient();
            int timeoutMins = Program.Config.GetValue<int>("Custom:WebRequestTimeoutMinutes");
            _client.Timeout = TimeSpan.FromMinutes(timeoutMins);

            Driver = GetNewChromeDriver();

            // Selenium FireFoxDriver initialization
            //var options = new FirefoxOptions();
            //options.AddArgument("--disable-gpu");
            //options.AddArgument("--width=100");
            //options.AddArgument("--height=100");
            //options.PageLoadStrategy = PageLoadStrategy.Eager;
            //options.SetPreference("permissions.default.image", 2);
            //options.SetPreference("browser.cache.disk.enable", false);
            //options.SetPreference("browser.cache.memory.enable", false);
            //options.SetPreference("dom.webnotifications.enabled", false);
            //options.SetPreference("dom.push.enabled", false);

            //var fds = FirefoxDriverService.CreateDefaultService();
            //Driver = new FirefoxDriver(options);
            //var processId = fds.ProcessId;
            //var process = Process.GetProcessById(processId);
            //ShowWindow(process.MainWindowHandle, SW_MINIMIZE);
        }

        /// <summary>
        /// Helper to get response string via normal http "GET" request with optional headers
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
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

            try
            {
                using (var response = _client.SendAsync(request).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                _errors.Add(_errors.Count, err);
                //_client.Dispose();
                return string.Empty;
            }
        }

        /// <summary>
        /// Helper to get HTTP response string via normal http "GET" request, with optional headers.
        /// </summary>
        /// <param name="uri">Formatted URL string with any query parameters</param>
        /// <param name="headers">Header key/value pairs</param>
        /// <returns>response body string</returns>
        public async Task<string> GetFromUriAsync(string uri, Dictionary<string, string>? headers = null)
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
            using (var response = await _client.SendAsync(request))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                catch (Exception ex)
                {
                    string err = ex.Message;
                    _errors.Add(_errors.Count, err);
                    return string.Empty;
                }
            }
        }

        //Helper to complete web request and return response as string with appropriate throttling
        //Will require migrating HttpWebRequest to HttpClient
        public string CompleteThrottledRequest(string uri, string method, object content = null)
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
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
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

        public TipRanksDataResponse? ScrapeTipRanksUriSelenium(string uri)
        {
            try
            {
                Driver.Navigate().GoToUrl(uri);
                string pageSrc = Driver.PageSource;

                // Chrome: Find the <pre> tag in the body to get the JSON
                var jsonElement = Driver.FindElement(By.TagName("pre"));

                // Firefox: Find the div id 'json' to get 
                //var jsonElement = Driver.FindElement(By.Id("json"));

                string responseStr = jsonElement.Text;
                //fDriver.Close();
                TipRanksDataResponse trResponse = JsonConvert.DeserializeObject<TipRanksDataResponse>(responseStr);
                return trResponse;
            }
            catch (Exception ex)
            {
                //Driver.Close();
                //Driver = GetNewChromeDriver();
                string err = ex.Message;
                _errors.Add(_errors.Count, err);
                return null;
            }
        }

        private ChromeDriver GetNewChromeDriver()
        {
            // Selenium ChromeDriver initialization
            var options = new ChromeOptions();
            //options.SetPreference("javascript.enabled", true);
            //options.SetPreference("network.http.accept.default", "application/json");
            //options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-software-rasterizer");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1,1");
            return new ChromeDriver(options);
        }
    }
}
