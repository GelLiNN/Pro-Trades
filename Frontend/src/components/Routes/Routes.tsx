import {Navigate, Route, Routes as RoutesBase} from 'react-router-dom'
import {PrivateOutlet} from './PrivateOutlet'
import {PublicOutlet} from './PublicOutlet'

import {lazyImport} from '@/utils'

const {Login} = lazyImport(() => import('@/features/auth/pages/Login'), 'Login')
const {RecoverPassword} = lazyImport(
  () => import('@/features/auth/pages/RecoverPassword'),
  'RecoverPassword'
)
const {Register} = lazyImport(() => import('@/features/auth/pages/Register'), 'Register')

const {Predictions} = lazyImport(
  () => import('@/features/predictions/pages/Predictions'),
  'Predictions'
)

export const Routes = () => {
  return (
    <RoutesBase>
      <Route element={<PublicOutlet />} path='auth'>
        <Route element={<Login />} path='login' />
        <Route element={<RecoverPassword />} path='recover-password' />
        <Route element={<Register />} path='register' />
      </Route>

      <Route element={<PrivateOutlet />}>
        <Route element={<Predictions />} path='predictions' />
        <Route element={<Navigate to='/predictions' />} path='*' />
      </Route>
    </RoutesBase>
  )
}
