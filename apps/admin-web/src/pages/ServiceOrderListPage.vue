<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ServiceOrderItem } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'

const orders = ref<ServiceOrderItem[]>([])
const loading = ref(true)
const errorMessage = ref('')

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    orders.value = await apiClient.request<ServiceOrderItem[]>('/api/v1/service-orders')
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '服务工单载入失败。'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section>
    <header class="page-heading">
      <p class="page-kicker">社区服务协同</p>
      <h1>服务工单</h1>
      <p>只显示完成当前工单所需的信息；模拟工单不会触发真实服务。</p>
    </header>

    <StatusNotice v-if="loading" kind="loading" title="正在载入服务工单" />
    <StatusNotice v-else-if="errorMessage" kind="error" :title="errorMessage" />
    <StatusNotice v-else-if="!orders.length" kind="empty" title="当前没有服务工单" />

    <div v-else class="work-table-wrap surface">
      <table>
        <thead>
          <tr>
            <th scope="col">老人</th>
            <th scope="col">服务类型</th>
            <th scope="col">预约时段</th>
            <th scope="col">联系说明</th>
            <th scope="col">状态</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="order in orders" :key="order.orderId">
            <td>{{ order.elderDisplayName }}</td>
            <td>{{ order.serviceType }}</td>
            <td>{{ order.scheduledWindow }}</td>
            <td>{{ order.contactInstruction }}</td>
            <td>{{ order.status }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.page-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

.work-table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  min-width: 840px;
  border-collapse: collapse;
}

th,
td {
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
  text-align: left;
  vertical-align: top;
}

th {
  color: var(--ink-muted);
  background: var(--paper);
  font-size: 13px;
}

tbody tr:last-child td {
  border-bottom: 0;
}
</style>
