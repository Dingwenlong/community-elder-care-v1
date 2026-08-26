<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ServiceOrderItem } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'
import AppBadge from '@/components/ui/AppBadge.vue'

const tasks = ref<ServiceOrderItem[]>([])
const loading = ref(true)
const errorMessage = ref('')
const completionTask = ref<ServiceOrderItem | null>(null)
const completionResult = ref('')
const validationError = ref('')

const taskStatusLabels: Record<string, string> = {
  Assigned: '已分派',
  Accepted: '已接单',
  InProgress: '处理中',
  Completed: '已完成',
  Cancelled: '已取消',
}

const taskStatusTones: Record<string, 'l2' | 'l3' | 'closed' | 'neutral'> = {
  Assigned: 'l3',
  Accepted: 'l3',
  InProgress: 'l2',
  Completed: 'closed',
  Cancelled: 'neutral',
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    tasks.value = await apiClient.request<ServiceOrderItem[]>('/api/v1/service-orders/my-tasks')
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '我的任务载入失败。'
  } finally {
    loading.value = false
  }
}

async function acceptTask(task: ServiceOrderItem) {
  errorMessage.value = ''
  try {
    await apiClient.request<ServiceOrderItem>(`/api/v1/service-orders/${task.orderId}/accept`, {
      method: 'POST',
    })
    await load()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '接收任务失败。'
  }
}

function openCompletion(task: ServiceOrderItem) {
  completionTask.value = task
  completionResult.value = ''
  validationError.value = ''
}

async function completeTask() {
  validationError.value = ''
  if (!completionTask.value) return
  if (!completionResult.value.trim()) {
    validationError.value = '请填写服务完成结果。'
    return
  }
  try {
    await apiClient.request<ServiceOrderItem>(
      `/api/v1/service-orders/${completionTask.value.orderId}/complete`,
      { method: 'POST', body: JSON.stringify({ result: completionResult.value.trim() }) },
    )
    completionTask.value = null
    await load()
  } catch (error) {
    validationError.value = error instanceof ApiError ? error.message : '任务完成失败。'
  }
}

onMounted(load)
</script>

<template>
  <section>
    <header class="page-heading">
      <p class="page-kicker">服务人员工作区</p>
      <h1>我的任务</h1>
      <p>这里只显示本人获派工单的最小执行信息。</p>
    </header>

    <StatusNotice v-if="loading" kind="loading" title="正在载入我的任务" />
    <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />
    <StatusNotice
      v-else-if="!tasks.length"
      kind="empty"
      illustration="care-work"
      title="当前没有获派任务"
    />

    <div v-else class="task-list">
      <article v-for="task in tasks" :key="task.orderId" class="task-card surface">
        <div>
          <p class="task-person">{{ task.elderDisplayName }}</p>
          <h2>{{ task.serviceType }}</h2>
        </div>
        <dl>
          <div><dt>预约时段</dt><dd>{{ task.scheduledWindow }}</dd></div>
          <div><dt>联系说明</dt><dd>{{ task.contactInstruction }}</dd></div>
          <div>
            <dt>当前状态</dt>
            <dd>
              <AppBadge :tone="taskStatusTones[task.status] ?? 'neutral'">
                {{ taskStatusLabels[task.status] ?? task.status }}
              </AppBadge>
            </dd>
          </div>
        </dl>
        <div class="task-actions">
          <button
            v-if="task.status === 'Assigned'"
            class="primary-button"
            type="button"
            @click="acceptTask(task)"
          >
            接收任务
          </button>
          <button
            v-if="task.status === 'InProgress'"
            class="primary-button"
            type="button"
            @click="openCompletion(task)"
          >
            填写完成结果
          </button>
        </div>
      </article>
    </div>

    <section v-if="completionTask" class="completion-panel surface">
      <h2>完成{{ completionTask.serviceType }}</h2>
      <form novalidate @submit.prevent="completeTask">
        <label for="service-result">服务完成结果</label>
        <textarea id="service-result" v-model="completionResult" rows="3" />
        <p v-if="validationError" class="form-error" role="alert">{{ validationError }}</p>
        <button class="primary-button" type="submit">提交完成结果</button>
      </form>
    </section>
  </section>
</template>

<style scoped>
.page-kicker,
.task-person {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

.task-list {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: var(--space-4);
}

.task-card {
  padding: var(--space-5);
  border-radius: var(--radius-lg);
  transition:
    box-shadow var(--duration-normal) var(--ease-standard),
    transform var(--duration-normal) var(--ease-standard);
}

.task-card:hover {
  box-shadow: var(--shadow-md);
}

.task-card h2 {
  margin-bottom: var(--space-4);
  font: var(--text-title);
}

dl {
  margin: 0;
}

dl div {
  display: grid;
  grid-template-columns: 88px 1fr;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid var(--line);
}

dt {
  color: var(--ink-muted);
}

dd {
  margin: 0;
}

.task-actions {
  margin-top: var(--space-4);
}

.completion-panel {
  max-width: 680px;
  padding: var(--space-5);
  margin-top: var(--space-5);
}

form {
  display: grid;
  gap: var(--space-2);
}

textarea {
  min-height: 88px;
  padding: var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  resize: vertical;
}

textarea:focus-visible {
  outline: none;
  border-color: var(--action);
  box-shadow: 0 0 0 3px var(--action-soft);
}

.form-error {
  color: var(--emergency);
  font-weight: 700;
}

form button {
  justify-self: start;
}
</style>
