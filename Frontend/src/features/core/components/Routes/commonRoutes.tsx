import {Landing} from '@/features/core/pages/Landing'
import {Navigate} from 'react-router-dom'

export const commonRoutes = [
  {
    element: <Landing />,
    path: '/',
  },
  {
    element: <Navigate to='.' />,
    path: '*',
  },
]
