using Newtonsoft.Json;
using PT.Core;
using PT.Models.RequestModels;
using PT.Services;
using System.Diagnostics;
using System.Globalization;
using static PT.Models.RequestModels.TipRanksDataResponse;

namespace PT.Middleware
{
    public static class TipRanks
    {
        private static readonly string TipRanksBaseUrl = @"https://www.tipranks.com/api/stocks/";

        // I obtained the context for these IDs by comparing the TipRanks API data to the listed insider trades on their site
        // Use them in the GetData method to decipher the insider buys and sells
        private static readonly int InsiderInformativeBuyTypeId = 2;
        private static readonly int InsiderNonInformativeBuyTypeId = 4;
        private static readonly int InsiderOtherBuyTypeId_1 = 50;
        private static readonly int InsiderInformativeSellTypeId = 7;
        private static readonly int InsiderNonInformativeSellTypeId = 51;
        private static readonly int InsiderOtherSellTypeId_1 = 3;
        // Seems like there's way too many of those though and actions are better

        // Found the meanings of these actions by visiting Insider.link URL from the HedgeFundsResult object
        // i.e. https://www.sec.gov/Archives/edgar/data/1486056/000112760223026226/xslF345X03/form4.xml
        public static HashSet<int> InsiderBuyActions = new HashSet<int> { 2, 3 };
        public static HashSet<int> InsiderSellActions = new HashSet<int> { 1, 4 };

        public static HedgeFundsResult GetTipRanksResult(string symbol, RequestManager rm)
        {
            // Return a data object that contains:
            // Aggregated ratings data and ratings providers
            // Aggregated insider transactions data, insider names, insider ratings
            // Aggregated insitutional transactions data, hedge fund ratings, hedge fund names
            // Composite score of the above 3 items

            try
            {
                //TipRanksDataResponse? trResponse = rm.ScrapeTipRanksUriSelenium(TipRanksBaseUrl + "getData/" + symbol);
                TipRanksDataResponse? trResponse = rm.ScrapeTipRanksUriPlaywright2(TipRanksBaseUrl + "getData/" + symbol);

                // OLD
                //Dictionary<string, string> headers = new();
                //headers.Add("Cookie", @"__cf_bm=3.Zb.tnW0m3kurXX1tnWG0vFPeOkGfxYiCWTKk9tVfA-1764367228-1.0.1.1-boj7o.sokeuUaSJAgtiTnUL1E3TF9ABmx03Wejjb3nrNxDHrH4eq91tDdJOpOWN1IfopanjjWn3CNUTZ_QjsDfCE1bMxST264IILOFhqVPQ; personal-message=none; tipranks-experiments=%7b%22Experiments%22%3a%5b%7b%22Name%22%3a%22general_A%22%2c%22Variant%22%3a%22v3%22%2c%22SendAnalytics%22%3afalse%7d%2c%7b%22Name%22%3a%22general_B%22%2c%22Variant%22%3a%22v3%22%2c%22SendAnalytics%22%3afalse%7d%2c%7b%22Name%22%3a%22general_C%22%2c%22Variant%22%3a%22v3%22%2c%22SendAnalytics%22%3afalse%7d%5d%7d; tipranks-experiments-slim=general_A%3av3%7cgeneral_B%3av3%7cgeneral_C%3av3; tr-experiments-version=1.14; tr-plan-id=0; tr-plan-name=free");
                //headers.Add("Accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                //headers.Add("Accept-Encoding", @"gzip, deflate, br, zstd");
                //headers.Add("Accept-Language", @"en-US,en;q=0.5");
                //headers.Add("Connection", @"keep-alive");
                //headers.Add("Host", @"www.tipranks.com");
                //headers.Add("Priority", @"u=0, i");
                //headers.Add("Sec-Fetch-Dest", @"document");
                //headers.Add("Sec-Fetch-Mode", @"navigate");
                //headers.Add("Sec-Fetch-Size", @"none");
                //headers.Add("Sec-Fetch-User", @"?1");
                //headers.Add("Upgrade-Insecure-Requests", @"1");
                //headers.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.360");
                //string responseStr = rm.GetFromUri(TipRanksBaseUrl + "getData/" + symbol, headers);

                if (trResponse == null)
                {
                    string msg = $"WARNING TipRanks.cs GetTipRanksResult Could not fetch API for symbol {symbol}";
                    Debug.WriteLine(msg);
                    return new HedgeFundsResult
                    {
                        RatingsComposite = Constants.CORE_INVALID_COMP,
                        RatingsBase = 0,
                        InsiderBonus = 0,
                        HoldingBonus = 0,
                        HedgeSentimentBonus = 0,
                        HedgeBsnBonus = 0,
                        PriceTarget = 0,
                        HedgeSentiment = 0,
                        HedgeTrendAction = 0,
                        HedgeTrendValue = 0,
                        Sector = "Not Found",
                        Description = "Not Found",
                        Insiders = null,
                        Holdings = null,
                        ThirdPartyRatings = null,
                        ConsensusOverTime = null,
                        ErrorMessage = msg
                    };
                }
                else
                {
                    // Filter results to the last 3 months
                    DateTime startDate = DateTime.Now.AddMonths(-3);
                    List<Insider> insiders = trResponse.insiders
                        .Where(x => DateTime.Compare(x.rDate, startDate) > 0)
                        .ToList();
                    List<BestConsensusOverTime> bsns = trResponse.bestConsensusOverTime
                        .Where(x => DateTime.Compare(x.date, startDate) > 0)
                        .ToList();
                    List<Expert> ratings = trResponse.experts
                        .Where(x => DateTime.Compare(x.ratings.FirstOrDefault().date, startDate) > 0 && x.rankings.FirstOrDefault().stars > 0)
                        .ToList();
                    List<HoldingsByTime> holdings = trResponse.hedgeFundData.holdingsByTime
                        .Where(x => DateTime.Compare(x.date, startDate) > 0)
                        .ToList();

                    // Other custom data extractions
                    string sector = string.IsNullOrWhiteSpace(trResponse.portfolioHoldingData.sectorId) ?
                        "Not Found" : trResponse.portfolioHoldingData.sectorId;
                    string description = string.IsNullOrWhiteSpace(trResponse.description) ?
                        "Not Found" : trResponse.description;

                    // Average hedge fund ratings will form score base
                    decimal averageRating = GetAverageRating(ratings, trResponse);
                    decimal ratingsBase = (averageRating / 7.0M) * 100; // Get score using the average rating as a percentage of (max rating + 2)
                    ratingsBase = Math.Min(ratingsBase, 55);
                    ratingsBase += ratingsBase == 55 ? Constants.CORE_BONUS : 0;

                    decimal bsnBonus = GetBsnBonus(bsns);

                    // Add price target bonus 5 if the target is more than 5% greater than last price
                    // Add price target bonus 10 if the target is more than 10% greater than last price
                    // Add price target bonus -10 if the target is less than last price
                    decimal priceTargetBonus = 0;
                    decimal priceTarget = 0;
                    decimal lastPrice = Convert.ToDecimal(trResponse.prices[trResponse.prices.Length - 1].p);
                    if (trResponse.portfolioHoldingData.bestPriceTarget != null)
                    {
                        priceTarget = Convert.ToDecimal(trResponse.portfolioHoldingData.bestPriceTarget);
                    }
                    else if (trResponse.portfolioHoldingData.priceTarget != null)
                    {
                        priceTarget = Convert.ToDecimal(trResponse.portfolioHoldingData.priceTarget);
                    }
                    else if (bsns.Count > 0)
                    {
                        priceTarget = Convert.ToDecimal(bsns[bsns.Count - 1].priceTarget);
                    }
                    else
                    {
                        var chicken = "nuggets"; // Find some other way to get price target
                    }

                    // Protect against failure to get price target
                    if (priceTarget != 0)
                    {
                        // Formulate price target bonus
                        decimal diff = priceTarget - lastPrice;
                        decimal percentChange = (diff / Math.Abs(lastPrice)) * 100;
                        priceTargetBonus += percentChange >= 5 ? 5 : 0;
                        priceTargetBonus += percentChange >= 10 ? 5 + Math.Min(15, diff) : 0;
                        priceTargetBonus += percentChange < 0 ? -15 : 0;
                    }

                    // Other bonuses for recent insider buy-ins, institutional holdings, and hedge funds
                    decimal insiderBonus = GetInsiderBonus(insiders);
                    decimal holdingBonus = GetHoldingBonus(holdings);
                    decimal hedgeSentimentBonus = GetHedgeSentimentBonus(trResponse.hedgeFundData);

                    decimal ratingsComposite = 0;
                    ratingsComposite += ratingsBase;
                    ratingsComposite += bsnBonus; // Add bsn bonus from above
                    ratingsComposite += priceTargetBonus; // Add price target bonus from above
                    ratingsComposite += insiderBonus; // Add insider bonus from above
                    ratingsComposite += holdingBonus; // Add holding bonus from above
                    ratingsComposite += hedgeSentimentBonus; // Add hedge sentiment bonus from above

                    ratingsComposite = Math.Min(ratingsComposite, 100); // cap composite at 100, no extra weight
                    ratingsComposite = Math.Max(ratingsComposite, 0); // limit composite at 0, no negatives

                    return new HedgeFundsResult
                    {
                        Name = trResponse.companyFullName,
                        Sector = sector.Substring(0, 1).ToUpper() + sector.Substring(1),
                        Description = description,
                        RatingsComposite = ratingsComposite,
                        TipRanksScore = Convert.ToDecimal(trResponse.tipranksStockScore.score),
                        RatingsBase = ratingsBase,
                        InsiderBonus = insiderBonus,
                        HoldingBonus = holdingBonus,
                        HedgeSentimentBonus = hedgeSentimentBonus,
                        HedgeBsnBonus = bsnBonus,
                        PriceTarget = priceTarget,
                        HedgeSentiment = Convert.ToDecimal(trResponse.hedgeFundData.sentiment),
                        HedgeTrendAction = Convert.ToDecimal(trResponse.hedgeFundData.trendAction),
                        HedgeTrendValue = Convert.ToDecimal(trResponse.hedgeFundData.trendValue),
                        Insiders = insiders,
                        Holdings = holdings,
                        ThirdPartyRatings = ratings,
                        ConsensusOverTime = bsns,
                        ErrorMessage = string.Empty
                    };
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR TipRanks.cs GetTipRanksResult Exception for symbol " + symbol + ", message: " + e.Message);
                return new HedgeFundsResult
                {
                    RatingsComposite = Constants.CORE_INVALID_COMP,
                    RatingsBase = 0,
                    InsiderBonus = 0,
                    HoldingBonus = 0,
                    HedgeSentimentBonus = 0,
                    HedgeBsnBonus = 0,
                    PriceTarget = 0,
                    HedgeSentiment = 0,
                    HedgeTrendAction = 0,
                    HedgeTrendValue = 0,
                    Sector = "Not Found",
                    Description = "Not Found",
                    Insiders = null,
                    Holdings = null,
                    ThirdPartyRatings = null,
                    ConsensusOverTime = null,
                    ErrorMessage = e.Message
                };
            }
        }

        public static string GetSentiment(string symbol, RequestManager rm)
        {
            // Return a data object that contains:
            // total bullish sentiment
            // total bearish sentiment
            // Composite score of sentiment
            string responseString = rm.GetFromUri(TipRanksBaseUrl + "getNewsSentiments/?ticker=" + symbol);
            TipRanksSentimentResponse trResponse = JsonConvert.DeserializeObject<TipRanksSentimentResponse>(responseString);

            int totalBuySentiments = 0;
            int totalSellSentiments = 0;
            int totalNeutralSentiments = 0;

            foreach (TipRanksSentimentResponse.Count c in trResponse.counts)
            {
                totalBuySentiments += c.buy != null ? (int)c.buy : 0;
                totalSellSentiments += c.sell != null ? (int)c.sell : 0;
                totalNeutralSentiments += c.neutral != null ? (int)c.neutral : 0;
            }

            string bsn = "Buy Sentiments: " + totalBuySentiments + ", Sell Sentiments: " + totalSellSentiments + ", Neutral Sentiments: " + totalNeutralSentiments;

            // Have to do these annoying null checks apparently
            string bullish = "Average Bullish Sentiment: ";
            string bearish = "Average Bearish Sentiment: ";
            if (trResponse.sentiment != null)
            {
                bullish += trResponse.sentiment.bullishPercent.ToString();
                bearish += trResponse.sentiment.bearishPercent.ToString();
            }
            else
            {
                bullish += "n/a";
                bearish += "n/a";
            }

            string sectorBullish = "Sector Average Bullish Sentiment: " + trResponse.sectorAverageBullishPercent;
            string sectorAverageNews = "Sector Average News Score: " + trResponse.sectorAverageNewsScore;
            string score = "TipRanks Score: " + trResponse.score;

            //TODO: formulate into Sentiment Analysis Composite Score
            //TODO: do stuff with the word clouds possibly
            //TODO: see if I can adjust the params on the API call to get better data back

            return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                bsn, bullish, bearish, sectorBullish, sectorAverageNews, score);
        }

        public static TipRanksTrendingCompany[] GetTrendingCompanies(RequestManager rm)
        {
            // Return a data object that contains:
            // all recent top stocks from TipRanks
            // other stuff maybe?

            string responseString = rm.GetFromUri(TipRanksBaseUrl + "gettrendingstocks/?daysago=30&which=most");
            TipRanksTrendingCompany[] trResponse = JsonConvert.DeserializeObject<TipRanksTrendingCompany[]>(responseString);
            return trResponse;
        }

        // Super hacky date string converter because C# hates dd/MM/yy and it is super inconsistent with 1/11/23 for example
        private static string ConvertDateStr(string dateStr)
        {
            string[] tokens = dateStr.Split("/");
            tokens[2] = "20" + tokens[2];
            if (tokens[0].Length == 1)
            {
                tokens[0] = "0" + tokens[0];
            }
            if (tokens[1].Length == 1)
            {
                tokens[1] = "0" + tokens[1];
            }
            string convertedDateStr = string.Join("/", tokens);
            return convertedDateStr;
        }

        private static decimal GetAverageRating(List<Expert> ratings, TipRanksDataResponse trResponse)
        {
            // find the average rating normally
            if (ratings.Count > 0)
            {
                List<decimal> ratingNums = ratings
                    .Select(x => Convert.ToDecimal(x.rankings.FirstOrDefault().stars))
                    .ToList();

                return ratingNums.Sum() / ratingNums.Count;
            }
            else
            {
                // Use consensuses to get average rating if it cannot be parsed from trResponse.experts
                DateTime startDate = DateTime.Now.AddMonths(-3);
                List<TipRanksDataResponse.Consensus> consensuses = trResponse.consensuses
                    .Where(x => DateTime.Compare(DateTime.ParseExact(ConvertDateStr(x.d), "dd/MM/yyyy", CultureInfo.InvariantCulture), startDate) > 0)
                    .ToList();

                List<decimal> mStars = consensuses
                    .Select(x => Convert.ToDecimal(x.mStars))
                    .ToList();

                decimal averageMStars = mStars.Sum() / mStars.Count;
                decimal latestRating = Convert.ToDecimal(consensuses[0].rating);
                return latestRating;
            }
        }

        private static decimal GetBsnBonus(List<BestConsensusOverTime> bsns)
        {
            // Add hedge fund buy sell rankings bonus
            // find the slope of the buy and sell consensuses (net) from bsns
            List<decimal> bsnYList = bsns
                .Select(x => Convert.ToDecimal(x.buy - x.sell))
                .ToList();

            List<decimal> bsnXList = new List<decimal>();
            int counter = 1;
            foreach (decimal bsn in bsnYList)
            {
                bsnXList.Add(counter);
                counter++;
            }

            bool hasBsns = bsnXList.Count > 0;
            decimal bsnSlope = hasBsns ? Maths.GetSlope(bsnXList, bsnYList) : 0;
            decimal bsnSlopeMultiplier = Maths.GetSlopeMultiplier(bsnSlope);
            decimal bsnBonus = 0;
            if (hasBsns)
            {
                decimal bsnWeight = Constants.CORE_BONUS / 2;
                bsnBonus += (bsnYList[bsnYList.Count - 1] * bsnWeight) + ((decimal)bsns[^1].consensus);
                bsnBonus += bsnSlope >= 0 ? (bsnSlope * bsnSlopeMultiplier) + bsnWeight : -(bsnSlope * bsnSlopeMultiplier) - bsnWeight;
                bsnBonus = Math.Min(15, bsnBonus); // Limit bsn bonus to 15
            }
            return bsnBonus;
        }

        private static decimal GetInsiderBonus(List<Insider> insiders)
        {
            // Add insider bonuses, 3 points per insider if more than 1mil holding
            // Add insider bonuses, 2 points per insider if more than 500k holding
            // Add insider bonuses, 1 points per insider if less than 500k holding
            // Add insider bonuses, negative inverse for above on sells
            decimal insiderBonus = 0;
            foreach (var insider in insiders)
            {
                decimal amount = Convert.ToDecimal(insider.amount);

                int curAction = Convert.ToInt32(insider.action);
                bool isBuy = InsiderBuyActions.Contains(curAction);
                bool isSell = InsiderSellActions.Contains(curAction);

                // Penalty cases
                if (isSell && amount < 500000)
                {
                    insiderBonus += -1;
                }
                else if (isSell && amount >= 500000 && amount < 1000000)
                {
                    insiderBonus += -2;
                }
                else if (isSell && amount >= 1000000)
                {
                    insiderBonus += -3;
                }

                // Reward cases
                else if (isBuy && amount < 500000)
                {
                    insiderBonus += 1;
                }
                else if (isBuy && amount >= 500000 && amount < 1000000)
                {
                    insiderBonus += 2;
                }
                else if (isBuy && amount >= 1000000)
                {
                    insiderBonus += 3;
                }
            }
            // Limit insider bonus to -10 and 15
            insiderBonus = Math.Max(-10, insiderBonus);
            insiderBonus = Math.Min(15, insiderBonus);
            return insiderBonus;
        }

        private static decimal GetHoldingBonus(List<HoldingsByTime> holdings)
        {
            // All reward cases, experiment with pi
            decimal holdingBonus = 0;
            foreach (var hold in holdings)
            {
                decimal amountHolding = Convert.ToDecimal(hold.holdingAmount);
                var iPercentage = hold.institutionHoldingPercentage;

                // Add holding bonuses if institutional holding percent > 0
                if (iPercentage != null && iPercentage > 0)
                {
                    holdingBonus += Constants.CORE_BONUS * 2;
                }
                // Add holding bonuses if 1mil or more holding
                if (amountHolding >= 1000000)
                {
                    holdingBonus += Constants.CORE_BONUS * 2;
                }
                // Add smaller holding bonuses if more than 0 holding
                else if (amountHolding > 0)
                {
                    holdingBonus += Constants.CORE_BONUS;
                }
            }
            // Limit holding bonus to 15
            holdingBonus = Math.Min(15, holdingBonus);
            return holdingBonus;
        }

        private static decimal GetHedgeSentimentBonus(HedgeFundData hedgeData)
        {
            decimal hedgeBonus = 0;
            decimal sentiment = Convert.ToDecimal(hedgeData.sentiment);
            //int trendAction = Convert.ToInt32(hedgeData.trendAction);
            decimal trendValue = Convert.ToDecimal(hedgeData.trendValue);

            // Penalty cases
            if (0.35M <= sentiment && sentiment < 0.5M)
            {
                hedgeBonus += (sentiment * -1) - 3;
            }
            else if (0.0M <= sentiment && sentiment < 0.35M)
            {
                hedgeBonus += (sentiment * -7) - 7;
            }
            // Reward cases
            else if (sentiment == 0.5M)
            {
                hedgeBonus += sentiment + 1;
            }
            else if (0.5M < sentiment && sentiment < 0.6M)
            {
                hedgeBonus += (sentiment * 2) + 3;
            }
            else if (0.6M <= sentiment && sentiment < 0.7M)
            {
                hedgeBonus += (sentiment * 4) + 6;
            }
            else if (sentiment >= 0.7M)
            {
                hedgeBonus += (sentiment * 7) + 7;
            }

            // Add little trend value bonus although Idk
            decimal trendValueBonus = 0;
            if (trendValue > 0)
            {
                trendValueBonus = 2.0M * Constants.CORE_BONUS;
            }
            else if (trendValue < 0)
            {
                trendValueBonus = -2.0M * Constants.CORE_BONUS;
            }
            // Limit hedge sentiment bonus to 15
            hedgeBonus += trendValueBonus;
            hedgeBonus = Math.Min(15, hedgeBonus);
            return hedgeBonus;
        }
    }
}
