<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { apiClient, ApiError } from '@/api/apiClient'
import type {
  CareEvent,
  CareEventStatus,
  ElderDetail,
  FollowUpItem,
  ServiceOrderItem,
  VisitItem,
} from '@/api/contracts'
import BreakGlassDialog from '@/components/BreakGlassDialog.vue'
import EventLevelBadge from '@/components/EventLevelBadge.vue'
import EventTimeline from '@/components/EventTimeline.vue'
import SimulationActionPanel from '@/components/SimulationActionPanel.vue'
import StatusNotice from '@/components/StatusNotice.vue'
import TransitionDialog from '@/components/TransitionDialog.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const auth = useAuthStore()
const eventId = computed(() => String(route.params.eventId))
const careEvent = ref<CareEvent | null>(null)
const elder = ref<ElderDetail | null>(null)
const visits = ref<VisitItem[]>([])
const serviceOrders = ref<ServiceOrderItem[]>([])
const followUps = ref<FollowUpItem[]>([])
const loading = ref(true)
const errorMessage = ref('')
const selectedTransition = ref<CareEventStatus | null>(null)
const actionError = ref('')
const submitting = ref(false)
const breakGlassMessage = ref('')

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
const displaySummary = computed(() =>
  careEvent.value?.source === 'AiCue'
    ? 'AI 结构化风险提示（原始内容不展示）'
    : careEvent.value?.summary,
)

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const event = await apiClient.request<CareEvent>(`/api/v1/care-events/${eventId.value}`)
    const query = `?careEventId=${encodeURIComponent(event.id)}`
    const [elderDetail, visitItems, orderItems, followUpItems] = await Promise.all([
      apiClient.request<ElderDetail>(`/api/v1/elders/${event.elderId}`),
      apiClient.request<VisitItem[]>(`/api/v1/visits${query}`),
      apiClient.request<ServiceOrderItem[]>(`/api/v1/service-orders${query}`),
      apiClient.request<FollowUpItem[]>(`/api/v1/follow-ups${query}`),
    ])
    careEvent.value = event
    elder.value = elderDetail
    visits.value = visitItems
    serviceOrders.value = orderItems
    followUps.value = followUpItems
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '事件详情载入失败。'
  } finally {
    loading.value = false
  }
}

async function acceptEvent() {
  if (!careEvent.value) return
  submitting.value = true
  actionError.value = ''
  try {
    careEvent.value = await apiClient.request<CareEvent>(
      `/api/v1/care-events/${careEvent.value.id}/accept`,
      { method: 'POST' },
    )
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : '事件受理失败。'
  } finally {
    submitting.value = false
  }
}

async function submitTransition(reason: string, resolution: string | null) {
  if (!careEvent.value || !selectedTransition.value) return
  submitting.value = true
  actionError.value = ''
  try {
    careEvent.value = await apiClient.request<CareEvent>(
      `/api/v1/care-events/${careEvent.value.id}/transitions`,
      {
        method: 'POST',
        body: JSON.stringify({
          toStatus: selectedTransition.value,
          reason: reason || null,
          resolution,
        }),
      },
    )
    selectedTransition.value = null
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : '状态更新失败。'
  } finally {
    submitting.value = false
  }
}

async function requestBreakGlass(reason: string) {
  if (!careEvent.value) return
  submitting.value = true
  actionError.value = ''
  breakGlassMessage.value = ''
  try {
    const grant = await apiClient.request<{ expiresAt: string }>(
      `/api/v1/elders/${careEvent.value.elderId}/break-glass`,
      {
        method: 'POST',
        body: JSON.stringify({ reason, durationMinutes: 15 }),
      },
    )
    breakGlassMessage.value = `临时授权有效至 ${new Date(grant.expiresAt).toLocaleTimeString('zh-CN', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })}`
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : '临时授权申请失败。'
  } finally {
    submitting.value = false
  }
}

function chooseTransition(target: CareEventStatus) {
  selectedTransition.value = target
  actionError.value = ''
}

onMounted(load)
</script>

<template>
  <section>
    <StatusNotice v-if="loading" kind="loading" title="正在载入事件详情" />
    <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />

    <template v-else-if="careEvent">
      <header class="detail-heading">
        <div>
          <RouterLink to="/care-events">返回照料事件</RouterLink>
          <p class="page-kicker">{{ elder?.demoDisplayName ?? '演示老人' }}</p>
          <h1>{{ displaySummary }}</h1>
        </div>
        <div class="heading-status">
          <EventLevelBadge :level="careEvent.level" />
          <strong>{{ statusLabels[careEvent.status] }}</strong>
        </div>
      </header>

      <div class="event-facts surface">
        <div><span>责任队列</span><strong>{{ careEvent.responsibilityQueue }}</strong></div>
        <div>
          <span>当前负责人</span>
          <strong>{{ careEvent.currentOwnerUserId ? '已分派' : '等待受理' }}</strong>
        </div>
        <div><span>最近活动</span><strong>{{ new Date(careEvent.lastActivityAt).toLocaleString('zh-CN') }}</strong></div>
      </div>

      <section
        v-if="auth.role === 'CommunityStaff'"
        class="action-section surface"
        aria-labelledby="event-actions-title"
      >
        <h2 id="event-actions-title">可执行操作</h2>
        <p>按钮只发起服务器状态操作；页面以服务器返回的持久化状态为准。</p>
        <div class="action-row">
          <button
            v-if="careEvent.allowedTransitions.includes('Accepted')"
            class="primary-button"
            type="button"
            :disabled="submitting"
            @click="acceptEvent"
          >
            受理事件
          </button>
          <button
            v-for="target in careEvent.allowedTransitions.filter((item) => item !== 'Accepted')"
            :key="target"
            class="secondary-button"
            type="button"
            @click="chooseTransition(target)"
          >
            转为{{ statusLabels[target] }}
          </button>
        </div>
        <p v-if="actionError && !selectedTransition" class="action-error" role="alert">
          {{ actionError }}
        </p>
      </section>

      <TransitionDialog
        v-if="auth.role === 'CommunityStaff' && selectedTransition"
        :target="selectedTransition"
        :submitting="submitting"
        :server-error="actionError"
        @submit="submitTransition"
      />

      <div class="detail-grid">
        <section class="surface detail-section">
          <h2>任务与分派</h2>
          <div v-if="visits.length || serviceOrders.length || followUps.length" class="work-groups">
            <p v-for="visit in visits" :key="visit.visitId">
              探访 · {{ visit.elderDisplayName }} · {{ visit.status }}
            </p>
            <p v-for="order in serviceOrders" :key="order.orderId">
              工单 · {{ order.serviceType }} · {{ order.status }}
            </p>
            <p v-for="followUp in followUps" :key="followUp.followUpId">
              随访 · {{ followUp.elderDisplayName }} · {{ followUp.status }}
            </p>
          </div>
          <p v-else class="empty-copy">当前事件尚无探访、服务工单或随访任务。</p>
        </section>

        <SimulationActionPanel
          :attempts="careEvent.contactAttempts"
          :event-id="careEvent.id"
          :can-record="auth.role === 'CommunityStaff'"
        />
      </div>

      <BreakGlassDialog
        v-if="auth.role === 'CommunityStaff'"
        :event-level="careEvent.level"
        :elder-id="careEvent.elderId"
        :submitting="submitting"
        :server-error="actionError"
        @submit="requestBreakGlass"
      />
      <p v-if="breakGlassMessage" class="grant-message" role="status">{{ breakGlassMessage }}</p>

      <section class="timeline-section surface" aria-labelledby="timeline-title">
        <h2 id="timeline-title">照料时间线</h2>
        <EventTimeline
          :evidence="careEvent.evidence"
          :transitions="careEvent.transitions"
          :contact-attempts="careEvent.contactAttempts"
          :visits="visits"
          :service-orders="serviceOrders"
          :follow-ups="followUps"
        />
      </section>
    </template>
  </section>
</template>

<style scoped>
.detail-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--space-5);
  margin-bottom: var(--space-5);
}

.detail-heading a {
  display: inline-block;
  margin-bottom: var(--space-4);
}

.page-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-weight: 700;
}

.detail-heading h1 {
  max-width: 780px;
  margin-bottom: 0;
  font-size: 28px;
}

.heading-status {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.event-facts {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-bottom: var(--space-5);
}

.event-facts div {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-4);
  border-left: 1px solid var(--line);
}

.event-facts div:first-child {
  border-left: 0;
}

.event-facts span {
  color: var(--ink-muted);
  font-size: 13px;
}

.action-section,
.detail-section,
.timeline-section {
  padding: var(--space-5);
  margin-bottom: var(--space-5);
}

.action-section h2,
.detail-section h2,
.timeline-section h2 {
  margin-bottom: var(--space-2);
  font-size: 20px;
}

.action-row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-3);
}

.action-error {
  margin: var(--space-3) 0 0;
  color: var(--emergency);
  font-weight: 700;
}

.detail-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: var(--space-5);
  margin-top: var(--space-5);
}

.work-groups p {
  padding: var(--space-3) 0;
  margin: 0;
  border-top: 1px solid var(--line);
}

.work-groups p:first-child {
  border-top: 0;
}

.empty-copy {
  color: var(--ink-muted);
}

.grant-message {
  padding: var(--space-3);
  margin: var(--space-3) 0 var(--space-5);
  color: #1d5d2d;
  background: #e7f6eb;
  font-weight: 700;
}

@media (max-width: 900px) {
  .detail-heading,
  .event-facts,
  .detail-grid {
    display: grid;
    grid-template-columns: 1fr;
  }

  .event-facts div,
  .event-facts div:first-child {
    border-top: 1px solid var(--line);
    border-left: 0;
  }

  .event-facts div:first-child {
    border-top: 0;
  }
}
</style>
