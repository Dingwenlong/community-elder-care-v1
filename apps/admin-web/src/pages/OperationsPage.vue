<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiClient } from '@/api/apiClient'
import type { OperationsTask, Personnel, TaskReassignment } from '@/api/operations'
import { formatTime, taskLabels, taskRoutes, workLabels } from '@/api/operations'
import AppTable from '@/components/ui/AppTable.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import StatusNotice from '@/components/StatusNotice.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const people = ref<Personnel[]>([])
const tasks = ref<OperationsTask[]>([])
const loading = ref(true)
const error = ref('')
const message = ref('')
const typeFilter = ref('')
const personFilter = ref('')
const statusFilter = ref('')
const overdueOnly = ref(false)
const selected = ref<OperationsTask | null>(null)
const mode = ref<'reassign' | 'followUp'>('reassign')
const assignee = ref('')
const reason = ref('')
const saving = ref(false)
const actionError = ref('')
const history = ref<TaskReassignment[] | null>(null)
const historyTask = ref('')
const filtered = computed(() => tasks.value.filter(t => (!typeFilter.value || t.taskType === typeFilter.value)
  && (!personFilter.value || t.assignedUserId === personFilter.value)
  && (!statusFilter.value || t.status === statusFilter.value) && (!overdueOnly.value || t.isOverdue)))
const candidates = computed(() => people.value.filter(p => selected.value && p.areaCode === selected.value.areaCode
  && p.userId !== selected.value.assignedUserId
  && p.role === (selected.value.taskType === 'ServiceOrder' ? 'ServiceWorker' : 'CommunityStaff')))
const name = (id: string) => people.value.find(p => p.userId === id)?.displayName ?? '原工作人员'

async function load() {
  loading.value = true
  error.value = ''
  try {
    const result = await Promise.all([
      apiClient.request<Personnel[]>('/api/v1/operations/personnel'),
      apiClient.request<OperationsTask[]>('/api/v1/operations/tasks'),
    ])
    people.value = result[0]
    tasks.value = result[1]
  } catch (e) { error.value = e instanceof Error ? e.message : '任务载入失败。' }
  finally { loading.value = false }
}
function open(task: OperationsTask, action: 'reassign' | 'followUp') {
  selected.value = task
  mode.value = action
  assignee.value = ''
  reason.value = ''
  actionError.value = ''
}
async function submit() {
  if (!selected.value || saving.value) return
  saving.value = true
  actionError.value = ''
  try {
    const t = selected.value
    const body = mode.value === 'reassign'
      ? { assignedUserId: assignee.value, reason: reason.value, expectedVersion: t.version }
      : { result: reason.value }
    const action = mode.value === 'reassign' ? 'reassign' : 'complete'
    await apiClient.request('/api/v1/' + taskRoutes[t.taskType] + '/' + t.taskId + '/' + action,
      { method: 'POST', body: JSON.stringify(body) })
    selected.value = null
    history.value = null
    message.value = mode.value === 'reassign' ? '任务已转派，原工作人员不能再操作此任务。' : '回访结果已保存。'
    await load()
  } catch (e) { actionError.value = e instanceof Error ? e.message : '操作未完成。' }
  finally { saving.value = false }
}
async function showHistory(task: OperationsTask) {
  error.value = ''
  try {
    history.value = await apiClient.request<TaskReassignment[]>('/api/v1/operations/tasks/' + task.taskId + '/reassignments')
    historyTask.value = task.elderDisplayName + '的' + taskLabels[task.taskType]
  } catch (e) { error.value = e instanceof Error ? e.message : '转派记录载入失败。' }
}
onMounted(load)
</script>

<template>
  <section class="operations-page">
    <header class="ops-heading"><div><p class="page-kicker">社区运营</p><h1>人员与任务</h1>
      <p>查看人员任务量，安排照料工作，跟进未完成事项。</p></div>
      <AppButton variant="secondary" :disabled="loading" @click="load">刷新</AppButton></header>
    <p v-if="message" class="ops-success" role="status">{{ message }}</p>
    <StatusNotice v-if="error" kind="error" :title="error" />
    <StatusNotice v-if="loading" kind="loading" title="正在载入人员和任务" />
    <template v-else>
      <h2>人员任务量</h2>
      <div class="personnel-grid"><article v-for="person in people" :key="person.userId" class="personnel-item">
        <h3>{{ person.displayName }}</h3><p>{{ person.role === 'CommunityStaff' ? '社区人员' : '服务人员' }} · {{ person.areaCode }}</p>
        <p><strong>{{ person.pendingCount }}</strong> 项待办 <span :class="{ 'ops-error': person.overdueCount > 0 }"> · {{ person.overdueCount }} 项逾期</span></p>
        <button class="ops-link" type="button" @click="personFilter = person.userId">查看此人任务</button>
      </article></div>
      <h2>照料任务</h2>
      <div class="ops-filters">
        <label>任务类型<select v-model="typeFilter"><option value="">全部类型</option><option v-for="(label, key) in taskLabels" :key="key" :value="key">{{ label }}</option></select></label>
        <label>负责人<select v-model="personFilter"><option value="">全部人员</option><option v-for="p in people" :key="p.userId" :value="p.userId">{{ p.displayName }}</option></select></label>
        <label>任务状态<select v-model="statusFilter"><option value="">全部状态</option><option v-for="(label, key) in workLabels" :key="key" :value="key">{{ label }}</option></select></label>
        <label class="ops-check"><input v-model="overdueOnly" type="checkbox" />仅看逾期</label>
      </div>
      <p class="ops-muted">共 {{ filtered.length }} 项；逾期仅提示跟进，不会自动升级事件或结案。</p>
      <StatusNotice v-if="!filtered.length" kind="empty" title="没有符合条件的任务" illustration="care-work" />
      <AppTable v-else min-width="940px"><thead><tr><th>老人 / 类型</th><th>负责人</th><th>截止时间（北京时间）</th><th>状态</th><th>操作</th></tr></thead>
        <tbody><tr v-for="task in filtered" :key="task.taskId" :data-task-id="task.taskId"><td>{{ task.elderDisplayName }}<br /><span class="ops-muted">{{ taskLabels[task.taskType] }}{{ task.isMandatory ? ' · 必须完成' : '' }}</span></td>
          <td>{{ task.assignedDisplayName }}</td><td>{{ formatTime(task.dueAt) }}<strong v-if="task.isOverdue" class="ops-error"> · 已逾期</strong></td><td>{{ workLabels[task.status] }}</td>
          <td><div class="ops-actions"><RouterLink :to="'/care-events/' + task.careEventId">查看事件</RouterLink>
            <button v-if="auth.role === 'CommunityStaff' && task.eventOwnerUserId === auth.userId && task.status === 'Assigned'" type="button" class="ops-link" @click="open(task, 'reassign')">转派</button>
            <RouterLink v-if="task.taskType === 'Visit' && task.assignedUserId === auth.userId && (task.status === 'Assigned' || task.status === 'InProgress')" to="/visits">处理探访</RouterLink>
            <button v-if="task.taskType === 'FollowUp' && task.assignedUserId === auth.userId && task.status === 'Assigned'" type="button" class="ops-link" @click="open(task, 'followUp')">完成回访</button>
            <button type="button" class="ops-link" @click="showHistory(task)">转派记录</button></div></td>
        </tr></tbody></AppTable>
      <section v-if="history !== null" class="ops-panel"><h2>{{ historyTask }} · 转派记录</h2>
        <p v-if="!history.length">尚无转派记录。</p><ol v-else><li v-for="item in history" :key="item.id">
          {{ name(item.fromUserId) }} → {{ name(item.toUserId) }} · {{ formatTime(item.occurredAt) }}<br />
          {{ item.reason }}（操作人：{{ name(item.actorUserId) }}）
        </li></ol></section>
    </template>
    <AppModal :open="selected !== null" :title="mode === 'reassign' ? '转派未开始任务' : '填写回访结果'" :persistent="saving" @close="selected = null">
      <form class="ops-form" @submit.prevent="submit"><p>{{ selected?.elderDisplayName }} · {{ selected ? taskLabels[selected.taskType] : '' }}</p>
        <label v-if="mode === 'reassign'">新负责人<select v-model="assignee" required><option disabled value="">请选择</option><option v-for="p in candidates" :key="p.userId" :value="p.userId">{{ p.displayName }}（待办 {{ p.pendingCount }} 项）</option></select></label>
        <label>{{ mode === 'reassign' ? '转派原因' : '回访结果' }}<textarea v-model="reason" required maxlength="512" rows="3"></textarea></label>
        <p v-if="actionError" class="ops-error" role="alert">{{ actionError }}</p>
        <AppButton type="submit" :disabled="saving || !reason.trim() || (mode === 'reassign' && !assignee)">{{ saving ? '正在保存' : '确认保存' }}</AppButton>
      </form>
    </AppModal>
  </section>
</template>
