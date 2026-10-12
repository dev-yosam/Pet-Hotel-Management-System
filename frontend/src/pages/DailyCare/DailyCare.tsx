import { Paper, Typography } from '@mui/material'

function DailyCare() {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>Daily Care</Typography>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Care tasks for pets staying at the hotel will be available here.
        </Typography>
      </Paper>
    </>
  )
}

export default DailyCare
