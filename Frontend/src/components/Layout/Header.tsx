import {Button, Link, Toolbar} from '@mui/material'
import {SITE_NAME} from '@/constants'

export const Header = () => {
  return (
    <Toolbar sx={{flexWrap: 'wrap'}}>
      <Link color='inherit' href='/' noWrap sx={{flexGrow: 1}} underline='none' variant='h6'>
        {SITE_NAME}
      </Link>

      <nav>
        <Link color='text.primary' href='/predictions' sx={{my: 1, mx: 1.5}} variant='button'>
          Predictions
        </Link>
      </nav>

      <Button href='/auth/login' sx={{my: 1, ml: 1.5}} variant='outlined'>
        Login
      </Button>

      <Button href='/auth/register' sx={{my: 1, ml: 1.5}} variant='contained'>
        Register
      </Button>
    </Toolbar>
  )
}
