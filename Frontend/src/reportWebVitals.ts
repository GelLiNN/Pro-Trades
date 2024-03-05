import type {ReportCallback} from 'web-vitals'

const IS_REPORT_ENABLED = false
const REPORT_CALLBACK: ReportCallback = console.log

export const reportWebVitals = () => {
  if (IS_REPORT_ENABLED) {
    import('web-vitals').then(({onCLS, onFCP, onFID, onLCP, onTTFB}) => {
      onCLS(REPORT_CALLBACK)
      onFCP(REPORT_CALLBACK)
      onFID(REPORT_CALLBACK)
      onLCP(REPORT_CALLBACK)
      onTTFB(REPORT_CALLBACK)
    })
  }
}
