import type {User} from '@/features/users/types'
import type {SessionResponse} from './types'

export const getUserFromResponse = (response: SessionResponse): User => ({
  email: response.email,
  id: response.userId,
  username: response.username,
})
