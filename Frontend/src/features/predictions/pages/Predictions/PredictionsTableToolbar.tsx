import {useCallback, useRef} from 'react'

import {Search} from '@mui/icons-material'
import {InputAdornment, TextField, Toolbar, Typography} from '@mui/material'

import type {ChangeEvent} from 'react'

interface Props {
  onChangeSearch: (event: ChangeEvent<HTMLInputElement>) => void
}

export const PredictionsTableToolbar = ({onChangeSearch}: Props) => {
  const timerRef = useRef<NodeJS.Timeout>()

  const handleChangeSearch = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }

      timerRef.current = setTimeout(() => {
        onChangeSearch(event)
      }, 250)
    },
    [onChangeSearch]
  )

  return (
    <Toolbar sx={{px: {xs: 2}}}>
      <Typography id='tableTitle' sx={{mr: 'auto'}} variant='h6'>
        Stock Predictions
      </Typography>

      <TextField
        InputProps={{
          startAdornment: (
            <InputAdornment position='start'>
              <Search />
            </InputAdornment>
          ),
        }}
        onChange={handleChangeSearch}
        placeholder='Search...'
        size='small'
      />
    </Toolbar>
  )
}
