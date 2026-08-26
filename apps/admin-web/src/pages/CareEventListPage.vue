<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { CareEvent, CareEventStatus, ElderSummary } from '@/api/contracts'
import EventLevelBadge from '@/components/EventLevelBadge.vue'
import StatusNotice from '@/components/StatusNotice.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppTable from '@/components/ui/AppTable.vue'

const events = ref<CareEvent[]>([])
const elders = ref<ElderSummary[]>([])
const loading = ref(true)
const errorMessage = ref('')

const statusLabels: Record<CareEventStatus, string> = {
  PendingConfirmation: '待确认',
  Accepted: '已受理',
  UnableToConfirm: '无法确认',
  InProgress: '处理中',
  Resolved: '已解决',
  FollowUpPending: '待随访',
  Closed: '已结案',
  FalseAlarm: '误报',
}

const statusTones: Record<CareEventStatus, 'l1' | 'l2' | 'l3' | 'closed' | 'neutral'> = {
  PendingConfirmation: 'l2',
  Accepted: 'l3',
  UnableToConfirm: 'l2',
  InProgress: 'l3',
  Resolved: 'l3',
  FollowUpPending: 'l3',
  Closed: 'closed',
  FalseAlarm: 'neutral',
}

const nextActionLabels: Partial<Record<CareEventStatus, string>> = {
  Accepted: '受理并分派',
  InProgress: '开始处理',
  UnableToConfirm: '升级联系',
  Resolved: '记录解决结果',
  FollowUpPending: '安排随访',
  Closed: '结案',
  FalseAlarm: '判定误报',
}

const elderNames = computed(() => new Map(elders.value.map((item) => [item.id, item.demoDisplayName])))
const sortedEvents = computed(() => {
  const rank = { Emergency: 0, NeedsConfirmation: 1, GeneralService: 2 }
  return [...events.value].sort(
    (left, right) =>
      rank[left.level] - rank[right.level] ||
      new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime(),
  )
})

function waitingTime(createdAt: string) {
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(createdAt).getTime()) / 60000))
  if (minutes < 60) return `${minutes} 分钟`
  return `${Math.floor(minutes / 60)} 小时 ${minutes % 60} 分钟`
}

function nextAction(event: CareEvent) {
  const target = event.allowedTransitions[0]
  return target ? (nextActionLabels[target] ?? statusLabels[target]) : '无需操作'
}

const eventSummary = (event: CareEvent) =>
  event.source === 'AiCue' ? 'AI 结构化风险提示（原始内容不展示）' : event.summary

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [eventItems, elderItems] = await Promise.all([
      apiClient.request<CareEvent[]>('/api/v1/care-events'),
      apiClient.request<ElderSummary[]>('/api/v1/elders'),
    ])
    events.value = eventItems
    elders.value = elderItems
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '照料事件载入失败。'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section>
    <header class="page-heading">
      <p class="page-kicker">社区事件队列</p>
      <h1>照料事件</h1>
      <p>紧急事件优先，其次按等待时间从长到短排列。</p>
    </header>

    <StatusNotice v-if="loading" kind="loading" title="正在载入照料事件" />
    <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />
    <StatusNotice
      v-else-if="!sortedEvents.length"
      kind="empty"
      illustration="care-events"
      title="当前没有待处理照料事件"
    />

    <AppTable v-else>
      <thead>
        <tr>
          <th scope="col">级别</th>
          <th scope="col">状态</th>
          <th scope="col">老人</th>
          <th scope="col">当前负责人</th>
          <th scope="col">等待时间</th>
          <th scope="col">下一步</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="event in sortedEvents" :key="event.id">
          <td>
            <EventLevelBadge :level="event.level" />
            <span
              v-if="
                event.contactAttempts.some((item) => item.isSimulation) ||
                event.evidence.some((item) => item.isSimulation)
              "
              class="simulation-label"
              >模拟</span
            >
          </td>
          <td>
            <AppBadge :tone="statusTones[event.status]">
              {{ statusLabels[event.status] }}
            </AppBadge>
          </td>
          <td>
            <RouterLink :to="`/care-events/${event.id}`">
              {{ elderNames.get(event.elderId) ?? '老人' }}
            </RouterLink>
            <span class="event-summary">{{ eventSummary(event) }}</span>
          </td>
          <td>{{ event.currentOwnerUserId ? '已分派' : event.responsibilityQueue }}</td>
          <td>{{ waitingTime(event.createdAt) }}</td>
          <td>{{ nextAction(event) }}</td>
        </tr>
      </tbody>
    </AppTable>
  </section>
</template>

<style scoped>
.page-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

tbody tr:has(.event-level--Emergency) {
  background: var(--emergency-soft);
}

tbody tr:has(.event-level--Emergency):hover {
  background: var(--emergency-soft);
}

.simulation-label {
  display: block;
  margin-top: var(--space-2);
  color: var(--action);
  font-size: 12px;
  font-weight: 700;
}

.event-summary {
  display: block;
  max-width: 300px;
  margin-top: var(--space-1);
  color: var(--ink-muted);
  font-size: 13px;
}
</style>
