<script setup lang="ts">
withDefaults(
  defineProps<{
    kind?: 'loading' | 'empty' | 'error' | 'info'
    title: string
    message?: string
  }>(),
  { kind: 'info', message: '' },
)
</script>

<template>
  <div
    class="status-notice"
    :class="`status-notice--${kind}`"
    :role="kind === 'error' ? 'alert' : 'status'"
  >
    <strong>{{ title }}</strong>
    <span v-if="message">{{ message }}</span>
    <slot />
  </div>
</template>

<style scoped>
.status-notice {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-5);
  border: 1px solid var(--line);
  background: var(--surface-muted);
}

.status-notice strong {
  color: var(--ink-strong);
}

.status-notice span {
  color: var(--ink-muted);
}

.status-notice--error {
  border-color: #e3a2a2;
  background: var(--emergency-soft);
}
</style>
