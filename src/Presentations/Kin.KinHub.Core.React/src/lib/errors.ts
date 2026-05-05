export interface ApiErrorShape {
  message?: string
  fields?: Record<string, string[]>
}

export function extractApiError(err: unknown): ApiErrorShape {
  const axiosErr = err as {
    response?: { data?: { message?: string; errors?: Record<string, string[]> } }
  }
  const data = axiosErr?.response?.data

  if (data?.errors) {
    return { fields: data.errors }
  }
  if (data?.message) {
    return { message: data.message }
  }
  return {}
}

export function getApiErrorMessage(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  const { message, fields } = extractApiError(err)
  if (message) return message
  if (fields) {
    const firstKey = Object.keys(fields)[0]
    return fields[firstKey]?.[0] ?? fallback
  }
  return fallback
}

export function isHttpStatus(err: unknown, status: number): boolean {
  const axiosErr = err as { response?: { status?: number } }
  return axiosErr?.response?.status === status
}
