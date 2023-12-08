import type {AlertProps} from '@mui/material'

export type NotificationSeverity = AlertProps['severity']

export interface Notification {
  id: string
  message: string
  severity: NotificationSeverity
}
