import { Paper, Typography } from '@mui/material'

function Dashboard() {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>Dashboard</Typography>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Room occupancy, arrivals, departures and daily care summaries will be available here.
        </Typography>
      </Paper>
    </>
  )
}

export default Dashboard
