using System.Diagnostics;
using NodaTime;
using PT.Models.RequestModels;
using PT.Services;
using YahooQuotesApi;

namespace PT.Middleware
{
    // No API key needed for this one?
    // YahooQuotesApi Documentation https://github.com/dshe/YahooQuotesApi
    public class YahooFinance
    {

        // With YahooQuotesApi
        public static async Task<CompanyStats> GetCompanyStatsAsync(string symbol, RequestManager rm)
        {
            Stopwatch sw = Stopwatch.StartNew();
            CompanyStats companyStat = new CompanyStats();
            try
            {
                // Yahoo Quote
                Security quote = GetQuoteAsync(symbol).Result;
                if (quote != null)
                {
                    // parse quote data into company stat
                    companyStat.Symbol = symbol;
                    companyStat.Exchange = quote.FullExchangeName;
                    companyStat.CompanyName = quote.LongName;
                    companyStat.CompanyTrading = quote.Tradeable == null || quote.Tradeable == true ? true : false;

                    companyStat.CompanyQuoteType = quote.QuoteType;
                    companyStat.CompanyMarket = quote.Market;

                    // TODO: This is what we should do for below parse blocks
                    companyStat.MarketCap = quote.MarketCap != null ? Convert.ToDecimal(quote.MarketCap) : null;
                    companyStat.SharesOutstanding = quote.SharesOutstanding != null ? Convert.ToDecimal(quote.SharesOutstanding) : null;

                    decimal peTrailing = 0.0M;
                    try { peTrailing = decimal.Parse(quote.TrailingPE.ToString()); }
                    catch (Exception e) { /*do nothing*/ }
                    companyStat.PeRatioTrailing = peTrailing;

                    decimal peForward = 0.0M;
                    try { peForward = decimal.Parse(quote.ForwardPE.ToString()); }
                    catch (Exception e) { /*set to trailing*/ peForward = peTrailing; }
                    companyStat.PeRatioForward = peForward;

                    decimal epsTrailing = 0.0M;
                    try { epsTrailing = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()); }
                    catch (Exception e) { /*do nothing*/ }
                    companyStat.EpsTrailing = epsTrailing;

                    decimal epsForward = 0.0M;
                    try { epsForward = decimal.Parse(quote.EpsForward.ToString()); }
                    catch (Exception e) { /*set to trailing*/ epsForward = epsTrailing; }
                    companyStat.EpsForward = epsForward;

                    companyStat.BookValue = quote.BookValue;

                    companyStat.Price = decimal.Parse(quote.RegularMarketPrice.ToString());

                    companyStat.PriceAverage50DayUSD = decimal.Parse(quote.FiftyDayAverage.ToString());
                    companyStat.PriceAverage200DayUSD = decimal.Parse(quote.TwoHundredDayAverage.ToString());

                    companyStat.PriceHigh52w = decimal.Parse(quote.FiftyTwoWeekHigh.ToString());
                    companyStat.PriceLow52w = decimal.Parse(quote.FiftyTwoWeekLow.ToString());
                    companyStat.PriceAverageEstimate52w = (companyStat.PriceHigh52w + companyStat.PriceLow52w) / 2;

                    //companyStat.PriceToBook = decimal.Parse(quote.PriceToBook.ToString());

                    companyStat.VolumeToday = decimal.Parse(quote.RegularMarketVolume.ToString());
                    companyStat.VolumeTodayUSD = (companyStat.VolumeToday * companyStat.Price);
                    companyStat.VolumeAverage10d = decimal.Parse(quote.AverageDailyVolume10Day.ToString());
                    companyStat.VolumeAverage10dUSD = (companyStat.VolumeAverage10d * companyStat.Price);
                    companyStat.VolumeAverage3m = decimal.Parse(quote.AverageDailyVolume3Month.ToString());
                    companyStat.VolumeAverage3mUSD = (companyStat.VolumeAverage3m * companyStat.Price);

                    // Could get more trade data from IEX possibly
                    companyStat.TradeData = new TradeDataYF
                    {
                        BidPrice = decimal.Parse(quote.Bid.ToString()),
                        BidSize = decimal.Parse(quote.BidSize.ToString()),
                        AskPrice = decimal.Parse(quote.Ask.ToString()),
                        AskSize = decimal.Parse(quote.AskSize.ToString())
                    };
                }

                var score = GetCompositeScoreInternal(symbol, quote, rm);
                score.ScoreTimeMS = sw.ElapsedMilliseconds;
                if (score.CompositeScoreValue > 0)
                    companyStat.CompositeScoreResult = score;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION CAUGHT: YF.cs YF.GetComanyStatsAsync for symbol " + symbol + ", message: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        // With YahooQuotesApi
        public static async Task<Security> GetQuoteAsync(string symbol)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            YahooQuotes yahooQuotes = new YahooQuotesBuilder().Build();

            Dictionary<string, Security?> securities = await yahooQuotes.GetAsync(new[] { symbol });

            Security security = securities[symbol] ?? throw new ArgumentException("Unknown symbol");
            return security;
        }

        // With YahooQuotesApi
        public static async Task<List<PriceTick>> GetHistoryAsync(string symbol, int days)
        {
            // You should be able to query data from various markets including US, HK, TW
            // The timezone here may or may not impact accuracy
            days *= -1;
            var timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
            var localTime = LocalDateTime.FromDateTime(DateTime.Now.AddDays(days));
            var zonedTimeInstant = localTime.InZoneStrictly(timeZone).ToInstant();

            YahooQuotes yahooQuotes = new YahooQuotesBuilder()
                .WithHistoryStartDate(zonedTimeInstant)
                .Build();

            Security security = await yahooQuotes.GetAsync(symbol, Histories.PriceHistory)
                ?? throw new ArgumentException("Unknown symbol.");

            var history = security.PriceHistory.Value.ToList();
            return history;
        }

        public static async Task<CompaniesListYF> GetScreenedCompaniesAsync(CompaniesListYF allCompanies, string screenId)
        {
            CompaniesListYF screened = new CompaniesListYF
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyYF companyObject = company.Value;
                CompanyStats stats = companyObject.Stats;
                if (stats.Price < 20 && stats.Price > .01M && stats.VolumeAverage10dUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return await Task.FromResult(screened);
        }

        // Used internally for cache loading
        public static CompositeScoreResult GetCompositeScoreInternal(string symbol, Security quote, RequestManager rm)
        {
            return Indicators.GetCompositeScoreResult(symbol, quote, rm);
        }
    }
}
