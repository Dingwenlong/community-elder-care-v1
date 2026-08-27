<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { VisitItem } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppTable from '@/components/ui/AppTable.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const visits = ref<VisitItem[]>([])
const loading = ref(true)
const errorMessage = ref('')
const selectedVisit = ref<VisitItem | null>(null)
const rawStaffNote = ref('')
const confirmedSummary = ref('')
const result = ref('')
const validationError = ref('')
const submitting = ref(false)

const visitStatusLabels: Record<string, string> = {
  Assigned: '已分派',
  InProgress: '处理中',
  Completed: '已完成',
  Cancelled: '已取消',
}

const visitStatusTones: Record<string, 'l2' | 'l3' | 'closed' | 'neutral'> = {
  Assigned: 'l3',
  InProgress: 'l2',
  Completed: 'closed',
  Cancelled: 'neutral',
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    visits.value = await apiClient.request<VisitItem[]>('/api/v1/visits')
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '探访任务载入失败。'
  } finally {
    loading.value = false
  }
}

async function startVisit(visit: VisitItem) {
  errorMessage.value = ''
  try {
    await apiClient.request<VisitItem>(`/api/v1/visits/${visit.visitId}/start`, {
      method: 'POST',
    })
    await load()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '探访开始失败。'
  }
}

function openCompletion(visit: VisitItem) {
  selectedVisit.value = visit
  rawStaffNote.value = ''
  confirmedSummary.value = ''
  result.value = ''
  validationError.value = ''
}

async function completeVisit() {
  validationError.value = ''
  if (!selectedVisit.value) return
  if (!rawStaffNote.value.trim() || !confirmedSummary.value.trim() || !result.value.trim()) {
    validationError.value = '请完整填写内部记录、确认摘要和探访结果。'
    return
  }
  submitting.value = true
  try {
    await apiClient.request<VisitItem>(
      `/api/v1/visits/${selectedVisit.value.visitId}/complete`,
      {
        method: 'POST',
        body: JSON.stringify({
          rawStaffNote: rawStaffNote.value.trim(),
          confirmedSummary: confirmedSummary.value.trim(),
          result: result.value.trim(),
        }),
      },
    )
    selectedVisit.value = null
    await load()
  } catch (error) {
    validationError.value = error instanceof ApiError ? error.message : '探访完成失败。'
  } finally {
    submitting.value = false
  }
}

const formatTime = (value: string) => new Date(value).toLocaleString('zh-CN')

onMounted(load)
</script>

<template>
  <section>
    <header class="page-heading">
      <p class="page-kicker">线下照料执行</p>
      <h1>探访任务</h1>
      <p>内部原始记录与可共享的确认摘要分开保存、分开展示。</p>
    </header>

    <StatusNotice v-if="loading" kind="loading" title="正在载入探访任务" />
    <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />
    <StatusNotice
      v-else-if="!visits.length"
      kind="empty"
      illustration="care-work"
      title="当前没有探访任务"
    />

    <AppTable v-else min-width="850px">
      <thead>
        <tr>
          <th scope="col">老人</th>
          <th scope="col">预约时段</th>
          <th scope="col">状态</th>
          <th scope="col">确认摘要</th>
          <th scope="col">操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="visit in visits" :key="visit.visitId">
          <td>{{ visit.elderDisplayName }}</td>
          <td>{{ formatTime(visit.scheduledStartAt) }}—{{ formatTime(visit.scheduledEndAt) }}</td>
          <td>
            <AppBadge :tone="visitStatusTones[visit.status] ?? 'neutral'">
              {{ visitStatusLabels[visit.status] ?? visit.status }}
            </AppBadge>
          </td>
          <td>{{ visit.confirmedSummary || '尚未确认' }}</td>
          <td>
            <button
              v-if="auth.role === 'CommunityStaff' && visit.assignedStaffUserId === auth.userId && visit.status === 'Assigned'"
              class="secondary-button"
              type="button"
              @click="startVisit(visit)"
            >
              开始探访
            </button>
            <button
              v-if="auth.role === 'CommunityStaff' && visit.assignedStaffUserId === auth.userId && visit.status === 'InProgress'"
              class="primary-button"
              type="button"
              @click="openCompletion(visit)"
            >
              完成探访
            </button>
          </td>
        </tr>
      </tbody>
    </AppTable>

    <section v-if="selectedVisit" class="completion-panel surface" aria-labelledby="visit-result-title">
      <h2 id="visit-result-title">提交探访结果</h2>
      <form novalidate @submit.prevent="completeVisit">
        <label for="raw-staff-note">内部原始记录</label>
        <textarea id="raw-staff-note" v-model="rawStaffNote" rows="3" />
        <p class="field-hint">仅供社区内部处理，不进入家属或服务人员摘要。</p>

        <label for="confirmed-summary">对外确认摘要</label>
        <textarea id="confirmed-summary" v-model="confirmedSummary" rows="3" />

        <label for="visit-result">探访结果</label>
        <textarea id="visit-result" v-model="result" rows="3" />

        <p v-if="validationError" class="form-error" role="alert">{{ validationError }}</p>
        <div class="form-actions">
          <button class="primary-button" type="submit" :disabled="submitting">提交探访结果</button>
          <button class="secondary-button" type="button" @click="selectedVisit = null">取消</button>
        </div>
      </form>
    </section>
  </section>
</template>

<style scoped>
.page-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

.work-table-wrap {
  overflow-x: auto;
}

.completion-panel {
  max-width: 760px;
  padding: var(--space-5);
  margin-top: var(--space-5);
}

.completion-panel h2 {
  font: var(--text-title);
}

form {
  display: grid;
  gap: var(--space-2);
}

textarea {
  width: 100%;
  min-height: 84px;
  padding: var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  resize: vertical;
  transition:
    border-color var(--duration-fast) var(--ease-standard),
    box-shadow var(--duration-fast) var(--ease-standard);
}

textarea:hover {
  border-color: var(--line-strong);
}

textarea:focus-visible {
  outline: none;
  border-color: var(--action);
  box-shadow: 0 0 0 3px var(--action-soft);
}

.field-hint {
  margin-bottom: var(--space-3);
  color: var(--ink-muted);
  font-size: 13px;
}

.form-error {
  color: var(--emergency);
  font-weight: 700;
}

.form-actions {
  display: flex;
  gap: var(--space-3);
  margin-top: var(--space-2);
}
</style>
