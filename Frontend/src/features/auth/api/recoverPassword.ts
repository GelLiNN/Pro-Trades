import {api} from '@/store'

export interface RecoverPasswordBody {
  accessCode: string
  email: string
}

export interface RecoverPasswordResponse {}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    recoverPassword: builder.mutation<RecoverPasswordResponse, RecoverPasswordBody>({
      query: recoverPasswordBody => ({
        body: recoverPasswordBody,
        method: 'POST',
        url: 'auth/RecoverPassword',
      }),
    }),
  }),
})

export const {useRecoverPasswordMutation} = extendedApi
