<script setup lang="ts">
import { computed, useId } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: string
    label?: string
    placeholder?: string
    error?: string
    required?: boolean
    disabled?: boolean
    size?: 'default' | 'large'
    type?: string
  }>(),
  { size: 'default', type: 'text', disabled: false, required: false },
)

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const inputId = useId()
const describedBy = computed(() => (props.error ? `${inputId}-error` : undefined))
</script>

<template>
  <div class="app-field" :class="{ 'app-field--error': error }">
    <label v-if="label" class="app-field__label" :for="inputId">
      {{ label }}
      <span v-if="required" class="app-field__required" aria-hidden="true">*</span>
    </label>
    <input
      :id="inputId"
      class="app-field__input"
      :class="`app-field__input--${size}`"
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :required="required"
      :aria-invalid="error ? true : undefined"
      :aria-describedby="describedBy"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" :id="`${inputId}-error`" class="app-field__error">{{ error }}</p>
  </div>
</template>

<style scoped>
.app-field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.app-field__label {
  color: var(--ink);
  font: var(--text-secondary);
  font-weight: 600;
}

.app-field__required {
  color: var(--emergency);
}

.app-field__input {
  width: 100%;
  padding: 0 var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  color: var(--ink);
  background: var(--surface);
  font: var(--text-body);
  transition:
    border-color var(--duration-fast) var(--ease-standard),
    box-shadow var(--duration-fast) var(--ease-standard);
}

.app-field__input--default {
  min-height: 36px;
}

.app-field__input--large {
  min-height: 40px;
}

.app-field__input::placeholder {
  color: var(--ink-muted);
}

.app-field__input:hover:not(:disabled) {
  border-color: var(--line-strong);
}

.app-field__input:focus-visible {
  outline: none;
  border-color: var(--action);
  box-shadow: 0 0 0 3px var(--action-soft);
}

.app-field__input:disabled {
  background: var(--surface-muted);
  cursor: not-allowed;
  opacity: 0.6;
}

.app-field--error .app-field__input {
  border-color: var(--emergency);
}

.app-field--error .app-field__input:focus-visible {
  box-shadow: 0 0 0 3px var(--emergency-soft);
}

.app-field__error {
  margin: 0;
  color: var(--emergency);
  font: var(--text-caption);
}
</style>
