<script setup lang="ts">
import type { ContactAttempt } from '@/api/contracts'

defineProps<{ attempts: ContactAttempt[] }>()
</script>

<template>
  <section class="simulation-panel surface" aria-labelledby="simulation-title">
    <p class="simulation-label">模拟外部动作</p>
    <h2 id="simulation-title">联系与转运记录</h2>
    <p>电话、短信、家属联系和 120 动作只记录模拟结果，不会连接真实外部服务。</p>
    <ul v-if="attempts.length">
      <li v-for="attempt in attempts" :key="attempt.id">
        <span>{{ attempt.targetLabel }}</span>
        <strong>{{ attempt.outcome }}</strong>
        <span v-if="attempt.isSimulation" class="simulation-tag">模拟</span>
      </li>
    </ul>
    <p v-else class="empty-copy">尚无模拟联系记录。</p>
  </section>
</template>

<style scoped>
.simulation-panel {
  padding: var(--space-5);
}

.simulation-label {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin-bottom: var(--space-2);
  font-size: 20px;
}

ul {
  padding: 0;
  margin: var(--space-4) 0 0;
  list-style: none;
}

li {
  display: grid;
  grid-template-columns: minmax(120px, 0.7fr) minmax(180px, 1fr) auto;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid var(--line);
}

.simulation-tag {
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

.empty-copy {
  color: var(--ink-muted);
}
</style>
