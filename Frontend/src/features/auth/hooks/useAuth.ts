import {useMemo} from 'react'
import {getCurrentUser} from '@/features/auth/state'
import {useSelector} from '@/store'

export const useAuth = () => {
  const user = useSelector(getCurrentUser)

  return useMemo(() => ({user}), [user])
}
