import { apiRequest } from './apiClient'

export interface StayResponse {
  id: number
  petId: number
  roomId: number
  scheduledCheckIn: string
  scheduledCheckOut: string
  actualCheckIn: string | null
  actualCheckOut: string | null
  status: 'Booked' | 'CheckedIn' | 'CheckedOut' | 'Cancelled'
  totalCharge: number | null
}

export function changeStayStatus(id: number, action: 'check-in' | 'check-out') {
  return apiRequest<StayResponse>(`stays/${id}/${action}`, { method: 'POST' })
}
