import { useEffect, useState } from 'react'
import {
  Alert, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogContentText, DialogTitle, FormControlLabel, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material'
import { ApiError } from '../../services/apiClient'
import { createCareTask, deleteCareTask, getCareStays, getCareTasks, updateCareCompletion } from '../../services/careService'
import type { CareStay, CareTask } from '../../types/care'
import CareTaskForm from './CareTaskForm'

function errorMessage(error: unknown) {
  if (error instanceof ApiError) {
    const details = error.details
    if (details && typeof details === 'object' && 'errors' in details &&
        details.errors && typeof details.errors === 'object') {
      const messages = Object.values(details.errors).flat().filter((value) => typeof value === 'string')
      if (messages.length) return messages.join(' ')
    }
    return error.message
  }
  return 'Could not reach the server. Check the connection and try again.'
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('en-AU')
}

function DailyCare() {
  const [stays, setStays] = useState<CareStay[]>([])
  const [stayId, setStayId] = useState<number | ''>('')
  const [tasks, setTasks] = useState<CareTask[]>([])
  const [loadingStays, setLoadingStays] = useState(true)
  const [loadingTasks, setLoadingTasks] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [refresh, setRefresh] = useState(0)
  const [filter, setFilter] = useState('all')
  const [deleteTarget, setDeleteTarget] = useState<CareTask | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getCareStays(controller.signal)
      .then((data) => {
        setStays(data)
        setStayId((current) => data.some((stay) => stay.id === current) ? current : '')
      })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) {
          setStays([])
          setStayId('')
          setLoadingTasks(false)
          setError(errorMessage(reason))
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingStays(false)
      })
    return () => controller.abort()
  }, [refresh])

  useEffect(() => {
    const controller = new AbortController()
    if (stayId === '') {
      return () => controller.abort()
    }
    getCareTasks(stayId, controller.signal)
      .then(setTasks)
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) setError(errorMessage(reason))
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingTasks(false)
      })
    return () => controller.abort()
  }, [stayId, refresh])

  const selectedStay = stays.find((stay) => stay.id === stayId)
  const canEdit = selectedStay?.status === 'CheckedIn' && Boolean(selectedStay.actualCheckIn)
  const disabled = busy || loadingStays || (stayId !== '' && loadingTasks)
  const visibleTasks = tasks.filter((task) =>
    filter === 'all' || (filter === 'completed' ? task.isCompleted : !task.isCompleted))

  function reload() {
    setLoadingStays(true)
    setLoadingTasks(stayId !== '')
    setTasks([])
    setRefresh((value) => value + 1)
  }

  async function saveChange(action: () => Promise<unknown>, message: string) {
    setBusy(true)
    setError('')
    setSuccess('')
    try {
      await action()
      setSuccess(message)
      setDeleteTarget(null)
      reload()
      return true
    } catch (reason) {
      setDeleteTarget(null)
      setError(errorMessage(reason))
      // Reload the stay when another user has changed its status.
      if (reason instanceof ApiError && (reason.status === 409 || reason.status === 404)) reload()
      return false
    } finally {
      setBusy(false)
    }
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" component="h1" gutterBottom>Daily Care</Typography>
        <Typography color="text.secondary">Plan care tasks and record completed care for each stay.</Typography>
      </Box>

      {error && <Alert severity="error" action={
        <Button color="inherit" disabled={disabled} onClick={() => { setError(''); reload() }}>
          Retry
        </Button>
      }>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess('')}>{success}</Alert>}

      <TextField select fullWidth label="Pet stay" value={stayId} disabled={disabled}
        onChange={(event) => {
          setStayId(event.target.value === '' ? '' : Number(event.target.value))
          setTasks([])
          setLoadingTasks(event.target.value !== '')
          setError('')
          setSuccess('')
          setFilter('all')
        }}
        helperText="Checked-in stays and stays with existing care records."
      >
        <MenuItem value="">Select a stay</MenuItem>
        {stays.map((stay) => (
          <MenuItem key={stay.id} value={stay.id} sx={{ whiteSpace: 'normal' }}>
            {stay.petName} ? {stay.ownerName} ? Room {stay.roomNumber} ? {stay.status} ? Stay #{stay.id}
          </MenuItem>
        ))}
      </TextField>

      {(loadingStays || (stayId !== '' && loadingTasks)) && (
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }} role="status">
          <CircularProgress size={22} /><Typography>Loading care records...</Typography>
        </Stack>
      )}
      {!loadingStays && stays.length === 0 && !error && (
        <Alert severity="info">No stays are available yet. Check in a pet before adding care tasks.</Alert>
      )}

      {selectedStay && (
        <>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Stack spacing={1}>
              <Typography variant="h6" component="h2">{selectedStay.petName}</Typography>
              <Typography>Owner: {selectedStay.ownerName} ? Room: {selectedStay.roomNumber}</Typography>
              <Typography>Check-in: {selectedStay.actualCheckIn ? formatTime(selectedStay.actualCheckIn) : 'Not recorded'}</Typography>
              <Typography>Planned check-out: {formatTime(selectedStay.scheduledCheckOut)}</Typography>
              <Typography sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
                Care notes: {selectedStay.careInstructions || 'No special instructions.'}
              </Typography>
            </Stack>
          </Paper>

          {canEdit ? (
            <CareTaskForm key={selectedStay.id} stay={selectedStay} disabled={disabled}
              onCreate={(request) => saveChange(() => createCareTask(selectedStay.id, request), 'Care task added.')} />
          ) : (
            <Alert severity="info">This stay is read-only. Tasks can only be changed for checked-in stays with a check-in time.</Alert>
          )}

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
            <Typography variant="h6" component="h2">Care tasks</Typography>
            <TextField select size="small" label="Show tasks" value={filter}
              onChange={(event) => setFilter(event.target.value)} sx={{ minWidth: 180 }}>
              <MenuItem value="all">All tasks</MenuItem>
              <MenuItem value="pending">Unfinished</MenuItem>
              <MenuItem value="completed">Completed</MenuItem>
            </TextField>
          </Stack>

          {!loadingTasks && !error && visibleTasks.length === 0 && (
            <Typography color="text.secondary">
              {tasks.length === 0 ? 'No care tasks for this stay.' : 'No tasks match this filter.'}
            </Typography>
          )}
          <Stack component="ul" spacing={2} sx={{ listStyle: 'none', m: 0, p: 0 }}>
            {visibleTasks.map((task) => (
              <Paper component="li" variant="outlined" key={task.id} sx={{ p: 2 }}>
                <Stack spacing={1}>
                  <Typography sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{task.description}</Typography>
                  <Typography variant="body2" color="text.secondary">Scheduled: {formatTime(task.scheduledAt)}</Typography>
                  {task.completedAt && <Typography variant="body2" color="text.secondary">Completed: {formatTime(task.completedAt)}</Typography>}
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap' }}>
                    <FormControlLabel label={task.isCompleted ? 'Completed' : 'Unfinished'} control={
                      <Checkbox checked={task.isCompleted} disabled={!canEdit || disabled}
                        slotProps={{ input: { 'aria-label': `Completed: ${task.description}` } }}
                        onChange={(_event, checked) => {
                          void saveChange(() => updateCareCompletion(task.id, checked), 'Care task updated.')
                        }} />
                    } />
                    <Button color="error" disabled={!canEdit || disabled || task.isCompleted}
                      onClick={() => setDeleteTarget(task)}>Delete</Button>
                  </Stack>
                </Stack>
              </Paper>
            ))}
          </Stack>
        </>
      )}

      <Dialog open={deleteTarget !== null} onClose={() => { if (!busy) setDeleteTarget(null) }}
        aria-labelledby="delete-care-title" aria-describedby="delete-care-description" fullWidth maxWidth="xs">
        <DialogTitle id="delete-care-title">Delete care task?</DialogTitle>
        <DialogContent>
          <DialogContentText id="delete-care-description" sx={{ overflowWrap: 'anywhere' }}>
            {deleteTarget?.description}
          </DialogContentText>
          <Typography variant="body2" sx={{ mt: 2 }}>This action cannot be undone.</Typography>
        </DialogContent>
        <DialogActions>
          <Button disabled={busy} onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="error" disabled={busy} onClick={() => {
            if (deleteTarget) void saveChange(() => deleteCareTask(deleteTarget.id), 'Care task deleted.')
          }}>Delete</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

export default DailyCare

