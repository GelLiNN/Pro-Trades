import {HEADER_HEIGHT} from '@/constants'

import {useGetPredictionsQuery} from '@/features/predictions/api'

import {Container} from '@mui/material'
import {Layout} from '@/components/Layout'
import {PredictionsTable} from './PredictionsTable'
// import {STOCKS} from '@/features/predictions/constants'

import type {Stock, StockScoreRank} from './types'

const createStock = (
  symbol: string,
  name: string,
  price: number,
  throughput: number,
  scoreValue: number,
  scoreRank: StockScoreRank
): Stock => ({
  name,
  price,
  scoreRank,
  scoreValue,
  symbol,
  throughput,
})

const DEFAULT_ROWS: Stock[] = [
  createStock('AAPL', 'Apple Inc.', 150.52, 1000000, 75.52, 'GOOD'),
  createStock('MSFT', 'Microsoft Corporation', 342.2, 150000000, 85.99, 'PRIME'),
  createStock('CHIK', 'Chicken Nuggets Corporation', 1721.0, 900000, 63.1, 'DISQUALIFIED'),
  createStock('GOOG', 'Alphabet Inc. Class C', 1721.0, 1900000, 65.1, 'FAIR'),
  createStock('GOOGL', 'Alphabet Inc. Class A', 15.72, 27910483, 59.01, 'BAD'),
  createStock('AMZN', 'Amazon.com, Inc.', 3456.32, 10000000, 55.12, 'BAD'),
  createStock('FB', 'Facebook, Inc.', 345.67, 5000000, 78.45, 'GOOD'),
  createStock('TSLA', 'Tesla, Inc.', 789.54, 2000000, 50.32, 'BAD'),
  createStock('NVDA', 'NVIDIA Corporation', 456.78, 3000000, 75.76, 'GOOD'),
  createStock('NFLX', 'Netflix, Inc.', 567.89, 1500000, 75.43, 'GOOD'),
  createStock('PYPL', 'PayPal Holdings, Inc.', 123.45, 2500000, 90.87, 'PRIME'),
  createStock('INTC', 'Intel Corporation', 234.56, 1800000, 65.98, 'FAIR'),
  createStock('AMD', 'Advanced Micro Devices, Inc.', 345.67, 2200000, 70.21, 'GOOD'),
  createStock('UBER', 'Uber Technologies, Inc.', 456.78, 2800000, 60.54, 'FAIR'),
  createStock('LYFT', 'Lyft, Inc.', 567.89, 1900000, 65.32, 'FAIR'),
  createStock('CRM', 'Salesforce.com, Inc.', 678.9, 2100000, 75.76, 'GOOD'),
  createStock('SQ', 'Square, Inc.', 789.01, 2400000, 90.43, 'PRIME'),
  createStock('ZM', 'Zoom Video Communications, Inc.', 890.12, 2700000, 95.67, 'PRIME'),
  createStock('SHOP', 'Shopify Inc.', 901.23, 2300000, 73.54, 'GOOD'),
  createStock('NET', 'Cloudflare, Inc.', 123.45, 2600000, 65.21, 'FAIR'),
  createStock('ROKU', 'Roku, Inc.', 234.56, 2000000, 60.98, 'FAIR'),
  createStock('DOCU', 'DocuSign, Inc.', 345.67, 2400000, 75.32, 'GOOD'),
  createStock('SNOW', 'Snowflake Inc.', 456.78, 2800000, 80.76, 'PRIME'),
  createStock('CRWD', 'CrowdStrike Holdings, Inc.', 567.89, 2200000, 85.43, 'PRIME'),
  createStock('OKTA', 'Okta, Inc.', 678.9, 2600000, 69.67, 'FAIR'),
]

export const Predictions = () => {
  const {data, isError} = useGetPredictionsQuery()

  const lastUpdatedDate = new Date()

  const rows: Stock[] = isError
    ? DEFAULT_ROWS
    : data!.map(dataRow => ({
        name: dataRow.name,
        price: dataRow.price,
        scoreRank: dataRow.compositeRank,
        scoreValue: dataRow.compositeScoreValue,
        symbol: dataRow.symbol,
        throughput: dataRow.fundamentals.averageVolumeUSD,
      }))

  return (
    <Layout description='Predictive composite scores.' title='Predictions'>
      <Container
        component='main'
        maxWidth='md'
        sx={{height: `calc(100vh - ${HEADER_HEIGHT}px)`, pt: 2, pb: 4}}
      >
        <PredictionsTable lastUpdatedDate={lastUpdatedDate} rows={rows} />
      </Container>
    </Layout>
  )
}
