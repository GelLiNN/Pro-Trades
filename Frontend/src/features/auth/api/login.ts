import {api} from '@/store'

import type {User} from '@/features/users/types'

export interface LoginBody {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    login: builder.mutation<LoginResponse, LoginBody>({
      query: loginBody => ({
        body: loginBody,
        method: 'POST',
        url: 'auth/Login',
      }),
    }),
  }),
})

export const {useLoginMutation} = extendedApi
