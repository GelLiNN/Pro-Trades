using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using PT.Models;

namespace PT.Middleware
{
    //MemoryCache wrapper for storing cache keys and otherwise assisting with cache functionality
    public class DataCache
    {

        public enum Channels
        {
            [Description("iex")] IEX,
            [Description("yf")] YF
        }

        public enum CacheTypes
        {
            [Description("companies")] Companies
        }

        //Settings
        private static readonly int MaxCacheUpdateAge = 720; //Minutes
        private static readonly int MinCacheUpdateAge = 60; //Minutes
        private static readonly bool AutoUpdateDisabled = true;

        //Globals
        public Dictionary<string, HashSet<string>> CachedSymbols; //CacheId => Cached Symbols
        public Dictionary<string, string> ExceptionReport;
        public List<string> CacheIds = new List<string> { "iex-companies", "yf-companies" };
        private IMemoryCache _iexCompaniesCache;
        private IMemoryCache _yfCompaniesCache;
        private RequestManager _requestManager;

        public DataCache(RequestManager manager)
        {
            _iexCompaniesCache = new MemoryCache(new MemoryCacheOptions());
            _yfCompaniesCache = new MemoryCache(new MemoryCacheOptions());
            _requestManager = manager;

            CachedSymbols = new Dictionary<string, HashSet<string>>();
            foreach (string cacheId in this.CacheIds)
                CachedSymbols.Add(cacheId, new HashSet<string>());

            ExceptionReport = new Dictionary<string, string>();
        }

        //Get object by cacheKey <channel>-<cacheType>-<symbol>
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
                        //Not in cache, so we can update the cache with private helper
                        UpdateIEXCompanyCacheEntry(cacheKey, null, EvictionReason.None, this);
                        _iexCompaniesCache.TryGetValue(cacheKey, out iexCompanyEntry);
                    }
                    return iexCompanyEntry;
                case "yf-companies":
                    CompanyStatsYF yfCompanyEntry;
                    if (!_yfCompaniesCache.TryGetValue(cacheKey, out yfCompanyEntry))
                    {
                        //Not in cache, so we can update the cache with private helper
                        UpdateYFCompanyCacheEntry(cacheKey, null, EvictionReason.None, this);
                        _yfCompaniesCache.TryGetValue(cacheKey, out yfCompanyEntry);
                    }
                    return yfCompanyEntry;
                default:
                    throw new Exception("Cache ID " + cacheId + " is not supported by PimService Cache.");
            }
        }

        //Add passed object to correct cache based on cacheKey <channel>-<cacheType>-<symbol>
        public void Add(object product, string cacheKey)
        {
            string[] tokens = cacheKey.Split('-');
            string cacheId = tokens[0] + "-" + tokens[1];
            int expirationMinutes = new Random().Next(MinCacheUpdateAge, MaxCacheUpdateAge);
            var expirationTime = DateTime.Now.Add(new TimeSpan(0, expirationMinutes, 0));
            //var expirationToken = new CancellationChangeToken(
            //new CancellationTokenSource(TimeSpan.FromMinutes(expirationMinutes + .01)).Token);

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
                    .SetAbsoluteExpiration(expirationTime);
                //.AddExpirationToken(expirationToken);
            }
            switch (cacheId)
            {
                case "iex-companies":
                    options.RegisterPostEvictionCallback(callback: UpdateIEXCompanyCacheEntry, state: this);
                    _iexCompaniesCache.Set(cacheKey, product, options);
                    break;
                case "yf-companies":
                    options.RegisterPostEvictionCallback(callback: UpdateYFCompanyCacheEntry, state: this);
                    _yfCompaniesCache.Set(cacheKey, product, options);
                    break;
                default:
                    throw new Exception("Cache ID " + cacheId + " is not supported by PimService Cache.");
            }
            string curSymbol = cacheKey.Split('-')[2];
            lock (CachedSymbols)
            {
                if (!this.CachedSymbols[cacheId].Contains(cacheKey))
                    this.CachedSymbols[cacheId].Add(cacheKey);
            }
        }

        //Investors Exchange Company Cache Entry Update Routine
        public void UpdateIEXCompanyCacheEntry(object key, object value, EvictionReason reason, object state = null)
        {
            try
            {
                Stopwatch sw = new Stopwatch(); sw.Start();
                string cacheKey = (string)key;
                string channel = cacheKey.Split('-')[0];
                string symbol = cacheKey.Split('-')[2];

                //Remove before updating and re-adding
                RemoveCachedSymbol(cacheKey);

                CompanyStatsIEX companyStats = IEX.GetCompanyStatsAsync(symbol).Result;

                //Save IEX Company to cache
                this.Add(companyStats, cacheKey);
                string perf = sw.ElapsedMilliseconds.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR UpdateIEXCompanyCacheEntry: " + e.Message);
            }
        }

        //Yahoo Finance Company Cache Entry Update Routine
        public void UpdateYFCompanyCacheEntry(object key, object value, EvictionReason reason, object state = null)
        {
            try
            {
                Stopwatch sw = new Stopwatch(); sw.Start();
                string cacheKey = (string)key;
                string channel = cacheKey.Split('-')[0];
                string symbol = cacheKey.Split('-')[2];

                //Remove before updating and re-adding
                RemoveCachedSymbol(cacheKey);

                CompanyStatsYF companyStats = YahooFinance.GetCompanyStatsAsync(symbol).Result;

                //Save YF Company to cache
                this.Add(companyStats, cacheKey);
                string perf = sw.ElapsedMilliseconds.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR UpdateYFCompanyCacheEntry: " + e.Message);
            }
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
                    throw new Exception("Cache ID " + cacheId + " is not supported by PimService Cache.");
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

        public async Task LoadCacheAsync(string cacheId)
        {
            int count = 0;
            await Task.Run(() =>
            {
                string nasdaqData = Companies.GetFromFtpUri(Companies.NasdaqSymbolsUri);
                string[] nasdaqDataLines = nasdaqData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                Random r1 = new Random();
                string[] randomizedNasdaqLines = nasdaqDataLines.OrderBy(x => r1.Next()).ToArray();
                for (int i = 1; i < randomizedNasdaqLines.Length - 1; i++) //trim first and last row
                {
                    string line = randomizedNasdaqLines[i];
                    string[] data = line.Split('|');
                    if (data.Count() > 3)
                    {
                        string symbol = data[1];
                        if (!String.IsNullOrEmpty(symbol) && !this.CachedSymbols[cacheId].Contains(symbol))
                        {
                            bool isNasdaq = data[0] == "Y";
                            if (isNasdaq)
                            {
                                string cacheKey = String.Format("{0}-{1}", cacheId, symbol);
                                this.Get(cacheKey);
                                count++;
                            }
                        }
                    }
                }

                string otcData = Companies.GetFromFtpUri(Companies.OtcSymbolsUri);
                string[] otcDataLines = otcData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                Random r2 = new Random();
                string[] randomizedOtcLines = otcDataLines.OrderBy(x => r2.Next()).ToArray();
                for (int j = 1; j < randomizedOtcLines.Length - 1; j++) //trim first and last row
                {
                    string line = randomizedOtcLines[j];
                    string[] data = line.Split('|');
                    if (data.Count() > 3)
                    {
                        string symbol = data[0];
                        if (!String.IsNullOrEmpty(symbol) && !this.CachedSymbols[cacheId].Contains(symbol))
                        {
                            string cacheKey = String.Format("{0}-{1}", cacheId, symbol);
                            this.Get(cacheKey);
                            count++;
                        }
                    }
                }

                string otcMarketsData = Companies.GetFromUri(Companies.OtcMarketsUri);
                string[] otcMarketsDataLines = otcMarketsData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                Random r3 = new Random();
                string[] randomizedOtcMarketsLines = otcMarketsDataLines.OrderBy(x => r3.Next()).ToArray();
                for (int k = 1; k < randomizedOtcMarketsLines.Length; k++) //trim first row
                {
                    string line = randomizedOtcMarketsLines[k];
                    string[] data = line.Split(',');
                    if (data.Count() > 3)
                    {
                        string symbol = data[0];
                        if (!String.IsNullOrEmpty(symbol) && !this.CachedSymbols[cacheId].Contains(symbol))
                        {
                            string cacheKey = String.Format("{0}-{1}", cacheId, symbol);
                            this.Get(cacheKey);
                            count++;
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

                /*Single-Thread edition
                for (int i = 0; i < ids.Count(); i++)
                {
                    string entityId = ids[i].ToString();
                    string cacheKey = string.Format("{0}-{1}", cacheId, entityId);
                    try
                    {
                        if (!this.CachedStyles.ContainsKey(cacheKey))
                        {
                            this.Get(cacheKey);
                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        lock (ExceptionReport)
                        {
                            ExceptionReport.Add(cacheKey, "Error: " + e.Message + ", StackTrace: " + e.StackTrace);
                        }
                        continue;
                    }
                }*/
            });
        }
    }
}
