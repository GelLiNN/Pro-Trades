import type {RootState} from '@/store'

export const getCurrentUser = (state: RootState) => state.auth.user

export const getToken = (state: RootState) => state.auth.token
