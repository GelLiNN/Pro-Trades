import {api} from '@/store'

import type {User} from '@/features/users/types'

export interface RegisterBody {
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
    register: builder.mutation<RegisterResponse, RegisterBody>({
      query: registerBody => ({
        body: registerBody,
        method: 'POST',
        url: 'auth/Register',
      }),
    }),
  }),
})

export const {useRegisterMutation} = extendedApi
