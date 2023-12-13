import {useCallback, useState} from 'react'
import * as zod from 'zod'
import {useRecoverPasswordMutation} from '@/features/auth/api'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Button, Grid, Link, TextField, Typography} from '@mui/material'
import {Check} from '@mui/icons-material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {RecoverPasswordRequest} from '@/features/auth/api'

interface RecoverPasswordData extends RecoverPasswordRequest {}

const recoverPasswordSchema = zod.object({
  accessCode: zod.string().min(1, 'Access code is required'),
  email: zod.string().min(1, 'Email is required'),
})

export const RecoverPassword = () => {
  const dispatch = useDispatch()

  const [recoverPassword, {isLoading, isSuccess}] = useRecoverPasswordMutation()
  const [email, setEmail] = useState('')

  const handleSubmit = useCallback(
    async (recoverPasswordData: RecoverPasswordData) => {
      try {
        await recoverPassword(recoverPasswordData).unwrap()
        setEmail(recoverPasswordData.email)
      } catch (error) {
        dispatch(
          addNotification({
            message: 'Failed to recover password',
            severity: 'error',
          })
        )
      }
    },
    [dispatch, recoverPassword]
  )

  return (
    <AuthLayout
      IconComponent={isSuccess ? Check : undefined}
      title={isSuccess ? 'Email Sent' : 'Recover Password'}
    >
      <Form<RecoverPasswordData, typeof recoverPasswordSchema>
        onSubmit={handleSubmit}
        schema={recoverPasswordSchema}
      >
        {({formState, register}) =>
          isSuccess ? (
            <Typography sx={{my: 2}} variant='subtitle1'>
              {'Check your email at '}
              <Typography color='secondary' component='span'>
                {email}
              </Typography>
              {` and open the link we sent to reset your password.`}
            </Typography>
          ) : (
            <>
              <Typography sx={{my: 2}} variant='subtitle1'>
                {
                  "Enter your email and access code and we'll send you a link to reset your password."
                }
              </Typography>

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
            </>
          )
        }
      </Form>
    </AuthLayout>
  )
}
