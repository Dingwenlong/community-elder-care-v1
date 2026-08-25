<script setup lang="ts">
import { ref } from 'vue'

import type { CareEventLevel } from '@/api/contracts'

defineProps<{ eventLevel: CareEventLevel; elderId: string; submitting?: boolean; serverError?: string }>()
const emit = defineEmits<{ submit: [reason: string] }>()

const reason = ref('')
const validationError = ref('')

function submit() {
  validationError.value = ''
  if (!reason.value.trim()) {
    validationError.value = '请填写紧急访问原因'
    return
  }
  emit('submit', reason.value.trim())
}
</script>

<template>
  <section v-if="eventLevel === 'Emergency'" class="break-glass surface">
    <p class="emergency-label">紧急访问</p>
    <h2>申请临时摘要权限</h2>
    <p>仅用于当前紧急事件。</p>
    <p><strong>临时授权将在 15 分钟后失效</strong></p>
    <form novalidate @submit.prevent="submit">
      <label for="break-glass-reason">紧急访问原因</label>
      <textarea id="break-glass-reason" v-model="reason" rows="3" />
      <p v-if="validationError" class="form-error" role="alert">{{ validationError }}</p>
      <p v-if="serverError" class="form-error" role="alert">{{ serverError }}</p>
      <button class="emergency-button" type="submit" :disabled="submitting">申请临时授权</button>
    </form>
  </section>
</template>

<style scoped>
.break-glass {
  padding: var(--space-5);
  border: 1px solid var(--emergency);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: var(--shadow-sm);
}

.emergency-label {
  margin-bottom: var(--space-1);
  color: var(--emergency);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin-bottom: var(--space-2);
  font: var(--text-title);
}

form {
  display: grid;
  gap: var(--space-2);
}

textarea {
  width: 100%;
  min-height: 88px;
  padding: var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: var(--surface);
  resize: vertical;
  transition:
    border-color var(--duration-fast) var(--ease-standard),
    box-shadow var(--duration-fast) var(--ease-standard);
}

textarea:hover {
  border-color: var(--line-strong);
}

textarea:focus-visible {
  outline: none;
  border-color: var(--emergency);
  box-shadow: 0 0 0 3px var(--emergency-soft);
}

.emergency-button {
  min-height: 44px;
  justify-self: start;
  padding: 0 var(--space-5);
  border: 1px solid var(--emergency);
  border-radius: var(--radius-md);
  color: white;
  background: var(--emergency);
  font-weight: 700;
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.emergency-button:hover:not(:disabled) {
  background: #a82121;
}

.emergency-button:active:not(:disabled) {
  transform: scale(0.97);
}

.emergency-button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.form-error {
  margin: 0;
  color: var(--emergency);
  font-weight: 700;
}
</style>
