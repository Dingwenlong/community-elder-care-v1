<script setup lang="ts">
import { ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { DeviceSignalResponse, DeviceSignalType } from '@/api/contracts'

const demoDeviceId = '77777777-7777-7777-7777-777777777701'
const isSending = ref(false)
const latest = ref<DeviceSignalResponse | null>(null)
const errorMessage = ref('')

const actions: Array<{ label: string; signalType: DeviceSignalType; buttonState: string | null }> = [
  { label: '模拟 SOS', signalType: 'SosButton', buttonState: 'Held2Seconds' },
  { label: '模拟长时间无用水', signalType: 'NoWaterActivity', buttonState: null },
  { label: '模拟设备离线', signalType: 'DeviceOffline', buttonState: null },
]

async function send(signalType: DeviceSignalType, buttonState: string | null) {
  if (isSending.value) return
  isSending.value = true
  latest.value = null
  errorMessage.value = ''
  try {
    latest.value = await apiClient.request<DeviceSignalResponse>('/api/v1/demo/device-signals', {
      method: 'POST',
      body: JSON.stringify({
        deviceId: demoDeviceId,
        eventId: crypto.randomUUID(),
        deviceTime: new Date().toISOString(),
        signalType,
        buttonState,
      }),
    })
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? error.message : '模拟信号未送达，请检查后端状态。'
  } finally {
    isSending.value = false
  }
}
</script>

<template>
  <section class="device-simulator" aria-labelledby="device-simulator-title">
    <header>
      <div>
        <p class="eyebrow">Web 设备模拟器</p>
        <h2 id="device-simulator-title">安全设备信号</h2>
      </div>
      <span class="demo-label">仅演示</span>
    </header>
    <p class="explanation">
      下列操作调用与 ESP32 相同的设备信号服务。它们只创建演示记录，不连接真实设备或联系人。
    </p>
    <div class="actions">
      <button
        v-for="action in actions"
        :key="action.signalType"
        type="button"
        :disabled="isSending"
        @click="send(action.signalType, action.buttonState)"
      >
        {{ action.label }}
      </button>
    </div>
    <p v-if="isSending" class="pending" role="status">模拟信号发送中</p>
    <div v-else-if="latest" class="receipt" role="status">
      <strong>模拟信号</strong>
      <span>服务端已保存，并关联到照料事件。</span>
      <RouterLink :to="`/care-events/${latest.careEventId}`">查看照料事件</RouterLink>
    </div>
    <p v-else-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
  </section>
</template>

<style scoped>
.device-simulator {
  padding: var(--space-5);
  border: 1px solid var(--line-strong);
  background: var(--surface);
}

header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: var(--space-4);
}

.eyebrow {
  margin: 0 0 var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin: 0;
  color: var(--ink-strong);
  font-size: 22px;
}

.demo-label {
  padding-left: var(--space-3);
  border-left: 3px solid var(--warning);
  color: var(--warning);
  font-size: 13px;
  font-weight: 700;
}

.explanation {
  max-width: 760px;
  margin: var(--space-4) 0;
  color: var(--ink-muted);
  line-height: 1.7;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-3);
}

button {
  min-height: 44px;
  padding: 0 var(--space-4);
  border: 1px solid var(--navy);
  color: var(--navy);
  background: var(--surface);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

button:first-child {
  border-color: var(--emergency);
  color: white;
  background: var(--emergency);
}

button:disabled {
  border-color: var(--line-strong);
  color: var(--ink-muted);
  background: var(--surface-muted);
  cursor: wait;
}

button:focus-visible,
a:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.pending,
.receipt,
.error {
  margin: var(--space-4) 0 0;
}

.pending {
  color: var(--action);
  font-weight: 700;
}

.receipt {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: var(--space-3);
  padding: var(--space-4);
  border-left: 3px solid var(--success);
  background: var(--success-soft);
}

.receipt a {
  color: var(--action);
  font-weight: 700;
}

.error {
  padding: var(--space-3);
  border-left: 3px solid var(--emergency);
  color: var(--emergency);
  background: var(--emergency-soft);
}

@media (max-width: 640px) {
  .actions,
  .receipt {
    display: grid;
    grid-template-columns: 1fr;
  }
}
</style>
