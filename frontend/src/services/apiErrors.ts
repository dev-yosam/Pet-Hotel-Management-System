import { ApiError } from './apiClient'

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const details = error.details

    if (
      details &&
      typeof details === 'object' &&
      'errors' in details &&
      details.errors &&
      typeof details.errors === 'object'
    ) {
      const messages = Object.values(details.errors)
        .flatMap(value => Array.isArray(value) ? value : [])
        .filter((value): value is string => typeof value === 'string')

      if (messages.length > 0) {
        return messages.join(' ')
      }
    }

    return error.message
  }

  return 'Unable to connect to the server. Please try again.'
}