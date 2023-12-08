import {Navigate, Outlet, useLocation} from 'react-router-dom'
import {useAuth} from '@/features/auth/hooks'

export const PublicOutlet = () => {
  const auth = useAuth()
  const location = useLocation()

  return auth.user ? (
    <Navigate
      state={{
        from: location,
      }}
      to='/'
    />
  ) : (
    <Outlet />
  )
}
