<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiClient } from '@/api/apiClient'
import type { OperationsReport, ReportSummary } from '@/api/operations'
import { beijingInput, formatTime } from '@/api/operations'
import AppTable from '@/components/ui/AppTable.vue'
import AppButton from '@/components/ui/AppButton.vue'
import StatusNotice from '@/components/StatusNotice.vue'
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
const report = ref<OperationsReport | null>(null)
const from = ref(beijingInput(new Date(Date.now() - 29 * 86400000)).slice(0, 10))
const to = ref(beijingInput(new Date()).slice(0, 10))
const areaCode = ref('')
const loading = ref(false)
const exporting = ref(false)
const error = ref('')
const labels: Array<[keyof ReportSummary, string]> = [
  ['newEventCount', '新增事件'], ['closedEventCount', '结案事件'], ['completedVisitCount', '完成探访'],
  ['completedOrderCount', '完成工单'], ['completedFollowUpCount', '完成回访'], ['visitedElderCount', '探访覆盖人数'],
]
const chartMax = computed(() => Math.max(1, ...(report.value?.daily.map(d => Math.max(d.newEventCount, d.closedEventCount)) ?? [])))
const barWidth = computed(() => 840 / (report.value?.daily.length || 1))
const exportParams = ref('')
function params() { const p = new URLSearchParams({ from: from.value, to: to.value }); if (areaCode.value.trim()) p.set('areaCode', areaCode.value.trim()); return p.toString() }
async function load() {
  loading.value = true
  error.value = ''
  report.value = null
  const query = params()
  try { report.value = await apiClient.request<OperationsReport>('/api/v1/reports/operations?' + query); exportParams.value = query }
  catch (e) { error.value = e instanceof Error ? e.message : '报告载入失败。' }
  finally { loading.value = false }
}
function preset(days: number | 'month') {
  const today = beijingInput(new Date()).slice(0, 10)
  to.value = today
  from.value = days === 'month' ? today.slice(0, 8) + '01' : beijingInput(new Date(Date.now() - (days - 1) * 86400000)).slice(0, 10)
  void load()
}
async function download(section: 'summary' | 'daily' | 'personnel') {
  if (!report.value || exporting.value) return
  exporting.value = true
  error.value = ''
  try {
    const blob = await apiClient.download('/api/v1/reports/operations.csv?' + exportParams.value + '&section=' + section)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'operations-' + section + '-' + report.value.from + '.csv'
    document.body.append(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
  } catch (e) { error.value = e instanceof Error ? e.message : '导出失败。' }
  finally { exporting.value = false }
}
function printReport() { window.print() }
onMounted(load)
</script>

<template>
  <section class="operations-page report-print">
    <header class="ops-heading"><div><p class="page-kicker">运营统计</p><h1>社区照料报告</h1><p>统计任务与处理记录，不代表救援效果或政策达标率。</p></div>
      <RouterLink v-if="auth.role === 'Administrator'" class="no-print" to="/audit">查看审计记录</RouterLink></header>
    <div class="ops-actions no-print"><AppButton variant="secondary" :disabled="loading" @click="preset(7)">最近 7 天</AppButton><AppButton variant="secondary" :disabled="loading" @click="preset(30)">最近 30 天</AppButton><AppButton variant="secondary" :disabled="loading" @click="preset('month')">本月</AppButton></div>
    <form class="ops-filters no-print" @submit.prevent="load">
      <label>开始日期<input v-model="from" type="date" required /></label><label>结束日期<input v-model="to" type="date" required /></label>
      <label v-if="auth.role === 'Administrator'">片区代码<input v-model="areaCode" placeholder="留空为全部片区，如 A01" maxlength="32" /></label>
      <AppButton type="submit" :disabled="loading">查询报告</AppButton>
    </form>
    <p class="ops-muted no-print">按北京时间统计，最多查询 90 天。修改筛选后请查询；导出和打印以已显示的查询范围为准。</p>
    <StatusNotice v-if="loading" kind="loading" title="正在生成照料报告" />
    <p v-if="error" class="ops-error no-print" role="alert">{{ error }}</p>
    <template v-if="report">
      <p class="report-range">{{ report.from }} 至 {{ report.to }} · {{ report.areaLabel }} · 北京时间<br /><span class="ops-muted">生成时间：{{ formatTime(report.generatedAt) }}</span></p>
      <div class="ops-actions no-print"><AppButton variant="secondary" :disabled="exporting" @click="download('summary')">导出汇总 CSV</AppButton><AppButton variant="secondary" :disabled="exporting" @click="download('daily')">导出每日 CSV</AppButton><AppButton variant="secondary" :disabled="exporting" @click="download('personnel')">导出人员 CSV</AppButton><AppButton @click="printReport">打印报告 / 另存 PDF</AppButton></div>
      <div class="report-metrics"><article v-for="[key, label] in labels" :key="key"><span>{{ label }}</span><strong>{{ report.summary[key] }}</strong></article></div>
      <section class="report-current"><p>平均首次接单时长：<strong>{{ report.summary.averageAcceptanceMinutes === null ? '暂无数据' : report.summary.averageAcceptanceMinutes + ' 分钟' }}</strong></p>
        <p>当前未结任务：<strong>{{ report.summary.currentOpenTaskCount }}</strong> 项 · 当前逾期任务：<strong class="ops-error">{{ report.summary.currentOverdueTaskCount }}</strong> 项</p>
        <p class="ops-muted">“当前”指标反映生成报告时的全部待办，不受上方日期范围限制。</p></section>
      <h2>每日变化</h2>
      <p class="ops-muted">蓝色：新增事件 · 绿色：结案事件；图下方提供完整数值。</p>
      <svg class="report-chart" viewBox="0 0 900 210" role="img" aria-label="每日新增和结案事件柱状图">
        <line x1="40" x2="880" y1="170" y2="170" stroke="currentColor" />
        <g v-for="(day, i) in report.daily" :key="day.date">
          <title>{{ day.date }}：新增 {{ day.newEventCount }}，结案 {{ day.closedEventCount }}</title>
          <rect :x="40 + i * barWidth" :y="170 - day.newEventCount / chartMax * 140" :width="barWidth * 0.38" :height="day.newEventCount / chartMax * 140" fill="var(--action)" />
          <rect :x="40 + i * barWidth + barWidth * 0.4" :y="170 - day.closedEventCount / chartMax * 140" :width="barWidth * 0.38" :height="day.closedEventCount / chartMax * 140" fill="var(--success)" />
        </g>
        <text x="40" y="195">{{ report.from }}</text><text x="880" y="195" text-anchor="end">{{ report.to }}</text>
      </svg>
      <AppTable min-width="700px"><thead><tr><th>日期</th><th>新增事件</th><th>结案事件</th><th>完成探访</th><th>完成工单</th><th>完成回访</th></tr></thead><tbody><tr v-for="day in report.daily" :key="day.date">
        <td>{{ day.date }}</td><td>{{ day.newEventCount }}</td><td>{{ day.closedEventCount }}</td><td>{{ day.completedVisitCount }}</td><td>{{ day.completedOrderCount }}</td><td>{{ day.completedFollowUpCount }}</td>
      </tr></tbody></AppTable>
      <h2>人员任务统计</h2>
      <AppTable min-width="700px"><thead><tr><th>人员 / 片区</th><th>完成探访</th><th>完成工单</th><th>完成回访</th><th>当前待办</th><th>当前逾期</th></tr></thead><tbody><tr v-for="person in report.personnel" :key="person.userId">
        <td>{{ person.displayName }} · {{ person.areaCode }}</td><td>{{ person.completedVisitCount }}</td><td>{{ person.completedOrderCount }}</td><td>{{ person.completedFollowUpCount }}</td><td>{{ person.pendingCount }}</td><td>{{ person.overdueCount }}</td>
      </tr></tbody></AppTable>
      <p class="ops-muted">口径：新增按创建时间，结案与完成按完成时间；探访覆盖人数去重；接单时长只统计期间首次接单的事件。缺少截止时间的旧工单不计逾期，已取消任务不计待办和完成。</p>
    </template>
  </section>
</template>

<style scoped>
.report-metrics { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); border-top: 1px solid var(--line); border-left: 1px solid var(--line); }
.report-metrics article { padding: var(--space-4); border-right: 1px solid var(--line); border-bottom: 1px solid var(--line); background: var(--surface); }
.report-metrics span, .report-metrics strong { display: block; }
.report-metrics strong { margin-top: var(--space-2); font: var(--text-display); }
.report-current { padding: var(--space-4); background: var(--surface); border-left: 3px solid var(--action); }
.report-chart { width: 100%; height: auto; color: var(--ink-muted); font-size: 13px; background: var(--surface); }
.report-range { font-weight: 600; }
@media (max-width: 600px) { .report-metrics { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
</style>
<style>
@media print {
  @page { size: A4; margin: 14mm; }
  .community-sidebar, .workspace-header, .no-print { display: none !important; }
  .community-workspace { margin: 0 !important; }
  .workspace-main { padding: 0 !important; }
  .report-print { display: block; color: #000; }
  .report-print .app-table-wrap { overflow: visible !important; }
  .report-print table { min-width: 0 !important; width: 100% !important; font-size: 10px; }
  .report-print th, .report-print td { padding: 5px !important; white-space: normal; }
  .report-print th { position: static !important; }
  .report-print thead { display: table-header-group; }
  .report-print tr, .report-chart, .report-metrics, .report-current { break-inside: avoid; }
  .report-print h2 { break-after: avoid; margin-top: 16px; }
  .report-print .ops-muted { color: #444; }
}
</style>
