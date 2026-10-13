import { useState } from 'react'
import { Alert, Stack, Typography } from '@mui/material'
import type { OwnerResponse } from '../../services/ownerService'
import OwnerInformation from './OwnerInformation'
import PetRegistration from './PetRegistration'

function PetCheckIn() {
  const [owner, setOwner] = useState<OwnerResponse | null>(null)

  return (
    <Stack spacing={3}>
      <Typography variant="h4" component="h1">
        Pet Check-in
      </Typography>

      <OwnerInformation onOwnerChange={setOwner} />

      {owner ? (
        <PetRegistration key={owner.id} owner={owner} />
      ) : (
        <Alert severity="info">
          Select or create an owner to register their pets.
        </Alert>
      )}
    </Stack>
  )
}

export default PetCheckIn