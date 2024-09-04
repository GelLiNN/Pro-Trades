import {LockOutlined} from '@mui/icons-material'
import {Avatar, Box, Container, Paper, Typography} from '@mui/material'
import {Layout} from '@/components/Layout'

import type {FunctionComponent, ReactNode} from 'react'

interface Props {
  children: ReactNode
  IconComponent?: FunctionComponent
  title: string
}

export const AuthLayout = ({children, IconComponent = LockOutlined, title}: Props) => {
  return (
    <Layout title={title}>
      <Container component='main' maxWidth='sm'>
        <Paper elevation={2} sx={{mt: 8, mb: 4}}>
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              p: 6,
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
              <Avatar sx={{m: 1, bgcolor: 'secondary.main'}}>
                <IconComponent />
              </Avatar>

              <Typography component='h1' variant='h5'>
                {title}
              </Typography>
            </Box>

            {children}
          </Box>
        </Paper>
      </Container>
    </Layout>
  )
}
