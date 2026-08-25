<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { DemoSummary } from '@/api/contracts'

const summary = ref<DemoSummary | null>(null)
const errorMessage = ref('')

async function load() {
  try {
    summary.value = await apiClient.request<DemoSummary>('/api/v1/reports/demo-summary')
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '运行报告载入失败。'
  }
}

onMounted(load)
</script>

<template>
  <section class="report-page">
    <header class="page-header">
      <div>
        <p class="page-kicker">报告与审计</p>
        <h1>运行概览</h1>
      </div>
      <RouterLink to="/audit">查看审计记录</RouterLink>
    </header>
    <p class="demo-statement">当前数据</p>
    <p v-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
    <div v-else-if="summary" class="metrics" aria-label="运行指标">
      <article><span>老人档案</span><strong>{{ summary.elderCount }}</strong></article>
      <article><span>未结事件</span><strong>{{ summary.openEventCount }}</strong></article>
      <article><span>已完成探访</span><strong>{{ summary.completedVisitCount }}</strong></article>
      <article><span>进行中工单</span><strong>{{ summary.activeServiceOrderCount }}</strong></article>
      <article><span>模拟通知</span><strong>{{ summary.simulationAttemptCount }}</strong></article>
      <article><span>设备信号</span><strong>{{ summary.deviceSignalCount }}</strong></article>
      <article><span>已确认 AI 记忆</span><strong>{{ summary.confirmedMemoryCount }}</strong></article>
    </div>
  </section>
</template>

<style scoped>
.report-page { display: grid; gap: var(--space-5); }
.page-header { display: flex; align-items: end; justify-content: space-between; gap: var(--space-4); padding-bottom: var(--space-4); border-bottom: 1px solid var(--line); }
.page-kicker { margin: 0 0 var(--space-1); color: var(--action); font-size: 13px; font-weight: 700; }
h1 { margin: 0; color: var(--ink-strong); font-family: var(--font-display); font-size: 32px; }
.page-header a { color: var(--action); font-weight: 700; }
.demo-statement { width: fit-content; padding-left: var(--space-3); border-left: 3px solid var(--warning); color: var(--warning); font-weight: 700; }
.metrics { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); border-top: 1px solid var(--line); border-left: 1px solid var(--line); }
article { display: grid; gap: var(--space-2); min-height: 128px; padding: var(--space-5); border-right: 1px solid var(--line); border-bottom: 1px solid var(--line); background: var(--surface); }
article span { color: var(--ink-muted); }
article strong { color: var(--navy); font-family: var(--font-numeric); font-size: 34px; }
.error { color: var(--emergency); }
@media (max-width: 760px) { .metrics { grid-template-columns: 1fr; } .page-header { align-items: start; flex-direction: column; } }
</style>
