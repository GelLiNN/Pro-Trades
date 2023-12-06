import {ThemeProvider} from '@emotion/react'
import {CssBaseline} from '@mui/material'
import {StrictMode, Suspense} from 'react'
import {ErrorBoundary} from 'react-error-boundary'
import {HelmetProvider} from 'react-helmet-async'
import {BrowserRouter as Router} from 'react-router-dom'
import {Provider as ReduxProvider} from 'react-redux'
import {Notifications} from '@/components/Notifications'
import {Routes} from '@/components/Routes'

import {store} from '@/store'
import {theme} from '@/theme'

const Loading = () => {
  return <span>Loading ...</span>
}

const ErrorFallback = () => {
  return <span>Something went wrong :(</span>
}

export const App = () => {
  return (
    <StrictMode>
      <ThemeProvider theme={theme}>
        <CssBaseline />

        <Suspense fallback={<Loading />}>
          <ErrorBoundary FallbackComponent={ErrorFallback}>
            <HelmetProvider>
              <ReduxProvider store={store}>
                <Notifications />

                <Router>
                  <Routes />
                </Router>
              </ReduxProvider>
            </HelmetProvider>
          </ErrorBoundary>
        </Suspense>
      </ThemeProvider>
    </StrictMode>
  )
}
