<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { CareEvent, CareEventStatus, ElderSummary } from '@/api/contracts'
import EventLevelBadge from '@/components/EventLevelBadge.vue'
import StatusNotice from '@/components/StatusNotice.vue'
import AppCard from '@/components/ui/AppCard.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const canViewElders = computed(
  () => auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)

const events = ref<CareEvent[]>([])
const elders = ref<ElderSummary[]>([])
const loading = ref(true)
const errorMessage = ref('')

const closedStatuses: CareEventStatus[] = ['Closed', 'FalseAlarm']

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

const elderNames = computed(
  () => new Map(elders.value.map((item) => [item.id, item.demoDisplayName])),
)

const openEvents = computed(() => {
  const rank = { Emergency: 0, NeedsConfirmation: 1, GeneralService: 2 }
  return events.value
    .filter((event) => !closedStatuses.includes(event.status))
    .sort(
      (left, right) =>
        rank[left.level] - rank[right.level] ||
        new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime(),
    )
})

const emergencyCount = computed(
  () => openEvents.value.filter((event) => event.level === 'Emergency').length,
)
const confirmationCount = computed(
  () =>
    openEvents.value.filter((event) => event.level === 'NeedsConfirmation')
      .length,
)
const priorityElderCount = computed(
  () =>
    elders.value.filter((elder) => elder.attentionLevel === 'Priority').length,
)
const topPending = computed(() => openEvents.value.slice(0, 5))

const kpis = computed(() => [
  {
    key: 'open',
    label: '待处理事件',
    value: loading.value ? '—' : String(openEvents.value.length),
    tone: 'primary' as const,
  },
  {
    key: 'emergency',
    label: '其中紧急',
    value: loading.value ? '—' : String(emergencyCount.value),
    tone: 'danger' as const,
    alert: emergencyCount.value > 0,
  },
  {
    key: 'confirmation',
    label: '待确认安全',
    value: loading.value ? '—' : String(confirmationCount.value),
    tone: 'warning' as const,
    alert: confirmationCount.value > 0,
  },
  {
    key: 'priorityElders',
    label: '重点关注老人',
    value: loading.value ? '—' : String(priorityElderCount.value),
    tone: 'success' as const,
  },
])

function waitingTime(createdAt: string) {
  const minutes = Math.max(
    0,
    Math.floor((Date.now() - new Date(createdAt).getTime()) / 60000),
  )
  if (minutes < 60) return `${minutes} 分钟`
  return `${Math.floor(minutes / 60)} 小时 ${minutes % 60} 分钟`
}

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
    errorMessage.value =
      error instanceof ApiError ? error.message : '工作台数据载入失败。'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <header class="page-heading dashboard-heading">
    <p class="page-context">A01 社区 · 今日工作</p>
    <h1>社区工作台</h1>
    <p>先处理需要确认的安全事件，再安排探访和生活服务。</p>
  </header>

  <section class="kpi-grid" aria-label="今日关键指标">
    <AppCard
      v-for="kpi in kpis"
      :key="kpi.key"
      class="kpi-card"
      :class="[`kpi-card--${kpi.tone}`, { 'kpi-card--alert': kpi.alert }]"
    >
      <span class="kpi-icon" aria-hidden="true">
        <svg
          v-if="kpi.key === 'open'"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="12" cy="12" r="9" />
          <path d="M12 7v5l3 3" />
        </svg>
        <svg
          v-else-if="kpi.key === 'emergency'"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M12 3 2.5 20h19L12 3Z" />
          <path d="M12 10v4" />
          <path d="M12 17h.01" />
        </svg>
        <svg
          v-else-if="kpi.key === 'confirmation'"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M8 11a4 4 0 1 1 8 0c0 4 2 5 2 5H6s2-1 2-5Z" />
          <path d="M10.3 20a2 2 0 0 0 3.4 0" />
        </svg>
        <svg
          v-else
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="12" cy="8" r="4" />
          <path d="M4 21c0-4 3.6-6 8-6s8 2 8 6" />
        </svg>
      </span>
      <span class="kpi-body">
        <span class="kpi-value">{{ kpi.value }}</span>
        <span class="kpi-label">{{ kpi.label }}</span>
      </span>
    </AppCard>
  </section>

  <div class="dashboard-grid">
    <section class="surface pending-panel" aria-labelledby="pending-title">
      <div class="section-heading">
        <div>
          <h2 id="pending-title">待处理事项</h2>
          <p>按等级和等待时长排序，最多显示前五条</p>
        </div>
        <RouterLink v-if="canViewElders" class="secondary-button" to="/elders">
          查看老人档案
        </RouterLink>
      </div>

      <StatusNotice v-if="loading" kind="loading" title="正在载入今日数据" />
      <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />
      <StatusNotice
        v-else-if="!topPending.length"
        title="当前没有载入事件"
        message="照料事件接入后会在这里按优先级显示。"
      />

      <ul v-else class="pending-list">
        <li v-for="event in topPending" :key="event.id" class="pending-item">
          <EventLevelBadge :level="event.level" />
          <div class="pending-item__main">
            <RouterLink :to="`/care-events/${event.id}`" class="pending-item__name">
              {{ elderNames.get(event.elderId) ?? '老人' }}
            </RouterLink>
            <span class="pending-item__meta">
              {{ statusLabels[event.status] }} · 已等待 {{ waitingTime(event.createdAt) }}
            </span>
          </div>
          <span class="pending-item__chevron" aria-hidden="true">›</span>
        </li>
      </ul>
    </section>

    <aside class="surface operating-note" aria-labelledby="operating-title">
      <h2 id="operating-title">处理原则</h2>
      <ol>
        <li>紧急事件立即核实，不以设备信号直接结案。</li>
        <li>联系不上老人时保留记录并继续升级。</li>
        <li>探访、工单和随访完成后才能申请结案。</li>
      </ol>
    </aside>
  </div>
</template>

<style scoped>
.page-context {
  margin-bottom: var(--space-2);
  color: var(--action);
  font-size: 14px;
  font-weight: 700;
}

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.kpi-card {
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.kpi-icon {
  display: grid;
  width: 40px;
  height: 40px;
  flex: none;
  place-items: center;
  border-radius: var(--radius-md);
}

.kpi-icon svg {
  width: 22px;
  height: 22px;
}

.kpi-card--primary .kpi-icon {
  color: var(--action);
  background: var(--action-soft);
}

.kpi-card--danger .kpi-icon {
  color: var(--emergency);
  background: var(--emergency-soft);
}

.kpi-card--warning .kpi-icon {
  color: var(--warning);
  background: var(--warning-soft);
}

.kpi-card--success .kpi-icon {
  color: var(--success);
  background: var(--success-soft);
}

.kpi-body {
  display: grid;
  gap: 2px;
}

.kpi-value {
  color: var(--ink-strong);
  font-family: var(--font-numeric);
  font-size: 32px;
  font-weight: 700;
  line-height: 1.2;
}

.kpi-card--alert .kpi-value {
  color: var(--emergency);
}

.kpi-label {
  color: var(--ink-muted);
  font: var(--text-secondary);
}

.dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: var(--space-5);
}

.pending-panel,
.operating-note {
  padding: var(--space-5);
}

.section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.section-heading h2,
.section-heading p,
.operating-note h2 {
  margin-bottom: 0;
}

.section-heading h2,
.operating-note h2 {
  font: var(--text-title);
}

.section-heading p {
  margin-top: var(--space-1);
  color: var(--ink-muted);
  font: var(--text-secondary);
}

.pending-list {
  display: grid;
  gap: var(--space-2);
  padding: 0;
  margin: 0;
  list-style: none;
}

.pending-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: 56px;
  padding: var(--space-2) var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  background: var(--surface);
  transition:
    border-color var(--duration-fast) var(--ease-standard),
    background-color var(--duration-fast) var(--ease-standard);
}

.pending-item:hover {
  border-color: var(--action);
  background: var(--action-soft);
}

.pending-item__main {
  display: grid;
  flex: 1;
  gap: 2px;
  min-width: 0;
}

.pending-item__name {
  font-weight: 600;
}

.pending-item__meta {
  color: var(--ink-muted);
  font: var(--text-caption);
}

.pending-item__chevron {
  color: var(--ink-muted);
  font-size: 20px;
}

.operating-note ol {
  display: grid;
  gap: var(--space-4);
  padding-left: 22px;
  margin: var(--space-5) 0 0;
}

@media (max-width: 1279px) {
  .kpi-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 1023px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 767px) {
  .kpi-grid {
    grid-template-columns: 1fr;
  }
}
</style>
