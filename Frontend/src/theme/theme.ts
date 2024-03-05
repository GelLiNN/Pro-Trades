import {createTheme} from '@mui/material'
import {components} from './components'
import {palette} from './palette'

// (https://mui.com/material-ui/customization/theming/#api)
export const theme = createTheme({
  components,
  palette,
})
