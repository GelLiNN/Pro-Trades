import {HEAD_CELLS} from './constants'

import {useCallback} from 'react'
import {visuallyHidden} from '@mui/utils'

import {Box, TableCell, TableHead, TableRow, TableSortLabel} from '@mui/material'

import type {MouseEvent} from 'react'
import type {Order, Stock} from './types'

interface Props {
  onRequestSort: (event: MouseEvent<unknown>, key: keyof Stock) => void
  order: Order
  orderBy: keyof Stock
}

export const PredictionsTableHead = ({onRequestSort, order, orderBy}: Props) => {
  const createSortHandler = useCallback(
    (key: keyof Stock) => (event: MouseEvent<unknown>) => {
      onRequestSort(event, key)
    },
    [onRequestSort]
  )

  return (
    <TableHead>
      <TableRow>
        {HEAD_CELLS.map(HEAD_CELL => {
          const {align, isSortable, key, label} = HEAD_CELL
          const isOrderedBy = orderBy === key

          return (
            <TableCell align={align} key={key} sortDirection={isOrderedBy ? order : false}>
              {isSortable ? (
                <TableSortLabel
                  active={isOrderedBy}
                  direction={isOrderedBy ? order : 'asc'}
                  onClick={createSortHandler(key)}
                >
                  {label}

                  {isOrderedBy ? (
                    <Box component='span' sx={visuallyHidden}>
                      {`sorted ${order === 'desc' ? 'descended' : 'ascended'}`}
                    </Box>
                  ) : null}
                </TableSortLabel>
              ) : (
                label
              )}
            </TableCell>
          )
        })}
      </TableRow>
    </TableHead>
  )
}
