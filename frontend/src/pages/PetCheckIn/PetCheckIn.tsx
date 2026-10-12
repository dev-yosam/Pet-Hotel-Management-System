import { Paper, Typography } from '@mui/material'

function PetCheckIn() {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>Pet Check-in</Typography>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Pet registration, bookings and check-in will be available here.
        </Typography>
      </Paper>
    </>
  )
}

export default PetCheckIn
