import {useCallback, useEffect, useState} from 'react'
import {getNotifications, removeNotification} from '@/features/notifications/state'
import {useDispatch, useSelector} from '@/store'

import {IconButton, Snackbar as MuiSnackbar} from '@mui/material'
import {Close} from '@mui/icons-material'
import {Alert} from './Alert'

import type {SyntheticEvent} from 'react'
import type {SnackbarCloseReason} from '@mui/material'
import type {Notification} from '@/features/notifications/types'

export const Snackbar = () => {
  const dispatch = useDispatch()
  const notifications = useSelector(getNotifications)

  const [isOpen, setIsOpen] = useState(false)
  const [currentNotification, setCurrentNotification] = useState<Notification | null>(null)

  useEffect(() => {
    if (notifications.length > 0 && !currentNotification) {
      const currentNotification = notifications[0]

      setCurrentNotification(currentNotification)
      setIsOpen(true)

      dispatch(removeNotification(currentNotification.id))
    } else if (notifications.length > 0 && !!currentNotification && isOpen) {
      setIsOpen(false)
    }
  }, [currentNotification, dispatch, isOpen, notifications])

  const handleClose = useCallback(
    (_event: SyntheticEvent | Event, reason?: SnackbarCloseReason) => {
      if (reason === 'clickaway') {
        return
      }

      setIsOpen(false)
    },
    []
  )

  const handleExited = useCallback(() => {
    setCurrentNotification(null)
  }, [])

  return (
    <MuiSnackbar
      anchorOrigin={{horizontal: 'center', vertical: 'bottom'}}
      autoHideDuration={6000}
      key={currentNotification ? currentNotification.id : undefined}
      onClose={handleClose}
      open={isOpen}
      TransitionProps={{
        onExited: handleExited,
      }}
    >
      <Alert
        action={
          <>
            <IconButton aria-label='close' color='inherit' onClick={handleClose} sx={{p: 0.25}}>
              <Close />
            </IconButton>
          </>
        }
        onClose={handleClose}
        severity={currentNotification ? currentNotification.severity : undefined}
        sx={{maxWidth: 400}}
      >
        {currentNotification ? currentNotification.message : undefined}
      </Alert>
    </MuiSnackbar>
  )
}
