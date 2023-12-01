import * as React from 'react'

import {ErrorBoundary} from 'react-error-boundary'
import {HelmetProvider} from 'react-helmet-async'
import {BrowserRouter as Router} from 'react-router-dom'
import {Provider as ReduxProvider} from 'react-redux'
import {Notifications} from '@/features/core/components/Notifications'
import {Routes} from '@/features/core/components/Routes'

import {store} from '@/store'

const Loading = () => {
  return <span>Loading ...</span>
}

const ErrorFallback = () => {
  return <span>Something went wrong :(</span>
}

export const App = () => {
  return (
    <React.Suspense fallback={<Loading />}>
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
    </React.Suspense>
  )
}
