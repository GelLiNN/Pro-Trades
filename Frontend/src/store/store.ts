import {configureStore} from '@reduxjs/toolkit'

import {authApi} from '@/features/auth/api'
import {authSlice} from '@/features/auth/state'

import {notificationsSlice} from '@/features/notifications/state'

export const store = configureStore({
  middleware: getDefaultMiddleware => getDefaultMiddleware().concat(authApi.middleware),
  reducer: {
    [authApi.reducerPath]: authApi.reducer,
    [authSlice.name]: authSlice.reducer,

    [notificationsSlice.name]: notificationsSlice.reducer,
  },
})
