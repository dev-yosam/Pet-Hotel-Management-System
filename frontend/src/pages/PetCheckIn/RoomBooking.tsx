import DateInput from '../../components/DateInput'
import { useState } from 'react'
import StayActions from '../../components/StayActions'
import { Link } from 'react-router'
import type { FormEvent } from 'react'
import {
  Alert,
  Button,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { ApiError } from '../../services/apiClient'
import { getErrorMessage } from '../../services/apiErrors'
import { createBooking } from '../../services/bookingService'
import type { BookingResponse } from '../../services/bookingService'
import {
  formatSydneyTime,
  sydneyTimeToUtc,
} from '../../services/bookingTime'
import type { PetResponse } from '../../services/petService'
import { getAvailableRooms } from '../../services/roomService'
import type { RoomResponse } from '../../services/roomService'

interface RoomBookingProps {
  pet: PetResponse
}

function RoomBooking({ pet }: RoomBookingProps) {
  const [checkIn, setCheckIn] = useState('')
  const [checkOut, setCheckOut] = useState('')
  const [rooms, setRooms] = useState<RoomResponse[]>([])
  const [roomId, setRoomId] = useState('')
  const [searched, setSearched] = useState(false)
  const [searching, setSearching] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [booking, setBooking] = useState<BookingResponse | null>(null)

  const busy = searching || saving

  function changeDates(value: string, field: 'checkIn' | 'checkOut') {
    if (field === 'checkIn') {
      setCheckIn(value)
    } else {
      setCheckOut(value)
    }

    setRooms([])
    setRoomId('')
    setSearched(false)
    setError('')
    setBooking(null)
  }

  function getDates() {
    if (!checkIn || !checkOut) {
      throw new Error('Enter both check-in and check-out times.')
    }

    const start = sydneyTimeToUtc(checkIn)
    const end = sydneyTimeToUtc(checkOut)

    if (new Date(end).getTime() <= new Date(start).getTime()) {
      throw new Error('Check-out must be after check-in.')
    }
    if (new Date(end).getTime() <= Date.now()) {
      throw new Error('Check-out must be in the future.')
    }

    return { start, end }
  }

  function showError(error: unknown) {
    setError(
      error instanceof Error && !(error instanceof ApiError)
        ? error.message
        : getErrorMessage(error),
    )
  }

  async function searchRooms() {
    setError('')
    setBooking(null)
    setRooms([])
    setRoomId('')
    setSearched(false)
    setSearching(true)

    try {
      const { start, end } = getDates()
      const available = await getAvailableRooms(start, end)

      setRooms(available)
      setSearched(true)
    } catch (error) {
      showError(error)
    } finally {
      setSearching(false)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')

    if (!searched || !roomId) {
      setError('Search for available rooms and select a room.')
      return
    }

    setSaving(true)

    try {
      const { start, end } = getDates()
      const saved = await createBooking({
        petId: pet.id,
        roomId: Number(roomId),
        scheduledCheckIn: start,
        scheduledCheckOut: end,
      })

      setBooking(saved)
      setRooms([])
      setRoomId('')
      setSearched(false)
    } catch (error) {
      showError(error)

      if (error instanceof ApiError && error.status === 409) {
        setRooms([])
        setRoomId('')
        setSearched(false)
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack component="form" noValidate spacing={2} onSubmit={handleSubmit}>
        <Typography variant="h6" component="h2">
          Room Booking
        </Typography>

        <Typography>
          Pet: {pet.name} — {pet.species} (ID: {pet.id})
        </Typography>

        <Typography color="text.secondary">
          All times use Sydney time (Australia/Sydney).
        </Typography>

        {error && <Alert severity="error">{error}</Alert>}

        {booking && (
          <Alert severity="success">
            Stay #{booking.id}. Status: {booking.status}.
            {' '}Check-in: {formatSydneyTime(booking.scheduledCheckIn)}.
            {' '}Check-out: {formatSydneyTime(booking.scheduledCheckOut)}.
            {' '}Sydney time.
          </Alert>
        )}

        {booking && <StayActions key={booking.id} stayId={booking.id} status={booking.status} onChanged={setBooking} />}
        <Button component={Link} to="/stay-history" sx={{ alignSelf: 'flex-start' }}>Find an existing booking or stay</Button>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <DateInput
            label="Check-in (Sydney time)"
            withTime
            value={checkIn}
            onChange={value => changeDates(value, 'checkIn')}
            disabled={busy}
            required
          />

          <DateInput
            label="Check-out (Sydney time)"
            withTime
            value={checkOut}
            onChange={value => changeDates(value, 'checkOut')}
            disabled={busy}
            required
          />
        </Stack>

        <Button
          type="button"
          variant="outlined"
          onClick={searchRooms}
          disabled={busy}
          sx={{ alignSelf: { xs: 'stretch', sm: 'flex-start' } }}
        >
          {searching ? 'Searching…' : 'Find available rooms'}
        </Button>

        {searched && rooms.length === 0 && (
          <Alert severity="info">
            No rooms are available for these dates.
          </Alert>
        )}

        {searched && rooms.length > 0 && (
          <TextField
            select
            label="Available room"
            value={roomId}
            onChange={event => setRoomId(event.target.value)}
            disabled={busy}
            required
          >
            {rooms.map(room => (
              <MenuItem key={room.id} value={String(room.id)}>
                {room.roomNumber} — {room.roomType}
              </MenuItem>
            ))}
          </TextField>
        )}

        <Button
          type="submit"
          variant="contained"
          disabled={busy || !searched || !roomId}
          sx={{ alignSelf: { xs: 'stretch', sm: 'flex-start' } }}
        >
          {saving ? 'Booking…' : 'Create booking'}
        </Button>
      </Stack>
    </Paper>
  )
}

export default RoomBooking
