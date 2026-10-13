import { lazy, Suspense } from 'react'
import { Button, Stack, Typography } from '@mui/material'
import { Link, Navigate, Route, Routes } from 'react-router'
import AppLayout from './components/AppLayout'
import Dashboard from './pages/Dashboard/Dashboard'
import PetCheckIn from './pages/PetCheckIn/PetCheckIn'
import StayHistory from './pages/StayHistory/StayHistory'

const DailyCare = lazy(() => import('./pages/DailyCare/DailyCare'))

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="pet-check-in" element={<PetCheckIn />} />
        <Route path="daily-care" element={
          <Suspense fallback={<Typography role="status">Loading Daily Care...</Typography>}>
            <DailyCare />
          </Suspense>
        } />
        <Route path="stay-history" element={<StayHistory />} />
        <Route path="*" element={
          <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Typography variant="h4" component="h1">Page not found</Typography>
            <Typography>The page you requested does not exist.</Typography>
            <Button component={Link} to="/dashboard" variant="contained">
              Back to Dashboard
            </Button>
          </Stack>
        } />
      </Route>
    </Routes>
  )
}

export default App
