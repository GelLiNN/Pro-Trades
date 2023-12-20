import {STORAGE_PREFIX} from '@/constants'

const AUTH_STORAGE_TOKEN_KEY = `${STORAGE_PREFIX}token`

export const authStorage = {
  getToken: () => localStorage.getItem(AUTH_STORAGE_TOKEN_KEY),

  setToken: (token: string) => {
    localStorage.setItem(AUTH_STORAGE_TOKEN_KEY, token)
  },

  clearToken: () => {
    localStorage.removeItem(AUTH_STORAGE_TOKEN_KEY)
  },
}
