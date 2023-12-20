import {api} from '@/store'
import {getUserFromResponse} from './utils'

import type {User} from '@/features/users/types'
import type {RawLoginResponse} from './types'

export interface LoginBody {
  password: string
  username: string
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
      transformResponse: (response: RawLoginResponse) => ({
        token: response.authToken,
        user: getUserFromResponse(response),
      }),
    }),
  }),
})

export const {useLoginMutation} = extendedApi
