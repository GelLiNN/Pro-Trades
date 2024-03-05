import {createApi, fetchBaseQuery, retry} from '@reduxjs/toolkit/query/react'
import {getToken} from '@/features/auth/state'

import type {RootState} from './types'

const baseQuery = fetchBaseQuery({
  baseUrl: 'https://localhost:7777/api',
  prepareHeaders: (headers, {getState}) => {
    const token = getToken(getState() as RootState)

    if (token) {
      headers.set('authorization', token)
    }

    return headers
  },
})

const baseQueryWithRetry = retry(baseQuery, {maxRetries: 0})

export const api = createApi({
  baseQuery: baseQueryWithRetry,
  endpoints: () => ({}),
})
