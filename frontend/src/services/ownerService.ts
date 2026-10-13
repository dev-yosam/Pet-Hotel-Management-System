import { apiRequest } from './apiClient'

export interface OwnerRequest {
  fullName: string
  phone: string
  email: string | null
}

export interface OwnerResponse extends OwnerRequest {
  id: number
}

export function getOwners(): Promise<OwnerResponse[]> {
  return apiRequest<OwnerResponse[]>('owners')
}

export function getOwner(id: number): Promise<OwnerResponse> {
  return apiRequest<OwnerResponse>(`owners/${id}`)
}

export function createOwner(
  request: OwnerRequest,
): Promise<OwnerResponse> {
  return apiRequest<OwnerResponse>('owners', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function updateOwner(
  id: number,
  request: OwnerRequest,
): Promise<OwnerResponse> {
  return apiRequest<OwnerResponse>(`owners/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}