import * as React from 'react'

export const lazyImport = <
  T extends React.ComponentType<unknown>,
  I extends {[K2 in K]: T},
  K extends keyof I
>(
  factory: () => Promise<I>,
  name: K
): I => {
  return Object.create({
    [name]: React.lazy(() => factory().then(module => ({default: module[name]}))),
  })
}
