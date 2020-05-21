using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PT.Middleware;
using PT.Node;
using PT.Models;
using YahooFinanceApi;

namespace PT.Controllers
{
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;

        private static DataCache _cache;

        private readonly NodeInterop _node;

        public SearchController(ILogger<SearchController> logger, DataCache cache, NodeInterop node)
        {
            _logger = logger;
            _cache = cache;
            _node = node;
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
            Security quote = YahooFinance.GetQuoteAsync(symbol).Result;
            return Indicators.GetCompositeScoreResult(symbol, quote); //no indicator API needed
        }

        //Used internally for cache loading
        public static CompositeScoreResult GetCompositeScoreInternal(string symbol, Security quote)
        {
            return Indicators.GetCompositeScoreResult(symbol, quote); //no indicator API needed
        }


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

        [HttpGet("/GetCompositeScoreTD/{symbol}")]
        public CompositeScoreResult GetCompositeScoreTD(string symbol)
        {
            Security quote = YahooFinance.GetQuoteAsync(symbol).Result;
            return TwelveData.GetCompositeScoreResult(symbol, quote);
        }

        //Used internally for cache loading
        public static CompositeScoreResult GetCompositeScoreInternalTD(string symbol, Security quote)
        {
            return TwelveData.GetCompositeScoreResult(symbol, quote);
        }

        /*
         * Investors Exchange dependent endpoints
         */
        [HttpGet("/GetCompanyStatsIEX/{symbol}")]
        public CompanyStatsIEX GetCompanyStatsIEX(string symbol)
        {
            return IEX.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteIEX/{symbol}")]
        public IEXSharp.Model.Shared.Response.Quote GetQuoteIEX(string symbol)
        {
            return IEX.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompaniesIEX")]
        public CompaniesListIEX GetAllCompaniesIEX()
        {
            return IEX.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesIEX/{screenId}")]
        public CompaniesListIEX GetScreenedCompaniesIEX(string screenId)
        {
            CompaniesListIEX companies = IEX.GetAllCompaniesAsync().Result;
            return IEX.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

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
            return FMP.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesFMP/{screenId}")]
        public CompaniesListFMP GetScreenedCompaniesFMP(string screenId)
        {
            CompaniesListFMP companies = FMP.GetAllCompaniesAsync().Result;
            return FMP.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Yahoo Finance dependent endpoints
         */
        [HttpGet("/GetCompanyStatsYF/{symbol}")]
        public CompanyStatsYF GetCompanyStatsYF(string symbol)
        {
            return YahooFinance.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteYF/{symbol}")]
        public Security GetQuoteYF(string symbol)
        {
            return YahooFinance.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompaniesYF")]
        public CompaniesListYF GetAllCompaniesYF()
        {
            return YahooFinance.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesYF/{screenId}")]
        public CompaniesListYF GetScreenedCompaniesYF(string screenId)
        {
            CompaniesListYF companies = YahooFinance.GetAllCompaniesAsync().Result;
            return YahooFinance.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Cache related endpoints
         */
        [HttpGet("/GetCachedSymbolsYF")]
        public HashSet<string> GetCachedSymbolsYF()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            return cachedSymbols;
        }

        [HttpGet("/GetCachedSymbolsIEX")]
        public HashSet<string> GetCachedSymbolsIEX()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("iex-companies");
            return cachedSymbols;
        }

        [HttpGet("/GetCachedCompaniesYF")]
        public List<CompanyStatsYF> GetCachedCompaniesYF()
        {
            List<CompanyStatsYF> cachedCompanies = new List<CompanyStatsYF>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompanyStatsYF company = (CompanyStatsYF)_cache.Get(cacheKey);
                if (company != null)
                    cachedCompanies.Add(company);
            }
            return cachedCompanies;
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

        [HttpGet("/GetZacksRank/{symbol}")]
        public string GetZacksRank(string symbol)
        {
            //return _node.TestNodeInterop();
            //did not actually need Node Interop for this, but will keep around just in case.
            return Zacks.GetZacksRank(symbol.ToUpper());
        }

        [HttpGet("/GetTipRanksData/{symbol}")]
        public TipRanksResult GetTipRanksData(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetTipRanksResult(symbol.ToLower());
        }

        [HttpGet("/GetTipRanksSentiment/{symbol}")]
        public string GetTipRanksSentiment(string symbol)
        {
            //TipRanks takes lower case symbols
            return TipRanks.GetSentiment(symbol.ToLower());
        }

        [HttpGet("/GetTipRanksTrending")]
        public TipRanksTrendingCompany[] GetTipRanksTrending()
        {
            return TipRanks.GetTrendingCompanies();
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

            //Stuff from 2-4 Interview for Jasleen / Collabera
            //Interviewees Veronica and Brad

            //return string.Empty;
        }
    }
}
