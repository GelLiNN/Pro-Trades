import {createSlice} from '@reduxjs/toolkit'

import type {PayloadAction} from '@reduxjs/toolkit'
import type {LoginResponse} from '@/features/auth/api'
import type {User} from '@/features/users/types'

interface AuthState {
  token: string | null
  user: User | null
}

interface SetCredentialsPayload extends LoginResponse {}

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
    clearCredentials: state => {
      state.token = null
      state.user = null
    },
    setCredentials: (state, action: PayloadAction<SetCredentialsPayload>) => {
      const {
        payload: {token, user},
      } = action

      state.token = token
      state.user = user
    },
  },
})

export const {clearCredentials, setCredentials} = authSlice.actions
