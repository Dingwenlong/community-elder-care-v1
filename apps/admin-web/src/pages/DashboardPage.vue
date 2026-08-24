<script setup lang="ts">
import { computed } from 'vue'

import StatusNotice from '@/components/StatusNotice.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const canViewElders = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
</script>

<template>
  <header class="page-heading dashboard-heading">
    <p class="page-context">A01 社区 · 今日工作</p>
    <h1>社区工作台</h1>
    <p>先处理需要确认的安全事件，再安排探访和生活服务。</p>
  </header>

  <div class="dashboard-grid">
    <section class="surface pending-panel" aria-labelledby="pending-title">
      <div class="section-heading">
        <div>
          <h2 id="pending-title">待处理事项</h2>
          <p>按等级和等待时长排序</p>
        </div>
        <RouterLink v-if="canViewElders" class="secondary-button" to="/elders">
          查看老人档案
        </RouterLink>
      </div>
      <StatusNotice title="当前没有载入事件" message="照料事件接入后会在这里按优先级显示。" />
    </section>

    <aside class="surface operating-note" aria-labelledby="operating-title">
      <h2 id="operating-title">处理原则</h2>
      <ol>
        <li>紧急事件立即核实，不以设备信号直接结案。</li>
        <li>联系不上老人时保留记录并继续升级。</li>
        <li>探访、工单和随访完成后才能申请结案。</li>
      </ol>
    </aside>
  </div>
</template>

<style scoped>
.page-context {
  margin-bottom: var(--space-2);
  color: var(--action);
  font-size: 14px;
  font-weight: 700;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: var(--space-5);
}

.pending-panel,
.operating-note {
  padding: var(--space-5);
}

.section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.section-heading h2,
.section-heading p,
.operating-note h2 {
  margin-bottom: 0;
}

.section-heading h2,
.operating-note h2 {
  font-size: 20px;
}

.section-heading p {
  margin-top: var(--space-1);
  color: var(--ink-muted);
  font-size: 14px;
}

.operating-note ol {
  display: grid;
  gap: var(--space-4);
  padding-left: 22px;
  margin: var(--space-5) 0 0;
}

@media (max-width: 980px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}
</style>
