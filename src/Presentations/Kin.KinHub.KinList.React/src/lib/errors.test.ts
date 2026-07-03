import { describe, expect, it } from 'vitest'
import { getApiErrorMessage } from '@/lib/errors'

describe('getApiErrorMessage', () => {
  it('returns the first message from ProblemDetails string-array errors', () => {
    const error = {
      response: {
        data: {
          errors: ['Audio payload cannot be empty.'],
        },
      },
    }

    expect(getApiErrorMessage(error)).toBe('Audio payload cannot be empty.')
  })

  it('falls back to ProblemDetails detail when message is absent', () => {
    const error = {
      response: {
        data: {
          detail: 'Audio draft processing is not available in this environment.',
        },
      },
    }

    expect(getApiErrorMessage(error)).toBe('Audio draft processing is not available in this environment.')
  })
})
