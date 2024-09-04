import {api} from '@/store'

export interface LogoutResponse {}

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    logout: builder.mutation<LogoutResponse, void>({
      query: () => ({
        method: 'POST',
        url: 'auth/Logout',
      }),
    }),
  }),
})

export const {useLogoutMutation} = extendedApi
