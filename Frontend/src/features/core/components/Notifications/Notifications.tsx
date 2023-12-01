import {useCallback} from 'react'

import {Notification} from './Notification'

import {Notification as NotificationType} from './types'

export const Notifications = () => {
  const notifications: NotificationType[] = []

  const handleDismiss = useCallback((id: string) => {
    console.log(IDBObjectStore)
  }, [])

  return (
    <div>
      {notifications.map(notification => (
        <Notification key={notification.id} notification={notification} onDismiss={handleDismiss} />
      ))}
    </div>
  )
}
