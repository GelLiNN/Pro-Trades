import {
  TypedUseSelectorHook,
  useDispatch as useDispatchBase,
  useSelector as useSelectorBase,
} from 'react-redux'

import type {AppDispatch, RootState} from './types'

export const useDispatch: () => AppDispatch = useDispatchBase

export const useSelector: TypedUseSelectorHook<RootState> = useSelectorBase
