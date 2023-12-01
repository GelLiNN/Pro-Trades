import {lazyImport} from '@/utils'

const {Login} = lazyImport(() => import('@/features/auth/pages/Login'), 'Login')
const {Register} = lazyImport(() => import('@/features/auth/pages/Register'), 'Register')

export const publicRoutes = [
  {
    element: <Login />,
    path: '/auth/login',
  },
  {
    element: <Register />,
    path: '/auth/register',
  },
]
