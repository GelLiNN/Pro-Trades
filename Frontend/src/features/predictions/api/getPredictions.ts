import {api} from '@/store'

import type {Stock} from '@/features/predictions/types'

export type GetPredictionsResponse = Stock[]

const extendedApi = api.injectEndpoints({
  endpoints: builder => ({
    getPredictions: builder.query<GetPredictionsResponse, void>({
      query: () => ({
        url: 'search/DumpCache',
      }),
    }),
  }),
})

export const {useGetPredictionsQuery} = extendedApi
