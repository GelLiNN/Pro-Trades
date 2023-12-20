import {createSlice} from '@reduxjs/toolkit'
import {authStorage} from '@/features/auth/utils'

import type {PayloadAction} from '@reduxjs/toolkit'
import type {User} from '@/features/users/types'

interface AuthState {
  token: string | null
  user: User | null
}

interface SetCredentialsPayload {
  token?: string
  user?: User
}

export const authSlice = createSlice({
  name: 'auth',
  initialState: {
    token: authStorage.getToken(),
    user: null,
  } as AuthState,
  reducers: {
    clearCredentials: state => {
      authStorage.clearToken()

      state.token = null
      state.user = null
    },

    setCredentials: (state, action: PayloadAction<SetCredentialsPayload>) => {
      const {
        payload: {token, user},
      } = action

      if (token) {
        authStorage.setToken(token)
        state.token = token
      }

      if (user) {
        state.user = user
      }
    },
  },
})

export const {clearCredentials, setCredentials} = authSlice.actions
