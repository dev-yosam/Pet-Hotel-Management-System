import { Paper, Typography } from '@mui/material'

function StayHistory() {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>Stay History</Typography>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Past stays and searches by pet, owner, date and status will be available here.
        </Typography>
      </Paper>
    </>
  )
}

export default StayHistory
