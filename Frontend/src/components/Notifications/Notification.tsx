import {Notification as NotificationType} from './types'

interface Props {
  notification: NotificationType
  onDismiss: (id: string) => void
}

export const Notification = ({notification, onDismiss}: Props) => {
  return (
    <div>
      <button onClick={() => onDismiss(notification.id)}>Dismiss</button>

      {notification.id}
    </div>
  )
}
