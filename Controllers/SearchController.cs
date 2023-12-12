using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        [HttpGet("/Search")]
        public IActionResult Search()
        {
            return View("Search");
        }

        /*
         * Composite Score Endpoints
         */
        [HttpGet("/GetCompositeScore/{symbol}")]
        public CompositeScoreResult GetCompositeScore(string symbol)
        {
            symbol = symbol.ToUpper();
            YahooQuotesApi.Security quote = YahooFinance.GetQuoteAsync(symbol).Result;
            return Indicators.GetCompositeScoreResult(symbol, quote, _rm); //no indicator API needed
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
        [HttpGet("/GetCompanyStatsYF/{symbol}")]
        public CompanyStatsYF GetCompanyStatsYF(string symbol)
        {
            return YahooFinance.GetCompanyStatsAsync(symbol, _rm).Result;
        }

        // TODO: Fix this endpoint
        [HttpGet("/GetQuoteYF/{symbol}")]
        public YahooQuotesApi.Security GetQuoteYF(string symbol)
        {
            return YahooFinance.GetQuoteAsync(symbol).Result;
        }

        // This is primamrily to test the cache functionality outside of a background task
        [HttpGet("/GetAllCompaniesYF")]
        public CompaniesListYF GetAllCompaniesYF()
        {
            return Companies.GetAllCompaniesAsync(_rm).Result;
        }

        [HttpGet("/GetScreenedCompaniesYF/{screenId}")]
        public CompaniesListYF GetScreenedCompaniesYF(string screenId)
        {
            CompaniesListYF companies = Companies.GetAllCompaniesAsync(_rm).Result;
            return YahooFinance.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Cache related endpoints
         */
        [HttpGet("/GetCachedSymbolsYF")]
        public CacheViewResult GetCachedSymbolsYF()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            return new CacheViewResult
            {
                CacheCount = cachedSymbols.Count,
                ScrapeCount = _cache.ScrapedSymbols.Count,
                CacheKeys = cachedSymbols
            };
        }

        // Main endpoint for getting all scores for now
        [HttpGet("/GetCachedCompaniesYF")]
        public List<CompanyStatsYF> GetCachedCompaniesYF()
        {
            List<CompanyStatsYF> cachedCompanies = new List<CompanyStatsYF>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompanyStatsYF company = (CompanyStatsYF)_cache.Get(cacheKey);
                if (company != null)
                {
                    cachedCompanies.Add(company);
                }
            }
            return cachedCompanies;
        }

        // Endpoint for getting primes
        [HttpGet("/GetCachedPrimesYF")]
        public List<CompanyStatsYF> GetCachedPrimesYF()
        {
            List<CompanyStatsYF> cachedPrimes = new List<CompanyStatsYF>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompanyStatsYF company = (CompanyStatsYF)_cache.Get(cacheKey);
                if (company != null && company.CompositeScoreResult != null && company.CompositeScoreResult.CompositeRank == "PRIME")
                {
                    cachedPrimes.Add(company);
                }
            }
            return cachedPrimes;
        }

        [HttpGet("/GetCachedSymbolsIEX")]
        public HashSet<string> GetCachedSymbolsIEX()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("iex-companies");
            return cachedSymbols;
        }

        [HttpGet("/GetCachedCompaniesIEX")]
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
        [HttpGet("/GetIndicatorAV/{function}/{symbol}/{days}")] //indicator == function
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

        [HttpGet("/GetCompositeScoreAV/{symbol}")]
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
        [HttpGet("/GetIndicatorTD/{function}/{symbol}/{days}")] //indicator == function
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
        [HttpGet("/GetCompanyStatsFMP/{symbol}")]
        public CompanyStatsFMP GetCompanyStatsFMP(string symbol)
        {
            return FMP.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteFMP/{symbol}")]
        public string GetQuoteFMP(string symbol)
        {
            return FMP.GetQuote(symbol);
        }

        [HttpGet("/GetAllCompaniesFMP")]
        public CompaniesListFMP GetAllCompaniesFMP()
        {
            return FMP.GetAllCompaniesAsync(_rm).Result;
        }

        [HttpGet("/GetScreenedCompaniesFMP/{screenId}")]
        public CompaniesListFMP GetScreenedCompaniesFMP(string screenId)
        {
            CompaniesListFMP companies = FMP.GetAllCompaniesAsync(_rm).Result;
            return FMP.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * ZacksRank related endpoints
         */
        [HttpGet("/GetZacksRank/{symbol}")]
        public string GetZacksRank(string symbol)
        {
            //return _node.TestNodeInterop();
            //did not actually need Node Interop for this, but will keep around just in case.
            return Zacks.GetZacksRank(symbol.ToUpper());
        }

        /*
         * TipRanks related endpoints
         */
        [HttpGet("/GetTipRanksData/{symbol}")]
        public TipRanksResult GetTipRanksData(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetTipRanksResult(symbol.ToLower(), _rm);
        }

        [HttpGet("/GetTipRanksSentiment/{symbol}")]
        public string GetTipRanksSentiment(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetSentiment(symbol.ToLower(), _rm);
        }

        [HttpGet("/GetTipRanksTrending")]
        public TipRanksTrendingCompany[] GetTipRanksTrending()
        {
            return TipRanks.GetTrendingCompanies(_rm);
        }

        /*
         * Other endpoints
         */
        [HttpGet("/Test")]
        public string Test()
        {
            //Stuff from my codillity assessment for Sameksha
            //Job links

            /*int[] A = new int[5] { 1, 2, 3, 4, 5 };
            int[] sortA = new int[5];
            int len = A.Length;
            A.CopyTo(sortA, 0);
            Array.Sort(sortA);
            Console.WriteLine();

            HashSet<int> ints = new HashSet<int>();
            foreach (int val in A)
            {
                if (!ints.Contains(val))
                    ints.Add(val);
            }*/

            string s = "abc";
            char[] chars = s.ToCharArray();
            List<string> combos = new List<string>();
            for (int i = 0; i < chars.Length; i++)
            {
                char toRemove = chars[i];
                string combo = string.Empty;

                for (int y = 0; y < chars.Length; i++)
                {
                    char current = chars[y];
                    if (current != toRemove)
                    {
                        combo += current;
                    }
                }
                Console.WriteLine(combo);
                combos.Add(combo);
            }
            string[] combosArray = combos.ToArray();
            Array.Sort(combosArray);
            return String.Join(",", combosArray);
        }
    }
}
