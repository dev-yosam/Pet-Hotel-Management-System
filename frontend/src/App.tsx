import { lazy, Suspense } from 'react'
import { Button, Stack, Typography } from '@mui/material'
import { Link, Navigate, Route, Routes } from 'react-router'
import AppLayout from './components/AppLayout'
const Dashboard = lazy(() => import('./pages/Dashboard/Dashboard'))
const PetCheckIn = lazy(() => import('./pages/PetCheckIn/PetCheckIn'))
const StayHistory = lazy(() => import('./pages/StayHistory/StayHistory'))

const DailyCare = lazy(() => import('./pages/DailyCare/DailyCare'))

function App() {
  return (
    <Suspense fallback={<Typography role="status" sx={{ p: 3 }}>Loading page...</Typography>}>
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
    </Suspense>
  )
}

export default App
