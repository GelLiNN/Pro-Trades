import {useCallback} from 'react'
import {useNavigate} from 'react-router-dom'
import * as zod from 'zod'
import {useLoginMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Button, Grid, Link, TextField} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {LoginRequest} from '@/features/auth/api'

interface LoginData extends LoginRequest {}

const loginSchema = zod.object({
  email: zod.string().min(1, 'Email is required'),
  password: zod.string().min(1, 'Password is required'),
})

export const Login = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [login, {isLoading}] = useLoginMutation()

  const handleSubmit = useCallback(
    async (loginData: LoginData) => {
      try {
        const loginResponse = await login(loginData).unwrap()
        dispatch(setCredentials(loginResponse))
        dispatch(
          addNotification({
            message: 'Log in successful',
            severity: 'success',
          })
        )
        navigate('/')
      } catch (error) {
        dispatch(
          addNotification({
            message: 'Failed to log in',
            severity: 'error',
          })
        )
      }
    },
    [dispatch, login, navigate]
  )

  return (
    <AuthLayout title='Log In'>
      <Form<LoginData, typeof loginSchema> onSubmit={handleSubmit} schema={loginSchema}>
        {({formState, register}) => (
          <>
            <TextField
              autoComplete='email'
              autoFocus
              error={!!formState.errors.email}
              fullWidth
              helperText={formState.errors.email?.message}
              label='Email'
              margin='normal'
              required
              type='text'
              {...register('email')}
            />

            <TextField
              autoComplete='current-password'
              error={!!formState.errors.password}
              fullWidth
              helperText={formState.errors.password?.message}
              label='Password'
              margin='normal'
              required
              type='password'
              {...register('password')}
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
          </>
        )}
      </Form>
    </AuthLayout>
  )
}
