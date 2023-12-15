import {useCallback, useMemo, useState} from 'react'
import {formatNumber, getFilterFunction, getSortFunction} from './helpers'

import {
  Box,
  Divider,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TablePagination,
  TableRow,
  Typography,
} from '@mui/material'
import {PredictionsTableHead} from './PredictionsTableHead'
import {PredictionsTableToolbar} from './PredictionsTableToolbar'

import type {ChangeEvent, MouseEvent} from 'react'
import type {Order, Stock} from './types'

interface Props {
  lastUpdatedDate: Date
  rows: Stock[]
}

export const PredictionsTable = ({lastUpdatedDate, rows}: Props) => {
  const [search, setSearch] = useState('')
  const [order, setOrder] = useState<Order>('desc')
  const [orderBy, setOrderBy] = useState<keyof Stock>('scoreValue')
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(50)

  const handleChangeSearch = useCallback((event: ChangeEvent<HTMLInputElement>) => {
    setSearch(event.target.value)
  }, [])

  const handleRequestSort = useCallback(
    (_event: MouseEvent<unknown>, key: keyof Stock) => {
      const isAsc = key === orderBy && order === 'asc'
      setOrder(isAsc ? 'desc' : 'asc')
      setOrderBy(key)
    },
    [order, orderBy]
  )

  const handleClick = useCallback((_event: MouseEvent<unknown>, symbol: string) => {
    // TODO: show more details
    console.log('symbol:', symbol)
  }, [])

  const handleChangePage = useCallback(
    (_event: MouseEvent<HTMLButtonElement> | null, page: number) => {
      setPage(page)
    },
    []
  )

  const handleChangeRowsPerPage = useCallback((event: ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10))
    setPage(0)
  }, [])

  const visibleRows = useMemo(
    () =>
      rows
        .filter(getFilterFunction(search, ['name', 'symbol']))
        .toSorted(getSortFunction(order, orderBy))
        .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage),
    [order, orderBy, page, rows, rowsPerPage, search]
  )

  return (
    <Paper
      elevation={2}
      sx={{display: 'grid', gridTemplateRows: 'auto auto 1fr auto auto', height: '100%'}}
    >
      <PredictionsTableToolbar onChangeSearch={handleChangeSearch} />

      <Divider />

      <TableContainer
        sx={{
          // Required to keep scroll bar on final page with empty rows
          overflowY: 'scroll',
        }}
      >
        <Table aria-labelledby='tableTitle' size='small' stickyHeader>
          <PredictionsTableHead onRequestSort={handleRequestSort} order={order} orderBy={orderBy} />

          <TableBody>
            {visibleRows.map(visibleRow => {
              const {name, price, scoreRank, scoreValue, symbol, throughput} = visibleRow

              return (
                <TableRow
                  hover
                  key={symbol}
                  onClick={event => handleClick(event, symbol)}
                  sx={{cursor: 'pointer'}}
                >
                  <TableCell component='th' scope='row' width={1}>
                    {symbol}
                  </TableCell>

                  <TableCell>{name}</TableCell>

                  <TableCell align='right' width={150}>
                    ${formatNumber(price, 2)}
                  </TableCell>

                  <TableCell align='right' width={150}>
                    ${formatNumber(throughput, 2)}
                  </TableCell>

                  <TableCell align='right' width={100}>
                    {formatNumber(scoreValue, 2)}
                  </TableCell>

                  <TableCell
                    align='center'
                    sx={theme => ({
                      backgroundColor: {
                        BAD: theme.palette.error.light,
                        DISQUALIFIED: theme.palette.error.main,
                        FAIR: theme.palette.warning.light,
                        GOOD: theme.palette.success.light,
                        PRIME: theme.palette.success.main,
                      }[scoreRank],
                    })}
                    width={125}
                  >
                    {scoreRank}
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <Divider />

      <Box sx={{display: 'grid', gridAutoFlow: 'column', alignItems: 'center', pl: 2}}>
        <Typography
          color='text.secondary'
          fontSize={10}
          fontStyle='italic'
          fontWeight={500}
          variant='body2'
        >
          Last updated {lastUpdatedDate.toLocaleString()}
        </Typography>

        <TablePagination
          component='div'
          count={rows.length}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
          page={page}
          rowsPerPage={rowsPerPage}
          rowsPerPageOptions={[25, 50, 100]}
        />
      </Box>
    </Paper>
  )
}
