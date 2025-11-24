using PT.Models.RequestModels;
using PT.Services;

namespace PT.Middleware
{
    // Module for getting all or most company symbols and names from all exchanges
    // Used to be FTP for nasdaq but now apparently not...
    public static class AssetAggregator
    {
        public static readonly string NasdaqSymbolsUri = @"https://www.nasdaqtrader.com/dynamic/SymDir/nasdaqtraded.txt";
        public static readonly string OtcMarketsUri = @"https://www.otcmarkets.com/research/stock-screener/api/downloadCSV";

        public static async Task<CompaniesListYF> GetAllCompaniesAsync(RequestManager rm)
        {
            CompaniesListYF companies = new CompaniesListYF()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
            };

            string nasdaqData = await rm.GetFromUriAsync(NasdaqSymbolsUri);
            string[] nasdaqDataLines = nasdaqData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 1; i < nasdaqDataLines.Length - 1; i++) //trim first and last row
            {
                string line = nasdaqDataLines[i];
                string[] data = line.Split('|');
                if (data.Count() > 3)
                {
                    string symbol = data[1];
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol) && !String.IsNullOrEmpty(symbol))
                    {
                        bool isNasdaq = data[0] == "Y";
                        if (isNasdaq)
                        {
                            // Below makes it slow
                            // CompanyStats stats = YahooFinance.GetCompanyStatsAsync(symbol).Result;
                            CompanyStats stats = new CompanyStats();
                            CompanyYF company = new CompanyYF
                            {
                                Symbol = symbol,
                                Exchange = "NASDAQ",
                                Stats = stats
                            };
                            companies.SymbolsToCompanies.Add(symbol, company);
                        }
                    }
                }
            }

            string otcMarketsData = await rm.GetFromUriAsync(OtcMarketsUri);
            string[] otcMarketsDataLines = otcMarketsData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int k = 1; k < otcMarketsDataLines.Length; k++) //trim first row
            {
                string line = otcMarketsDataLines[k];
                string[] data = line.Split(',');
                if (data.Count() > 3)
                {
                    string symbol = data[0];
                    if (!companies.SymbolsToCompanies.ContainsKey(symbol) && !String.IsNullOrEmpty(symbol))
                    {
                        // Below makes it slow
                        // CompanyStats stats = YahooFinance.GetCompanyStatsAsync(symbol).Result;
                        CompanyStats stats = new CompanyStats();
                        CompanyYF company = new CompanyYF
                        {
                            Symbol = symbol,
                            Exchange = data[2],
                            Stats = stats
                        };
                        companies.SymbolsToCompanies.Add(symbol, company);
                    }
                }
            }
            return await Task.FromResult(companies);
        }

        /// <summary>
        /// Get a randomized HashSet of active company symbols from Nasdaq and OTC master lists
        /// </summary>
        /// <param name="rm"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static HashSet<string> GetRandomizedCompanySymbols(RequestManager rm, int limit)
        {
            HashSet<string> symbols = new HashSet<string>();

            // Get Nasdaq symbols
            string nasdaqData = rm.GetFromUri(AssetAggregator.NasdaqSymbolsUri);
            string[] nasdaqDataLines = nasdaqData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 1; i < nasdaqDataLines.Length - 1; i++) // trim first and last row
            {
                string line = nasdaqDataLines[i];
                string[] data = line.Split('|');
                if (data.Count() > 3)
                {
                    string symbol = data[1];
                    if (!string.IsNullOrEmpty(symbol) && !symbols.Contains(symbol))
                    {
                        bool isNasdaq = data[0] == "Y";
                        if (isNasdaq)
                        {
                            symbols.Add(symbol);
                        }
                    }
                }
            }

            // Get OTC Markets symbols
            string otcMarketsData = rm.GetFromUri(AssetAggregator.OtcMarketsUri);
            string[] otcMarketsDataLines = otcMarketsData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int j = 1; j < otcMarketsDataLines.Length; j++) // trim first row
            {
                string line = otcMarketsDataLines[j];
                string[] data = line.Split(',');
                if (data.Length > 3)
                {
                    string symbol = data[0];
                    if (!string.IsNullOrEmpty(symbol) && !symbols.Contains(symbol))
                    {
                        symbols.Add(symbol);
                    }
                }
            }

            // Ensure combined set is randomized, then start loading cache with Get function
            Random r = new Random();
            var randomizedSymbols = symbols.OrderBy(x => r.Next());
            return randomizedSymbols.Take(limit).ToHashSet();
        }
    }
}
