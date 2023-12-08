import {configureStore} from '@reduxjs/toolkit'

import {authApi} from '@/features/auth/api'
import {authSlice} from '@/features/auth/state'

export const store = configureStore({
  middleware: getDefaultMiddleware => getDefaultMiddleware().concat(authApi.middleware),
  reducer: {
    [authApi.reducerPath]: authApi.reducer,
    [authSlice.name]: authSlice.reducer,
  },
})
