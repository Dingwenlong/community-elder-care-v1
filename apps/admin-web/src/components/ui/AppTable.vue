<script setup lang="ts">
/**
 * 统一表格容器：sticky 表头、斑马纹、行悬停、48px 行高。
 * 列结构由调用方通过默认插槽提供（thead/tbody），保持 table 原生语义。
 * 数字列在 td/th 上加 class="col-numeric" 即可右对齐并使用等宽数字。
 */
withDefaults(defineProps<{ minWidth?: string }>(), { minWidth: '920px' })
</script>

<template>
  <div class="app-table-wrap">
    <table class="app-table" :style="{ minWidth }">
      <slot />
    </table>
  </div>
</template>

<style scoped>
.app-table-wrap {
  overflow-x: auto;
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: var(--shadow-sm);
}

.app-table {
  width: 100%;
  border-collapse: collapse;
}

.app-table :deep(th),
.app-table :deep(td) {
  min-height: 48px;
  padding: var(--space-3) var(--space-4);
  border-bottom: 1px solid var(--line);
  text-align: left;
  vertical-align: middle;
}

.app-table :deep(th) {
  position: sticky;
  top: 0;
  z-index: 1;
  border-bottom: 1px solid var(--line-strong);
  color: var(--ink-muted);
  background: var(--surface-muted);
  font: var(--text-secondary);
  font-weight: 600;
}

.app-table :deep(tbody tr:nth-child(even)) {
  background: var(--surface-zebra);
}

.app-table :deep(tbody tr:hover) {
  background: var(--action-soft);
}

.app-table :deep(tbody tr:last-child td) {
  border-bottom: 0;
}

.app-table :deep(.col-numeric) {
  font-family: var(--font-numeric);
  text-align: right;
  font-variant-numeric: tabular-nums;
}
</style>
