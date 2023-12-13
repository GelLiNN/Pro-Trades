import {api} from '@/store'

export interface ResetPasswordBody {
  password: string
  token: string
}

export interface ResetPasswordResponse {}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    resetPassword: builder.mutation<ResetPasswordResponse, ResetPasswordBody>({
      query: resetPasswordBody => ({
        body: resetPasswordBody,
        method: 'POST',
        url: 'auth/ResetPassword',
      }),
    }),
  }),
})

export const {useResetPasswordMutation} = extendedApi
