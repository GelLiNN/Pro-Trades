import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import {useRegisterMutation} from '@/features/auth/api'
import {setCredentials} from '@/features/auth/state'
import {addNotification} from '@/features/notifications/state'
import {useDispatch} from '@/store'

import {Box, Button, Grid, Link, TextField} from '@mui/material'
import {AuthLayout} from '@/features/auth/components/AuthLayout'

import type {ChangeEvent, FormEvent} from 'react'
import type {RegisterRequest} from '@/features/auth/api'

export const Register = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const [formState, setFormState] = useState<RegisterRequest>({
    accessCode: '',
    email: '',
    password: '',
    username: '',
  })

  const [register, {isLoading}] = useRegisterMutation()

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
        const registerResponse = await register(formState).unwrap()
        dispatch(setCredentials(registerResponse))
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
    [dispatch, formState, navigate, register]
  )

  return (
    <AuthLayout title='Register'>
      <Box component='form' noValidate onSubmit={handleSubmit}>
        <TextField
          autoComplete='username'
          autoFocus
          fullWidth
          id='username'
          label='Username'
          margin='normal'
          name='username'
          onChange={handleChange}
          required
          type='text'
        />

        <TextField
          autoComplete='email'
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
          autoComplete='new-password'
          fullWidth
          id='password'
          label='Password'
          margin='normal'
          name='password'
          onChange={handleChange}
          required
          type='password'
        />

        <TextField
          fullWidth
          id='accessCode'
          label='Access Code'
          margin='normal'
          name='accessCode'
          onChange={handleChange}
          required
          type='text'
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
      </Box>
    </AuthLayout>
  )
}
