import {Layout} from '@/components/Layout'

import reactLogo from '@/assets/react.svg'

export const Landing = () => {
  return (
    <Layout>
      <a href='https://react.dev' rel='noreferrer' target='_blank'>
        <img alt='React logo' className='logo react' src={reactLogo} />
      </a>
    </Layout>
  )
}
