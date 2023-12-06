import {Route, Routes as RoutesBase} from 'react-router-dom'
import {Landing} from '@/features/core/pages/Landing'
import {NotFound} from '@/features/core/pages/NotFound'

import {lazyImport} from '@/utils'

const {ForgotPassword} = lazyImport(
  () => import('@/features/auth/pages/ForgotPassword'),
  'ForgotPassword'
)
const {Login} = lazyImport(() => import('@/features/auth/pages/Login'), 'Login')
const {Register} = lazyImport(() => import('@/features/auth/pages/Register'), 'Register')

const {Predictions} = lazyImport(
  () => import('@/features/predictions/pages/Predictions'),
  'Predictions'
)

const {CounterHome} = lazyImport(
  () => import('@/features/counter/pages/CounterHome'),
  'CounterHome'
)

export const Routes = () => {
  return (
    <RoutesBase>
      <Route element={<Landing />} index />

      <Route path='auth'>
        <Route element={<ForgotPassword />} path='forgot-password' />
        <Route element={<Login />} path='login' />
        <Route element={<Register />} path='register' />
      </Route>

      <Route path='predictions'>
        <Route element={<Predictions />} index />
      </Route>

      <Route path='counter'>
        <Route element={<CounterHome />} index />
      </Route>

      <Route element={<NotFound />} path='*' />
    </RoutesBase>
  )
}
