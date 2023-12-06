import {Box, Button, Checkbox, FormControlLabel, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

export const Register = () => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    console.log({
      email: data.get('email'),
      password: data.get('password'),
    })
  }

  return (
    <AuthLayout title='Register'>
      <Box component='form' noValidate onSubmit={handleSubmit} sx={{mt: 3}}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <TextField
              autoComplete='username'
              autoFocus
              fullWidth
              id='username'
              label='Username'
              name='username'
              required
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              autoComplete='email'
              fullWidth
              id='email'
              label='Email Address'
              name='email'
              required
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              autoComplete='new-password'
              fullWidth
              id='password'
              label='Password'
              name='password'
              required
              type='password'
            />
          </Grid>

          <Grid item xs={12}>
            <FormControlLabel
              control={<Checkbox color='primary' value='allowExtraEmails' />}
              label='I want to receive updates via email.'
            />
          </Grid>
        </Grid>

        <Button fullWidth sx={{mt: 3, mb: 2}} type='submit' variant='contained'>
          Register
        </Button>

        <Grid container justifyContent='flex-end'>
          <Grid item>
            <Link href='/auth/login' variant='body2'>
              Already have an account?
            </Link>
          </Grid>
        </Grid>
      </Box>
    </AuthLayout>
  )
}
