using Microsoft.AspNetCore.Mvc;
using PT.Middleware;
using PT.Models.RequestModels;
using PT.Services;

namespace PT.Controllers
{
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;
        private static DataCache _cache;
        protected static RequestManager _rm;

        public SearchController(ILogger<SearchController> logger, DataCache cache, RequestManager rm)
        {
            _logger = logger;
            _cache = cache;
            _rm = rm;
        }

        /*
         * Composite Score Endpoints
         */
        [HttpGet("api/search/GetCompanyStats/{symbol}")]
        public CompanyStats GetCompanyStats(string symbol)
        {
            symbol = symbol.ToUpper();
            return YahooFinance.GetCompanyStatsAsync(symbol, _rm).Result;
        }

        // TODO: update to add to cache if not present
        [HttpGet("api/search/GetCompositeScore/{symbol}")]
        public CompositeScoreResult GetCompositeScore(string symbol)
        {
            symbol = symbol.ToUpper();
            YahooQuotesApi.Security quote = YahooFinance.GetQuoteAsync(symbol).Result;
            return Indicators.GetCompositeScoreResult(symbol, quote, _rm);
        }

        /*
         * Cache related endpoints
         */
        [HttpGet("api/search/GetCacheView")]
        public CacheViewResult GetCacheView()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            return new CacheViewResult
            {
                CacheCount = cachedSymbols.Count,
                ScrapeCount = _cache.ScrapedSymbols.Count,
                ScrapeAttemptCount = _cache.ScrapedSymbolsAttempted,
                CacheKeys = cachedSymbols
            };
        }

        // Main endpoint for getting all company scores for now
        [HttpGet("api/search/DumpCache")]
        public List<CompositeScoreResult> DumpCache()
        {
            List<CompositeScoreResult> cachedScores = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult company = (CompositeScoreResult)_cache.Get(cacheKey);
                if (company != null)
                {
                    cachedScores.Add(company);
                }
            }
            return cachedScores;
        }

        // Endpoint for getting primes
        [HttpGet("api/search/GetCachedPrimes")]
        public List<CompositeScoreResult> GetCachedPrimesYF()
        {
            List<CompositeScoreResult> cachedPrimes = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeRank == "PRIME")
                {
                    cachedPrimes.Add(companyScore);
                }
            }
            return cachedPrimes;
        }

        /*
         * Alpaca related endpoints
         
        [HttpGet("/GetCompanyStatsA/{symbol}")]
        public CompanyStatsA GetCompanyStatsA(string symbol)
        {
            return Alpaca.GetCompanyStatsAsync(symbol, _rm).Result;
        }

        [HttpGet("/GetQuoteA/{symbol}")]
        public YahooQuotesApi.Security GetQuoteYF(string symbol)
        {
            return YahooFinance.GetQuoteAsync(symbol).Result;
        }

        // This is primamrily to test the cache functionality outside of a background task
        [HttpGet("/GetAllCompaniesA")]
        public CompaniesListYF GetAllCompaniesYF()
        {
            return Companies.GetAllCompaniesAsync(_rm).Result;
        }*/

        /*
         * Yahoo Finance related endpoints
         */
        [HttpGet("api/search/GetQuoteYF/{symbol}")]
        public YahooQuotesApi.Security GetQuoteYF(string symbol)
        {
            return YahooFinance.GetQuoteAsync(symbol).Result;
        }

        // This is primamrily to test the cache functionality outside of a background task
        [HttpGet("api/search/GetAllCompanies")]
        public CompaniesListYF GetAllCompaniesYF()
        {
            return Companies.GetAllCompaniesAsync(_rm).Result;
        }

        [HttpGet("api/search/GetScreenedCompaniesYF/{screenId}")]
        public CompaniesListYF GetScreenedCompaniesYF(string screenId)
        {
            CompaniesListYF companies = Companies.GetAllCompaniesAsync(_rm).Result;
            return YahooFinance.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        [HttpGet("api/search/GetCachedSymbolsIEX")]
        public HashSet<string> GetCachedSymbolsIEX()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("iex-companies");
            return cachedSymbols;
        }

        [HttpGet("api/search/GetCachedCompaniesIEX")]
        public List<CompanyStatsIEX> GetCachedCompaniesIEX()
        {
            List<CompanyStatsIEX> cachedCompanies = new List<CompanyStatsIEX>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("iex-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompanyStatsIEX company = (CompanyStatsIEX)_cache.Get(cacheKey);
                if (company != null)
                    cachedCompanies.Add(company);
            }
            return cachedCompanies;
        }

        /*
         * AlphaVantage related endpoints
         */
        [HttpGet("api/search/GetIndicatorAV/{function}/{symbol}/{days}")] //indicator == function
        public IActionResult GetIndicatorAV(string function, string symbol, string days)
        {
            int numOfDays = Int32.Parse(days);
            string avResponse = AlphaVantage.CompleteAlphaVantageRequest(function, symbol).Result;
            decimal avCompositeScore = AlphaVantage.GetCompositeScore(function, avResponse, numOfDays);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score for function " + function + ": " + avCompositeScore
            };
        }

        [HttpGet("api/search/GetCompositeScoreAV/{symbol}")]
        public CompositeScoreResult GetCompositeScoreAV(string symbol)
        {
            string adxResponse = AlphaVantage.CompleteAlphaVantageRequest("ADX", symbol).Result;
            decimal adxCompositeScore = AlphaVantage.GetCompositeScore("ADX", adxResponse, 7);
            string aroonResponse = AlphaVantage.CompleteAlphaVantageRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = AlphaVantage.GetCompositeScore("AROON", aroonResponse, 7);
            string macdResponse = AlphaVantage.CompleteAlphaVantageRequest("MACD", symbol).Result;
            decimal macdCompositeScore = AlphaVantage.GetCompositeScore("MACD", macdResponse, 7);

            ShortInterestResult shortResult = new ShortInterestResult(); //FINRA.GetShortInterest(symbol, 7);

            return new CompositeScoreResult
            {
                Symbol = symbol,
                DataProviders = "AlphaVantage",
                ADXComposite = adxCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                CompositeScoreValue = (adxCompositeScore + aroonCompositeScore + macdCompositeScore + shortResult.ShortInterestCompositeScore) / 4,
                ShortInterest = shortResult
            };
        }

        /*
         * TwelveData related endpoints
         */
        [HttpGet("api/search/GetIndicatorTD/{function}/{symbol}/{days}")] //indicator == function
        public IActionResult GetIndicatorTD(string function, string symbol, string days)
        {
            int numOfDays = Int32.Parse(days);
            string tdResponse = TwelveData.CompleteTwelveDataRequest(function, symbol).Result;
            decimal tdCompositeScore = TwelveData.GetCompositeScore(symbol, function, tdResponse, numOfDays);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score for function " + function + ": " + tdCompositeScore
            };
        }

        /*[HttpGet("/GetCompositeScoreTD/{symbol}")]
        public CompositeScoreResult GetCompositeScoreTD(string symbol)
        {
            Security quote = YahooFinance.GetQuoteAsync(symbol).Result;
            return TwelveData.GetCompositeScoreResult(symbol, quote);
        }

        //Used internally for cache loading
        public static CompositeScoreResult GetCompositeScoreInternalTD(string symbol, Security quote)
        {
            return TwelveData.GetCompositeScoreResult(symbol, quote);
        }*/

        /*
         * Financial Modeling Prep dependent endpoints
         */
        [HttpGet("api/search/GetCompanyStatsFMP/{symbol}")]
        public CompanyStatsFMP GetCompanyStatsFMP(string symbol)
        {
            return FMP.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("api/search/GetQuoteFMP/{symbol}")]
        public string GetQuoteFMP(string symbol)
        {
            return FMP.GetQuote(symbol);
        }

        [HttpGet("api/search/GetAllCompaniesFMP")]
        public CompaniesListFMP GetAllCompaniesFMP()
        {
            return FMP.GetAllCompaniesAsync(_rm).Result;
        }

        [HttpGet("api/search/GetScreenedCompaniesFMP/{screenId}")]
        public CompaniesListFMP GetScreenedCompaniesFMP(string screenId)
        {
            CompaniesListFMP companies = FMP.GetAllCompaniesAsync(_rm).Result;
            return FMP.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * ZacksRank related endpoints
         */
        [HttpGet("api/search/GetZacksRank/{symbol}")]
        public string GetZacksRank(string symbol)
        {
            //return _node.TestNodeInterop();
            //did not actually need Node Interop for this, but will keep around just in case.
            return Zacks.GetZacksRank(symbol.ToUpper());
        }

        /*
         * TipRanks related endpoints
         */
        [HttpGet("api/search/GetTipRanksData/{symbol}")]
        public HedgeFundsResult GetTipRanksData(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetTipRanksResult(symbol.ToLower(), _rm);
        }

        [HttpGet("api/search/GetTipRanksSentiment/{symbol}")]
        public string GetTipRanksSentiment(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetSentiment(symbol.ToLower(), _rm);
        }

        [HttpGet("api/search/GetTipRanksTrending")]
        public TipRanksTrendingCompany[] GetTipRanksTrending()
        {
            return TipRanks.GetTrendingCompanies(_rm);
        }
    }
}
