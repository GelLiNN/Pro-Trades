import {HEADER_HEIGHT} from '@/constants'

import {Container} from '@mui/material'
import {Layout} from '@/components/Layout'
import {PredictionsTable} from './PredictionsTable'
// import {STOCKS} from '@/features/predictions/constants'

import type {Stock, StockScoreRank} from './types'

const createStock = (
  symbol: string,
  name: string,
  price: number,
  scoreRank: StockScoreRank,
  scoreValue: number
): Stock => ({
  name,
  price,
  scoreRank,
  scoreValue,
  symbol,
})

const rows: Stock[] = [
  createStock('AAPL', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  createStock('MSFT', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  createStock('GOOG', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  createStock('GOOGL', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL1', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT1', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG1', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL1', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL2', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT2', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG2', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL2', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL3', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT3', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG3', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL3', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL4', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT4', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG4', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL4', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL11', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT11', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG11', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL11', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL21', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT21', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG21', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL21', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL31', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT31', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG31', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL31', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
  // createStock('AAPL41', 'Apple Inc.', 150.52, 'GOOD', 75.52),
  // createStock('MSFT41', 'Microsoft Corporation', 342.2, 'PRIME', 85.99),
  // createStock('GOOG41', 'Alphabet Inc. Class C', 1721.0, 'FAIR', 65.1),
  // createStock('GOOGL41', 'Alphabet Inc. Class A', 15.72, 'BAD', 59.01),
]

export const Predictions = () => {
  return (
    <Layout description='Predictive composite scores.' title='Predictions'>
      <Container
        component='main'
        maxWidth='md'
        sx={{height: `calc(100vh - ${HEADER_HEIGHT}px)`, pt: 2, pb: 4}}
      >
        <PredictionsTable rows={rows} />
      </Container>
    </Layout>
  )
}
