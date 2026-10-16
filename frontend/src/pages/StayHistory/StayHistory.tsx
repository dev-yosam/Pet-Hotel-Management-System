import DateInput from '../../components/DateInput'
import { useEffect, useState, type FormEvent } from 'react'
import StayActions from '../../components/StayActions'
import { getErrorMessage } from '../../services/apiErrors'
import { Temporal } from '@js-temporal/polyfill'
import { formatSydneyTime } from '../../services/bookingTime'
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  LinearProgress, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material'
import {
  searchStayHistory, type StayHistoryFilters, type StayHistoryItem, type StayHistoryResult,
} from '../../services/stayHistoryService'

const emptyFilters: StayHistoryFilters = { pet: '', owner: '', from: '', to: '', status: '', sort: 'newest' }
const statuses: Record<string, string> = {
  Booked: 'Booked', CheckedIn: 'Checked in', CheckedOut: 'Checked out', Cancelled: 'Cancelled',
}
function formatTime(value: string | null) {
  return value ? formatSydneyTime(value) : 'Not recorded'
}

function StayHistory() {
  const [filters, setFilters] = useState(emptyFilters)
  const [request, setRequest] = useState({ filters: emptyFilters, page: 1 })
  const [result, setResult] = useState<StayHistoryResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [validationError, setValidationError] = useState('')
  const [selected, setSelected] = useState<StayHistoryItem | null>(null)
  const [success, setSuccess] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    searchStayHistory(request.filters, request.page, controller.signal)
      .then(data => { if (!controller.signal.aborted) setResult(data) })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) setError(getErrorMessage(reason))
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [request])

  function load(nextFilters: StayHistoryFilters, page = 1) {
    setLoading(true)
    setError('')
    setResult(null)
    setSelected(null)
    setRequest({ filters: { ...nextFilters }, page })
  }

  function search(event: FormEvent) {
    event.preventDefault()
    if (event.currentTarget.querySelector('[aria-invalid="true"]')) {
      setValidationError('Enter a valid date before year 9999.')
      return
    }
    try {
      for (const date of [filters.from, filters.to]) {
        if (date && (!/^\d{4}-\d{2}-\d{2}$/.test(date) || Temporal.PlainDate.from(date).year > 9998))
          throw new Error()
      }
    } catch {
      setValidationError('Enter a valid date before year 9999.')
      return
    }
    if (filters.from && filters.to && filters.from > filters.to) {
      setValidationError('End date must be on or after start date.')
      return
    }
    setValidationError('')
    load(filters)
  }

  function reset() {
    setFilters(emptyFilters)
    setValidationError('')
    load(emptyFilters)
  }

  function updateFilter(key: keyof StayHistoryFilters, value: string) {
    setFilters(current => ({ ...current, [key]: value }))
  }

  return (
    <Stack spacing={3}>
      <Typography variant="h4" component="h1">Stay History</Typography>
      {success && <Alert severity="success" onClose={() => setSuccess('')}>{success}</Alert>}
      <Paper component="form" noValidate variant="outlined" onSubmit={search} sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Typography color="text.secondary">Search planned arrival dates. All times use Australia/Sydney.</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 2 }}>
            <TextField label="Pet name" value={filters.pet} onChange={e => updateFilter('pet', e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
            <TextField label="Owner name" value={filters.owner} onChange={e => updateFilter('owner', e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
            <TextField select label="Status" value={filters.status} onChange={e => updateFilter('status', e.target.value)}>
              <MenuItem value="">All statuses</MenuItem>
              {Object.entries(statuses).map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
            </TextField>
            <DateInput label="Planned arrival from" value={filters.from} onChange={value => updateFilter('from', value)} />
            <DateInput label="Planned arrival to" value={filters.to} onChange={value => updateFilter('to', value)} />
            <TextField select label="Sort by" value={filters.sort} onChange={e => updateFilter('sort', e.target.value)}>
              <MenuItem value="newest">Latest planned arrival</MenuItem>
              <MenuItem value="oldest">Earliest planned arrival</MenuItem>
              <MenuItem value="pet">Pet name</MenuItem>
              <MenuItem value="owner">Owner name</MenuItem>
            </TextField>
          </Box>
          {validationError && <Alert severity="error">{validationError}</Alert>}
          <Stack direction="row" spacing={2}>
            <Button type="submit" variant="contained" disabled={loading}>Search</Button>
            <Button type="button" variant="outlined" onClick={reset} disabled={loading}>Reset</Button>
          </Stack>
        </Stack>
      </Paper>
      {loading && <Stack spacing={1} role="status">
        <Typography>Loading stays...</Typography>
        <LinearProgress aria-label="Loading stays" />
      </Stack>}
      {error && <Alert severity="error" action={<Button color="inherit" onClick={() => load(request.filters, request.page)}>Retry</Button>}>{error}</Alert>}
      {result && <Stack spacing={2}>
        <Typography role="status">{result.totalCount} stays found</Typography>
        {result.items.length === 0 && <Alert severity="info">No stays match your search.</Alert>}
        {result.items.map(stay => <Paper key={stay.id} variant="outlined" sx={{ p: 3 }}>
          <Stack spacing={2}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ justifyContent: 'space-between', alignItems: { xs: 'flex-start', sm: 'center' } }}>
              <Typography variant="h6" component="h2" sx={{ overflowWrap: 'anywhere' }}>{stay.petName} · Stay #{stay.id}</Typography>
              <Chip label={statuses[stay.status] ?? stay.status} variant="outlined" sx={{ borderRadius: 0 }} />
            </Stack>
            <Typography sx={{ overflowWrap: 'anywhere' }}>Owner: {stay.ownerName} · Room: {stay.roomNumber}</Typography>
            <Typography>Planned arrival: {formatTime(stay.scheduledCheckIn)}</Typography>
            <Typography>Planned departure: {formatTime(stay.scheduledCheckOut)}</Typography>
            <Button variant="outlined" onClick={() => setSelected(stay)} sx={{ alignSelf: 'flex-start' }}>View details</Button>
          </Stack>
        </Paper>)}
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap', rowGap: 1 }}>
          <Button disabled={result.page <= 1} onClick={() => load(request.filters, result.page - 1)}>Previous</Button>
          <Typography>Page {result.page} of {Math.max(1, Math.ceil(result.totalCount / result.pageSize))}</Typography>
          <Button disabled={result.page * result.pageSize >= result.totalCount} onClick={() => load(request.filters, result.page + 1)}>Next</Button>
        </Stack>
      </Stack>}
      <Dialog open={selected !== null} onClose={() => setSelected(null)} fullWidth maxWidth="sm" aria-labelledby="stay-details-title">
        <DialogTitle id="stay-details-title">Stay #{selected?.id}</DialogTitle>
        <DialogContent dividers>
          {selected && <Stack spacing={2} sx={{ overflowWrap: 'anywhere' }}>
            <Typography>Pet: {selected.petName} ({selected.species})</Typography>
            <Typography>Owner: {selected.ownerName}</Typography>
            <Typography>Phone: {selected.ownerPhone}</Typography>
            <Typography>Room: {selected.roomNumber} ({selected.roomType})</Typography>
            <Typography>Status: {statuses[selected.status] ?? selected.status}</Typography>
            <Typography>Planned arrival: {formatTime(selected.scheduledCheckIn)}</Typography>
            <Typography>Planned departure: {formatTime(selected.scheduledCheckOut)}</Typography>
            <Typography>Actual check-in: {formatTime(selected.actualCheckIn)}</Typography>
            <Typography>Actual check-out: {formatTime(selected.actualCheckOut)}</Typography>
            <Typography>Total charge: {selected.totalCharge === null ? 'Not calculated' : new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(selected.totalCharge)}</Typography>
            <Typography variant="body2" color="text.secondary">All times use Australia/Sydney.</Typography>
            <StayActions key={selected.id} stayId={selected.id} status={selected.status} onChanged={stay => {
              setSuccess(stay.status === 'CheckedIn' ? 'Pet checked in.' : 'Pet checked out. Open the stay details to view the final charge.')
              load(request.filters, request.page)
            }} />
          </Stack>}
        </DialogContent>
        <DialogActions><Button onClick={() => setSelected(null)}>Close</Button></DialogActions>
      </Dialog>
    </Stack>
  )
}

export default StayHistory

