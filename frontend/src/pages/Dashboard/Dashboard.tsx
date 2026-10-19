import { useEffect, useState } from 'react'
import { Alert, Box, Button, LinearProgress, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material'
import { getDashboardSummary, type DashboardSummary, type DashboardRoom } from '../../services/dashboardService'
import { getErrorMessage } from '../../services/apiErrors'

// Show current occupancy, not future room bookings.
function roomStatus(room: DashboardRoom) {
  return room.petName !== null ? 'Occupied' : room.isActive ? 'Available' : 'Inactive'
}

// Display the statistics and room list returned by the backend.
function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refresh, setRefresh] = useState(0)

  // Clear the old summary and request the latest figures.
  function refreshSummary() {
    setLoading(true)
    setError('')
    setSummary(null)
    setRefresh(value => value + 1)
  }

  // Load the summary and cancel the request if the user leaves the page.
  useEffect(() => {
    const controller = new AbortController()
    getDashboardSummary(controller.signal)
      .then(data => { if (!controller.signal.aborted) setSummary(data) })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) {
          setError(getErrorMessage(reason))
        }
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [refresh])

  return (
    <Stack spacing={3}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }} spacing={2}>
        <Typography variant="h4" component="h1">Dashboard</Typography>
        <Button variant="outlined" disabled={loading} onClick={refreshSummary}>
          Refresh
        </Button>
      </Stack>
      {loading && <Stack spacing={1} role="status">
        <Typography>Loading dashboard...</Typography>
        <LinearProgress aria-label="Loading dashboard" />
      </Stack>}
      {error && <Alert severity="error" action={
        <Button color="inherit" onClick={refreshSummary}>Retry</Button>
      }>{error}</Alert>}
      {summary && <>
        <Typography color="text.secondary">{summary.date} · Australia/Sydney · Actual arrivals and departures</Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' }, gap: 2 }}>
          {[
            { label: 'Pets staying now', value: summary.currentPets },
            { label: 'Room occupancy', value: `${summary.occupancyPercent}%` },
            { label: 'Checked in today', value: summary.todayCheckIns },
            { label: 'Checked out today', value: summary.todayCheckOuts },
          ].map(card => <Paper key={card.label} variant="outlined" sx={{ p: 3 }}>
            <Typography component="h2" color="text.secondary">{card.label}</Typography>
            <Typography variant="h4" component="p" sx={{ mt: 1 }}>{card.value}</Typography>
          </Paper>)}
        </Box>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' }, gap: 2 }}>
          <Paper variant="outlined" sx={{ p: 3 }}>
            <Stack spacing={2}>
              <Typography variant="h6" component="h2">Active rooms</Typography>
              <Typography>{summary.occupiedRooms} occupied / {summary.activeRooms} active</Typography>
              <LinearProgress variant="determinate" value={summary.occupancyPercent} aria-label="Active room occupancy" sx={{ height: 12 }} />
              {summary.activeRooms === 0 && <Typography color="text.secondary">No active rooms available.</Typography>}
            </Stack>
          </Paper>
          <Paper variant="outlined" sx={{ p: 3 }}>
            <Stack spacing={2}>
              <Typography variant="h6" component="h2">Today's care tasks</Typography>
              <Typography>{summary.completedCareTasks} completed / {summary.careTasks} scheduled</Typography>
              <LinearProgress variant="determinate" value={summary.careCompletionPercent} aria-label="Today's care completion" sx={{ height: 12 }} />
              <Typography color="text.secondary">{summary.careTasks === 0 ? 'No care tasks scheduled today.' : `${summary.pendingCareTasks} pending · ${summary.careCompletionPercent}% complete`}</Typography>
            </Stack>
          </Paper>
        </Box>
        <Stack spacing={2}>
          <Typography variant="h6" component="h2">Rooms</Typography>
          <Typography color="text.secondary">Current occupancy. Future bookings are not shown here.</Typography>
          <TableContainer component={Paper} variant="outlined" sx={{ display: { xs: 'none', sm: 'block' } }}>
            <Table aria-label="Room occupancy" sx={{ tableLayout: 'fixed' }}>
              <TableHead><TableRow>
                {['Room', 'Type', 'Status', 'Pet'].map(label => <TableCell key={label}>{label}</TableCell>)}
              </TableRow></TableHead>
              <TableBody>
                {summary.rooms.map(room => <TableRow key={room.id}>
                  <TableCell component="th" scope="row">{room.roomNumber}</TableCell>
                  <TableCell>{room.roomType}</TableCell>
                  <TableCell>{roomStatus(room)}</TableCell>
                  <TableCell sx={{ overflowWrap: 'anywhere' }}>{room.petName ?? '—'}</TableCell>
                </TableRow>)}
              </TableBody>
            </Table>
          </TableContainer>
          <Stack spacing={1} sx={{ display: { xs: 'flex', sm: 'none' } }}>
            {summary.rooms.map(room => <Paper key={room.id} variant="outlined" sx={{ p: 2 }}>
              <Typography component="h3">Room {room.roomNumber} · {room.roomType}</Typography>
              <Typography>{roomStatus(room)}</Typography>
              <Typography sx={{ overflowWrap: 'anywhere' }}>Pet: {room.petName ?? '—'}</Typography>
            </Paper>)}
          </Stack>
          {summary.rooms.length === 0 && <Typography>No rooms found.</Typography>}
        </Stack>
      </>}
    </Stack>
  )
}

export default Dashboard
