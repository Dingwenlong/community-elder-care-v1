<script setup lang="ts">
import careEventsIllustration from '@/assets/illustrations/care-events-empty.webp'
import careWorkIllustration from '@/assets/illustrations/care-work-empty.webp'
import elderRecordsIllustration from '@/assets/illustrations/elder-records-empty.webp'

type StatusNoticeIllustration = 'care-events' | 'elder-records' | 'care-work'

const illustrationSources: Record<StatusNoticeIllustration, string> = {
  'care-events': careEventsIllustration,
  'elder-records': elderRecordsIllustration,
  'care-work': careWorkIllustration,
}

withDefaults(
  defineProps<{
    kind?: 'loading' | 'empty' | 'error' | 'info'
    title: string
    message?: string
    illustration?: StatusNoticeIllustration
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
    <img
      v-if="kind === 'empty' && illustration"
      class="status-notice__illustration"
      :src="illustrationSources[illustration]"
      alt=""
      aria-hidden="true"
    />
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

.status-notice__illustration {
  width: min(180px, 100%);
  margin-bottom: var(--space-2);
}

.status-notice span {
  color: var(--ink-muted);
}

.status-notice--error {
  border-color: #e3a2a2;
  background: var(--emergency-soft);
}
</style>
