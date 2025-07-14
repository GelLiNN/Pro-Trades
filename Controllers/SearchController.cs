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
            return Indicators.GetCompositeScoreResult(symbol, _rm);
        }

        // For getting backtesting data for single composite
        [HttpGet("api/search/BacktestComposite/{composite}/{days}")]
        public object GetBacktestingData(string composite, int days)
        {
            composite = composite.ToUpper();
            var result = BacktestingHelper.BacktestSingleComposite(composite, days, _rm);
            return result;
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

        [HttpGet("api/search/GetIncidenceView")]
        public IncidenceViewResult GetIncidenceView()
        {
            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            int scoreCount = cachedSymbols.Count;
            if (scoreCount > 0)
            {
                int disqualifiedCount = 0;
                int shortCount = 0;
                int badCount = 0;
                int neutralCount = 0;
                int fairCount = 0;
                int goodCount = 0;
                int primeCount = 0;
                int earningsCount = 0;
                foreach (string cacheKey in cachedSymbols)
                {
                    CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);

                    if (companyScore != null && companyScore.CompositeScoreRank.StartsWith(Constants.RANK_DISQUALIFIED))
                    {
                        disqualifiedCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank.EndsWith(Constants.RANK_E))
                    {
                        earningsCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_SHORT)
                    {
                        shortCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_BAD)
                    {
                        badCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_NEUTRAL)
                    {
                        neutralCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_FAIR)
                    {
                        fairCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_GOOD)
                    {
                        goodCount++;
                    }
                    else if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_PRIME)
                    {
                        primeCount++;
                    }
                }
                return new IncidenceViewResult
                {
                    ScoreCount = scoreCount,
                    ScoreAttemptCount = _cache.ScrapedSymbolsAttempted,
                    DisqualifiedCount = disqualifiedCount,
                    DisqualifiedIncidenceRate = ((decimal)disqualifiedCount / (decimal)scoreCount) * 100,
                    EarningsCount = earningsCount,
                    EarningsIncidenceRate = ((decimal)earningsCount / (decimal)scoreCount) * 100,
                    ShortCount = shortCount,
                    ShortIncidenceRate = ((decimal)shortCount / (decimal)scoreCount) * 100,
                    BadCount = badCount,
                    BadIncidenceRate = ((decimal)badCount / (decimal)scoreCount) * 100,
                    NeutralCount = neutralCount,
                    NeutralIncidenceRate = ((decimal)neutralCount / (decimal)scoreCount) * 100,
                    FairCount = fairCount,
                    FairIncidenceRate = ((decimal)fairCount / (decimal)scoreCount) * 100,
                    GoodCount = goodCount,
                    GoodIncidenceRate = ((decimal)goodCount / (decimal)scoreCount) * 100,
                    PrimeCount = primeCount,
                    PrimeIncidenceRate = ((decimal)primeCount / (decimal)scoreCount) * 100,
                };
            }
            return new IncidenceViewResult();
        }

        // Main endpoint for getting all prediction scores in the entire set
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

        // Endpoint for getting prime predictions ordered descending
        [HttpGet("api/search/GetPrimes")]
        public List<CompositeScoreResult> GetCachedPrimesYF()
        {
            List<CompositeScoreResult> cachedPrimes = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_PRIME)
                {
                    cachedPrimes.Add(companyScore);
                }
            }
            cachedPrimes = cachedPrimes.OrderByDescending(x => x.CompositeScoreValue).ToList();
            return cachedPrimes;
        }

        // Endpoint for getting good predictions ordered descending
        [HttpGet("api/search/GetGoods")]
        public List<CompositeScoreResult> GetCachedGoodsYF()
        {
            List<CompositeScoreResult> cachedGoods = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_GOOD)
                {
                    cachedGoods.Add(companyScore);
                }
            }
            cachedGoods = cachedGoods.OrderByDescending(x => x.CompositeScoreValue).ToList();
            return cachedGoods;
        }

        // Endpoint for getting short predictions ordered ascending
        [HttpGet("api/search/GetShorts")]
        public List<CompositeScoreResult> GetShortsYF()
        {
            List<CompositeScoreResult> cachedShorts = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == Constants.RANK_SHORT)
                {
                    cachedShorts.Add(companyScore);
                }
            }
            cachedShorts = cachedShorts.OrderBy(x => x.CompositeScoreValue).ToList();
            return cachedShorts;
        }

        // Endpoint for getting all predictions with earnings during attrition
        [HttpGet("api/search/GetEarnings")]
        public List<CompositeScoreResult> GetEarnings()
        {
            List<CompositeScoreResult> cachedEarnings = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && !companyScore.CompositeScoreRank.StartsWith(Constants.RANK_DISQUALIFIED)
                    && companyScore.CompositeScoreRank.EndsWith(Constants.RANK_E))
                {
                    cachedEarnings.Add(companyScore);
                }
            }
            cachedEarnings = cachedEarnings.OrderByDescending(x => x.CompositeScoreValue).ToList();
            return cachedEarnings;
        }

        // Endpoint for getting top 20 HS1 predictions ordered descending
        [HttpGet("api/search/GetTopTwentyHS1")]
        public List<CompositeScoreResult> GetTopTwentyHS1YF()
        {
            List<CompositeScoreResult> cachedTopTwentyClean = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == "PRIME" && companyScore.ParameterSet.Type == Constants.HS1
                    && companyScore.FundamentalsComposite != Constants.CORE_INVALID_COMP)
                {
                    cachedTopTwentyClean.Add(companyScore);
                }
                else if (companyScore != null && companyScore.CompositeScoreRank == "GOOD" && companyScore.ParameterSet.Type == Constants.HS1
                    && companyScore.FundamentalsComposite != Constants.CORE_INVALID_COMP)
                {
                    cachedTopTwentyClean.Add(companyScore);
                }
            }
            cachedTopTwentyClean = cachedTopTwentyClean.OrderByDescending(x => x.CompositeScoreValue).ToList();
            cachedTopTwentyClean = cachedTopTwentyClean.Take(20).ToList();
            return cachedTopTwentyClean;
        }

        // Endpoint for getting top 20 clean predictions without invalid hot swaps
        [HttpGet("api/search/GetTopTwentyClean")]
        public List<CompositeScoreResult> GetTopTwentyCleanYF()
        {
            List<CompositeScoreResult> cachedTopTwentyClean = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == "PRIME" && companyScore.RatingsComposite != Constants.CORE_INVALID_COMP
                    && companyScore.FundamentalsComposite != Constants.CORE_INVALID_COMP)
                {
                    cachedTopTwentyClean.Add(companyScore);
                }
                else if (companyScore != null && companyScore.CompositeScoreRank == "GOOD" && companyScore.RatingsComposite != Constants.CORE_INVALID_COMP
                    && companyScore.FundamentalsComposite != Constants.CORE_INVALID_COMP)
                {
                    cachedTopTwentyClean.Add(companyScore);
                }
            }
            cachedTopTwentyClean = cachedTopTwentyClean.OrderByDescending(x => x.CompositeScoreValue).ToList();
            cachedTopTwentyClean = cachedTopTwentyClean.Take(20).ToList();
            return cachedTopTwentyClean;
        }

        // Endpoint for getting bottom 20 predictions
        [HttpGet("api/search/GetBottomTwenty")]
        public List<CompositeScoreResult> GetBottomTwentyYF()
        {
            List<CompositeScoreResult> cachedBottomTwenty = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == "BAD")
                {
                    cachedBottomTwenty.Add(companyScore);
                }
            }
            cachedBottomTwenty = cachedBottomTwenty.OrderBy(x => x.CompositeScoreValue).ToList();
            cachedBottomTwenty = cachedBottomTwenty.Take(20).ToList();
            return cachedBottomTwenty;
        }

        // Endpoint for getting bottom 20 clean predictions without invalid hot swaps
        [HttpGet("api/search/GetBottomTwentyClean")]
        public List<CompositeScoreResult> GetBottomTwentyCleanYF()
        {
            List<CompositeScoreResult> cachedBottomTwenty = new List<CompositeScoreResult>();

            HashSet<string> cachedSymbols = _cache.GetCachedSymbols("yf-companies");
            foreach (string cacheKey in cachedSymbols)
            {
                CompositeScoreResult companyScore = (CompositeScoreResult)_cache.Get(cacheKey);
                if (companyScore != null && companyScore.CompositeScoreRank == "BAD" && companyScore.RatingsComposite != Constants.CORE_INVALID_COMP
                    && companyScore.FundamentalsComposite != Constants.CORE_INVALID_COMP)
                {
                    cachedBottomTwenty.Add(companyScore);
                }
            }
            cachedBottomTwenty = cachedBottomTwenty.OrderBy(x => x.CompositeScoreValue).ToList();
            cachedBottomTwenty = cachedBottomTwenty.Take(20).ToList();
            return cachedBottomTwenty;
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
        public YahooQuotesApi.Snapshot GetQuoteYF(string symbol)
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
                CompositeScoreValue = (adxCompositeScore + aroonCompositeScore + macdCompositeScore + shortResult.ShortInterestComposite) / 4,
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
