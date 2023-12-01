import {configureStore} from '@reduxjs/toolkit'
import {counterReducer} from '@/features/counter/state/counterSlice'

export const store = configureStore({
  reducer: {
    counter: counterReducer,
  },
})
