import {lazyImport} from '@/utils'

import {Navigate, Route, Routes as RoutesBase} from 'react-router-dom'
import {AuthOutlet} from './AuthOutlet'

const {Login} = lazyImport(() => import('@/features/auth/pages/Login'), 'Login')
const {RecoverPassword} = lazyImport(
  () => import('@/features/auth/pages/RecoverPassword'),
  'RecoverPassword'
)
const {Register} = lazyImport(() => import('@/features/auth/pages/Register'), 'Register')
const {ResetPassword} = lazyImport(
  () => import('@/features/auth/pages/ResetPassword'),
  'ResetPassword'
)

const {Predictions} = lazyImport(
  () => import('@/features/predictions/pages/Predictions'),
  'Predictions'
)

export const Routes = () => {
  return (
    <RoutesBase>
      {/* Common routes */}

      {/* Routes that require being not authed */}
      <Route element={<AuthOutlet redirectTo='/' requiresAuth={false} />} path='auth'>
        <Route element={<Login />} path='login' />
        <Route element={<RecoverPassword />} path='recover-password' />
        <Route element={<Register />} path='register' />
        <Route element={<ResetPassword />} path='reset-password' />

        <Route element={<Navigate to='/login' />} index path='*' />
      </Route>

      {/* Routes that require being authed */}
      <Route element={<AuthOutlet redirectTo='/auth/login' requiresAuth={true} />}>
        <Route element={<Predictions />} path='predictions' />

        <Route element={<Navigate to='/predictions' />} index path='*' />
      </Route>
    </RoutesBase>
  )
}
