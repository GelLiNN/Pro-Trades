import {Box, Container} from '@mui/material'
import {Copyright} from './Copyright'

export const Footer = () => {
  return (
    <Box
      component='footer'
      sx={{
        py: 3,
        px: 2,
        mt: 'auto',
        backgroundColor: theme => theme.palette.grey[200],
      }}
    >
      <Container maxWidth='sm'>
        <Copyright />
      </Container>
    </Box>
  )
}
