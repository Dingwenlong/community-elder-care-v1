<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiClient } from '@/api/apiClient'
import type { CareEvent } from '@/api/contracts'
import type { Personnel } from '@/api/operations'
import { beijingInput, beijingIso } from '@/api/operations'
import AppButton from '@/components/ui/AppButton.vue'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ careEvent: CareEvent }>()
const emit = defineEmits<{ created: [] }>()
const auth = useAuthStore()
const people = ref<Personnel[]>([])
const kind = ref<'visits' | 'service-orders' | 'follow-ups' | ''>('')
const assigned = ref('')
const start = ref(beijingInput(new Date(Date.now() + 3600000)))
const end = ref(beijingInput(new Date(Date.now() + 7200000)))
const serviceType = ref('助餐配送')
const contact = ref('')
const mandatory = ref(true)
const saving = ref(false)
const error = ref('')
const canCreate = computed(() => auth.role === 'CommunityStaff' && props.careEvent.currentOwnerUserId === auth.userId)
const canSchedule = computed(() => ['Accepted', 'InProgress'].includes(props.careEvent.status))
const candidates = computed(() => people.value.filter(p => p.role === (kind.value === 'service-orders' ? 'ServiceWorker' : 'CommunityStaff')))
function choose(value: typeof kind.value) { kind.value = value; assigned.value = ''; error.value = '' }
async function create() {
  if (saving.value || !kind.value) return
  error.value = ''
  if (kind.value !== 'follow-ups' && end.value <= start.value) { error.value = '结束时间必须晚于开始时间。'; return }
  saving.value = true
  try {
    const body = kind.value === 'visits'
      ? { assignedStaffUserId: assigned.value, scheduledStartAt: beijingIso(start.value), scheduledEndAt: beijingIso(end.value), isMandatory: mandatory.value }
      : kind.value === 'follow-ups'
        ? { assignedStaffUserId: assigned.value, dueAt: beijingIso(end.value) }
        : { assignedWorkerUserId: assigned.value, serviceType: serviceType.value, scheduledWindow: start.value.replace('T', ' ') + ' 至 ' + end.value.replace('T', ' '), contactInstruction: contact.value, dueAt: beijingIso(end.value), isMandatory: mandatory.value }
    await apiClient.request('/api/v1/care-events/' + props.careEvent.id + '/' + kind.value,
      { method: 'POST', body: JSON.stringify(body) })
    kind.value = ''
    emit('created')
  } catch (e) { error.value = e instanceof Error ? e.message : '任务创建失败。' }
  finally { saving.value = false }
}
onMounted(async () => {
  if (!canCreate.value) return
  try { people.value = await apiClient.request<Personnel[]>('/api/v1/operations/personnel') }
  catch (e) { error.value = e instanceof Error ? e.message : '人员名单载入失败。' }
})
</script>

<template>
  <section v-if="canCreate" class="ops-panel">
    <h2>安排照料任务</h2>
    <div class="ops-actions"><AppButton v-if="canSchedule" variant="secondary" @click="choose('visits')">安排探访</AppButton>
      <AppButton v-if="canSchedule" variant="secondary" @click="choose('service-orders')">创建工单</AppButton>
      <AppButton v-if="careEvent.status === 'Resolved'" variant="secondary" @click="choose('follow-ups')">安排回访</AppButton>
      <RouterLink to="/operations">查看人员与任务</RouterLink></div>
    <form v-if="kind" class="ops-form" @submit.prevent="create">
      <label>任务负责人<select v-model="assigned" required><option disabled value="">请选择负责人</option><option v-for="p in candidates" :key="p.userId" :value="p.userId">{{ p.displayName }}（待办 {{ p.pendingCount }} 项）</option></select></label>
      <div class="ops-filters"><label v-if="kind !== 'follow-ups'">开始时间（北京时间）<input v-model="start" type="datetime-local" required /></label>
        <label>{{ kind === 'follow-ups' ? '回访截止时间' : '结束时间' }}（北京时间）<input v-model="end" type="datetime-local" required /></label></div>
      <template v-if="kind === 'service-orders'"><label>服务类型<input v-model="serviceType" required maxlength="96" /></label><label>联系说明<textarea v-model="contact" required maxlength="256" rows="2"></textarea></label></template>
      <label v-if="kind !== 'follow-ups'" class="ops-check"><input v-model="mandatory" type="checkbox" />必须完成后才能结案</label>
      <div class="ops-actions"><AppButton type="submit" :disabled="saving">{{ saving ? '正在保存' : '保存任务' }}</AppButton><AppButton variant="secondary" :disabled="saving" @click="kind = ''">取消</AppButton></div>
    </form>
    <p v-if="error" class="ops-error" role="alert">{{ error }}</p>
  </section>
</template>
