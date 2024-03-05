import type {Order} from './types'

export const getFilterFunction =
  <T, Key extends keyof T>(search: string, keys: Key[]) =>
  (a: T) => {
    const lowerSearch = search.toLowerCase()

    return keys.some(key => {
      try {
        return (a[key] as string).toLowerCase().includes(lowerSearch)
      } catch (error) {
        return false
      }
    })
  }

const descendingComparator = <T>(a: T, b: T, orderBy: keyof T) => {
  if (b[orderBy] < a[orderBy]) {
    return -1
  }

  if (b[orderBy] > a[orderBy]) {
    return 1
  }

  return 0
}

export const getSortFunction = <T, Key extends keyof T>(order: Order, orderBy: Key) =>
  order === 'desc'
    ? (a: T, b: T) => descendingComparator(a, b, orderBy)
    : (a: T, b: T) => -descendingComparator(a, b, orderBy)

export const formatNumber = (number: number, decimalPlaces: number = 0) =>
  number.toLocaleString('en-US', {
    maximumFractionDigits: decimalPlaces,
    minimumFractionDigits: decimalPlaces,
  })
