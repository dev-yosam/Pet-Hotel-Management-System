import { Temporal } from '@js-temporal/polyfill'

export const bookingTimeZone = 'Australia/Sydney'

export function sydneyTimeToUtc(value: string): string {
  try {
    return Temporal.PlainDateTime.from(value)
      .toZonedDateTime(bookingTimeZone, {
        // Ask for another time if daylight saving makes this time missing or repeated.
        disambiguation: 'reject',
      })
      .toInstant()
      .toString()
  } catch {
    throw new Error(
      'Enter a valid Sydney date and time. Avoid the repeated or skipped hour during daylight saving changes.',
    )
  }
}

export function formatSydneyTime(value: string): string {
  return new Intl.DateTimeFormat('en-AU', {
    timeZone: bookingTimeZone,
    dateStyle: 'short',
    timeStyle: 'short',
    hour12: false,
  }).format(new Date(value))
}
