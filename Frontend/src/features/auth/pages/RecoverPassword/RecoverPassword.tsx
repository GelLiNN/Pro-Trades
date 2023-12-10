import {useCallback} from 'react'
import * as zod from 'zod'
import {useRecoverPasswordMutation} from '@/features/auth/api'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Button, Grid, Link, TextField} from '@mui/material'
import {Form} from '@/components/Form'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {RecoverPasswordRequest} from '@/features/auth/api'

interface RecoverPasswordData extends RecoverPasswordRequest {}

const recoverPasswordSchema = zod.object({
  email: zod.string().min(1, 'Email is required'),
})

export const RecoverPassword = () => {
  const dispatch = useDispatch()

  const [recoverPassword, {isLoading}] = useRecoverPasswordMutation()

  const handleSubmit = useCallback(
    async (recoverPasswordData: RecoverPasswordData) => {
      try {
        await recoverPassword(recoverPasswordData).unwrap()
        // TODO: Switch to "check your email" screen
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
    <AuthLayout title='Recover Password'>
      <Form<RecoverPasswordData, typeof recoverPasswordSchema>
        onSubmit={handleSubmit}
        schema={recoverPasswordSchema}
      >
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
        )}
      </Form>
    </AuthLayout>
  )
}
