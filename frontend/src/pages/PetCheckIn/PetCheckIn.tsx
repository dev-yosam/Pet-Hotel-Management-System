import { useState } from 'react'
import { Alert, Stack, Typography } from '@mui/material'
import type { OwnerResponse } from '../../services/ownerService'
import type { PetResponse } from '../../services/petService'
import OwnerInformation from './OwnerInformation'
import PetRegistration from './PetRegistration'
import RoomBooking from './RoomBooking'

function PetCheckIn() {
  const [owner, setOwner] = useState<OwnerResponse | null>(null)
  const [pet, setPet] = useState<PetResponse | null>(null)

  function handleOwnerChange(nextOwner: OwnerResponse | null) {
    if (nextOwner?.id !== owner?.id) {
      setPet(null)
    }

    setOwner(nextOwner)
  }

  return (
    <Stack spacing={3}>
      <Typography variant="h4" component="h1">
        Pet Check-in
      </Typography>

      <OwnerInformation onOwnerChange={handleOwnerChange} />

      {owner ? (
        <PetRegistration
          key={owner.id}
          owner={owner}
          onPetChange={setPet}
        />
      ) : (
        <Alert severity="info">
          Select or create an owner to register their pets.
        </Alert>
      )}

      {pet && (
        <RoomBooking key={pet.id} pet={pet} />
      )}
    </Stack>
  )
}

export default PetCheckIn