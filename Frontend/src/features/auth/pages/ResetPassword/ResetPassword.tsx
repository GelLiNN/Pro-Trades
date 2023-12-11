import {useCallback, useEffect} from 'react'
import {useNavigate, useSearchParams} from 'react-router-dom'
import * as zod from 'zod'
import {useResetPasswordMutation} from '@/features/auth/api'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Button, Link, TextField, Typography} from '@mui/material'
import {Check} from '@mui/icons-material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {ResetPasswordRequest} from '@/features/auth/api'

interface ResetPasswordData extends Omit<ResetPasswordRequest, 'token'> {}

const resetPasswordSchema = zod.object({
  password: zod.string().min(1, 'Password is required'),
})

export const ResetPassword = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

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
                label='New Password'
                margin='normal'
                required
                type='password'
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
