import {useCallback, useEffect, useState} from 'react'
import {useNavigate, useSearchParams} from 'react-router-dom'
import * as zod from 'zod'
import {useResetPasswordMutation} from '@/features/auth/api'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Check, Lock, Visibility, VisibilityOff} from '@mui/icons-material'
import {Button, IconButton, InputAdornment, Link, TextField, Typography} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {ResetPasswordBody} from '@/features/auth/api'

interface ResetPasswordData extends Omit<ResetPasswordBody, 'token'> {}

const resetPasswordSchema = zod.object({
  password: zod.string().min(1, 'Password is required'),
})

export const ResetPassword = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [isPasswordVisible, setIsPasswordVisible] = useState(false)

  const handleTogglePasswordVisibility = useCallback(() => {
    setIsPasswordVisible(isPasswordVisible => !isPasswordVisible)
  }, [])

  const [resetPassword, {isLoading, isSuccess}] = useResetPasswordMutation()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  useEffect(() => {
    if (!token) {
      navigate('/auth/login')
    }
  }, [navigate, token])

  const handleSubmit = useCallback(
    async (resetPasswordData: ResetPasswordData) => {
      try {
        await resetPassword({
          ...resetPasswordData,
          token: token!,
        }).unwrap()
      } catch (error) {
        dispatch(
          addNotification({
            message: 'Failed to reset password',
            severity: 'error',
          })
        )
      }
    },
    [dispatch, resetPassword, token]
  )

  return (
    <AuthLayout
      IconComponent={isSuccess ? Check : undefined}
      title={isSuccess ? 'Password Reset' : 'Reset Password'}
    >
      <Form<ResetPasswordData, typeof resetPasswordSchema>
        onSubmit={handleSubmit}
        schema={resetPasswordSchema}
      >
        {({formState, register}) =>
          isSuccess ? (
            <>
              <Typography sx={{my: 2}} variant='subtitle1'>
                {'Your password has been reset. '}
                <Link href='/auth/login' variant='body2'>
                  Go to Login
                </Link>
              </Typography>
            </>
          ) : (
            <>
              <Typography sx={{my: 2}} variant='subtitle1'>
                Choose a new password.
              </Typography>

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

              <Button disabled={isLoading} fullWidth sx={{my: 2}} type='submit' variant='contained'>
                Reset Password
              </Button>
            </>
          )
        }
      </Form>
    </AuthLayout>
  )
}
