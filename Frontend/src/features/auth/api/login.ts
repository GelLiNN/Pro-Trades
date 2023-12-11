import {AUTH_API_BASE_URL} from './constants'

import {api} from '@/store'

import type {User} from '@/features/users/types'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}
const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    login: builder.mutation<LoginResponse, LoginRequest>({
      query: loginRequest => ({
        body: loginRequest,
        method: 'POST',
        url: `${AUTH_API_BASE_URL}/login`,
      }),
    }),
  }),
})

export const {useLoginMutation} = extendedApi
