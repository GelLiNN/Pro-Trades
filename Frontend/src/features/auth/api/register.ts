import {api} from '@/store'
import {getUserFromResponse} from './utils'

import type {User} from '@/features/users/types'
import type {RawRegisterResponse} from './types'

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
      transformResponse: (response: RawRegisterResponse) => ({
        token: response.authToken,
        user: getUserFromResponse(response),
      }),
    }),
  }),
})

export const {useRegisterMutation} = extendedApi
