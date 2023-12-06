import {Helmet} from 'react-helmet-async'

interface Props {
  description?: string
  title?: string
}

export const Head = ({description = '', title = ''}: Props) => {
  return (
    <Helmet defaultTitle='Pro-Trades' title={title ? `Pro-Trades | ${title}` : undefined}>
      <meta content={description} name='description' />
    </Helmet>
  )
}
