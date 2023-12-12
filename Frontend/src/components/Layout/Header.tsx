import {SITE_NAME} from '@/constants'

import {useAuth} from '@/features/auth/hooks'

import {Button, Link, Toolbar} from '@mui/material'

export const Header = () => {
  const auth = useAuth()

  return (
    <Toolbar sx={{flexWrap: 'wrap'}}>
      <Link color='inherit' href='/' noWrap sx={{flexGrow: 1}} underline='none' variant='h6'>
        {SITE_NAME}
      </Link>

      <nav>
        {auth.user ? (
          <Link color='text.primary' href='/predictions' sx={{my: 1, mx: 1.5}} variant='button'>
            Predictions
          </Link>
        ) : null}
      </nav>

      {auth.user ? (
        <Button sx={{my: 1, ml: 1.5}} variant='contained'>
          Logout
        </Button>
      ) : (
        <>
          <Button href='/auth/login' sx={{my: 1, ml: 1.5}} variant='outlined'>
            Login
          </Button>

          <Button href='/auth/register' sx={{my: 1, ml: 1.5}} variant='contained'>
            Register
          </Button>
        </>
      )}
    </Toolbar>
  )
}
