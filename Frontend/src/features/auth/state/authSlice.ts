import {createSlice} from '@reduxjs/toolkit'

import type {PayloadAction} from '@reduxjs/toolkit'
import type {User} from '@/features/users/types'

interface AuthState {
  token: string | null
  user: User | null
}

export const authSlice = createSlice({
  name: 'auth',
  initialState: {
    token: null,
    user: null,
    // user: {
    //   id: 1,
    // },
  } as AuthState,
  reducers: {
    setCredentials: (state, action: PayloadAction<AuthState>) => {
      const {
        payload: {token, user},
      } = action

      state.token = token!
      state.user = user!
    },
  },
})

export const {setCredentials} = authSlice.actions
