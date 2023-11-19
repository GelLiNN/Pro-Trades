using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PT.Models;

namespace PT.Middleware
{
    //API Documentation https://financialmodelingprep.com/developer/docs/
    //No API key needed for this one?
    public class FMP
    {
        private static readonly string FMPURI = @"https://financialmodelingprep.com/api/v3/";
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<string> CompleteFMPRequestAsync(string path, string function = null, string symbol = null)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response = await HttpClient.GetAsync(FMPURI + path);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return await Task.FromResult(responseBody);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("EXCEPTION Message: {0}, StackTrace: {1}", e.Message, e.StackTrace);
                return string.Empty;
            }
        }

        public static async Task<CompanyStatsFMP> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsFMP companyStat = new CompanyStatsFMP();
            try
            {
                //FMP quote endpoint
                string quoteResponse = CompleteFMPRequestAsync(String.Format("quote/{0}", symbol)).Result;
                JArray quoteData = JArray.Parse(quoteResponse); //JArray since quote endpoint accepts multiple symbols
                foreach (JObject quote in quoteData)
                {
                    companyStat.Symbol = symbol;
                    companyStat.CompanyName = quote.GetValue("name").ToString();
                    companyStat.MarketCap = decimal.Parse(quote.GetValue("marketCap").ToString());
                    companyStat.SharesOutstanding = decimal.Parse(quote.GetValue("sharesOutstanding").ToString());
                    companyStat.PeRatio = decimal.Parse(quote.GetValue("pe").ToString());
                    companyStat.EarningsPerShare = decimal.Parse(quote.GetValue("eps").ToString());
                    companyStat.EarningsReportDate = DateTime.Parse(quote.GetValue("earningsAnnouncement").ToString());

                    companyStat.Price = decimal.Parse(quote.GetValue("price").ToString());
                    companyStat.PriceOpenToday = decimal.Parse(quote.GetValue("open").ToString());
                    companyStat.PricePreviousClose = decimal.Parse(quote.GetValue("previousClose").ToString());

                    companyStat.PriceHighToday = decimal.Parse(quote.GetValue("dayHigh").ToString());
                    companyStat.PriceLowToday = decimal.Parse(quote.GetValue("dayLow").ToString());
                    companyStat.PriceAverageToday = (companyStat.PriceHighToday + companyStat.PriceLowToday) / 2;

                    companyStat.PriceChangeTodayUSD = decimal.Parse(quote.GetValue("change").ToString());
                    companyStat.PriceChangeTodayPercent = decimal.Parse(quote.GetValue("changesPercentage").ToString());
                    companyStat.PriceAverage50Day = decimal.Parse(quote.GetValue("priceAvg50").ToString());

                    companyStat.PriceHighYTD = decimal.Parse(quote.GetValue("yearHigh").ToString());
                    companyStat.PriceLowYTD = decimal.Parse(quote.GetValue("yearLow").ToString());
                    companyStat.PriceAverageEstimateYTD = (companyStat.PriceHighYTD + companyStat.PriceLowYTD) / 2;

                    companyStat.VolumeToday = decimal.Parse(quote.GetValue("volume").ToString());
                    companyStat.VolumeAverage = decimal.Parse(quote.GetValue("avgVolume").ToString());
                    companyStat.VolumeAverageUSD = (companyStat.VolumeAverage * companyStat.PriceAverageToday);
                }

                //FMP company profile endpoint
                string companyProfileResponse = CompleteFMPRequestAsync(String.Format("company/profile/{0}", symbol)).Result;
                JObject profileData = JObject.Parse(companyProfileResponse);
                if (profileData != null)
                {
                    JObject profile = (JObject)profileData.GetValue("profile");
                    companyStat.Exchange = profile.GetValue("exchange").ToString();
                    companyStat.CompanyDescription = profile.GetValue("description").ToString();
                    companyStat.CompanyCEO = profile.GetValue("ceo").ToString();
                    companyStat.CompanyIndustry = profile.GetValue("industry").ToString();
                    companyStat.CompanySector = profile.GetValue("sector").ToString();
                    companyStat.CompanyImageLink = profile.GetValue("image").ToString();

                    companyStat.BetaValue = decimal.Parse(profile.GetValue("beta").ToString());
                }

                //FMP income statement endpoint
                string financialsResponse = CompleteFMPRequestAsync(String.Format("financials/income-statement/{0}?period=quarter", symbol)).Result;
                JObject financialsData = JObject.Parse(financialsResponse);
                if (financialsData != null)
                {
                    JArray financials = (JArray)financialsData.GetValue("financials");
                    foreach (JObject financial in financials)
                    {
                        FinancialsFMP fin = new FinancialsFMP
                        {
                            RevenueTotal = decimal.Parse(financial.GetValue("Revenue").ToString()),
                            RevenueGrowth = decimal.Parse(financial.GetValue("Revenue Growth").ToString()),
                            ExpensesRD = decimal.Parse(financial.GetValue("R&D Expenses").ToString()),
                            ExpensesSGA = decimal.Parse(financial.GetValue("SG&A Expense").ToString()),
                            ExpensesOperating = decimal.Parse(financial.GetValue("Operating Expenses").ToString()),
                            IncomeOperating = decimal.Parse(financial.GetValue("Operating Income").ToString()),
                            IncomeNet = decimal.Parse(financial.GetValue("Net Income").ToString()),
                            IncomeConsolidated = decimal.Parse(financial.GetValue("Consolidated Income").ToString()),
                            MarginGross = decimal.Parse(financial.GetValue("Gross Margin").ToString()),
                            MarginEBITDA = decimal.Parse(financial.GetValue("EBITDA Margin").ToString()),
                            MarginEBIT = decimal.Parse(financial.GetValue("EBIT Margin").ToString()),
                            MarginCashFlow = decimal.Parse(financial.GetValue("Free Cash Flow margin").ToString()),
                            MarginProfit = decimal.Parse(financial.GetValue("Profit Margin").ToString()),
                            LastFinancialReportDate = DateTime.Parse(financial.GetValue("date").ToString())
                        };
                        companyStat.Financials = fin;
                        break; //only get most recent quarterly sumbission or 10Q
                    }
                }
                //Get Trade Data from somewhere?
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR Utility.cs FMP.GetComanyStatsAsync: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        public static string GetQuote(string symbol)
        {
            return CompleteFMPRequestAsync(String.Format("quote/{0}", symbol)).Result;
        }

        public static async Task<CompaniesListFMP> GetScreenedCompaniesAsync(CompaniesListFMP allCompanies, string screenId)
        {
            CompaniesListFMP screened = new CompaniesListFMP
            {
                SymbolsToCompanies = new Dictionary<string, CompanyFMP>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyFMP companyObject = company.Value;
                CompanyStatsFMP stats = companyObject.Stats;
                if (stats.PriceAverageToday < 20 && stats.PriceAverageToday > .01M && stats.VolumeAverageUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return await Task.FromResult(screened);
        }

        public static async Task<CompaniesListFMP> GetAllCompaniesAsync(RequestManager rm)
        {
            CompaniesListFMP companies = new CompaniesListFMP()
            {
                SymbolsToCompanies = new Dictionary<string, CompanyFMP>()
            };

            string nasdaqData = rm.GetFromUri(Companies.NasdaqSymbolsUri);
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
                            CompanyStatsFMP stats = FMP.GetCompanyStatsAsync(symbol).Result;
                            CompanyFMP company = new CompanyFMP
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

            string otcMarketsData = rm.GetFromUri(Companies.OtcMarketsUri);
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
                        CompanyStatsFMP stats = FMP.GetCompanyStatsAsync(symbol).Result;
                        CompanyFMP company = new CompanyFMP
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
