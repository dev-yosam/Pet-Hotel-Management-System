import dayjs from 'dayjs'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker'
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider'
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs'
import { enUS } from '@mui/x-date-pickers/locales'

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
  // Keep the entered date and time unchanged. The form handles timezone conversion.
  const props = {
    label,
    value: value ? dayjs(value) : null,
    onChange: (date: dayjs.Dayjs | null) => onChange(date ? date.format(withTime ? 'YYYY-MM-DDTHH:mm' : 'YYYY-MM-DD') : ''),
    disabled,
    maxDate: dayjs('9998-12-31'),
    slotProps: {
      textField: { fullWidth: true, required, helperText, ...(error ? { error: true } : {}) },
      actionBar: { actions: ['clear', 'today', 'cancel', 'accept'] as ('clear' | 'today' | 'cancel' | 'accept')[] },
      day: { sx: { borderRadius: 0 } },
    },
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="en" localeText={enUS.components.MuiLocalizationProvider.defaultProps.localeText}>
      {withTime
        ? <DateTimePicker {...props} format="DD/MM/YYYY HH:mm" ampm={false} />
        : <DatePicker {...props} format="DD/MM/YYYY" />}
    </LocalizationProvider>
  )
}
