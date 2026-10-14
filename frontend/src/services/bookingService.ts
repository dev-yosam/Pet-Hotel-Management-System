import { apiRequest } from './apiClient'

export interface BookingRequest {
  petId: number
  roomId: number
  scheduledCheckIn: string
  scheduledCheckOut: string
}

export interface BookingResponse {
  id: number
  petId: number
  roomId: number
  scheduledCheckIn: string
  scheduledCheckOut: string
  status: 'Booked' | 'CheckedIn' | 'CheckedOut' | 'Cancelled'
}

export function createBooking(
  request: BookingRequest,
): Promise<BookingResponse> {
  return apiRequest<BookingResponse>('bookings', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function getBooking(id: number): Promise<BookingResponse> {
  return apiRequest<BookingResponse>(`bookings/${id}`)
}