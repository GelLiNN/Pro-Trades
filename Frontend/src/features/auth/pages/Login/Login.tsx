import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import * as zod from 'zod'
import {useLoginMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Email, Key, Visibility, VisibilityOff} from '@mui/icons-material'
import {Button, Grid, IconButton, InputAdornment, Link, TextField} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {LoginBody} from '@/features/auth/api'

interface LoginData extends LoginBody {}

const loginSchema = zod.object({
  email: zod.string().min(1, 'Email is required'),
  password: zod.string().min(1, 'Password is required'),
})

export const Login = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [isPasswordVisible, setIsPasswordVisible] = useState(false)

  const handleTogglePasswordVisibility = useCallback(() => {
    setIsPasswordVisible(isPasswordVisible => !isPasswordVisible)
  }, [])

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
            severity: 'info',
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
              InputProps={{
                startAdornment: (
                  <InputAdornment position='start'>
                    <Email />
                  </InputAdornment>
                ),
              }}
              label='Email'
              margin='normal'
              placeholder='email@example.com'
              required
              type='text'
              {...register('email')}
            />

            <TextField
              autoComplete='current-password'
              error={!!formState.errors.password}
              fullWidth
              helperText={formState.errors.password?.message}
              InputProps={{
                endAdornment: (
                  <InputAdornment position='end'>
                    <IconButton
                      aria-label='toggle password visibility'
                      edge='end'
                      onClick={handleTogglePasswordVisibility}
                    >
                      {isPasswordVisible ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                ),
                startAdornment: (
                  <InputAdornment position='start'>
                    <Key />
                  </InputAdornment>
                ),
              }}
              label='Password'
              margin='normal'
              placeholder='••••••••••'
              required
              type={isPasswordVisible ? 'text' : 'password'}
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
