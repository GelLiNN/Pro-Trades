import {ThemeProvider} from '@emotion/react'
import {CssBaseline} from '@mui/material'
import {StrictMode, Suspense} from 'react'
import {ErrorBoundary} from 'react-error-boundary'
import {HelmetProvider} from 'react-helmet-async'
import {BrowserRouter as Router} from 'react-router-dom'
import {Provider as ReduxProvider} from 'react-redux'
import {AppLoading} from '@/components/AppLoading'
import {Routes} from '@/components/Routes'
import {Snackbar} from '@/features/notifications/components/Snackbar'

import {store} from '@/store'
import {theme} from '@/theme'

export const App = () => {
  return (
    <StrictMode>
      <ThemeProvider theme={theme}>
        <CssBaseline />

        <Suspense fallback={<AppLoading />}>
          <ErrorBoundary FallbackComponent={AppErrorFallback}>
            <HelmetProvider>
              <ReduxProvider store={store}>
                <Snackbar />

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

const AppErrorFallback = () => {
  return <span>Something went wrong :(</span>
}
