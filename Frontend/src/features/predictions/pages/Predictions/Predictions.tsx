import {Box, Container, Typography} from '@mui/material'
import {Layout} from '@/components/Layout'
import {STOCKS} from '@/features/predictions/constants'

export const Predictions = () => {
  return (
    <Layout description='Something, something, predictions.' title='Predictions'>
      <Container>
        <Box>
          <Typography variant='h2'>Predictions</Typography>

          {STOCKS.map((STOCK, index) => (
            <Box key={index}>{STOCK.symbol}</Box>
          ))}
        </Box>
      </Container>
    </Layout>
  )
}
