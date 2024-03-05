import {forwardRef} from 'react'

import {Alert as MuiAlert} from '@mui/material'

import type {AlertProps} from '@mui/material'

export const Alert = forwardRef<HTMLDivElement, AlertProps>(function Alert(props, ref) {
  return <MuiAlert elevation={6} ref={ref} variant='filled' {...props} />
})
