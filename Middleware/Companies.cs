using PT.Models.RequestModels;
using PT.Services;

namespace PT.Middleware
{
    // Module for getting all or most company symbols and names from all exchanges
    // Used to be FTP for nasdaq but now apparently not...
    public static class Companies
    {
        public static readonly string NasdaqSymbolsUri = @"https://www.nasdaqtrader.com/dynamic/SymDir/nasdaqtraded.txt";
        public static readonly string OtcMarketsUri = @"https://www.otcmarkets.com/research/stock-screener/api/downloadCSV";

        public static async Task<CompaniesListYF> GetAllCompaniesAsync(RequestManager rm)
        {
            CompaniesListYF companies = new CompaniesListYF()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
            };

            string nasdaqData = rm.GetFromUri(NasdaqSymbolsUri);
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

            string otcMarketsData = rm.GetFromUri(OtcMarketsUri);
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
    }
}
