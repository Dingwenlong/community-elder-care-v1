export type DemoRole =
  | 'Elder'
  | 'Family'
  | 'CommunityStaff'
  | 'ServiceWorker'
  | 'Administrator'

export type AppShell = 'elder' | 'family' | 'community' | 'service' | 'admin'

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  role: DemoRole
  shell: AppShell
  isDemoMode: true
}

export interface ServerLoginResponse {
  accessToken: string
  expiresAt: string
  role: DemoRole
  shell: string
  isDemoMode: true
}

export interface ElderSummary {
  id: string
  demoDisplayName: string
  areaCode: string
  attentionLevel: 'Routine' | 'Priority' | 'HighAttention'
  nextCheckInDueAt: string
  isDemoData: true
  latestStatus?: string
  nextVisit?: string
  currentOpenEvent?: string
}

export interface LabelValue {
  code: string
  demoLabel: string
}

export interface EmergencyContact {
  demoName: string
  relationship: string
  phoneNumber: string
  contactOrder: number
}

export interface ElderDetail {
  id: string
  demoDisplayName: string
  birthDate?: string
  areaCode?: string
  attentionLevel?: ElderSummary['attentionLevel']
  nextCheckInDueAt?: string
  isDemoData: true
  recentStatus?: { state: string; latestCheckInAt: string | null }
  careEventSummary?: { activeCount: number }
  visitSummary?: { latestVisitAt: string | null }
  reminderCompletion?: { completedToday: number; totalToday: number }
  healthRisks?: LabelValue[]
  serviceNeeds?: LabelValue[]
  emergencyContacts?: EmergencyContact[]
}
