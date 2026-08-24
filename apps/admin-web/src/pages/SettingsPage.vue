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
  if (!window.confirm('再次确认：恢复演示数据会清除当前演示过程，是否继续？')) return
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
    errorMessage.value = error instanceof ApiError ? error.message : '演示数据重置失败。'
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  try { await refresh() } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '系统状态载入失败。'
  }
})
</script>

<template>
  <section class="settings-page">
    <header class="page-header"><p class="page-kicker">系统设置</p><h1>演示运行状态</h1></header>
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
    <section class="reset-section" aria-labelledby="reset-title">
      <h2 id="reset-title">恢复演示数据</h2>
      <p>只清理本系统已知的演示记录，并恢复固定的 20 份虚构档案。当前档案数：{{ summary?.elderCount ?? '—' }}。</p>
      <label for="reset-confirmation">输入 RESET-20</label>
      <input id="reset-confirmation" v-model="confirmation" autocomplete="off" />
      <button type="button" :disabled="!canReset" @click="resetDemo">
        {{ submitting ? '正在重置' : '恢复 20 人演示数据' }}
      </button>
      <p v-if="message" class="success" role="status">{{ message }}</p>
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
