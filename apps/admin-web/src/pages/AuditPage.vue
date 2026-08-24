<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { AuditEntry } from '@/api/contracts'

const entries = ref<AuditEntry[]>([])
const entityType = ref('')
const entityId = ref('')
const from = ref('')
const to = ref('')
const errorMessage = ref('')
const loading = ref(false)

async function load() {
  loading.value = true
  errorMessage.value = ''
  const query = new URLSearchParams()
  if (entityType.value.trim()) query.set('entityType', entityType.value.trim())
  if (entityId.value.trim()) query.set('entityId', entityId.value.trim())
  if (from.value) query.set('from', new Date(from.value).toISOString())
  if (to.value) query.set('to', new Date(to.value).toISOString())
  try {
    entries.value = await apiClient.request<AuditEntry[]>(`/api/v1/audit?${query}`)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '审计记录载入失败。'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="audit-page">
    <header class="page-header">
      <div>
        <p class="page-kicker">报告与审计</p>
        <h1>审计记录</h1>
      </div>
      <RouterLink to="/reports">返回报告</RouterLink>
    </header>
    <form class="filters" @submit.prevent="load">
      <label>实体类型<input v-model="entityType" /></label>
      <label>实体 ID<input v-model="entityId" /></label>
      <label>开始时间<input v-model="from" type="datetime-local" /></label>
      <label>结束时间<input v-model="to" type="datetime-local" /></label>
      <button type="submit" :disabled="loading">{{ loading ? '查询中' : '查询' }}</button>
    </form>
    <p v-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
    <div v-else class="audit-table-wrap">
      <table>
        <thead><tr><th>时间</th><th>操作者</th><th>动作</th><th>实体</th><th>状态</th><th>原因</th></tr></thead>
        <tbody>
          <tr v-for="entry in entries" :key="entry.id">
            <td>{{ new Date(entry.occurredAt).toLocaleString('zh-CN') }}</td>
            <td>{{ entry.actorKind }}</td>
            <td><strong>{{ entry.action }}</strong></td>
            <td>{{ entry.entityType }}</td>
            <td>{{ entry.beforeStatus && entry.afterStatus ? `${entry.beforeStatus} → ${entry.afterStatus}` : '—' }}</td>
            <td>{{ entry.reason }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="!entries.length && !loading" class="empty">当前筛选条件下没有审计记录。</p>
    </div>
  </section>
</template>

<style scoped>
.audit-page { display: grid; gap: var(--space-5); }
.page-header { display: flex; align-items: end; justify-content: space-between; gap: var(--space-4); padding-bottom: var(--space-4); border-bottom: 1px solid var(--line); }
.page-kicker { margin: 0 0 var(--space-1); color: var(--action); font-size: 13px; font-weight: 700; }
h1 { margin: 0; color: var(--ink-strong); font-family: var(--font-display); font-size: 32px; }
.page-header a { color: var(--action); font-weight: 700; }
.filters { display: grid; grid-template-columns: repeat(4, minmax(140px, 1fr)) auto; gap: var(--space-3); align-items: end; padding: var(--space-4); border: 1px solid var(--line); background: var(--surface); }
label { display: grid; gap: var(--space-1); color: var(--ink-muted); font-size: 13px; font-weight: 700; }
input, button { min-height: 44px; border: 1px solid var(--line-strong); font: inherit; }
input { padding: 0 var(--space-3); color: var(--ink); background: white; }
button { padding: 0 var(--space-4); border-color: var(--navy); color: white; background: var(--navy); font-weight: 700; }
.audit-table-wrap { overflow-x: auto; border: 1px solid var(--line); background: var(--surface); }
table { width: 100%; border-collapse: collapse; }
th, td { padding: var(--space-3); border-bottom: 1px solid var(--line); text-align: left; white-space: nowrap; }
th { color: var(--ink-muted); font-size: 13px; }
.error { color: var(--emergency); }
.empty { padding: var(--space-5); color: var(--ink-muted); }
@media (max-width: 900px) { .filters { grid-template-columns: 1fr 1fr; } }
</style>
