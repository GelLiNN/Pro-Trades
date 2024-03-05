import {configureStore} from '@reduxjs/toolkit'

import {authSlice} from '@/features/auth/state'
import {notificationsSlice} from '@/features/notifications/state'
import {api} from './api'

export const store = configureStore({
  middleware: getDefaultMiddleware => getDefaultMiddleware().concat(api.middleware),
  reducer: {
    [api.reducerPath]: api.reducer,

    [authSlice.name]: authSlice.reducer,
    [notificationsSlice.name]: notificationsSlice.reducer,
  },
})
