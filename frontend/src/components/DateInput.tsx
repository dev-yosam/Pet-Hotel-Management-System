import dayjs from 'dayjs'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker'
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider'
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs'
import { enUS } from '@mui/x-date-pickers/locales'
import { useState, type FocusEvent } from 'react'
import utc from 'dayjs/plugin/utc'
import { Temporal } from '@js-temporal/polyfill'

dayjs.extend(utc)

interface Props {
  label: string
  value: string
  onChange: (value: string) => void
  withTime?: boolean
  disabled?: boolean
  required?: boolean
  error?: boolean
  helperText?: string
}

export default function DateInput({ label, value, onChange, withTime = false, disabled, required, error, helperText }: Props) {
  const [input, setInput] = useState({ external: value, date: value ? dayjs.utc(value) : null, version: 0 })
  if (value !== input.external) {
    setInput({ external: value, date: value ? dayjs.utc(value) : null, version: input.version + 1 })
  }
  const format = withTime ? 'YYYY-MM-DDTHH:mm' : 'YYYY-MM-DD'
  const invalid = input.external === 'Invalid Date' || Boolean(input.date && !input.date.isValid())
  function validateSections(event: FocusEvent<HTMLDivElement>) {
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) return
    const sections = [...event.currentTarget.querySelectorAll('[role="spinbutton"]')]
    const filled = sections.filter(section => section.hasAttribute('aria-valuenow')).length
    // A partly entered optional date must not be treated as an empty filter.
    if (filled > 0 && filled < sections.length) {
      setInput(current => ({ ...current, external: 'Invalid Date' }))
      onChange('Invalid Date')
    } else if (filled === 0 && input.external === 'Invalid Date') {
      setInput(current => ({ ...current, external: '', date: null }))
      onChange('')
    }
  }
  // Use neutral dates so the computer's timezone cannot change the entered time.
  const props = {
    label,
    value: input.date,
    onChange: (date: dayjs.Dayjs | null) => {
      const external = date ? date.format(format) : ''
      setInput(current => ({ ...current, external, date }))
      onChange(external)
    },
    timezone: 'UTC',
    referenceDate: dayjs.utc(Temporal.Now.zonedDateTimeISO('Australia/Sydney').toPlainDateTime().toString()),
    disabled,
    maxDate: dayjs.utc('9998-12-31'),
    slotProps: {
      textField: { fullWidth: true, required, onBlur: validateSections, helperText: invalid ? 'Enter a valid date and time.' : (helperText || ' '), ...((error || invalid) ? { error: true } : {}) },
      actionBar: { actions: ['clear', 'cancel', 'accept'] as ('clear' | 'cancel' | 'accept')[] },
      day: { sx: { borderRadius: 0 } },
    },
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="en" localeText={enUS.components.MuiLocalizationProvider.defaultProps.localeText}>
      {withTime
        ? <DateTimePicker key={input.version} {...props} format="DD/MM/YYYY HH:mm" ampm={false} />
        : <DatePicker key={input.version} {...props} format="DD/MM/YYYY" />}
    </LocalizationProvider>
  )
}
