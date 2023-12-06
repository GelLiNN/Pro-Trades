import {forwardRef} from 'react'
import {Link as RouterLink, LinkProps as RouterLinkProps} from 'react-router-dom'

export const LinkBehavior = forwardRef<
  HTMLAnchorElement,
  Omit<RouterLinkProps, 'to'> & {href: RouterLinkProps['to']}
>(function LinkBehavior(props, ref) {
  const {href, ...other} = props
  // Map href (Material UI) -> to (react-router)
  return <RouterLink ref={ref} to={href} {...other} />
})
