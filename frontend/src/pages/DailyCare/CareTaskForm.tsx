import DateInput from '../../components/DateInput'
import { useState } from 'react'
import { sydneyTimeToUtc } from '../../services/bookingTime'
import type { FormEvent } from 'react'
import { Button, Paper, Stack, TextField, Typography } from '@mui/material'
import type { CareStay, CreateCareTask } from '../../types/care'

interface Props {
  stay: CareStay
  disabled: boolean
  onCreate: (request: CreateCareTask) => Promise<boolean>
}

function CareTaskForm({ stay, disabled, onCreate }: Props) {
  const [description, setDescription] = useState('')
  const [scheduledAt, setScheduledAt] = useState('')
  const [errors, setErrors] = useState({ description: '', scheduledAt: '' })

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const text = description.trim()
    let date = new Date(NaN)
    const nextErrors = { description: '', scheduledAt: '' }
    try {
      date = new Date(sydneyTimeToUtc(scheduledAt))
    } catch {
      nextErrors.scheduledAt = 'Enter a valid Sydney date and time. Avoid the repeated or skipped daylight saving hour.'
    }

    if (!text || text.length > 500) {
      nextErrors.description = 'Enter 1 to 500 characters.'
    }
    if (!nextErrors.scheduledAt && (!stay.actualCheckIn || date < new Date(stay.actualCheckIn) ||
        date >= new Date(stay.scheduledCheckOut))) {
      nextErrors.scheduledAt = 'Choose a time on or after check-in and before planned check-out.'
    }
    setErrors(nextErrors)
    if (nextErrors.description || nextErrors.scheduledAt || disabled) return

    // Send Sydney input as UTC.
    if (await onCreate({ description: text, scheduledAt: date.toISOString() })) {
      setDescription('')
      setScheduledAt('')
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack component="form" noValidate onSubmit={handleSubmit} spacing={2}>
        <Typography variant="h6" component="h2">New care task</Typography>
        <TextField
          label="Care instructions" required multiline minRows={2}
          value={description} onChange={(event) => setDescription(event.target.value)}
          disabled={disabled} error={Boolean(errors.description)}
          helperText={errors.description || 'Up to 500 characters.'}
          slotProps={{ htmlInput: { maxLength: 500 } }}
        />
        <DateInput
          label="Scheduled time (Sydney)" withTime required
          value={scheduledAt} onChange={setScheduledAt}
          disabled={disabled} error={Boolean(errors.scheduledAt)}
          helperText={errors.scheduledAt || 'Australia/Sydney'}
        />
        <Button type="submit" variant="contained" disabled={disabled} sx={{ alignSelf: 'flex-start' }}>
          Add task
        </Button>
      </Stack>
    </Paper>
  )
}

export default CareTaskForm

