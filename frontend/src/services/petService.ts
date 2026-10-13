import { apiRequest } from './apiClient'

export interface PetRequest {
  ownerId: number
  name: string
  species: string
  breed: string | null
  careInstructions: string | null
}

export interface PetResponse extends PetRequest {
  id: number
}

export function getPets(ownerId?: number): Promise<PetResponse[]> {
  const path = ownerId === undefined
    ? 'pets'
    : `pets?ownerId=${ownerId}`

  return apiRequest<PetResponse[]>(path)
}

export function getPet(id: number): Promise<PetResponse> {
  return apiRequest<PetResponse>(`pets/${id}`)
}

export function createPet(
  request: PetRequest,
): Promise<PetResponse> {
  return apiRequest<PetResponse>('pets', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function updatePet(
  id: number,
  request: PetRequest,
): Promise<PetResponse> {
  return apiRequest<PetResponse>(`pets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}