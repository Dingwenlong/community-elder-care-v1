<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { DemoSummary, ReadinessResponse } from '@/api/contracts'

const readiness = ref<ReadinessResponse | null>(null)
const summary = ref<DemoSummary | null>(null)
const confirmation = ref('')
const submitting = ref(false)
const message = ref('')
const errorMessage = ref('')
const canReset = computed(() => confirmation.value === 'RESET-20' && !submitting.value)

async function refresh() {
  ;[readiness.value, summary.value] = await Promise.all([
    apiClient.request<ReadinessResponse>('/health/ready'),
    apiClient.request<DemoSummary>('/api/v1/reports/demo-summary'),
  ])
}

async function resetDemo() {
  if (!canReset.value) return
  if (!window.confirm('再次确认：恢复初始数据会清除当前业务记录，是否继续？')) return
  submitting.value = true
  message.value = ''
  errorMessage.value = ''
  try {
    const result = await apiClient.request<{ elderCount: number }>('/api/v1/demo/reset', {
      method: 'POST',
      headers: { 'X-Confirm-Demo-Reset': 'RESET-20' },
    })
    await refresh()
    confirmation.value = ''
    message.value = `重置完成：${result.elderCount} 份老人档案`
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '初始数据恢复失败。'
  } finally {
    submitting.value = false
  }
}

async function loadScenario() {
  if (submitting.value || !window.confirm('加载近期虚构任务与设备信号用于运营演示，不删除当前记录。是否继续？')) return
  submitting.value = true
  message.value = ''
  errorMessage.value = ''
  try {
    const result = await apiClient.request<{ alreadyLoaded: boolean }>('/api/v1/demo/operations-scenario', {
      method: 'POST', headers: { 'X-Confirm-Operations-Scenario': 'LOAD-OPERATIONS' },
    })
    await refresh()
    message.value = result.alreadyLoaded ? '运营演示场景已存在，未重复添加。' : '运营演示场景已加载，可前往人员与任务、设备信号和运营报告查看。'
  } catch (e) { errorMessage.value = e instanceof Error ? e.message : '场景加载失败。' }
  finally { submitting.value = false }
}

onMounted(async () => {
  try { await refresh() } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '系统状态载入失败。'
  }
})
</script>

<template>
  <section class="settings-page">
    <header class="page-header"><p class="page-kicker">系统设置</p><h1>系统运行状态</h1></header>
    <p v-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
    <section class="readiness" aria-labelledby="readiness-title">
      <h2 id="readiness-title">就绪组件</h2>
      <div v-if="readiness" class="component-list">
        <article v-for="component in readiness.components" :key="component.name">
          <strong>{{ component.name }}</strong>
          <span :class="`status status--${component.status}`">{{ component.status }}</span>
          <p>{{ component.detail }}</p>
        </article>
      </div>
    </section>
    <section class="ops-panel"><h2>运营演示场景</h2><p>添加近期的虚构照料记录、多人任务和模拟设备信号。重复加载不会覆盖已有记录；恢复初始数据后可重新加载。</p>
      <button type="button" :disabled="submitting" @click="loadScenario">加载运营演示场景</button>
    </section>
    <p v-if="message" class="success" role="status">{{ message }}</p>
    <section class="reset-section" aria-labelledby="reset-title">
      <h2 id="reset-title">恢复初始数据</h2>
      <p>清除当前业务记录，并恢复 20 份初始老人档案。当前档案数：{{ summary?.elderCount ?? '—' }}。</p>
      <label for="reset-confirmation">输入 RESET-20</label>
      <input id="reset-confirmation" v-model="confirmation" autocomplete="off" />
      <button type="button" :disabled="!canReset" @click="resetDemo">
        {{ submitting ? '正在恢复' : '恢复 20 人初始数据' }}
      </button>
    </section>
  </section>
</template>

<style scoped>
.settings-page { display: grid; gap: var(--space-5); }
.page-header { padding-bottom: var(--space-4); border-bottom: 1px solid var(--line); }
.page-kicker { margin: 0 0 var(--space-1); color: var(--action); font-size: 13px; font-weight: 700; }
h1 { margin: 0; color: var(--ink-strong); font-family: var(--font-display); font-size: 32px; }
.readiness, .reset-section { padding: var(--space-5); border: 1px solid var(--line); background: var(--surface); }
h2 { margin-top: 0; }
.component-list { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); border-top: 1px solid var(--line); border-left: 1px solid var(--line); }
article { min-width: 0; padding: var(--space-3); border-right: 1px solid var(--line); border-bottom: 1px solid var(--line); }
article strong, article span { display: block; }
article p { margin-bottom: 0; color: var(--ink-muted); font-size: 13px; }
.status { margin-top: var(--space-2); font-weight: 700; }
.status--ready { color: var(--success); } .status--degraded { color: var(--warning); } .status--unavailable { color: var(--emergency); }
.reset-section { max-width: 760px; border-left: 4px solid var(--emergency); }
.reset-section label { display: block; margin: var(--space-4) 0 var(--space-1); font-weight: 700; }
input, button { min-height: 44px; font: inherit; }
input { width: min(100%, 320px); padding: 0 var(--space-3); border: 1px solid var(--line-strong); }
button { display: block; margin-top: var(--space-3); padding: 0 var(--space-4); border: 1px solid var(--emergency); color: white; background: var(--emergency); font-weight: 700; }
button:disabled { border-color: var(--line-strong); color: var(--ink-muted); background: var(--surface-muted); }
.success { color: var(--success); font-weight: 700; } .error { color: var(--emergency); }
@media (max-width: 900px) { .component-list { grid-template-columns: 1fr; } }
</style>
