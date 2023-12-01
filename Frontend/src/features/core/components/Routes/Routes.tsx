import {useRoutes} from 'react-router-dom'

import {commonRoutes} from './commonRoutes'
import {protectedRoutes} from './protectedRoutes'
import {publicRoutes} from './publicRoutes'

export const Routes = () => {
  const isUserAuthed = false
  const routes = isUserAuthed ? protectedRoutes : publicRoutes

  const routesElement = useRoutes([...routes, ...commonRoutes])

  return <>{routesElement}</>
}
