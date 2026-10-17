import { apiRequest } from './apiClient'

export interface DashboardSummary {
  date: string
  currentPets: number
  activeRooms: number
  occupiedRooms: number
  occupancyPercent: number
  todayCheckIns: number
  todayCheckOuts: number
  careTasks: number
  completedCareTasks: number
  pendingCareTasks: number
  careCompletionPercent: number
  rooms: DashboardRoom[]
}

export interface DashboardRoom {
  id: number
  roomNumber: string
  roomType: string
  isActive: boolean
  petName: string | null
}

export function getDashboardSummary(signal?: AbortSignal): Promise<DashboardSummary> {
  return apiRequest<DashboardSummary>('dashboard', { signal })
}
