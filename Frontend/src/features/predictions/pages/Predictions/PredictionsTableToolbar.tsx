import {FilterList, Search} from '@mui/icons-material'
import {IconButton, InputAdornment, TextField, Toolbar, Tooltip, Typography} from '@mui/material'

import type {ChangeEvent} from 'react'

interface Props {
  onChangeSearch: (event: ChangeEvent<HTMLInputElement>) => void
}

export const PredictionsTableToolbar = ({onChangeSearch}: Props) => {
  return (
    <Toolbar sx={{pl: {xs: 2}, pr: {xs: 1}}}>
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
        onChange={onChangeSearch}
        placeholder='Search...'
        size='small'
      />

      <Tooltip title='Filters'>
        <IconButton>
          <FilterList />
        </IconButton>
      </Tooltip>
    </Toolbar>
  )
}
