export interface Stock {
  // Basics
  dataProviders: string
  price: number
  priceHistoryDays: number
  symbol: string

  // Composites
  adxComposite: number
  aroonComposite: number
  compositeRank: string
  compositeScoreValue: number
  fundamentalsComposite: number
  macdComposite: number
  obvComposite: number
  ratingsComposite: number
  shortInterestComposite: number

  // Etc
  fundamentals: Fundamentals
  shortInterest: ShortInterest
  tipRanks: TipRanks
}

interface TipRanks {
  consensusOverTime: ConsensusOverTime[]
  errorMessage: string
  hedgeBonus: number
  hedgeSentiment: number
  hedgeTrendAction: number
  hedgeTrendValue: number
  holdingBonus: number
  holdings: Holding[]
  insiderBonus: number
  insiders: Insider[]
  priceTarget: number
  ratingsComposite: number
  thirdPartyRatings: ThirdPartyRating[]
}

interface ConsensusOverTime {
  buy: number
  consensus: number
  date: string
  hold: number
  priceTarget: number
  sell: number
}

interface ThirdPartyRating {
  eTypeId: number
  eUid: string
  expertImg?: string
  firm: string
  includedInConsensus: boolean
  newPictureUrl?: string
  rankings: Ranking[]
  ratings: Rating[]
  stockAverageReturn: number
  stockGoodRecommendations: number
  stockid: number
  stockSuccessRate: number
  stockTotalRecommendations: number
}

interface Ranking {
  avgReturn: number
  bench: number
  gRank: number
  gRecs: number
  lRank: number
  originalStars: number
  period: number
  stars: number
  tPos: number
  tRecs: number
}

interface Rating {
  actionId: number
  convertedPriceTargetCurrency: number
  convertedPriceTargetCurrencyCode: string
  d: string
  date: string
  id: number
  pos: number
  priceTargetCurrency: number
  priceTargetCurrencyCode: string
  quote: Quote
  ratingId: number
  rD: string
  time: string
  timestamp: string
}

interface Quote {
  date: string
  link: string
  quote: string
  site: string
  siteName: string
  title: string
}

interface Holding {
  date: string
  holdingAmount: number
  institutionHoldingPercentage: number
  isComplete: boolean
}

interface Insider {
  action: number
  amount: number
  company: string
  date: string
  isDirector: boolean
  isOfficer: boolean
  isOther: boolean
  isTenPercentOwner: boolean
  link: string
  name: string
  numberOfShares: number
  officerTitle: string
  otherText: string
  rank: number
  rDate: string
  stars: number
  transTypeId: number
  uId: string
}

interface Fundamentals {
  averageEPS: number
  averagePE: number
  averageVolumeUSD: number
  fundamentalsComposite: number
  growthEPS: number
  growthPE: number
  hasDividends: boolean
  isBlacklisted: boolean
  message: string
  priceSlope: number
  volumeSlope: number
  volumeUSD: number
}

interface ShortInterest {
  shortInterestCompositeScore: number
  shortInterestPercentAverage: number
  shortInterestPercentToday: number
  shortInterestSlope: number
  totalVolume: number
  totalVolumeShort: number
}
