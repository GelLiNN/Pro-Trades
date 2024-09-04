import {LinkBehavior} from '@/components/LinkBehavior'

import type {ThemeOptions} from '@mui/material'

export const components: ThemeOptions['components'] = {
  MuiButtonBase: {
    defaultProps: {
      LinkComponent: LinkBehavior,
    },
  },
  MuiLink: {
    defaultProps: {
      component: LinkBehavior,
    },
  },
}
