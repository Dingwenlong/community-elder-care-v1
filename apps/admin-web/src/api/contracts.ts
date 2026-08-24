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

export type CareEventLevel = 'GeneralService' | 'NeedsConfirmation' | 'Emergency'

export type CareEventStatus =
  | 'PendingConfirmation'
  | 'Accepted'
  | 'UnableToConfirm'
  | 'InProgress'
  | 'Resolved'
  | 'FollowUpPending'
  | 'Closed'
  | 'FalseAlarm'

export interface CareEventEvidence {
  id: string
  kind: string
  summary: string
  occurredAt: string
  recordedAt: string
  isSimulation: boolean
}

export interface CareEventTransition {
  id: string
  fromStatus: CareEventStatus
  toStatus: CareEventStatus
  actorKind: string
  actorUserId: string | null
  reason: string | null
  occurredAt: string
  isSimulation: boolean
}

export interface ContactAttempt {
  id: string
  kind: string
  targetLabel: string
  attemptedAt: string
  outcome: string
  isSimulation: boolean
}

export interface CareEvent {
  id: string
  elderId: string
  category: string
  level: CareEventLevel
  status: CareEventStatus
  source: string
  summary: string
  occurredAt: string
  createdAt: string
  lastActivityAt: string
  responsibilityQueue: string
  currentOwnerUserId: string | null
  resolution: string | null
  isDemoData: true
  isDuplicate: boolean
  evidence: CareEventEvidence[]
  transitions: CareEventTransition[]
  contactAttempts: ContactAttempt[]
  allowedTransitions: CareEventStatus[]
}

export type WorkStatus = 'Unassigned' | 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled'

export interface VisitItem {
  visitId: string
  careEventId: string
  elderDisplayName: string
  assignedStaffUserId: string
  scheduledStartAt: string
  scheduledEndAt: string
  startedAt: string | null
  completedAt: string | null
  confirmedSummary: string | null
  result: string | null
  status: WorkStatus
  isMandatory: boolean
  isDemoData: true
}

export interface ServiceOrderItem {
  orderId: string
  careEventId?: string
  elderDisplayName: string
  serviceType: string
  scheduledWindow: string
  contactInstruction: string
  status: WorkStatus
  result?: string | null
  isMandatory?: boolean
  isDemoData?: true
}

export interface FollowUpItem {
  followUpId: string
  careEventId: string
  elderDisplayName: string
  assignedStaffUserId: string
  dueAt: string
  completedAt: string | null
  result: string | null
  status: WorkStatus
  isDemoData: true
}

export type DeviceSignalType = 'SosButton' | 'NoWaterActivity' | 'DeviceOffline'

export interface DeviceSignalResponse {
  signalId: string
  careEventId: string
  receivedAt: string
  isDuplicate: boolean
  isSimulation: boolean
}
