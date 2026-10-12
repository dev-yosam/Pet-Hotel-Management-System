const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '')

export class ApiError extends Error {
  readonly status: number
  readonly details: unknown

  constructor(message: string, status: number, details: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }
  if (typeof options.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${apiBaseUrl}/${path.replace(/^\//, '')}`, {
    ...options,
    headers,
  })

  const text = await response.text()
  const isJson = response.headers.get('Content-Type')?.includes('json')
  let data: unknown = text || undefined
  if (text && isJson) {
    try {
      data = JSON.parse(text)
    } catch {
      throw new ApiError('The server returned an invalid response.', response.status, text)
    }
  }

  if (!response.ok) {
    let message = `Request failed (${response.status}).`
    if (data && typeof data === 'object') {
      if ('detail' in data && typeof data.detail === 'string' && data.detail) {
        message = data.detail
      } else if ('title' in data && typeof data.title === 'string' && data.title) {
        message = data.title
      }
    }
    throw new ApiError(message, response.status, data)
  }

  return data as T
}
