import {createApi, fetchBaseQuery} from '@reduxjs/toolkit/query/react'

import type {RootState} from './types'

export const api = createApi({
  baseQuery: fetchBaseQuery({
    baseUrl: '/api',
    prepareHeaders: (headers, {getState}) => {
      const token = (getState() as RootState).auth.token

      if (token) {
        headers.set('authorization', `Bearer ${token}`)
      }

      return headers
    },
  }),
  endpoints: () => ({}),
})
