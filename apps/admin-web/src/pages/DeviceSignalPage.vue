<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiClient } from '@/api/apiClient'
import type { ManagedDevice, SignalHistory } from '@/api/operations'
import { beijingInput, eventLabels, formatTime } from '@/api/operations'
import DeviceSimulator from '@/components/DeviceSimulator.vue'
import AppTable from '@/components/ui/AppTable.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import StatusNotice from '@/components/StatusNotice.vue'
const devices = ref<ManagedDevice[]>([])
const signals = ref<SignalHistory[]>([])
const deviceId = ref('')
const from = ref(beijingInput(new Date(Date.now() - 29 * 86400000)).slice(0, 10))
const to = ref(beijingInput(new Date()).slice(0, 10))
const signalType = ref('')
const source = ref('')
const loading = ref(false)
const error = ref('')
const message = ref('')
const target = ref<ManagedDevice | null>(null)
const reason = ref('')
const saving = ref(false)
const actionError = ref('')
const selected = computed(() => devices.value.find(d => d.deviceId === deviceId.value))
const signalNames: Record<string, string> = { SosButton: 'SOS 求助', NoWaterActivity: '无用水活动', DeviceOffline: '设备离线信号' }
const displayTime = (value: string | null) => value ? formatTime(value) : '暂无上报'
async function loadSignals() {
  signals.value = []
  if (!deviceId.value) return
  const params = new URLSearchParams({ from: from.value, to: to.value })
  if (signalType.value) params.set('signalType', signalType.value)
  if (source.value) params.set('isSimulation', source.value)
  signals.value = await apiClient.request<SignalHistory[]>('/api/v1/devices/' + deviceId.value + '/signals?' + params)
}
async function refresh() {
  loading.value = true
  error.value = ''
  try {
    devices.value = await apiClient.request<ManagedDevice[]>('/api/v1/devices')
    if (!devices.value.some(d => d.deviceId === deviceId.value)) deviceId.value = devices.value[0]?.deviceId ?? ''
    await loadSignals()
  } catch (e) { error.value = e instanceof Error ? e.message : '设备资料载入失败。' }
  finally { loading.value = false }
}
async function filterSignals() {
  loading.value = true
  error.value = ''
  try { await loadSignals() }
  catch (e) { error.value = e instanceof Error ? e.message : '信号历史载入失败。' }
  finally { loading.value = false }
}
function openToggle(device: ManagedDevice) { target.value = device; reason.value = ''; actionError.value = '' }
async function toggle() {
  if (!target.value || saving.value) return
  saving.value = true
  actionError.value = ''
  try {
    await apiClient.request('/api/v1/devices/' + target.value.deviceId + '/enabled', {
      method: 'PATCH', body: JSON.stringify({ enabled: !target.value.isEnabled, reason: reason.value, expectedVersion: target.value.version }),
    })
    message.value = target.value.isEnabled ? '设备已停用；历史记录保留，后续上报将被拒绝。' : '设备已启用，可以接收后续上报。'
    target.value = null
    await refresh()
  } catch (e) { actionError.value = e instanceof Error ? e.message : '设备状态修改失败。' }
  finally { saving.value = false }
}
onMounted(refresh)
</script>

<template>
  <section class="operations-page">
    <header class="ops-heading"><div><p class="page-kicker">设备管理</p><h1>设备台账与信号</h1><p>查看启停与上报记录。本系统未接入心跳监测，不能据此判断设备是否在线。</p></div><AppButton variant="secondary" :disabled="loading" @click="refresh">刷新设备</AppButton></header>
    <p v-if="message" class="ops-success" role="status">{{ message }}</p>
    <StatusNotice v-if="error" kind="error" :title="error" />
    <StatusNotice v-if="loading" kind="loading" title="正在载入设备记录" />
    <AppTable v-if="devices.length" min-width="980px"><thead><tr><th>设备</th><th>所属老人</th><th>启停</th><th>最近硬件上报</th><th>最近模拟上报</th><th>操作</th></tr></thead><tbody>
      <tr v-for="d in devices" :key="d.deviceId"><td>{{ d.displayName }}</td><td>{{ d.elderDisplayName }} · {{ d.areaCode }}</td><td>{{ d.isEnabled ? '已启用' : '已停用' }}</td><td>{{ displayTime(d.lastHardwareSignalAt) }}</td><td>{{ displayTime(d.lastSimulationSignalAt) }}</td>
        <td><button class="ops-link" type="button" @click="openToggle(d)">{{ d.isEnabled ? '停用' : '启用' }}</button></td></tr>
    </tbody></AppTable>
    <StatusNotice v-else-if="!loading && !error" kind="empty" title="暂无登记设备" />
    <h2>信号历史</h2>
    <form class="ops-filters" @submit.prevent="filterSignals">
      <label>查看设备<select v-model="deviceId" required><option v-for="d in devices" :key="d.deviceId" :value="d.deviceId">{{ d.displayName }}</option></select></label>
      <label>开始日期<input v-model="from" type="date" required /></label><label>结束日期<input v-model="to" type="date" required /></label>
      <label>信号类型<select v-model="signalType"><option value="">全部信号</option><option v-for="(label, key) in signalNames" :key="key" :value="key">{{ label }}</option></select></label>
      <label>信号来源<select v-model="source"><option value="">全部来源</option><option value="false">硬件上报</option><option value="true">模拟上报</option></select></label>
      <AppButton type="submit" :disabled="loading || !deviceId">查询记录</AppButton>
    </form>
    <p class="ops-muted">按服务端接收时间查询，日期为北京时间，最多 90 天。“设备离线信号”是上报类型，不代表实时在线状态。</p>
    <AppTable v-if="signals.length" min-width="800px"><thead><tr><th>接收时间</th><th>类型</th><th>来源</th><th>处理进度</th><th>关联记录</th></tr></thead><tbody>
      <tr v-for="s in signals" :key="s.signalId"><td>{{ formatTime(s.receivedAt) }}</td><td>{{ signalNames[s.signalType] }}</td><td>{{ s.isSimulation ? '模拟上报' : '硬件上报' }}</td><td>{{ eventLabels[s.careEventStatus] }}</td><td><RouterLink :to="'/care-events/' + s.careEventId">查看照料事件</RouterLink></td></tr>
    </tbody></AppTable>
    <p v-else-if="!loading">所选范围内暂无上报记录。</p>
    <DeviceSimulator v-if="selected" :key="selected.deviceId" :device-id="selected.deviceId" :disabled="!selected.isEnabled" @sent="refresh" />
    <AppModal :open="target !== null" :title="target?.isEnabled ? '停用设备' : '启用设备'" :persistent="saving" @close="target = null">
      <form class="ops-form" @submit.prevent="toggle"><p>{{ target?.displayName }} · {{ target?.elderDisplayName }}</p>
        <p v-if="target?.isEnabled">停用后将拒绝此设备的后续上报，历史信号和照料事件不受影响。</p>
        <label>启停原因<textarea v-model="reason" required maxlength="512" rows="3"></textarea></label>
        <p v-if="actionError" role="alert" class="ops-error">{{ actionError }}</p>
        <AppButton type="submit" :disabled="saving || !reason.trim()">{{ saving ? '正在保存' : '确认修改' }}</AppButton>
      </form>
    </AppModal>
  </section>
</template>
