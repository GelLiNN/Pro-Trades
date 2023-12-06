import {Box, Button, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

export const ForgotPassword = () => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    console.log({
      email: data.get('email'),
    })
  }

  return (
    <AuthLayout title='Forgot Password'>
      <Box component='form' noValidate onSubmit={handleSubmit} sx={{mt: 1}}>
        <TextField
          autoComplete='email'
          autoFocus
          fullWidth
          id='email'
          label='Email Address'
          margin='normal'
          name='email'
          required
        />

        <Button fullWidth sx={{mt: 3, mb: 2}} type='submit' variant='contained'>
          Recover Password
        </Button>

        <Grid container>
          <Grid item xs>
            <Link href='/auth/login' variant='body2'>
              Already have an account?
            </Link>
          </Grid>

          <Grid item>
            <Link href='/auth/register' variant='body2'>
              {"Don't have an account?"}
            </Link>
          </Grid>
        </Grid>
      </Box>
    </AuthLayout>
  )
}
