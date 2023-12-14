import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import * as zod from 'zod'
import {useRegisterMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Email, Key, Lock, Person, Visibility, VisibilityOff} from '@mui/icons-material'
import {Button, Grid, IconButton, InputAdornment, Link, TextField} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {RegisterBody} from '@/features/auth/api'

interface RegisterData extends RegisterBody {}

const registerSchema = zod.object({
  accessCode: zod.string().min(1, 'Access code is required'),
  email: zod.string().min(1, 'Email is required'),
  password: zod.string().min(1, 'Password is required'),
  username: zod.string().min(1, 'Username is required'),
})

export const Register = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [isPasswordVisible, setIsPasswordVisible] = useState(false)

  const handleTogglePasswordVisibility = useCallback(() => {
    setIsPasswordVisible(isPasswordVisible => !isPasswordVisible)
  }, [])

  const [register, {isLoading}] = useRegisterMutation()

  const handleSubmit = useCallback(
    async (registerData: RegisterData) => {
      try {
        const registerResponse = await register(registerData).unwrap()
        dispatch(setCredentials(registerResponse))
        dispatch(
          addNotification({
            message: 'Registration successful',
            severity: 'success',
          })
        )
        navigate('/')
      } catch (error) {
        dispatch(
          addNotification({
            message: 'Failed to register',
            severity: 'error',
          })
        )
      }
    },
    [dispatch, navigate, register]
  )

  return (
    <AuthLayout title='Register'>
      <Form<RegisterData, typeof registerSchema> onSubmit={handleSubmit} schema={registerSchema}>
        {({formState, register}) => (
          <>
            <TextField
              autoComplete='username'
              autoFocus
              error={!!formState.errors.username}
              fullWidth
              helperText={formState.errors.username?.message}
              InputProps={{
                startAdornment: (
                  <InputAdornment position='start'>
                    <Person />
                  </InputAdornment>
                ),
              }}
              label='Username'
              margin='normal'
              placeholder='Username99'
              required
              type='text'
              {...register('username')}
            />

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
              autoComplete='new-password'
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
                    <Lock />
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

            <TextField
              error={!!formState.errors.accessCode}
              fullWidth
              helperText={formState.errors.accessCode?.message}
              InputProps={{
                startAdornment: (
                  <InputAdornment position='start'>
                    <Key />
                  </InputAdornment>
                ),
              }}
              label='Access Code'
              margin='normal'
              placeholder='super-secret-access-code'
              required
              type='text'
              {...register('accessCode')}
            />

            <Button disabled={isLoading} fullWidth sx={{my: 2}} type='submit' variant='contained'>
              Register
            </Button>

            <Grid container justifyContent='flex-end'>
              <Grid item>
                <Link href='/auth/login' variant='body2'>
                  Already have an account?
                </Link>
              </Grid>
            </Grid>
          </>
        )}
      </Form>
    </AuthLayout>
  )
}
