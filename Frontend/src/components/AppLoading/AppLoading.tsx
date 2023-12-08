import {Box, CircularProgress} from '@mui/material'

export const AppLoading = () => {
  return (
    <Box sx={{display: 'grid', alignItems: 'center', justifyContent: 'center', minHeight: '100vh'}}>
      <CircularProgress size={80} />
    </Box>
  )
}
