import {Navigate, Outlet, useLocation} from 'react-router-dom'
import {useAuth} from '@/features/auth/hooks'

interface Props {
  redirectTo: string
  requiresAuth: boolean
}

export const AuthOutlet = ({redirectTo, requiresAuth}: Props) => {
  const auth = useAuth()
  const location = useLocation()

  if ((requiresAuth && !auth.user) || (!requiresAuth && !!auth.user)) {
    return (
      <Navigate
        state={{
          from: location,
        }}
        to={redirectTo}
      />
    )
  }

  return <Outlet />
}
