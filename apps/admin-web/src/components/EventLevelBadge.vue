<script setup lang="ts">
import { computed } from 'vue'

import type { CareEventLevel } from '@/api/contracts'

const props = defineProps<{ level: CareEventLevel }>()

const label = computed(
  () =>
    ({
      GeneralService: '一般服务',
      NeedsConfirmation: '需确认',
      Emergency: '紧急',
    })[props.level],
)
</script>

<template>
  <span class="event-level" :class="`event-level--${level}`">{{ label }}</span>
</template>

<style scoped>
.event-level {
  display: inline-flex;
  min-height: 28px;
  align-items: center;
  padding: 2px 9px;
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  color: var(--ink-strong);
  background: var(--surface);
  font-size: 13px;
  font-weight: 700;
}

.event-level--NeedsConfirmation {
  border-color: #b87600;
  color: #744900;
  background: #fff7e0;
}

.event-level--Emergency {
  border-color: var(--emergency);
  color: var(--emergency);
  background: var(--emergency-soft);
}
</style>
