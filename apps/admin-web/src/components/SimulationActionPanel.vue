<script setup lang="ts">
import { ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ContactAttempt } from '@/api/contracts'

const props = withDefaults(
  defineProps<{ attempts: ContactAttempt[]; eventId?: string; canRecord?: boolean }>(),
  { eventId: '', canRecord: true },
)

const sending = ref(false)
const latestOutcome = ref('')
const errorMessage = ref('')

const actions = [
  { label: '模拟站内通知', channel: 'InAppNotification', recipientRole: 'Elder' },
  { label: '模拟短信', channel: 'Sms', recipientRole: 'Family' },
  { label: '模拟电话', channel: 'Phone', recipientRole: 'Family' },
  { label: '模拟上门', channel: 'HomeVisit', recipientRole: 'CommunityStaff' },
  { label: '模拟急救转运', channel: 'EmergencyTransport', recipientRole: 'EmergencyService' },
] as const

async function record(channel: string, recipientRole: string) {
  if (!props.eventId || sending.value) return
  sending.value = true
  latestOutcome.value = ''
  errorMessage.value = ''
  try {
    const response = await apiClient.request<{ outcome: string }>(
      `/api/v1/care-events/${props.eventId}/simulation-attempts`,
      {
        method: 'POST',
        body: JSON.stringify({
          requestId: crypto.randomUUID(),
          channel,
          recipientRole,
          simulateFailure: false,
        }),
      },
    )
    latestOutcome.value = response.outcome
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? `模拟失败：${error.message}` : '模拟失败：请求未完成。'
  } finally {
    sending.value = false
  }
}
</script>

<template>
  <section class="simulation-panel surface" aria-labelledby="simulation-title">
    <p class="simulation-label">模拟外部动作</p>
    <h2 id="simulation-title">联系与转运记录</h2>
    <p>电话、短信、家属联系和 120 动作只记录模拟结果，不会连接真实外部服务。</p>
    <div v-if="eventId && canRecord" class="simulation-actions">
      <button
        v-for="action in actions"
        :key="action.channel"
        type="button"
        :disabled="sending"
        @click="record(action.channel, action.recipientRole)"
      >
        {{ action.label }}
      </button>
    </div>
    <p v-if="sending" class="pending" role="status">模拟发送中</p>
    <p v-else-if="latestOutcome" class="outcome" role="status">{{ latestOutcome }}</p>
    <p v-else-if="errorMessage" class="simulation-error" role="alert">{{ errorMessage }}</p>
    <ul v-if="attempts.length">
      <li v-for="attempt in attempts" :key="attempt.id">
        <span>{{ attempt.targetLabel }}</span>
        <strong>{{ attempt.outcome }}</strong>
        <span v-if="attempt.isSimulation" class="simulation-tag">模拟</span>
      </li>
    </ul>
    <p v-else class="empty-copy">尚无模拟联系记录。</p>
  </section>
</template>

<style scoped>
.simulation-panel {
  padding: var(--space-5);
}

.simulation-label {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin-bottom: var(--space-2);
  font-size: 20px;
}

ul {
  padding: 0;
  margin: var(--space-4) 0 0;
  list-style: none;
}

li {
  display: grid;
  grid-template-columns: minmax(120px, 0.7fr) minmax(180px, 1fr) auto;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid var(--line);
}

.simulation-tag {
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

.empty-copy {
  color: var(--ink-muted);
}

.simulation-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin-top: var(--space-4);
}

button {
  min-height: 44px;
  padding: 0 var(--space-3);
  border: 1px solid var(--navy);
  color: var(--navy);
  background: var(--surface);
  font: inherit;
  font-weight: 700;
}

button:disabled {
  border-color: var(--line-strong);
  color: var(--ink-muted);
  background: var(--surface-muted);
}

.pending {
  color: var(--action);
  font-weight: 700;
}

.outcome {
  color: var(--success);
  font-weight: 700;
}

.simulation-error {
  color: var(--emergency);
  font-weight: 700;
}
</style>
