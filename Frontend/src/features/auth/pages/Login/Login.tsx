import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import {useLoginMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Box, Button, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {ChangeEvent, FormEvent} from 'react'
import type {LoginRequest} from '@/features/auth/api'

export const Login = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [formState, setFormState] = useState<LoginRequest>({
    email: '',
    password: '',
  })

  const [login, {isLoading}] = useLoginMutation()

  const handleChange = useCallback((event: ChangeEvent<HTMLInputElement>) => {
    setFormState(formState => ({
      ...formState,
      [event.target.name]: event.target.value,
    }))
  }, [])

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()

      try {
        const loginResponse = await login(formState).unwrap()
        dispatch(setCredentials(loginResponse))
        navigate('/')
      } catch (error) {
        dispatch(
          addNotification({
            message: 'Failed to login',
            severity: 'error',
          })
        )
      }
    },
    [dispatch, formState, login, navigate]
  )

  return (
    <AuthLayout title='Log In'>
      <Box component='form' noValidate onSubmit={handleSubmit}>
        <TextField
          autoComplete='email'
          autoFocus
          fullWidth
          id='email'
          label='Email Address'
          margin='normal'
          name='email'
          onChange={handleChange}
          required
          type='text'
        />

        <TextField
          autoComplete='current-password'
          fullWidth
          id='password'
          label='Password'
          margin='normal'
          name='password'
          onChange={handleChange}
          required
          type='password'
        />

        <Button disabled={isLoading} fullWidth sx={{my: 2}} type='submit' variant='contained'>
          Log In
        </Button>

        <Grid container>
          <Grid item xs>
            <Link href='/auth/recover-password' variant='body2'>
              Forgot your password?
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
