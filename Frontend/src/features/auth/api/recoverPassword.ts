import {AUTH_API_BASE_URL} from './constants'

import {api} from '@/store'

export interface RecoverPasswordRequest {
  accessCode: string
  email: string
}

export interface RecoverPasswordResponse {}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    recoverPassword: builder.mutation<RecoverPasswordResponse, RecoverPasswordRequest>({
      query: recoverPasswordRequest => ({
        body: recoverPasswordRequest,
        method: 'POST',
        url: `${AUTH_API_BASE_URL}/recover-password`,
      }),
    }),
  }),
})

export const {useRecoverPasswordMutation} = extendedApi
