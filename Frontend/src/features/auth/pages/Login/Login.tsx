import {Box, Button, Checkbox, FormControlLabel, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

export const Login = () => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    console.log({
      email: data.get('email'),
      password: data.get('password'),
    })
  }

  return (
    <AuthLayout title='Log In'>
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

        <TextField
          autoComplete='current-password'
          fullWidth
          id='password'
          label='Password'
          margin='normal'
          name='password'
          required
          type='password'
        />

        <FormControlLabel
          control={<Checkbox color='primary' value='remember' />}
          label='Remember me'
        />

        <Button fullWidth sx={{mt: 3, mb: 2}} type='submit' variant='contained'>
          Log In
        </Button>

        <Grid container>
          <Grid item xs>
            <Link href='/auth/forgot-password' variant='body2'>
              Forgot password?
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
