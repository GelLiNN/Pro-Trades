import {Box} from '@mui/material'
import {Footer} from './Footer'
import {Head} from './Head'
import {Header} from './Header'

import type {ReactNode} from 'react'

interface Props {
  children: ReactNode
  description?: string
  title?: string
}

export const Layout = ({children, description, title}: Props) => {
  return (
    <>
      <Head description={description} title={title} />

      <Box sx={{display: 'flex', minHeight: '100vh', flexDirection: 'column'}}>
        <Header />

        {children}

        <Footer />
      </Box>
    </>
  )
}
