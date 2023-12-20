import {setCredentials} from '@/features/auth/state'
import {api} from '@/store'
import {getUserFromResponse} from './utils'

import type {User} from '@/features/users/types'
import type {RawGetUserResponse} from './types'

export type GetUserResponse = User

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    getUser: builder.query<GetUserResponse, void>({
      onQueryStarted: async (_args, {dispatch, queryFulfilled}) => {
        try {
          const {data: user} = await queryFulfilled
          dispatch(setCredentials({user}))
        } catch (error) {
          // Do nothing
        }
      },
      query: () => ({
        url: 'auth/CheckSession',
      }),
      transformResponse: (response: RawGetUserResponse) => getUserFromResponse(response),
    }),
  }),
})

export const {useGetUserQuery} = extendedApi
