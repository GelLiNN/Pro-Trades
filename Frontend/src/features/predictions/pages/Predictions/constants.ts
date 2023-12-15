import type {HeadCell} from './types'

export const HEAD_CELLS: HeadCell[] = [
  {
    align: 'left',
    isSortable: false,
    key: 'symbol',
    label: 'Symbol',
  },
  {
    align: 'left',
    isSortable: true,
    key: 'name',
    label: 'Name',
  },
  {
    align: 'right',
    isSortable: true,
    key: 'price',
    label: 'Last Price',
  },
  {
    align: 'right',
    isSortable: true,
    key: 'throughput',
    label: 'Throughput',
  },
  {
    align: 'right',
    isSortable: true,
    key: 'scoreValue',
    label: 'Score',
  },
  {
    align: 'center',
    isSortable: false,
    key: 'scoreRank',
    label: 'Rank',
  },
]
