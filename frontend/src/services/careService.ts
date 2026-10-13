import { apiRequest } from './apiClient'
import type { CareStay, CareTask, CreateCareTask } from '../types/care'

export function getCareStays(signal?: AbortSignal) {
  return apiRequest<CareStay[]>('/care/stays', { signal })
}

export function getCareTasks(stayId: number, signal?: AbortSignal) {
  return apiRequest<CareTask[]>(`/care/stays/${stayId}/tasks`, { signal })
}

export function createCareTask(stayId: number, request: CreateCareTask) {
  return apiRequest<CareTask>(`/care/stays/${stayId}/tasks`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function updateCareCompletion(taskId: number, isCompleted: boolean) {
  return apiRequest<CareTask>(`/care/tasks/${taskId}/completion`, {
    method: 'PATCH',
    body: JSON.stringify({ isCompleted }),
  })
}

export function deleteCareTask(taskId: number) {
  return apiRequest<void>(`/care/tasks/${taskId}`, { method: 'DELETE' })
}
