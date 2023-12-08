import {Navigate, Outlet, useLocation} from 'react-router-dom'
import {useAuth} from '@/features/auth/hooks'

export const PrivateOutlet = () => {
  const auth = useAuth()
  const location = useLocation()

  return auth.user ? (
    <Outlet />
  ) : (
    <Navigate
      state={{
        from: location,
      }}
      to='/auth/login'
    />
  )
}
