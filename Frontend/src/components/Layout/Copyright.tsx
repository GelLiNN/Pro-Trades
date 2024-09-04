import {Link, Typography} from '@mui/material'
import {SITE_NAME} from '@/constants'

export const Copyright = () => {
  return (
    <Typography align='center' color='text.secondary' variant='body2'>
      {'Copyright © '}
      <Link color='inherit' href='/'>
        {SITE_NAME}
      </Link>{' '}
      {new Date().getFullYear()}.
    </Typography>
  )
}
