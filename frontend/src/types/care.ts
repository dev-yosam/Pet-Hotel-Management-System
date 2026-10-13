export interface CareStay {
  id: number
  petName: string
  ownerName: string
  roomNumber: string
  status: 'Booked' | 'CheckedIn' | 'CheckedOut' | 'Cancelled'
  actualCheckIn: string | null
  scheduledCheckOut: string
  careInstructions: string | null
}

export interface CareTask {
  id: number
  stayId: number
  description: string
  scheduledAt: string
  isCompleted: boolean
  completedAt: string | null
}

export interface CreateCareTask {
  description: string
  scheduledAt: string
}
