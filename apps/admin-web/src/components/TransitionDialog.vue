<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import type { CareEventStatus } from '@/api/contracts'

const props = defineProps<{ target: CareEventStatus; submitting?: boolean; serverError?: string }>()
const emit = defineEmits<{ submit: [reason: string, resolution: string | null] }>()

const reason = ref('')
const resolution = ref('')
const validationError = ref('')

const targetLabel = computed(
  () =>
    ({
      PendingConfirmation: '待确认',
      Accepted: '已受理',
      UnableToConfirm: '无法确认',
      InProgress: '处理中',
      Resolved: '已解决',
      FollowUpPending: '待随访',
      Closed: '已结案',
      FalseAlarm: '误报',
    })[props.target],
)
const needsReason = computed(() =>
  ['FalseAlarm', 'UnableToConfirm', 'Resolved', 'Closed'].includes(props.target),
)
const needsResolution = computed(() => props.target === 'Resolved' || props.target === 'Closed')

watch(
  () => props.target,
  () => {
    reason.value = ''
    resolution.value = ''
    validationError.value = ''
  },
)

function submit() {
  validationError.value = ''
  if (needsReason.value && !reason.value.trim()) {
    validationError.value = props.target === 'FalseAlarm' ? '请填写判断依据' : '请填写处理依据'
    return
  }
  if (needsResolution.value && !resolution.value.trim()) {
    validationError.value = '请填写处理结果'
    return
  }
  emit('submit', reason.value.trim(), resolution.value.trim() || null)
}
</script>

<template>
  <section class="transition-panel surface" aria-labelledby="transition-title">
    <p class="section-kicker">状态处理</p>
    <h2 id="transition-title">转为“{{ targetLabel }}”</h2>
    <p v-if="target === 'UnableToConfirm'" class="escalation-warning">
      将进入联系升级，不能直接关单
    </p>
    <form novalidate @submit.prevent="submit">
      <label for="transition-reason">判断或处理依据</label>
      <textarea id="transition-reason" v-model="reason" rows="3" />
      <template v-if="needsResolution">
        <label for="transition-resolution">处理结果</label>
        <textarea id="transition-resolution" v-model="resolution" rows="3" />
      </template>
      <p v-if="validationError" class="form-error" role="alert">{{ validationError }}</p>
      <p v-if="serverError" class="form-error" role="alert">{{ serverError }}</p>
      <button class="primary-button" type="submit" :disabled="submitting">确认提交</button>
    </form>
  </section>
</template>

<style scoped>
.transition-panel {
  padding: var(--space-5);
}

.section-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin-bottom: var(--space-4);
  font-size: 20px;
}

.escalation-warning {
  padding: var(--space-3);
  border-left: 4px solid #b87600;
  color: #684300;
  background: #fff7e0;
}

form {
  display: grid;
  gap: var(--space-2);
}

textarea {
  width: 100%;
  min-height: 88px;
  padding: var(--space-3);
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  background: var(--surface);
  resize: vertical;
}

.form-error {
  margin-bottom: var(--space-2);
  color: var(--emergency);
  font-weight: 700;
}

button {
  justify-self: start;
}
</style>
