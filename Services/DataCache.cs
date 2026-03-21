using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using PT.Core;
using PT.Middleware;
using PT.Models.RequestModels;
using YahooQuotesApi;

namespace PT.Services
{
    // MemoryCache wrapper for storing unique cache keys and data objects in-memory
    public class DataCache
    {
        protected RequestManager _rm;

        public enum Channels
        {
            [Description("iex")] IEX,
            [Description("yf")] YF
        }

        public enum CacheTypes
        {
            [Description("companies")] Companies
        }

        // Settings
        private static readonly int MaxCacheUpdateAge = 720; // Minutes
        private static readonly int MinCacheUpdateAge = 60; // Minutes
        private static readonly bool AutoUpdateDisabled = true;

        // Globals
        public HashSet<string> ScrapedSymbols; // For seeing progress while loading
        public int ScrapedSymbolsAttempted;
        public Dictionary<string, HashSet<string>> CachedSymbols; // CacheId => Cached Symbols
        public Dictionary<string, string> ExceptionReport;
        public List<string> CacheIds = new List<string> { "iex-companies", "yf-companies" };
        private IMemoryCache _iexCompaniesCache;
        private IMemoryCache _yfCompaniesCache;

        // Constructor with dependency injection
        public DataCache(RequestManager rm)
        {
            _rm = rm;
            _iexCompaniesCache = new MemoryCache(new MemoryCacheOptions());
            _yfCompaniesCache = new MemoryCache(new MemoryCacheOptions());

            ScrapedSymbols = new HashSet<string>();
            ScrapedSymbolsAttempted = 0;

            FINRA.LoadFinraCache(15, rm);

            CachedSymbols = new Dictionary<string, HashSet<string>>();
            foreach (string cacheId in CacheIds)
                CachedSymbols.Add(cacheId, new HashSet<string>());

            ExceptionReport = new Dictionary<string, string>();
        }

        // Get object by cacheKey <channel>-<cacheType>-<symbol>
        public object Get(string cacheKey)
        {
            string[] tokens = cacheKey.Split('-');
            string cacheId = tokens[0] + "-" + tokens[1];
            switch (cacheId)
            {
                case "iex-companies":
                    CompanyStatsIEX iexCompanyEntry;
                    if (!_iexCompaniesCache.TryGetValue(cacheKey, out iexCompanyEntry))
                    {
                        // Not in cache, so we can update the cache with private helper
                        UpdateIEXCompanyCacheEntry(cacheKey, null, EvictionReason.None, this); //DEPRECATED
                        _iexCompaniesCache.TryGetValue(cacheKey, out iexCompanyEntry);
                    }
                    return iexCompanyEntry;
                case "yf-companies":
                    CompositeScoreResult scoreCacheEntry;
                    if (!_yfCompaniesCache.TryGetValue(cacheKey, out scoreCacheEntry))
                    {
                        // Not in cache, so we can update the cache with private helper
                        UpdateYFCompanyCacheEntry(cacheKey, null, EvictionReason.None, this);
                        _yfCompaniesCache.TryGetValue(cacheKey, out scoreCacheEntry);
                    }
                    return scoreCacheEntry;
                default:
                    throw new Exception("Cache ID " + cacheId + " is not supported by Pro-Trades Cache.");
            }
        }

        // Yahoo Finance Company Cache Entry Update Routine
        public void UpdateYFCompanyCacheEntry(object key, object value, EvictionReason reason, object state = null)
        {
            try
            {
                string cacheKey = (string)key;
                string channel = cacheKey.Split('-')[0];
                string symbol = cacheKey.Split('-')[2];

                // Remove before updating and re-adding
                RemoveCachedSymbol(cacheKey);
                CompositeScoreResult result = Predictor.GetCompositeScoreResult(symbol, _rm);

                // Save score to cache
                if (result.ParameterSet != null)
                {
                    Add(result, cacheKey);
                }
                else
                {
                    Debug.WriteLine($"ERROR DataCache UpdateYFCompanyCacheEntry: {symbol} failed to score");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION DataCache UpdateYFCompanyCacheEntry: " + e.Message);
            }
            ScrapedSymbolsAttempted++;
        }

        // Add passed object to correct cache based on cacheKey <channel>-<cacheType>-<symbol>
        public void Add(object scorePacket, string cacheKey)
        {
            string[] tokens = cacheKey.Split('-');
            string cacheId = tokens[0] + "-" + tokens[1];

            // If you want the cache entry to expire
            int expirationMinutes = new Random().Next(MinCacheUpdateAge, MaxCacheUpdateAge);
            var expirationTime = DateTime.Now.Add(new TimeSpan(0, expirationMinutes, 0));
            var expirationToken = new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(expirationMinutes + .01)).Token);

            MemoryCacheEntryOptions options;
            if (AutoUpdateDisabled)
            {
                options = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove);
            }
            else
            {
                options = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove)
                    .SetAbsoluteExpiration(expirationTime)
                    .AddExpirationToken(expirationToken);
            }
            switch (cacheId)
            {
                case "iex-companies":
                    options.RegisterPostEvictionCallback(callback: UpdateIEXCompanyCacheEntry, state: this); //DEPRECATED
                    _iexCompaniesCache.Set(cacheKey, scorePacket, options);
                    break;
                case "yf-companies":
                    options.RegisterPostEvictionCallback(callback: UpdateYFCompanyCacheEntry, state: this);
                    _yfCompaniesCache.Set(cacheKey, scorePacket, options);
                    break;
                default:
                    throw new Exception("Cache ID " + cacheId + " is not supported by Pro-Trades Cache.");
            }
            string curSymbol = cacheKey.Split('-')[2];
            lock (CachedSymbols)
            {
                if (!CachedSymbols[cacheId].Contains(cacheKey))
                    CachedSymbols[cacheId].Add(cacheKey);
            }
        }

        // Investors Exchange Company Cache Entry Update Routine
        // DEPRECATED
        public void UpdateIEXCompanyCacheEntry(object key, object value, EvictionReason reason, object state = null)
        {
            try
            {
                Stopwatch sw = new Stopwatch(); sw.Start();
                
                string cacheKey = (string)key;
                string channel = cacheKey.Split('-')[0];
                string symbol = cacheKey.Split('-')[2];

                // Remove before updating and re-adding
                RemoveCachedSymbol(cacheKey);

                CompanyStatsIEX companyStats = new CompanyStatsIEX(); //IEX.GetCompanyStatsAsync(symbol).Result;

                // Save IEX Company to cache
                Add(companyStats, cacheKey);
                string perf = sw.ElapsedMilliseconds.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR UpdateIEXCompanyCacheEntry: " + e.Message);
            }
            ScrapedSymbolsAttempted++;
        }

        public void AddCachedSymbol(string cacheKey)
        {
            string[] tokens = cacheKey.Split('-');
            string cacheId = tokens[0] + "-" + tokens[1];
            lock (CachedSymbols)
            {
                if (!CachedSymbols[cacheId].Contains(cacheKey))
                    CachedSymbols[cacheId].Add(cacheKey);
            }
        }

        public void RemoveCachedSymbol(string cacheKey)
        {
            string[] tokens = cacheKey.Split('-');
            string cacheId = tokens[0] + "-" + tokens[1];
            lock (CachedSymbols)
            {
                if (CachedSymbols[cacheId].Contains(cacheKey))
                    CachedSymbols[cacheId].Remove(cacheKey);
            }
        }

        public HashSet<string> GetCachedSymbols(string cacheId)
        {
            lock (CachedSymbols)
            {
                HashSet<string> items = CachedSymbols[cacheId]
                    .Where(x => x.ToString().StartsWith(cacheId)).ToHashSet();
                return new HashSet<string>(items);
            }
        }

        public string GetCacheId(string channel, string type)
        {
            string id = channel.ToLower() + "-" + type.ToLower();
            foreach (string cacheId in CacheIds)
                if (id == cacheId)
                    return cacheId;
            return null;
        }

        // Scrape all symbols first, then start cache loading
        public async Task LoadCacheAsync(string cacheId)
        {
            await Task.Run(() =>
            {
                // Use DefaultCacheLimit from custom config
                int limit = Program.Config.GetValue<int>("Custom:DefaultCacheLimit");
                ScrapedSymbols = AssetAggregator.GetRandomizedCompanySymbols(_rm, limit);

                foreach (var scrapedSymbol in ScrapedSymbols)
                {
                    if (!CachedSymbols[cacheId].Contains(scrapedSymbol))
                    {
                        if (CachedSymbols["yf-companies"].Count < limit)
                        {
                            string cacheKey = string.Format("{0}-{1}", cacheId, scrapedSymbol);
                            Get(cacheKey);
                        }
                        else
                        {
                            break; // Quick stop cache loading
                        }
                    }
                }

                /*Parallel Edition
                Parallel.ForEach(ids, Common.ParallelOptions, (entityId) =>
                {
                    string cacheKey = string.Format("{0}-{1}", cacheId, entityId);
                    try
                    {
                        if (!this.CachedStyles.ContainsKey(cacheKey))
                        {
                            this.Get(cacheKey);
                            Interlocked.Increment(ref count);
                        }
                    }
                    catch (Exception e)
                    {
                        lock (ExceptionReport) { ExceptionReport.Add(cacheKey, "Error: " + e.Message + ", StackTrace: " + e.StackTrace); }
                        return;
                    }
                });*/

            }).ConfigureAwait(false);
        }

        // Other things we may want
        /*public void ClearAll()
        {
            this.CachedUpcs = new CustomDictionary<string, HashSet<string>>();
            this.CachedStyles = new CustomDictionary<string, string>();

            _cbcorporateProductsCache = new MemoryCache(new MemoryCacheOptions());
            _psProductsCache = new MemoryCache(new MemoryCacheOptions());
            _psPricesCache = new MemoryCache(new MemoryCacheOptions());
        }

        public void ClearCache(string cacheId)
        {
            cacheId = cacheId.ToLower();
            switch (cacheId)
            {
                case "cbcorporate-products":
                    _cbcorporateProductsCache = new MemoryCache(new MemoryCacheOptions());
                    break;
                case "promostandards-products":
                    _psProductsCache = new MemoryCache(new MemoryCacheOptions());
                    break;
                case "promostandards-prices":
                    _psPricesCache = new MemoryCache(new MemoryCacheOptions());
                    break;
                default:
                    throw new Exception("Cache ID " + cacheId + " is not supported by Pro-Trades Cache.");
            }
            foreach (string cacheKey in CachedStyles.Keys)
            {
                if (cacheKey.StartsWith(cacheId))
                    lock (CachedStyles) { CachedStyles.Remove(cacheKey); }
            }
            foreach (string cacheKey in CachedUpcs.Keys)
            {
                if (cacheKey.StartsWith(cacheId))
                    lock (CachedUpcs) { CachedUpcs.Remove(cacheKey); }
            }
        }*/
    }
}
