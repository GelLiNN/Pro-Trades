import {LockOutlined} from '@mui/icons-material'
import {Avatar, Box, Container, Typography} from '@mui/material'
import {Layout} from '@/components/Layout'

import type {ReactNode} from 'react'

interface Props {
  children: ReactNode
  title: string
}

export const AuthLayout = ({children, title}: Props) => {
  return (
    <Layout>
      <Container component='main' maxWidth='xs'>
        <Box
          sx={{
            marginTop: 8,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Box
            sx={{
              display: 'grid',
              gridAutoFlow: 'column',
              alignItems: 'center',
              gridColumnGap: 8,
            }}
          >
            <Avatar
              sx={{
                m: 1,
                bgcolor: 'secondary.main',
              }}
            >
              <LockOutlined />
            </Avatar>

            <Typography component='h1' variant='h5'>
              {title}
            </Typography>
          </Box>

          {children}
        </Box>
      </Container>
    </Layout>
  )
}
