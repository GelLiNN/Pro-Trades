import {AUTH_API_BASE_URL} from './constants'

import {api} from '@/store'

export interface ResetPasswordRequest {
  password: string
  token: string
}

export interface ResetPasswordResponse {}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    resetPassword: builder.mutation<ResetPasswordResponse, ResetPasswordRequest>({
      query: resetPasswordRequest => ({
        body: resetPasswordRequest,
        method: 'POST',
        url: `${AUTH_API_BASE_URL}/reset-password`,
      }),
    }),
  }),
})

export const {useResetPasswordMutation} = extendedApi
