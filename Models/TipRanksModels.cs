using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PT.Models
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
        public int? sectorID { get; set; }
        public string market { get; set; }
        public string description { get; set; }
        public bool hasEarnings { get; set; }
        public bool hasDividends { get; set; }
        public Price[] prices { get; set; }
        public Consensus[] consensuses { get; set; }
        public Expert[] experts { get; set; }
        public Ptconsensu[] ptConsensus { get; set; }
        public Similarstock[] similarStocks { get; set; }
        public object insiderTrading { get; set; }
        public int? numOfAnalysts { get; set; }
        public int? numOfBloggers { get; set; }
        public int? numOfExperts { get; set; }
        public long? marketCap { get; set; }
        public Tipranksstockscore tipranksStockScore { get; set; }
        public int? expertRatingsFilteredCount { get; set; }
        public Consensusovertime[] consensusOverTime { get; set; }
        public Bestconsensusovertime[] bestConsensusOverTime { get; set; }
        public Bpbloggers bpBloggers { get; set; }
        public Bloggerarticledistribution[] bloggerArticleDistribution { get; set; }
        public Bloggersentiment bloggerSentiment { get; set; }
        public Corporateinsidertransaction[] corporateInsiderTransactions { get; set; }
        public Corporateinsideractivity corporateInsiderActivity { get; set; }
        public Insider1[] insiders { get; set; }
        public Insidrconfidencesignal insidrConfidenceSignal { get; set; }
        public object[] notRankedExperts { get; set; }
        public object notRankedConsensuses { get; set; }
        public Topstocksbysector topStocksBySector { get; set; }
        public int? indexStockId { get; set; }
        public int? numOfInsiders { get; set; }
        public float? yearlyDividendYield { get; set; }
        public float? yearlyDividend { get; set; }
        public float? insiderslast3MonthsSum { get; set; }
        public Hedgefunddata hedgeFundData { get; set; }
        public int? stockId { get; set; }
        public int? followerCount { get; set; }
        public Momentum momentum { get; set; }
        public Portfolioholdingdata portfolioHoldingData { get; set; }

        public class Tipranksstockscore
        {
            public int? score { get; set; }
            public float? returnOnAssets { get; set; }
            public float? returnOnEquity { get; set; }
            public float? sixMonthsMomentum { get; set; }
            public float? volatilityLevel { get; set; }
            public int? volatilityLevelRating { get; set; }
            public float? twelveMonthsMomentum { get; set; }
            public float? simpleMovingAverage { get; set; }
            public float? assetGrowth { get; set; }
        }

        public class Bpbloggers
        {
            public string[] bullish { get; set; }
            public object[] bearish { get; set; }
        }

        public class Bloggersentiment
        {
            public string bearish { get; set; }
            public string bullish { get; set; }
            public int? bullishCount { get; set; }
            public int? bearishCount { get; set; }
            public int? score { get; set; }
            public float? avg { get; set; }
            public string neutral { get; set; }
            public int? neutralCount { get; set; }
        }

        public class Corporateinsideractivity
        {
            public int? informativeSum { get; set; }
            public int? nonInformativeSum { get; set; }
            public int? totalSum { get; set; }
            public Informative[] informative { get; set; }
            public Noninformative[] nonInformative { get; set; }
        }

        public class Informative
        {
            public int? transactionTypeID { get; set; }
            public int? count { get; set; }
            public float? amount { get; set; }
        }

        public class Noninformative
        {
            public int? transactionTypeID { get; set; }
            public int? count { get; set; }
            public float? amount { get; set; }
        }

        public class Insidrconfidencesignal
        {
            public float? stockScore { get; set; }
            public float? sectorScore { get; set; }
            public int? score { get; set; }
        }

        public class Topstocksbysector
        {
            public Analyst[] analysts { get; set; }
            public Blogger[] bloggers { get; set; }
            public Insider[] insiders { get; set; }
        }

        public class Analyst
        {
            public string ticker { get; set; }
            public string name { get; set; }
            public Consensusdata consensusData { get; set; }
        }

        public class Consensusdata
        {
            public int? rating { get; set; }
            public int? nB { get; set; }
            public int? nH { get; set; }
            public int? nS { get; set; }
            public int? period { get; set; }
            public int? bench { get; set; }
            public int? mStars { get; set; }
            public object d { get; set; }
            public int? isLatest { get; set; }
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
            public int? bullishCount { get; set; }
            public int? bearishCount { get; set; }
            public int? score { get; set; }
            public float? avg { get; set; }
            public string neutral { get; set; }
            public int? neutralCount { get; set; }
        }

        public class Insider
        {
            public string ticker { get; set; }
            public string name { get; set; }
            public Confidencesignal confidenceSignal { get; set; }
        }

        public class Confidencesignal
        {
            public float? stockScore { get; set; }
            public float? sectorScore { get; set; }
            public int? score { get; set; }
        }

        public class Hedgefunddata
        {
            public int? stockID { get; set; }
            public Holdingsbytime[] holdingsByTime { get; set; }
            public float? sentiment { get; set; }
            public int? trendAction { get; set; }
            public float? trendValue { get; set; }
            public Institutionalholding[] institutionalHoldings { get; set; }
        }

        public class Holdingsbytime
        {
            public DateTime date { get; set; }
            public int? holdingAmount { get; set; }
            public float? institutionHoldingPercentage { get; set; }
            public bool isComplete { get; set; }
        }

        public class Institutionalholding
        {
            public int? institutionID { get; set; }
            public string managerName { get; set; }
            public string institutionName { get; set; }
            public int? action { get; set; }
            public int? value { get; set; }
            public string expertUID { get; set; }
            public float? change { get; set; }
            public float? percentageOfPortfolio { get; set; }
            public int? rank { get; set; }
            public int? totalRankedInstitutions { get; set; }
            public string imageURL { get; set; }
            public bool isActive { get; set; }
            public float? stars { get; set; }
        }

        public class Momentum
        {
            public DateTime baseDate { get; set; }
            public float? basePrice { get; set; }
            public float? latestPrice { get; set; }
            public DateTime latestDate { get; set; }
            public float? momentum { get; set; }
        }

        public class Portfolioholdingdata
        {
            public string ticker { get; set; }
            public string stockType { get; set; }
            public string sectorId { get; set; }
            public Analystconsensus analystConsensus { get; set; }
            public Bestanalystconsensus bestAnalystConsensus { get; set; }
            public object nextDividendDate { get; set; }
            public Lastreportedeps lastReportedEps { get; set; }
            public Nextearningsreport nextEarningsReport { get; set; }
            public string stockUid { get; set; }
            public string companyName { get; set; }
            public float? priceTarget { get; set; }
            public float? bestPriceTarget { get; set; }
            public float? dividend { get; set; }
            public float? dividendYield { get; set; }
            public float? peRatio { get; set; }
            public int? stockId { get; set; }
            public object high52Weeks { get; set; }
            public object low52Weeks { get; set; }
            public Hedgefundsentimentdata hedgeFundSentimentData { get; set; }
            public Insidersentimentdata insiderSentimentData { get; set; }
            public Bloggersentimentdata bloggerSentimentData { get; set; }
            public bool shouldAddLinkToStockPage { get; set; }
            public object expenseRatio { get; set; }
            public long? marketCap { get; set; }
            public int? newsSentiment { get; set; }
            public Landmarkprices landmarkPrices { get; set; }
            public int? priceTargetCurrencyId { get; set; }
        }

        public class Analystconsensus
        {
            public string consensus { get; set; }
            public int? rawConsensus { get; set; }
            public Distribution distribution { get; set; }
        }

        public class Distribution
        {
            public float? buy { get; set; }
            public float? hold { get; set; }
            public float? sell { get; set; }
        }

        public class Bestanalystconsensus
        {
            public string consensus { get; set; }
            public int? rawConsensus { get; set; }
            public Distribution1 distribution { get; set; }
        }

        public class Distribution1
        {
            public float? buy { get; set; }
            public float? hold { get; set; }
            public float? sell { get; set; }
        }

        public class Lastreportedeps
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
            public Ratingsandpt ratingsAndPT { get; set; }
            public Bpratingsandpt bpRatingsAndPT { get; set; }
            public long? marketCap { get; set; }
            public int? sector { get; set; }
            public int? stockId { get; set; }
            public int? stockTypeId { get; set; }
            public float? surprise { get; set; }
            public int? timeOfDay { get; set; }
            public bool isConfirmed { get; set; }
        }

        public class Ratingsandpt
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class Bpratingsandpt
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class Nextearningsreport
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
            public Ratingsandpt1 ratingsAndPT { get; set; }
            public Bpratingsandpt1 bpRatingsAndPT { get; set; }
            public long? marketCap { get; set; }
            public int? sector { get; set; }
            public int? stockId { get; set; }
            public int? stockTypeId { get; set; }
            public object surprise { get; set; }
            public int? timeOfDay { get; set; }
            public bool isConfirmed { get; set; }
        }

        public class Ratingsandpt1
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class Bpratingsandpt1
        {
            public object priceTarget { get; set; }
            public object numBuys { get; set; }
            public object numHolds { get; set; }
            public object numSells { get; set; }
        }

        public class Hedgefundsentimentdata
        {
            public int? rating { get; set; }
            public float? score { get; set; }
        }

        public class Insidersentimentdata
        {
            public int? rating { get; set; }
            public float? stockScore { get; set; }
        }

        public class Bloggersentimentdata
        {
            public int? ratingIfExists { get; set; }
            public int? rating { get; set; }
            public int? bearishCount { get; set; }
            public int? bullishCount { get; set; }
        }

        public class Landmarkprices
        {
            public Yeartodate yearToDate { get; set; }
            public Threemonthsago threeMonthsAgo { get; set; }
            public Yearago yearAgo { get; set; }
        }

        public class Yeartodate
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class Threemonthsago
        {
            public DateTime date { get; set; }
            public string d { get; set; }
            public float? p { get; set; }
        }

        public class Yearago
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
            public int? rating { get; set; }
            public int? nB { get; set; }
            public int? nH { get; set; }
            public int? nS { get; set; }
            public int? period { get; set; }
            public int? bench { get; set; }
            public int? mStars { get; set; }
            public string d { get; set; }
            public int? isLatest { get; set; }
            public object priceTarget { get; set; }
        }

        public class Expert
        {
            public string name { get; set; }
            public string firm { get; set; }
            public string eUid { get; set; }
            public int? eTypeId { get; set; }
            public string expertImg { get; set; }
            public Rating[] ratings { get; set; }
            public float? stockSuccessRate { get; set; }
            public float? stockAverageReturn { get; set; }
            public int? stockTotalRecommendations { get; set; }
            public int? stockGoodRecommendations { get; set; }
            public Ranking[] rankings { get; set; }
            public int? stockid { get; set; }
            public string newPictureUrl { get; set; }
            public bool includedInConsensus { get; set; }
        }

        public class Rating
        {
            public int? ratingId { get; set; }
            public int? actionId { get; set; }
            public DateTime date { get; set; }
            public string d { get; set; }
            public string url { get; set; }
            public int? pos { get; set; }
            public DateTime time { get; set; }
            public object priceTarget { get; set; }
            public object convertedPriceTarget { get; set; }
            public Quote quote { get; set; }
            public string siteName { get; set; }
            public string site { get; set; }
            public int? id { get; set; }
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
            public int? period { get; set; }
            public int? bench { get; set; }
            public int? lRank { get; set; }
            public int? gRank { get; set; }
            public int? gRecs { get; set; }
            public int? tRecs { get; set; }
            public float? avgReturn { get; set; }
            public float? stars { get; set; }
            public float? originalStars { get; set; }
            public float? tPos { get; set; }
        }

        public class Ptconsensu
        {
            public int? period { get; set; }
            public int? bench { get; set; }
            public float? priceTarget { get; set; }
            public int? priceTargetCurrency { get; set; }
            public string priceTargetCurrencyCode { get; set; }
            public float? high { get; set; }
            public float? low { get; set; }
        }

        public class Similarstock
        {
            public string uid { get; set; }
            public string name { get; set; }
            public string ticker { get; set; }
            public string mktCap { get; set; }
            public int? sectorId { get; set; }
            public int? stockTypeId { get; set; }
            public Consensusdata1[] consensusData { get; set; }
        }

        public class Consensusdata1
        {
            public int? nTotal { get; set; }
            public int? nB { get; set; }
            public int? nH { get; set; }
            public int? nS { get; set; }
            public int? period { get; set; }
            public int? benchmark { get; set; }
            public int? wCon { get; set; }
            public float? priceTarget { get; set; }
            public object priceTargetCurrency { get; set; }
        }

        public class Consensusovertime
        {
            public int? buy { get; set; }
            public int? hold { get; set; }
            public int? sell { get; set; }
            public DateTime date { get; set; }
            public int? consensus { get; set; }
            public float? priceTarget { get; set; }
        }

        public class Bestconsensusovertime
        {
            public int? buy { get; set; }
            public int? hold { get; set; }
            public int? sell { get; set; }
            public DateTime date { get; set; }
            public int? consensus { get; set; }
            public float? priceTarget { get; set; }
        }

        public class Bloggerarticledistribution
        {
            public string site { get; set; }
            public string siteName { get; set; }
            public string percentage { get; set; }
        }

        public class Corporateinsidertransaction
        {
            public object sharesBought { get; set; }
            public int? insidersBuyCount { get; set; }
            public object sharesSold { get; set; }
            public int? insidersSellCount { get; set; }
            public int? month { get; set; }
            public int? year { get; set; }
            public int? transBuyCount { get; set; }
            public int? transSellCount { get; set; }
            public float? transBuyAmount { get; set; }
            public float? transSellAmount { get; set; }
            public int? informativeBuyCount { get; set; }
            public int? informativeSellCount { get; set; }
            public float? informativeBuyAmount { get; set; }
            public float? informativeSellAmount { get; set; }
        }

        public class Insider1
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
            public int? transTypeId { get; set; }
            public int? action { get; set; }
            public string date { get; set; }
            public float? amount { get; set; }
            public int? rank { get; set; }
            public float? stars { get; set; }
            public string expertImg { get; set; }
            public DateTime rDate { get; set; }
            public string newPictureUrl { get; set; }
            public string link { get; set; }
            public int? numberOfShares { get; set; }
        }
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
        public int? sectorId { get; set; }
        public DateTime creationDate { get; set; }
        public float? sectorAverageNewsScore { get; set; }

        public class Buzz
        {
            public int? articlesInLastWeek { get; set; }
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
            public int? stockID { get; set; }
            public string ticker { get; set; }
            public string companyName { get; set; }
            public float? bullishPercent { get; set; }
            public float? bearishPercent { get; set; }
        }

        public class Wordcloud
        {
            public string ticker { get; set; }
            public int? stockID { get; set; }
            public string text { get; set; }
            public DateTime effectiveDate { get; set; }
            public int? grade { get; set; }
            public int? wordCloudEventID { get; set; }
            public DateTime addedOn { get; set; }
        }

        public class Count
        {
            public int? buy { get; set; }
            public int? sell { get; set; }
            public int? all { get; set; }
            public DateTime weekStart { get; set; }
            public int? neutral { get; set; }
        }
    }

    public class TipRanksTrendingCompany
    {
        public string ticker { get; set; }
        public int? popularity { get; set; }
        public int? sentiment { get; set; }
        public float? consensusScore { get; set; }
        public object operations { get; set; }
        public string sector { get; set; }
        public int? sectorID { get; set; }
        public long? marketCap { get; set; }
        public int? buy { get; set; }
        public int? sell { get; set; }
        public int? hold { get; set; }
        public float? priceTarget { get; set; }
        public int? rating { get; set; }
        public string companyName { get; set; }
        public int? quarterlyTrend { get; set; }
        public DateTime lastRatingDate { get; set; }
    }
}