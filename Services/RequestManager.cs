using Microsoft.Playwright;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Communication;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using PT.Models.RequestModels;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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
        //public FirefoxDriver Driver { get; private set; }

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

            // Selenium ChromeDriver initialization
            // Configure Chrome options
            //var options = new ChromeOptions();
            //options.AddArgument("--headless"); // Run in headless mode
            //options.AddArgument("--disable-gpu"); // Recommended for Windows
            //options.AddArgument("--window-size=1920,1080"); // Set a virtual window size
            //options.AddArgument("--disable-blink-features=AutomationControlled");
            //_driver = new ChromeDriver();

            //var options = new FirefoxOptions();
            //options.AddArgument("--headless");
            //Driver = new FirefoxDriver(options);

            // Init hidden Chrome process first
            /*var psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = @"--remote-debugging-port=9222 " +
                @"--user-data-dir=C:\Temp\PlaywrightProfile " +
                @"--disable-gpu --disable-software-rasterizer --disable-dev-shm-usage",
            };
            Process.Start(psi);*/
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
                //request.Headers.Referrer = new Uri("https://www.tipranks.com");
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

        public TipRanksDataResponse? ScrapeTipRanksUriSelenium(string uri)
        {
            //var options = new FirefoxOptions();
            var options = new ChromeOptions();
            //options.SetPreference("javascript.enabled", true);
            //options.SetPreference("network.http.accept.default", "application/json");
            //options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-software-rasterizer");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1,1");
            //options.AddAdditionalOption("useAutomationExtension", false);
            //options.AddArgument("--disable-blink-features=AutomationControlled");
            //FirefoxDriver fDriver = new FirefoxDriver();
            ChromeDriver cDriver = new ChromeDriver(options);
            try
            {
                // Clear navigator.webdriver
                //((IJavaScriptExecutor)cDriver).ExecuteScript(
                //    "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                cDriver.Navigate().GoToUrl(uri);
                //cDriver.Manage().Window.Minimize();

                // Find the <pre> tag in the body to get the JSON
                string pageSrc = cDriver.PageSource;
                var preElement = cDriver.FindElement(By.TagName("pre"));
                string responseStr = preElement.Text;
                cDriver.Close();
                TipRanksDataResponse trResponse = JsonConvert.DeserializeObject<TipRanksDataResponse>(responseStr);
                return trResponse;
            }
            catch (Exception ex)
            {
                cDriver.Close();
                string err = ex.Message;
                _errors.Add(_errors.Count, err);
                return null;
            }
        }

        public TipRanksDataResponse? ScrapeTipRanksUriPlaywright1(string uri)
        {
            // Initialize Playwright
            using var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

            // Launch a headless Chromium browser
            var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false, // Set to false for debugging
                Args = new[]
                {
                    "--disable-gpu",                       // disable GPU acceleration
                    "--disable-software-rasterizer",       // disable software rasterizer
                    "--disable-dev-shm-usage",             // avoid shared memory issues
                    "--disable-extensions",                // disable extensions
                    "--disable-background-networking",     // reduce background tasks
                    "--disable-background-timer-throttling",
                    "--disable-renderer-backgrounding",
                    "--mute-audio"                         // disable audio
                }
                //SlowMo = 50 // Optional: slows actions for visibility
            }).GetAwaiter().GetResult();
            /*var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--headless=new" } // stealthier headless mode
            }).GetAwaiter().GetResult();*/
            try
            {
                // Create a new browser page
                var page = browser.NewPageAsync().GetAwaiter().GetResult();
                page.SetViewportSizeAsync(1, 1).GetAwaiter().GetResult();
                // Navigate to target website
                page.GotoAsync(uri).GetAwaiter().GetResult();
                /*page.GotoAsync(uri, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 20000 // 20 seconds
                }).GetAwaiter().GetResult();*/

                // Extract page title
                //string htmlContent = page.ContentAsync().GetAwaiter().GetResult();
                string? jsonContent = page.TextContentAsync("pre").GetAwaiter().GetResult();
                TipRanksDataResponse trResponse = JsonConvert.DeserializeObject<TipRanksDataResponse>(jsonContent);
                browser.DisposeAsync().GetAwaiter().GetResult();
                return trResponse;
            }
            catch (Exception ex)
            {
                browser.DisposeAsync().GetAwaiter().GetResult();
                string err = ex.Message;
                _errors.Add(_errors.Count, err);
                return null;
            }
        }

        public TipRanksDataResponse? ScrapeTipRanksUriPlaywright2(string uri)
        {
            // Initialize Playwright
            using var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

            // Launch a headless Chromium browser
            var browser = playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222").GetAwaiter().GetResult();
            try
            {
                // Create a new browser page
                var page = browser.NewPageAsync().GetAwaiter().GetResult();
                page.SetViewportSizeAsync(1, 1).GetAwaiter().GetResult();
                // Navigate to target website
                page.GotoAsync(uri).GetAwaiter().GetResult();
                /*page.GotoAsync(uri, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 20000 // 20 seconds
                }).GetAwaiter().GetResult();*/

                // Extract page title
                //string htmlContent = page.ContentAsync().GetAwaiter().GetResult();
                string? jsonContent = page.TextContentAsync("pre").GetAwaiter().GetResult();
                TipRanksDataResponse trResponse = JsonConvert.DeserializeObject<TipRanksDataResponse>(jsonContent);
                browser.DisposeAsync().GetAwaiter().GetResult();
                return trResponse;
            }
            catch (Exception ex)
            {
                browser.DisposeAsync().GetAwaiter().GetResult();
                string err = ex.Message;
                _errors.Add(_errors.Count, err);
                return null;
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
    }
}
