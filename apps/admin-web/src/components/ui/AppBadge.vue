<script setup lang="ts">
/**
 * 通用状态徽章。事件等级语义与 docs/ui/design-tokens.json 的 eventLevel 一致：
 * l1 紧急 / l2 重要 / l3 常规 / closed 已结案 / neutral 中性。
 */
withDefaults(
  defineProps<{
    tone?: 'l1' | 'l2' | 'l3' | 'closed' | 'neutral'
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <span class="app-badge" :class="`app-badge--${tone}`">
    <slot />
  </span>
</template>

<style scoped>
.app-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  min-height: 24px;
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font: var(--text-caption);
  font-weight: 600;
  white-space: nowrap;
}

.app-badge--l1 {
  color: var(--emergency);
  background: var(--emergency-soft);
}

.app-badge--l2 {
  color: var(--warning);
  background: var(--warning-soft);
}

.app-badge--l3 {
  color: var(--navy);
  background: var(--action-soft);
}

.app-badge--closed {
  color: var(--success);
  background: var(--success-soft);
}

.app-badge--neutral {
  color: var(--ink-muted);
  background: var(--surface-muted);
}
</style>
