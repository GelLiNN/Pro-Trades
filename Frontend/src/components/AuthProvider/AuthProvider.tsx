import {useGetUserQuery} from '@/features/auth/api'

import {AppLoading} from '@/components/AppLoading'

import type {ReactNode} from 'react'

interface Props {
  children: ReactNode
}

export const AuthProvider = ({children}: Props) => {
  const {isLoading} = useGetUserQuery()

  if (isLoading) {
    return <AppLoading />
  }

  return children
}
