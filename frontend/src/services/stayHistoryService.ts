import { apiRequest } from './apiClient'

export interface StayHistoryItem {
  id: number
  petName: string
  species: string
  ownerName: string
  ownerPhone: string
  roomNumber: string
  roomType: string
  status: string
  scheduledCheckIn: string
  scheduledCheckOut: string
  actualCheckIn: string | null
  actualCheckOut: string | null
  totalCharge: number | null
}

export interface StayHistoryFilters {
  pet: string
  owner: string
  from: string
  to: string
  status: string
  sort: string
}

export interface StayHistoryResult {
  items: StayHistoryItem[]
  totalCount: number
  page: number
  pageSize: number
}

export function searchStayHistory(filters: StayHistoryFilters, page: number, signal: AbortSignal) {
  const params = new URLSearchParams({ page: String(page), pageSize: '20' })
  Object.entries(filters).forEach(([key, value]) => {
    if (value.trim()) params.set(key, value.trim())
  })
  return apiRequest<StayHistoryResult>(`stay-history?${params}`, { signal })
}
