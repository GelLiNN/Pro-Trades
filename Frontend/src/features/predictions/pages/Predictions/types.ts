export type StockScoreRank = 'BAD' | 'FAIR' | 'GOOD' | 'PRIME'

export interface Stock {
  name: string
  price: number
  scoreRank: StockScoreRank
  scoreValue: number
  symbol: string
}

export interface HeadCell {
  align: 'center' | 'left' | 'right'
  isSortable: boolean
  key: keyof Stock
  label: string
}

export type Order = 'asc' | 'desc'
