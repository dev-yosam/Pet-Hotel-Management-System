import { useEffect, useState } from 'react'
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
import { getErrorMessage } from '../../services/apiErrors'
import {
  createOwner,
  getOwners,
  updateOwner,
} from '../../services/ownerService'
import type { OwnerResponse } from '../../services/ownerService'


interface OwnerInformationProps {
  onOwnerChange: (owner: OwnerResponse | null) => void
}

function OwnerInformation({ onOwnerChange }: OwnerInformationProps) {
  const [owners, setOwners] = useState<OwnerResponse[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [fullName, setFullName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [refresh, setRefresh] = useState(0)
  const [fieldErrors, setFieldErrors] = useState({ fullName: '', phone: '' })

  useEffect(() => {
    let active = true

    getOwners()
      .then(data => {
        if (active) {
          setOwners(data)
        }
      })
      .catch(error => {
        if (active) {
          setError(getErrorMessage(error))
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [refresh])

  function selectOwner(value: string) {
    const owner = owners.find(item => String(item.id) === value)

    setSelectedId(value)
    onOwnerChange(owner ?? null)
    setFullName(owner?.fullName ?? '')
    setPhone(owner?.phone ?? '')
    setEmail(owner?.email ?? '')
    setError('')
    setSuccess('')
    setFieldErrors({ fullName: '', phone: '' })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSuccess('')

    const name = fullName.trim()
    const phoneNumber = phone.trim()
    const errors = {
      fullName: !name || name.length > 100 || !/^(?=.*\p{L})[\p{L}\p{M} .'’-]+$/u.test(name)
        ? 'Enter a name using letters, spaces, periods, apostrophes or hyphens. Numbers are not allowed.' : '',
      phone: !phoneNumber || phoneNumber.length > 30 || !/^(?=.*[0-9])\+?[0-9 ()-]+$/.test(phoneNumber)
        ? 'Enter a phone number using digits, a leading +, spaces, parentheses or hyphens.' : '',
    }
    setFieldErrors(errors)
    if (errors.fullName || errors.phone) return

    setSaving(true)

    try {
      const request = {
        fullName: fullName.trim(),
        phone: phone.trim(),
        email: email.trim() || null,
      }

      const saved = selectedId
        ? await updateOwner(Number(selectedId), request)
        : await createOwner(request)

      setOwners(current => [
        ...current.filter(owner => owner.id !== saved.id),
        saved,
      ])
      setSelectedId(String(saved.id))
      onOwnerChange(saved)
      setFullName(saved.fullName)
      setPhone(saved.phone)
      setEmail(saved.email ?? '')
      setSuccess(selectedId ? 'Owner updated.' : 'Owner created.')
    } catch (error) {
      setError(getErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  const disabled = loading || saving

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack component="form" noValidate spacing={2} onSubmit={handleSubmit}>
        <Typography variant="h6" component="h2">
          Owner Information
        </Typography>

        {error && <Alert severity="error" action={<Button color="inherit" disabled={disabled} onClick={() => {
          setError('')
          setLoading(true)
          setRefresh(value => value + 1)
        }}>Retry</Button>}>{error}</Alert>}
        {success && <Alert severity="success">{success}</Alert>}

        <TextField
          select
          label="Owner"
          value={selectedId}
          onChange={event => selectOwner(event.target.value)}
          disabled={disabled}
          helperText={loading
            ? 'Loading owners…'
            : 'Select an owner to edit, or choose Add new owner.'}
          fullWidth
        >
          <MenuItem value="">Add new owner</MenuItem>
          {owners.map(owner => (
            <MenuItem key={owner.id} value={String(owner.id)}>
              {owner.fullName} — {owner.phone}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          label="Full name"
          error={Boolean(fieldErrors.fullName)}
          helperText={fieldErrors.fullName}
          value={fullName}
          onChange={event => setFullName(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 100 } }}
          autoComplete="name"
          disabled={disabled}
          required
          fullWidth
        />

        <TextField
          label="Phone"
          error={Boolean(fieldErrors.phone)}
          helperText={fieldErrors.phone}
          type="tel"
          value={phone}
          onChange={event => setPhone(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 30 } }}
          autoComplete="tel"
          disabled={disabled}
          required
          fullWidth
        />

        <TextField
          label="Email"
          type="email"
          value={email}
          onChange={event => setEmail(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 254 } }}
          autoComplete="email"
          disabled={disabled}
          fullWidth
        />

        <Button
          type="submit"
          variant="contained"
          disabled={disabled}
          sx={{ alignSelf: { xs: 'stretch', sm: 'flex-start' } }}
        >
          {saving
            ? 'Saving…'
            : selectedId ? 'Save owner changes' : 'Create owner'}
        </Button>
      </Stack>
    </Paper>
  )
}

export default OwnerInformation
