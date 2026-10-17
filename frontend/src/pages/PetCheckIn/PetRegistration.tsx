import { validatePet } from '../../services/inputValidation'
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
import type { OwnerResponse } from '../../services/ownerService'
import {
  createPet,
  getPets,
  updatePet,
} from '../../services/petService'
import type { PetResponse } from '../../services/petService'

interface PetRegistrationProps {
  owner: OwnerResponse
  onPetChange: (pet: PetResponse | null) => void
}

function PetRegistration({
  owner,
  onPetChange,
}: PetRegistrationProps) {
  const [pets, setPets] = useState<PetResponse[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [name, setName] = useState('')
  const [species, setSpecies] = useState('')
  const [breed, setBreed] = useState('')
  const [careInstructions, setCareInstructions] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [loadFailed, setLoadFailed] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [refresh, setRefresh] = useState(0)
  const [fieldErrors, setFieldErrors] = useState({ name: '', species: '', breed: '', careInstructions: '' })

  useEffect(() => {
    let active = true

    getPets(owner.id)
      .then(data => {
        if (active) {
          setPets(data)
        }
      })
      .catch(error => {
        if (active) {
          setLoadFailed(true)
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
  }, [owner.id, refresh])

  function selectPet(value: string) {
    const pet = pets.find(item => String(item.id) === value)

    setSelectedId(value)
    onPetChange(pet ?? null)
    setName(pet?.name ?? '')
    setSpecies(pet?.species ?? '')
    setBreed(pet?.breed ?? '')
    setCareInstructions(pet?.careInstructions ?? '')
    setError('')
    setSuccess('')
    setFieldErrors({ name: '', species: '', breed: '', careInstructions: '' })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSuccess('')
    setFieldErrors({ name: '', species: '', breed: '', careInstructions: '' })

    const errors = validatePet(name.trim(), species.trim(), breed.trim(), careInstructions.trim())
    setFieldErrors(errors)
    if (Object.values(errors).some(Boolean)) return

    setSaving(true)

    try {
      const request = {
        ownerId: owner.id,
        name: name.trim(),
        species: species.trim(),
        breed: breed.trim() || null,
        careInstructions: careInstructions.trim() || null,
      }

      const saved = selectedId
        ? await updatePet(Number(selectedId), request)
        : await createPet(request)

      setPets(current => [
        ...current.filter(pet => pet.id !== saved.id),
        saved,
      ])
      setSelectedId(String(saved.id))
      onPetChange(saved)
      setName(saved.name)
      setSpecies(saved.species)
      setBreed(saved.breed ?? '')
      setCareInstructions(saved.careInstructions ?? '')
      setSuccess(selectedId ? 'Pet updated.' : 'Pet registered.')
    } catch (error) {
      setError(getErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  const disabled = loading || saving || loadFailed

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack component="form" noValidate spacing={2} onSubmit={handleSubmit}>
        <Typography variant="h6" component="h2">
          Pet Registration
        </Typography>

        <Typography color="text.secondary">
          Owner: {owner.fullName} — {owner.phone}
        </Typography>

        {error && <Alert severity="error" action={loadFailed ? <Button color="inherit" disabled={loading} onClick={() => {
          setError('')
          setLoadFailed(false)
          setLoading(true)
          setRefresh(value => value + 1)
        }}>Retry</Button> : undefined}>{error}</Alert>}
        {success && <Alert severity="success">{success}</Alert>}

        <TextField
          select
          label="Pet"
          value={selectedId}
          onChange={event => selectPet(event.target.value)}
          disabled={disabled}
          helperText={loading
            ? 'Loading pets…'
            : 'Select a pet to edit, or choose Register new pet.'}
          fullWidth
        >
          <MenuItem value="">Register new pet</MenuItem>
          {pets.map(pet => (
            <MenuItem key={pet.id} value={String(pet.id)}>
              {pet.name} — {pet.species} (ID: {pet.id})
            </MenuItem>
          ))}
        </TextField>

        <TextField
          label="Pet name"
          error={Boolean(fieldErrors.name)}
          helperText={fieldErrors.name}
          value={name}
          onChange={event => setName(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 100 } }}
          disabled={disabled}
          required
          fullWidth
        />

        <TextField
          label="Species"
          error={Boolean(fieldErrors.species)}
          helperText={fieldErrors.species}
          value={species}
          onChange={event => setSpecies(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 50 } }}

          disabled={disabled}
          required
          fullWidth
        />

        <TextField
          label="Breed"
          error={Boolean(fieldErrors.breed)}
          helperText={fieldErrors.breed}
          value={breed}
          onChange={event => setBreed(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 100 } }}
          disabled={disabled}
          fullWidth
        />

        <TextField
          label="Care instructions"
          error={Boolean(fieldErrors.careInstructions)}
          helperText={fieldErrors.careInstructions}
          value={careInstructions}
          onChange={event => setCareInstructions(event.target.value)}
          slotProps={{ htmlInput: { maxLength: 1000 } }}
          disabled={disabled}
          multiline
          minRows={3}
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
            : selectedId ? 'Save pet changes' : 'Register pet'}
        </Button>
      </Stack>
    </Paper>
  )
}

export default PetRegistration
