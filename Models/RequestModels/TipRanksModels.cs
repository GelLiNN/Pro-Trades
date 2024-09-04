using static PT.Models.RequestModels.TipRanksDataResponse;

namespace PT.Models.RequestModels
{
    /*
     * TipRanks Models
     * https://stackoverflow.com/questions/21611674/how-to-auto-generate-a-c-sharp-class-file-from-a-json-string
     * Generated with Visual Studio (Web Essentials) Edit -> Paste Special -> Paste JSON As Classes
     * Moved classes following the root object inside the root object
     * Find and replace all number types to be nullable i.e. "float " -> "float? "
     */
    public class TipRanksDataResponse
    {
        public string ticker { get; set; }
        public string companyName { get; set; }
        public string companyFullName { get; set; }
        public string stockUid { get; set; }
        public long? sectorID { get; set; }
        public string market { get; set; }
        public string description { get; set; }
        public bool hasEarnings { get; set; }
        public bool hasDividends { get; set; }
        public Price[] prices { get; set; }
        public Consensus[] consensuses { get; set; }
        public Expert[] experts { get; set; }
        public PtConsensus[] ptConsensus { get; set; }
        public SimilarStock[] similarStocks { get; set; }
        public object insiderTrading { get; set; }
        public long? numOfAnalysts { get; set; }
        public long? numOfBloggers { get; set; }
        public long? numOfExperts { get; set; }
        public long? marketCap { get; set; }
        public TipRanksStockScore tipranksStockScore { get; set; }
        public long? expertRatingsFilteredCount { get; set; }
        public ConsensusOverTime[] consensusOverTime { get; set; }
        public BestConsensusOverTime[] bestConsensusOverTime { get; set; }
        public BpBloggers bpBloggers { get; set; }
        public BloggerArticleDistribution[] bloggerArticleDistribution { get; set; }
        public BloggerSentiment bloggerSentiment { get; set; }
        public CorporateInsiderTransaction[] corporateInsiderTransactions { get; set; }
        public CorporateInsiderActivity corporateInsiderActivity { get; set; }
        public Insider[] insiders { get; set; }
        public InsiderConfidenceSignal insidrConfidenceSignal { get; set; }
        public object[] notRankedExperts { get; set; }
        public object notRankedConsensuses { get; set; }
        public TopStocksBySector topStocksBySector { get; set; }
        public long? indexStockId { get; set; }
        public long? numOfInsiders { get; set; }
        public float? yearlyDividendYield { get; set; }
        public float? yearlyDividend { get; set; }
        public float? insiderslast3MonthsSum { get; set; }
        public HedgeFundData hedgeFundData { get; set; }
        public long? stockId { get; set; }
        public long? followerCount { get; set; }
        public Momentum momentum { get; set; }
        public PortfolioHoldingData portfolioHoldingData { get; set; }

        public class TipRanksStockScore
        {
            public long? score { get; set; }
            public float? returnOnAssets { get; set; }
            public float? returnOnEquity { get; set; }
            public float? sixMonthsMomentum { get; set; }
            public float? volatilityLevel { get; set; }
            public long? volatilityLevelRating { get; set; }
            public float? twelveMonthsMomentum { get; set; }
            public float? simpleMovingAverage { get; set; }
            public float? assetGrowth { get; set; }
        }

        public class BpBloggers
        {
            public string[] bullish { get; set; }
            public object[] bearish { get; set; }
        }

        public class BloggerSentiment
        {
            public string bearish { get; set; }
            public string bullish { get; set; }
            public long? bullishCount { get; set; }
            public long? bearishCount { get; set; }
            public long? score { get; set; }
            public float? avg { get; set; }
            public string neutral { get; set; }
            public long? neutralCount { get; set; }
        }

        public class CorporateInsiderActivity
        {
            public long? informativeSum { get; set; }
            public long? nonInformativeSum { get; set; }
            public long? totalSum { get; set; }
            public Informative[] informative { get; set; }
            public NonInformative[] nonInformative { get; set; }
        }

        public class Informative
        {
            public long? transactionTypeID { get; set; }
            public long? count { get; set; }
            public float? amount { get; set; }
        }

        public class NonInformative
        {
            public long? transactionTypeID { get; set; }
            public long? count { get; set; }
            public float? amount { get; set; }
        }

        public class InsiderConfidenceSignal
        {
            public float? stockScore { get; set; }
            public float? sectorScore { get; set; }
            public long? score { get; set; }
        }

        public class TopStocksBySector
        {
            public Analyst[] analysts { get; set; }
            public Blogger[] bloggers { get; set; }
            public SectorInsider[] insiders { get; set; }
        }

        public class Analyst
        {
            public string ticker { get; set; }
            public string name { get; set; }
            public AnalystConsensusData consensusData { get; set; }
        }

        public class AnalystConsensusData
        {
            public long? rating { get; set; }
            public long? nB { get; set; }
            public long? nH { get; set; }
            public long? nS { get; set; }
            public long? period { get; set; }
            public long? bench { get; set; }
            public long? mStars { get; set; }
            public object d { get; set; }
            public long? isLatest { get; set; }
            public object priceTarget { get; set; }
        }

        public class Blogger
        {
            public string ticker { get; set; }
            public string name { get; set; }
            public Sentiment sentiment { get; set; }
        }

        public class Sentiment
        {
            public string bearish { get; set; }
            public string bullish { get; set; }
            public long? bullishCount { get; set; }
            public long? bearishCount { get; set; }
            public long? score { get; set; }
            public float? avg { get; set; }
            public string neutral { get; set; }
            public long? neutralCount { get; set; }
        }

        public class SectorInsider
        {
            public string ticker { get; set; }
            public string name { get; set; }
            public ConfidenceSignal confidenceSignal { get; set; }
        }

        public class ConfidenceSignal
        {
            public float? stockScore { get; set; }
            public float? sectorScore { get; set; }
            public long? score { get; set; }
        }

        public class Momentum
        {
            public DateTime baseDate { get; set; }
            public float? basePrice { get; set; }
            public float? latestPrice { get; set; }
            public DateTime latestDate { get; set; }
            public float? momentum { get; set; }
        }

        public class PortfolioHoldingData
        {
            public string ticker { get; set; }
            public string stockType { get; set; }
            public string sectorId { get; set; }
            public AnalystConsensus analystConsensus { get; set; }
            public BestAnalystConsensus bestAnalystConsensus { get; set; }
            public object nextDividendDate { get; set; }
            public LastReportedEps lastReportedEps { get; set; }
            public NextEarningsReport nextEarningsReport { get; set; }
            public string stockUid { get; set; }
            public string companyName { get; set; }
            public float? priceTarget { get; set; }
            public float? bestPriceTarget { get; set; }
            public float? dividend { get; set; }
            public float? dividendYield { get; set; }
            public float? peRatio { get; set; }
            public long? stockId { get; set; }
            public object high52Weeks { get; set; }
            public object low52Weeks { get; set; }
            public HedgeFundSentimentData hedgeFundSentimentData { get; set; }
            public InsiderSentimentData insiderSentimentData { get; set; }
            public BloggerSentimentData bloggerSentimentData { get; set; }
            public bool shouldAddLinkToStockPage { get; set; }
            public object expenseRatio { get; set; }
            public long? marketCap { get; set; }
            public long? newsSentiment { get; set; }
            public Landmarkprices landmarkPrices { get; set; }
            public long? priceTargetCurrencyId { get; set; }
        }

        public class AnalystConsensus
        {
            public string consensus { get; set; }
            public long? rawConsensus { get; set; }
            public Distribution distribution { get; set; }
        }

        public class BestAnalystConsensus
        {
            public string consensus { get; set; }
            public long? rawConsensus { get; set; }
            public Distribution distribution { get; set; }
        }

        public class Distribution
        {
            public float? buy { get; set; }
            public float? hold { get; set; }
            public float? sell { get; set; }
        }

        public class LastReportedEps
        {
            public DateTime date { get; set; }
            public string company { get; set; }
            public string ticker { get; set; }
            public string periodEnding { get; set; }
            public string eps { get; set; }
            public string reportedEPS { get; set; }
            public string lastEps { get; set; }
            public object consensus { get; set; }
            public object bpConsensus { get; set; }
            public RatingsAndPt ratingsAndPT { get; set; }
            public BpRatingsAndPt bpRatingsAndPT { get; set; }
            public long? marketCap { get; set; }
            public long? sector { get; set; }
            public long? stockId { get; set; }
            public long? stockTypeId { get; set; }
            public float? surprise { get; set; }
            public long? timeOfDay { get; set; }
            public bool isConfirmed { get; set; }
        }

        public class RatingsAndPt
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class BpRatingsAndPt
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class NextEarningsReport
        {
            public DateTime date { get; set; }
            public string company { get; set; }
            public string ticker { get; set; }
            public string periodEnding { get; set; }
            public string eps { get; set; }
            public object reportedEPS { get; set; }
            public string lastEps { get; set; }
            public object consensus { get; set; }
            public object bpConsensus { get; set; }
            public RatingsAndPt ratingsAndPT { get; set; }
            public BpRatingsAndPt bpRatingsAndPT { get; set; }
            public long? marketCap { get; set; }
            public long? sector { get; set; }
            public long? stockId { get; set; }
            public long? stockTypeId { get; set; }
            public object surprise { get; set; }
            public long? timeOfDay { get; set; }
            public bool isConfirmed { get; set; }
        }

        public class HedgeFundSentimentData
        {
            public long? rating { get; set; }
            public float? score { get; set; }
        }

        public class InsiderSentimentData
        {
            public long? rating { get; set; }
            public float? stockScore { get; set; }
        }

        public class BloggerSentimentData
        {
            public long? ratingIfExists { get; set; }
            public long? rating { get; set; }
            public long? bearishCount { get; set; }
            public long? bullishCount { get; set; }
        }

        public class Landmarkprices
        {
            public YearToDate yearToDate { get; set; }
            public ThreeMonthsAgo threeMonthsAgo { get; set; }
            public YearAgo yearAgo { get; set; }
        }

        public class YearToDate
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class ThreeMonthsAgo
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class YearAgo
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class Price
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class Consensus
        {
            public long? rating { get; set; }
            public long? nB { get; set; }
            public long? nH { get; set; }
            public long? nS { get; set; }
            public long? period { get; set; }
            public long? bench { get; set; }
            public long? mStars { get; set; }
            public string d { get; set; }
            public long? isLatest { get; set; }
            public object priceTarget { get; set; }
        }

        public class PtConsensus
        {
            public long? period { get; set; }
            public long? bench { get; set; }
            public float? priceTarget { get; set; }
            public long? priceTargetCurrency { get; set; }
            public string priceTargetCurrencyCode { get; set; }
            public float? high { get; set; }
            public float? low { get; set; }
        }

        public class SimilarStock
        {
            public string uid { get; set; }
            public string name { get; set; }
            public string ticker { get; set; }
            public string mktCap { get; set; }
            public long? sectorId { get; set; }
            public long? stockTypeId { get; set; }
            public ConsensusData[] consensusData { get; set; }
        }

        public class ConsensusData
        {
            public long? nTotal { get; set; }
            public long? nB { get; set; }
            public long? nH { get; set; }
            public long? nS { get; set; }
            public long? period { get; set; }
            public long? benchmark { get; set; }
            public long? wCon { get; set; }
            public float? priceTarget { get; set; }
            public object priceTargetCurrency { get; set; }
        }

        public class BestConsensusOverTime
        {
            public long? buy { get; set; }
            public long? hold { get; set; }
            public long? sell { get; set; }
            public DateTime date { get; set; }
            public long? consensus { get; set; }
            public float? priceTarget { get; set; }
        }

        public class BloggerArticleDistribution
        {
            public string site { get; set; }
            public string siteName { get; set; }
            public string percentage { get; set; }
        }

        public class CorporateInsiderTransaction
        {
            public object sharesBought { get; set; }
            public long? insidersBuyCount { get; set; }
            public object sharesSold { get; set; }
            public long? insidersSellCount { get; set; }
            public long? month { get; set; }
            public long? year { get; set; }
            public long? transBuyCount { get; set; }
            public long? transSellCount { get; set; }
            public float? transBuyAmount { get; set; }
            public float? transSellAmount { get; set; }
            public long? informativeBuyCount { get; set; }
            public long? informativeSellCount { get; set; }
            public float? informativeBuyAmount { get; set; }
            public float? informativeSellAmount { get; set; }
        }
    }

    //Data PT will use for Composite Score
    public class Insider
    {
        public string uId { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public bool isOfficer { get; set; }
        public bool isDirector { get; set; }
        public bool isTenPercentOwner { get; set; }
        public bool isOther { get; set; }
        public string officerTitle { get; set; }
        public string otherText { get; set; }
        public long? transTypeId { get; set; }
        public long? action { get; set; }
        public string date { get; set; }
        public float? amount { get; set; }
        public long? rank { get; set; }
        public float? stars { get; set; }
        public string expertImg { get; set; }
        public DateTime rDate { get; set; }
        public string newPictureUrl { get; set; }
        public string link { get; set; }
        public long? numberOfShares { get; set; }
    }

    public class HedgeFundData
    {
        public long? stockID { get; set; }
        public HoldingsByTime[] holdingsByTime { get; set; }
        public float? sentiment { get; set; }
        public long? trendAction { get; set; }
        public float? trendValue { get; set; }
        public InstitutionalHolding[] institutionalHoldings { get; set; }
    }

    public class HoldingsByTime
    {
        public DateTime date { get; set; }
        public long? holdingAmount { get; set; }
        public float? institutionHoldingPercentage { get; set; }
        public bool isComplete { get; set; }
    }

    public class InstitutionalHolding
    {
        public long? institutionID { get; set; }
        public string managerName { get; set; }
        public string institutionName { get; set; }
        public long? action { get; set; }
        public long? value { get; set; }
        public string expertUID { get; set; }
        public float? change { get; set; }
        public float? percentageOfPortfolio { get; set; }
        public long? rank { get; set; }
        public long? totalRankedInstitutions { get; set; }
        public string imageURL { get; set; }
        public bool isActive { get; set; }
        public float? stars { get; set; }
    }

    public class ConsensusOverTime
    {
        public long? buy { get; set; }
        public long? hold { get; set; }
        public long? sell { get; set; }
        public DateTime date { get; set; }
        public long? consensus { get; set; }
        public float? priceTarget { get; set; }
    }

    public class Expert
    {
        public string name { get; set; }
        public string firm { get; set; }
        public string eUid { get; set; }
        public long? eTypeId { get; set; }
        public string expertImg { get; set; }
        public Rating[] ratings { get; set; }
        public float? stockSuccessRate { get; set; }
        public float? stockAverageReturn { get; set; }
        public long? stockTotalRecommendations { get; set; }
        public long? stockGoodRecommendations { get; set; }
        public Ranking[] rankings { get; set; }
        public long? stockid { get; set; }
        public string newPictureUrl { get; set; }
        public bool includedInConsensus { get; set; }
    }

    public class Rating
    {
        public long? ratingId { get; set; }
        public long? actionId { get; set; }
        public DateTime date { get; set; }
        public string d { get; set; }
        public string url { get; set; }
        public long? pos { get; set; }
        public DateTime time { get; set; }
        public object priceTarget { get; set; }
        public object convertedPriceTarget { get; set; }
        public Quote quote { get; set; }
        public string siteName { get; set; }
        public string site { get; set; }
        public long? id { get; set; }
        public DateTime rD { get; set; }
        public DateTime timestamp { get; set; }
        public object priceTargetCurrency { get; set; }
        public object convertedPriceTargetCurrency { get; set; }
        public object convertedPriceTargetCurrencyCode { get; set; }
        public object priceTargetCurrencyCode { get; set; }
    }

    public class Quote
    {
        public string title { get; set; }
        public DateTime date { get; set; }
        public object quote { get; set; }
        public string site { get; set; }
        public string link { get; set; }
        public string siteName { get; set; }
    }

    public class Ranking
    {
        public long? period { get; set; }
        public long? bench { get; set; }
        public long? lRank { get; set; }
        public long? gRank { get; set; }
        public long? gRecs { get; set; }
        public long? tRecs { get; set; }
        public float? avgReturn { get; set; }
        public float? stars { get; set; }
        public float? originalStars { get; set; }
        public float? tPos { get; set; }
    }

    public class TipRanksSentimentResponse
    {
        public string ticker { get; set; }
        public string companyName { get; set; }
        public Buzz buzz { get; set; }
        public Sentiment sentiment { get; set; }
        public Sector[] sector { get; set; }
        public float? sectorAverageBullishPercent { get; set; }
        public float? score { get; set; }
        public Wordcloud[] wordCloud { get; set; }
        public Count[] counts { get; set; }
        public long? sectorId { get; set; }
        public DateTime creationDate { get; set; }
        public float? sectorAverageNewsScore { get; set; }

        public class Buzz
        {
            public long? articlesInLastWeek { get; set; }
            public float? weeklyAverage { get; set; }
            public float? buzz { get; set; }
        }

        public class Sentiment
        {
            public float? bullishPercent { get; set; }
            public float? bearishPercent { get; set; }
        }

        public class Sector
        {
            public long? stockID { get; set; }
            public string ticker { get; set; }
            public string companyName { get; set; }
            public float? bullishPercent { get; set; }
            public float? bearishPercent { get; set; }
        }

        public class Wordcloud
        {
            public string ticker { get; set; }
            public long? stockID { get; set; }
            public string text { get; set; }
            public DateTime effectiveDate { get; set; }
            public long? grade { get; set; }
            public long? wordCloudEventID { get; set; }
            public DateTime addedOn { get; set; }
        }

        public class Count
        {
            public long? buy { get; set; }
            public long? sell { get; set; }
            public long? all { get; set; }
            public DateTime weekStart { get; set; }
            public long? neutral { get; set; }
        }
    }

    public class TipRanksTrendingCompany
    {
        public string ticker { get; set; }
        public long? popularity { get; set; }
        public long? sentiment { get; set; }
        public float? consensusScore { get; set; }
        public object operations { get; set; }
        public string sector { get; set; }
        public long? sectorID { get; set; }
        public long? marketCap { get; set; }
        public long? buy { get; set; }
        public long? sell { get; set; }
        public long? hold { get; set; }
        public float? priceTarget { get; set; }
        public long? rating { get; set; }
        public string companyName { get; set; }
        public long? quarterlyTrend { get; set; }
        public DateTime lastRatingDate { get; set; }
    }

    public class HedgeFundsResult
    {
        // ratings composite score
        public decimal RatingsComposite { get; set; }

        // price target
        public decimal PriceTarget { get; set; }

        // ratings base score from average rating
        public decimal RatingsBase { get; set; }

        // insiders from past 3 months
        public decimal InsiderBonus { get; set; }

        // holdings from past 3 months
        public decimal HoldingBonus { get; set; }

        // hedge sentiment bonus
        public decimal HedgeSentimentBonus { get; set; }

        // hedge bsn bonus
        public decimal HedgeBsnBonus { get; set; }

        // hedge sentiment value with .5 as the median
        public decimal HedgeSentiment { get; set; }

        // hedge trend actions mean something
        public decimal HedgeTrendAction { get; set; }

        // hedge trend values mean something WRT the trend value above
        public decimal HedgeTrendValue { get; set; }

        // list of insider purchases with dates, ranks, names
        public List<Insider> Insiders { get; set; }

        // list of hedge funds with holdings with amounts, ranks, names
        // list of institutional holdings by date going back 2 years
        public List<HoldingsByTime> Holdings { get; set; }

        // list of Expert Ratings with names, stars, original stars
        public List<Expert> ThirdPartyRatings { get; set; }

        // list of TipRanks consensus ratings and price targets by date going back 3 months
        public List<BestConsensusOverTime> ConsensusOverTime { get; set; }

        // keep track of failures
        public string ErrorMessage { get; set; }
    }
}