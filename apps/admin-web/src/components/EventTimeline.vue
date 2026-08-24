<script setup lang="ts">
import { computed } from 'vue'

import type {
  CareEventEvidence,
  CareEventTransition,
  ContactAttempt,
  FollowUpItem,
  ServiceOrderItem,
  VisitItem,
} from '@/api/contracts'

interface TimelineItem {
  key: string
  occurredAt: string
  kind: string
  summary: string
  isSimulation: boolean
}

const props = withDefaults(
  defineProps<{
    evidence: CareEventEvidence[]
    transitions: CareEventTransition[]
    contactAttempts: ContactAttempt[]
    visits?: VisitItem[]
    serviceOrders?: ServiceOrderItem[]
    followUps?: FollowUpItem[]
  }>(),
  { visits: () => [], serviceOrders: () => [], followUps: () => [] },
)

const statusLabels: Record<string, string> = {
  PendingConfirmation: '待确认',
  Accepted: '已受理',
  UnableToConfirm: '无法确认',
  InProgress: '处理中',
  Resolved: '已解决',
  FollowUpPending: '待随访',
  Closed: '已结案',
  FalseAlarm: '误报',
}

const hasEvidenceKind = (prefix: string) =>
  props.evidence.some((item) => item.kind.startsWith(prefix))
const evidenceSummary = (item: CareEventEvidence) =>
  /^ai/i.test(item.kind) ? 'AI 已生成结构化风险提示，原始内容不展示。' : item.summary

const items = computed<TimelineItem[]>(() => {
  const result: TimelineItem[] = [
    ...props.evidence.map((item) => ({
      key: `evidence-${item.id}`,
      occurredAt: item.recordedAt,
      kind: '证据与任务记录',
      summary: evidenceSummary(item),
      isSimulation: item.isSimulation,
    })),
    ...props.transitions.map((item) => ({
      key: `transition-${item.id}`,
      occurredAt: item.occurredAt,
      kind: `状态：${statusLabels[item.toStatus] ?? item.toStatus}`,
      summary: item.reason || `状态已转为${statusLabels[item.toStatus] ?? item.toStatus}`,
      isSimulation: item.isSimulation,
    })),
    ...props.contactAttempts.map((item) => ({
      key: `contact-${item.id}`,
      occurredAt: item.attemptedAt,
      kind: `联系尝试：${item.targetLabel}`,
      summary: item.outcome,
      isSimulation: item.isSimulation,
    })),
  ]

  if (!hasEvidenceKind('Visit')) {
    result.push(
      ...props.visits.map((item) => ({
        key: `visit-${item.visitId}`,
        occurredAt: item.completedAt || item.startedAt || item.scheduledStartAt,
        kind: '探访任务',
        summary: item.confirmedSummary || `探访状态：${item.status}`,
        isSimulation: item.isDemoData,
      })),
    )
  }
  if (!hasEvidenceKind('ServiceOrder')) {
    result.push(
      ...props.serviceOrders.map((item) => ({
        key: `order-${item.orderId}`,
        occurredAt: item.scheduledWindow,
        kind: '服务工单',
        summary: item.result || `${item.serviceType}：${item.status}`,
        isSimulation: item.isDemoData === true,
      })),
    )
  }
  if (!hasEvidenceKind('FollowUp')) {
    result.push(
      ...props.followUps.map((item) => ({
        key: `follow-up-${item.followUpId}`,
        occurredAt: item.completedAt || item.dueAt,
        kind: '随访任务',
        summary: item.result || `随访状态：${item.status}`,
        isSimulation: item.isDemoData,
      })),
    )
  }

  return result.sort(
    (left, right) => new Date(left.occurredAt).getTime() - new Date(right.occurredAt).getTime(),
  )
})

const formatTime = (value: string) => {
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime())
    ? value
    : new Intl.DateTimeFormat('zh-CN', {
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
      }).format(parsed)
}
</script>

<template>
  <ol v-if="items.length" class="event-timeline">
    <li v-for="item in items" :key="item.key">
      <time :datetime="item.occurredAt">{{ formatTime(item.occurredAt) }}</time>
      <div>
        <p class="timeline-kind">
          {{ item.kind }} <span v-if="item.isSimulation" class="simulation-tag">模拟</span>
        </p>
        <p>{{ item.summary }}</p>
      </div>
    </li>
  </ol>
  <p v-else class="empty-copy">尚无已持久化的处理记录。</p>
</template>

<style scoped>
.event-timeline {
  padding: 0;
  margin: 0;
  list-style: none;
}

.event-timeline li {
  display: grid;
  grid-template-columns: 116px 1fr;
  gap: var(--space-4);
  padding: var(--space-4) 0;
  border-top: 1px solid var(--line);
}

.event-timeline li:first-child {
  border-top: 0;
}

time {
  color: var(--ink-muted);
  font-family: var(--font-numeric);
  font-size: 13px;
}

p {
  margin-bottom: 0;
}

.timeline-kind {
  margin-bottom: var(--space-1);
  color: var(--ink-strong);
  font-weight: 700;
}

.simulation-tag {
  display: inline-block;
  padding: 1px 6px;
  border: 1px solid var(--action);
  border-radius: 2px;
  color: var(--action);
  font-size: 12px;
}

.empty-copy {
  color: var(--ink-muted);
}
</style>
