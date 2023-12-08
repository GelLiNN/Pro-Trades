import {createApi, fetchBaseQuery} from '@reduxjs/toolkit/query/react'

import type {User} from '@/features/users/types'
import type {RootState} from '@/store'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface RecoverPasswordRequest {
  email: string
}

export interface RecoverPasswordResponse {}

export interface RegisterRequest {
  accessCode: string
  email: string
  password: string
  username: string
}

export interface RegisterResponse {
  token: string
  user: User
}

export const authApi = createApi({
  baseQuery: fetchBaseQuery({
    baseUrl: '/api/auth',
    prepareHeaders: (headers, {getState}) => {
      const token = (getState() as RootState).auth.token

      if (token) {
        headers.set('authorization', `Bearer ${token}`)
      }

      return headers
    },
  }),
  endpoints: builder => ({
    login: builder.mutation<LoginResponse, LoginRequest>({
      query: loginRequest => ({
        body: loginRequest,
        method: 'POST',
        url: 'login',
      }),
    }),

    recoverPassword: builder.mutation<RecoverPasswordResponse, RecoverPasswordRequest>({
      query: recoverPasswordRequest => ({
        body: recoverPasswordRequest,
        method: 'POST',
        url: 'recover-password',
      }),
    }),

    register: builder.mutation<RegisterResponse, RegisterRequest>({
      query: registerRequest => ({
        body: registerRequest,
        method: 'POST',
        url: 'register',
      }),
    }),
  }),
})

export const {useLoginMutation, useRecoverPasswordMutation, useRegisterMutation} = authApi
