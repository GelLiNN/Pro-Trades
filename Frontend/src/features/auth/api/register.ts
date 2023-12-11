import {AUTH_API_BASE_URL} from './constants'

import {api} from '@/store'

import type {User} from '@/features/users/types'

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

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    register: builder.mutation<RegisterResponse, RegisterRequest>({
      query: registerRequest => ({
        body: registerRequest,
        method: 'POST',
        url: `${AUTH_API_BASE_URL}/register`,
      }),
    }),
  }),
})

export const {useRegisterMutation} = extendedApi
