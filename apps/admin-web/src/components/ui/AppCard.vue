<script setup lang="ts">
withDefaults(
  defineProps<{
    title?: string
    hoverable?: boolean
    padded?: boolean
  }>(),
  { hoverable: false, padded: true },
)
</script>

<template>
  <section class="app-card" :class="{ 'app-card--hoverable': hoverable, 'app-card--padded': padded }">
    <header v-if="title || $slots.actions" class="app-card__header">
      <h2 class="app-card__title">{{ title }}</h2>
      <div v-if="$slots.actions" class="app-card__actions">
        <slot name="actions" />
      </div>
    </header>
    <slot />
  </section>
</template>

<style scoped>
.app-card {
  border: 1px solid transparent;
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: var(--shadow-sm);
  transition:
    box-shadow var(--duration-normal) var(--ease-standard),
    transform var(--duration-normal) var(--ease-standard);
}

.app-card--padded {
  padding: var(--space-5);
}

.app-card--hoverable:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.app-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  margin-bottom: var(--space-4);
}

.app-card__title {
  margin: 0;
  font: var(--text-title);
}

.app-card__actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
</style>
