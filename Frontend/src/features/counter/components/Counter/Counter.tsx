import {useState} from 'react'

import {useSelector, useDispatch} from '@/store'
import {
  decrement,
  increment,
  incrementByAmount,
  incrementAsync,
  incrementIfOdd,
  selectCount,
} from '@/features/counter/state/counterSlice'

import './Counter.css'

export function Counter() {
  const count = useSelector(selectCount)
  const dispatch = useDispatch()
  const [incrementAmount, setIncrementAmount] = useState('2')

  const incrementValue = Number(incrementAmount) || 0

  return (
    <div>
      <div className='row'>
        <button
          aria-label='Decrement value'
          className='button'
          onClick={() => dispatch(decrement())}
        >
          -
        </button>
        <span className='value'>{count}</span>
        <button
          aria-label='Increment value'
          className='button'
          onClick={() => dispatch(increment())}
        >
          +
        </button>
      </div>
      <div className='row'>
        <input
          aria-label='Set increment amount'
          className='textbox'
          onChange={e => setIncrementAmount(e.target.value)}
          value={incrementAmount}
        />
        <button className='button' onClick={() => dispatch(incrementByAmount(incrementValue))}>
          Add Amount
        </button>
        <button className='asyncButton' onClick={() => dispatch(incrementAsync(incrementValue))}>
          Add Async
        </button>
        <button className='button' onClick={() => dispatch(incrementIfOdd(incrementValue))}>
          Add If Odd
        </button>
      </div>
    </div>
  )
}
