import {useCallback} from 'react'
import {useNavigate} from 'react-router-dom'
import * as zod from 'zod'
import {useRegisterMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Button, Grid, Link, TextField} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {RegisterRequest} from '@/features/auth/api'

interface RegisterData extends RegisterRequest {}

const registerSchema = zod.object({
  accessCode: zod.string().min(1, 'Access code is required'),
  email: zod.string().min(1, 'Email is required'),
  password: zod.string().min(1, 'Password is required'),
  username: zod.string().min(1, 'Username is required'),
})

export const Register = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

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
              label='Username'
              margin='normal'
              required
              type='text'
              {...register('username')}
            />

            <TextField
              autoComplete='email'
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
              autoComplete='new-password'
              error={!!formState.errors.password}
              fullWidth
              helperText={formState.errors.password?.message}
              label='Password'
              margin='normal'
              required
              type='password'
              {...register('password')}
            />

            <TextField
              error={!!formState.errors.accessCode}
              fullWidth
              helperText={formState.errors.accessCode?.message}
              label='Access Code'
              margin='normal'
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
