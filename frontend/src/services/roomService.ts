import { apiRequest } from './apiClient'

export interface RoomResponse {
  id: number
  roomNumber: string
  roomType: string
  isActive: boolean
}

export function getRooms(): Promise<RoomResponse[]> {
  return apiRequest<RoomResponse[]>('rooms')
}

export function getAvailableRooms(
  checkIn: string,
  checkOut: string,
): Promise<RoomResponse[]> {
  const query = new URLSearchParams({
    checkIn,
    checkOut,
  })

  return apiRequest<RoomResponse[]>(`rooms/available?${query}`)
}
