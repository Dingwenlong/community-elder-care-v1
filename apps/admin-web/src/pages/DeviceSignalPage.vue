<script setup lang="ts">
import DeviceSimulator from '@/components/DeviceSimulator.vue'
</script>

<template>
  <div class="device-page">
    <header class="page-header">
      <p class="page-kicker">设备演示</p>
      <h1>设备信号</h1>
      <p>用于参赛演示的 SOS 与居家活动异常模拟，不代表已经部署真实硬件。</p>
    </header>
    <DeviceSimulator />
  </div>
</template>

<style scoped>
.device-page {
  display: grid;
  gap: var(--space-5);
}

.page-header {
  padding-bottom: var(--space-4);
  border-bottom: 1px solid var(--line);
}

.page-kicker {
  margin: 0 0 var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h1 {
  margin: 0;
  color: var(--ink-strong);
  font-family: var(--font-display);
  font-size: 32px;
}

.page-header p:last-child {
  margin-bottom: 0;
  color: var(--ink-muted);
}
</style>
