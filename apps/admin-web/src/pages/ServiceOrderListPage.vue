<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ServiceOrderItem } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppTable from '@/components/ui/AppTable.vue'

const orders = ref<ServiceOrderItem[]>([])
const loading = ref(true)
const errorMessage = ref('')

const orderStatusLabels: Record<string, string> = {
  Assigned: '已分派',
  Accepted: '已接单',
  InProgress: '处理中',
  Completed: '已完成',
  Cancelled: '已取消',
}

const orderStatusTones: Record<string, 'l2' | 'l3' | 'closed' | 'neutral'> = {
  Assigned: 'l3',
  Accepted: 'l3',
  InProgress: 'l2',
  Completed: 'closed',
  Cancelled: 'neutral',
}

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

    <AppTable v-else min-width="840px">
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
          <td>
            <AppBadge :tone="orderStatusTones[order.status] ?? 'neutral'">
              {{ orderStatusLabels[order.status] ?? order.status }}
            </AppBadge>
          </td>
        </tr>
      </tbody>
    </AppTable>
  </section>
</template>

<style scoped>
.page-kicker {
  margin-bottom: var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}
</style>
