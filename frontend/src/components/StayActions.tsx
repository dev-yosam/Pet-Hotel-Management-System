import { useState } from 'react'
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material'
import { changeStayStatus, type StayResponse } from '../services/stayService'
import { getErrorMessage } from '../services/apiErrors'

interface Props {
  stayId: number
  status: string
  onChanged: (stay: StayResponse) => void
}

export default function StayActions({ stayId, status, onChanged }: Props) {
  const [confirming, setConfirming] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  if (status !== 'Booked' && status !== 'CheckedIn') return null
  const checkingIn = status === 'Booked'
  const label = checkingIn ? 'Check in' : 'Check out'

  async function confirm() {
    if (busy) return
    setBusy(true)
    setError('')
    try {
      const stay = await changeStayStatus(stayId, checkingIn ? 'check-in' : 'check-out')
      setConfirming(false)
      onChanged(stay)
    } catch (reason) {
      setError(getErrorMessage(reason))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <Button type="button" variant="contained" sx={{ alignSelf: 'flex-start' }} onClick={() => { setError(''); setConfirming(true) }}>{label}</Button>
      <Dialog open={confirming} onClose={() => { if (!busy) setConfirming(false) }} aria-labelledby={`stay-action-${stayId}`}>
        <DialogTitle id={`stay-action-${stayId}`}>{label} stay #{stayId}?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            {checkingIn
              ? 'Check-in is allowed during the booked period. The room must be available.'
              : 'This will end the stay and calculate the charge from the booked dates. Care tasks will become read-only.'}
          </DialogContentText>
          {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button disabled={busy} onClick={() => setConfirming(false)}>Cancel</Button>
          <Button variant="contained" disabled={busy} onClick={confirm}>{busy ? 'Saving...' : label}</Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
