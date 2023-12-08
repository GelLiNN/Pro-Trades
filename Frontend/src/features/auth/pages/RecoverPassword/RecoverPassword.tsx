import {useCallback, useState} from 'react'
import {useRecoverPasswordMutation} from '@/features/auth/api'

import {Box, Button, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {ChangeEvent, FormEvent} from 'react'
import type {RecoverPasswordRequest} from '@/features/auth/api'

export const RecoverPassword = () => {
  const [formState, setFormState] = useState<RecoverPasswordRequest>({
    email: '',
  })

  const [recoverPassword, {isLoading}] = useRecoverPasswordMutation()

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
        await recoverPassword(formState).unwrap()
        // TODO: Switch to "check your email" screen
      } catch (error) {
        // TODO: add a notification
        console.log('Error', error)
      }
    },
    [formState, recoverPassword]
  )

  return (
    <AuthLayout title='Recover Password'>
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
      </Box>
    </AuthLayout>
  )
}
