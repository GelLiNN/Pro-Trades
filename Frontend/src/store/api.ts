import {createApi, fetchBaseQuery, retry} from '@reduxjs/toolkit/query/react'

import type {RootState} from './types'

const baseQuery = fetchBaseQuery({
  baseUrl: 'https://localhost:7777/api',
  prepareHeaders: (headers, {getState}) => {
    const token = (getState() as RootState).auth.token

    if (token) {
      headers.set('authorization', token)
    }

    return headers
  },
})

const baseQueryWithRetry = retry(baseQuery, {maxRetries: 1})

export const api = createApi({
  baseQuery: baseQueryWithRetry,
  endpoints: () => ({}),
})