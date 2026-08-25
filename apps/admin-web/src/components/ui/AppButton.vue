<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
    size?: 'default' | 'large'
    loading?: boolean
    disabled?: boolean
    type?: 'button' | 'submit'
  }>(),
  { variant: 'primary', size: 'default', loading: false, disabled: false, type: 'button' },
)
</script>

<template>
  <button
    class="app-button"
    :class="[`app-button--${variant}`, `app-button--${size}`, { 'is-loading': loading }]"
    :type="type"
    :disabled="disabled || loading"
  >
    <span v-if="loading" class="app-button__spinner" aria-hidden="true" />
    <slot />
  </button>
</template>

<style scoped>
.app-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  font: var(--text-action);
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.app-button--default {
  min-height: 36px;
  padding: 0 var(--space-4);
}

.app-button--large {
  min-height: 44px;
  padding: 0 var(--space-5);
}

.app-button:active:not(:disabled) {
  transform: scale(0.97);
}

.app-button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.app-button--primary {
  color: var(--surface);
  background: var(--action);
}

.app-button--primary:hover:not(:disabled) {
  background: var(--action-hover);
}

.app-button--secondary {
  border-color: var(--line-strong);
  color: var(--ink);
  background: var(--surface);
}

.app-button--secondary:hover:not(:disabled) {
  border-color: var(--action);
  color: var(--action);
  background: var(--action-soft);
}

.app-button--danger {
  color: var(--surface);
  background: var(--emergency);
}

.app-button--danger:hover:not(:disabled) {
  background: #a82121;
}

.app-button--ghost {
  color: var(--action);
  background: transparent;
}

.app-button--ghost:hover:not(:disabled) {
  background: var(--action-soft);
}

.app-button__spinner {
  width: 14px;
  height: 14px;
  border: 2px solid currentcolor;
  border-top-color: transparent;
  border-radius: var(--radius-pill);
  animation: app-button-spin 0.8s linear infinite;
}

@keyframes app-button-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .app-button__spinner {
    animation-duration: 1.6s;
  }
}
</style>
