import type { DemoRole, WorkStatus } from './contracts'

export interface Personnel {
  userId: string
  displayName: string
  role: DemoRole
  areaCode: string | null
  pendingCount: number
  overdueCount: number
}
export interface OperationsTask {
  taskId: string
  taskType: 'Visit' | 'ServiceOrder' | 'FollowUp'
  careEventId: string
  elderId: string
  elderDisplayName: string
  assignedUserId: string
  assignedDisplayName: string
  areaCode: string
  status: WorkStatus
  dueAt: string | null
  completedAt: string | null
  isMandatory: boolean
  version: string
  eventOwnerUserId: string | null
  isOverdue: boolean
}
export interface TaskReassignment {
  id: string
  fromUserId: string
  toUserId: string
  actorUserId: string
  reason: string
  occurredAt: string
}
export interface ManagedDevice {
  deviceId: string
  displayName: string
  elderDisplayName: string
  areaCode: string
  isEnabled: boolean
  version: string
  lastHardwareSignalAt: string | null
  lastSimulationSignalAt: string | null
}
export interface SignalHistory {
  signalId: string
  careEventId: string
  signalType: string
  receivedAt: string
  deviceTime: string
  isSimulation: boolean
  careEventStatus: string
}
export interface ReportSummary {
  newEventCount: number
  closedEventCount: number
  completedVisitCount: number
  completedOrderCount: number
  completedFollowUpCount: number
  visitedElderCount: number
  averageAcceptanceMinutes: number | null
  currentOpenTaskCount: number
  currentOverdueTaskCount: number
}
export interface DailyOperations {
  date: string
  newEventCount: number
  closedEventCount: number
  completedVisitCount: number
  completedOrderCount: number
  completedFollowUpCount: number
}
export interface PersonnelOperations extends Personnel {
  completedVisitCount: number
  completedOrderCount: number
  completedFollowUpCount: number
}
export interface OperationsReport {
  from: string
  to: string
  timeZone: string
  generatedAt: string
  areaLabel: string
  summary: ReportSummary
  daily: DailyOperations[]
  personnel: PersonnelOperations[]
}
export const taskLabels = { Visit: '探访', ServiceOrder: '工单', FollowUp: '回访' }
export const workLabels: Record<string, string> = {
  Assigned: '未开始', Accepted: '已接单', InProgress: '处理中', Completed: '已完成', Cancelled: '已取消',
}
export const eventLabels: Record<string, string> = {
  PendingConfirmation: '待确认', Accepted: '已受理', InProgress: '处理中', Resolved: '已解决',
  FollowUpPending: '待回访', Closed: '已结案', FalseAlarm: '误报', UnableToConfirm: '无法确认',
}
export const taskRoutes = { Visit: 'visits', ServiceOrder: 'service-orders', FollowUp: 'follow-ups' }
export const formatTime = (value: string | null) => value
  ? new Date(value).toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai', hour12: false })
  : '未设截止时间'
export const beijingInput = (date: Date) => new Date(date.getTime() + 8 * 3600000).toISOString().slice(0, 16)
export const beijingIso = (value: string) => new Date(value + ':00+08:00').toISOString()
