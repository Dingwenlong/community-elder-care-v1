<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ElderSummary } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'

const attentionLevel = ref('')
const elders = ref<ElderSummary[]>([])
const loading = ref(true)
const errorMessage = ref('')
let requestSequence = 0

const attentionLabels: Record<ElderSummary['attentionLevel'], string> = {
  Routine: '常规关注',
  Priority: '重点关注',
  HighAttention: '高关注',
}

async function loadElders() {
  const sequence = ++requestSequence
  loading.value = true
  errorMessage.value = ''
  try {
    const query = attentionLevel.value
      ? `?attentionLevel=${encodeURIComponent(attentionLevel.value)}`
      : ''
    const response = await apiClient.request<ElderSummary[]>(`/api/v1/elders${query}`)
    if (sequence === requestSequence) elders.value = response
  } catch (error) {
    if (sequence === requestSequence) {
      errorMessage.value = error instanceof ApiError ? error.message : '请求未完成，请稍后重试。'
      elders.value = []
    }
  } finally {
    if (sequence === requestSequence) loading.value = false
  }
}

function displayTime(value?: string) {
  if (!value) return '未载入'
  return new Intl.DateTimeFormat('zh-CN', {
    month: 'numeric',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

watch(attentionLevel, loadElders)
onMounted(loadElders)
</script>

<template>
  <header class="page-heading elder-list-heading">
    <div>
      <h1>老人档案</h1>
      <p>仅显示当前社区工作范围内的演示档案。</p>
    </div>
    <RouterLink class="secondary-button" to="/dashboard">返回工作台</RouterLink>
  </header>

  <section class="surface filter-bar" aria-label="档案筛选">
    <label for="attention-filter">关注等级</label>
    <select id="attention-filter" v-model="attentionLevel">
      <option value="">全部等级</option>
      <option value="HighAttention">高关注</option>
      <option value="Priority">重点关注</option>
      <option value="Routine">常规关注</option>
    </select>
    <span>共 {{ elders.length }} 份当前结果</span>
  </section>

  <StatusNotice v-if="loading" kind="loading" title="正在载入老人档案" />
  <StatusNotice v-else-if="errorMessage" kind="error" title="档案载入失败" :message="errorMessage">
    <button class="secondary-button retry-button" type="button" @click="loadElders">重新载入</button>
  </StatusNotice>
  <StatusNotice
    v-else-if="elders.length === 0"
    kind="empty"
    title="当前筛选条件下没有老人档案"
    message="可调整关注等级后再次查看。"
  />

  <div v-else class="surface table-wrap">
    <table>
      <caption class="visually-hidden">
        社区老人档案列表
      </caption>
      <thead>
        <tr>
          <th scope="col">姓名</th>
          <th scope="col">年龄</th>
          <th scope="col">关注等级</th>
          <th scope="col">最新状态</th>
          <th scope="col">下次探访</th>
          <th scope="col">当前事件</th>
          <th scope="col">操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="elder in elders" :key="elder.id">
          <td>
            <strong>{{ elder.demoDisplayName }}</strong>
            <small>{{ elder.areaCode }}</small>
          </td>
          <td aria-label="年龄未在列表接口提供">—</td>
          <td>
            <span class="attention-label" :data-level="elder.attentionLevel">
              {{ attentionLabels[elder.attentionLevel] }}
            </span>
          </td>
          <td>{{ elder.latestStatus ?? `下次平安确认 ${displayTime(elder.nextCheckInDueAt)}` }}</td>
          <td>{{ elder.nextVisit ?? '未载入' }}</td>
          <td>{{ elder.currentOpenEvent ?? '未载入' }}</td>
          <td><RouterLink :to="`/elders/${elder.id}`">查看档案</RouterLink></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.elder-list-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
}

.filter-bar {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
  padding: var(--space-4);
}

.filter-bar label {
  color: var(--ink-strong);
  font-weight: 700;
}

.filter-bar select {
  min-width: 180px;
  padding: 0 var(--space-3);
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  background: var(--surface);
}

.filter-bar span {
  margin-left: auto;
  color: var(--ink-muted);
  font-size: 14px;
}

.retry-button {
  margin-top: var(--space-3);
}

.table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  min-width: 940px;
  border-collapse: collapse;
}

th,
td {
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
  text-align: left;
  vertical-align: middle;
}

th {
  color: var(--ink-strong);
  background: var(--surface-muted);
  font-size: 14px;
  font-weight: 700;
}

tbody tr:last-child td {
  border-bottom: 0;
}

tbody tr:hover {
  background: #f8fafc;
}

td strong,
td small {
  display: block;
}

td small {
  margin-top: 2px;
  color: var(--ink-muted);
}

.attention-label {
  display: inline-block;
  padding: 2px 8px;
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  font-size: 14px;
  white-space: nowrap;
}

.attention-label[data-level='HighAttention'] {
  border-color: #e3a2a2;
  color: var(--emergency);
  background: var(--emergency-soft);
}

.attention-label[data-level='Priority'] {
  border-color: #e4c28d;
  color: var(--warning);
  background: var(--warning-soft);
}

@media (max-width: 720px) {
  .elder-list-heading,
  .filter-bar {
    align-items: stretch;
    flex-direction: column;
  }

  .filter-bar span {
    margin-left: 0;
  }
}
</style>
