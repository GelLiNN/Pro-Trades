import {createSlice} from '@reduxjs/toolkit'

import type {PayloadAction} from '@reduxjs/toolkit'
import type {Notification} from '@/features/notifications/types'
import type {PartialBy} from '@/types'

type NotificationsState = Notification[]

interface AddNotificationPayload extends PartialBy<Omit<Notification, 'id'>, 'severity'> {}

export const notificationsSlice = createSlice({
  name: 'notifications',
  initialState: [] as NotificationsState,
  reducers: {
    addNotification: (state, action: PayloadAction<AddNotificationPayload>) => {
      state.push({
        id: `${new Date().getTime()}`,
        severity: 'info',
        ...action.payload,
      })
    },

    removeNotification: (state, action: PayloadAction<string>) => {
      const notificationIndex = state.findIndex(notification => notification.id === action.payload)
      if (notificationIndex !== -1) {
        state.splice(notificationIndex, 1)
      }
    },
  },
})

export const {addNotification, removeNotification} = notificationsSlice.actions
