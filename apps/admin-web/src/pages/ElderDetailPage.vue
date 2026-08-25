<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ElderDetail } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const auth = useAuthStore()
const elder = ref<ElderDetail | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const elderId = computed(() => route.params.elderId as string)
const canEdit = computed(() => auth.role === 'CommunityStaff' || auth.role === 'Administrator')

const authorizationLabels = computed(() => {
  if (!elder.value) return []
  const visible: string[] = []
  if (elder.value.recentStatus) visible.push('近期状态')
  if (elder.value.careEventSummary) visible.push('照料事件摘要')
  if (elder.value.visitSummary) visible.push('探访摘要')
  if (elder.value.reminderCompletion) visible.push('提醒完成情况')
  if (elder.value.healthRisks) visible.push('健康风险摘要')
  if (elder.value.emergencyContacts) visible.push('紧急联系人')
  return visible
})

const hasTimeline = computed(() =>
  Boolean(
    elder.value?.recentStatus ||
      elder.value?.careEventSummary ||
      elder.value?.visitSummary ||
      elder.value?.reminderCompletion,
  ),
)

async function loadElder() {
  loading.value = true
  errorMessage.value = ''
  try {
    elder.value = await apiClient.request<ElderDetail>(`/api/v1/elders/${elderId.value}`)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '请求未完成，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium' }).format(new Date(value))
}

onMounted(loadElder)
</script>

<template>
  <StatusNotice v-if="loading" title="正在载入老人档案" />
  <StatusNotice v-else-if="errorMessage" kind="error" title="档案载入失败" :message="errorMessage" />

  <template v-else-if="elder">
    <header class="page-heading detail-heading">
      <div>
        <div class="name-line">
          <h1>{{ elder.demoDisplayName }}</h1>
        </div>
        <p v-if="elder.areaCode">{{ elder.areaCode }} 社区照料范围</p>
      </div>
      <div class="heading-actions">
        <RouterLink class="secondary-button" to="/elders">返回列表</RouterLink>
        <RouterLink v-if="canEdit" class="primary-button" :to="`/elders/${elder.id}/edit`">
          编辑照料档案
        </RouterLink>
      </div>
    </header>

    <div class="detail-grid">
      <section class="surface detail-section" aria-labelledby="basic-title">
        <h2 id="basic-title">基本信息</h2>
        <dl class="fact-grid">
          <div v-if="elder.birthDate">
            <dt>出生日期</dt>
            <dd>{{ formatDate(elder.birthDate) }}</dd>
          </div>
          <div v-if="elder.areaCode">
            <dt>社区区域</dt>
            <dd>{{ elder.areaCode }}</dd>
          </div>
          <div v-if="elder.attentionLevel">
            <dt>关注等级</dt>
            <dd>{{ elder.attentionLevel }}</dd>
          </div>
          <div v-if="elder.nextCheckInDueAt">
            <dt>下次平安确认</dt>
            <dd>{{ formatDate(elder.nextCheckInDueAt) }}</dd>
          </div>
        </dl>
      </section>

      <section v-if="elder.healthRisks" class="surface detail-section" aria-labelledby="risk-title">
        <h2 id="risk-title">健康风险</h2>
        <ul v-if="elder.healthRisks.length" class="record-list">
          <li v-for="risk in elder.healthRisks" :key="risk.code">{{ risk.demoLabel }}</li>
        </ul>
        <p v-else class="section-empty">当前授权范围内未记录健康风险。</p>
      </section>

      <section v-if="elder.serviceNeeds" class="surface detail-section" aria-labelledby="need-title">
        <h2 id="need-title">服务需求</h2>
        <ul v-if="elder.serviceNeeds.length" class="record-list">
          <li v-for="need in elder.serviceNeeds" :key="need.code">{{ need.demoLabel }}</li>
        </ul>
        <p v-else class="section-empty">当前档案未记录服务需求。</p>
      </section>

      <section
        v-if="elder.emergencyContacts"
        class="surface detail-section"
        aria-labelledby="contact-title"
      >
        <h2 id="contact-title">联系人</h2>
        <div v-if="elder.emergencyContacts.length" class="contact-list">
          <article v-for="contact in elder.emergencyContacts" :key="contact.contactOrder">
            <strong>{{ contact.demoName }}</strong>
            <span>{{ contact.relationship }}</span>
            <span>{{ contact.phoneNumber }}</span>
          </article>
        </div>
        <p v-else class="section-empty">当前授权范围内未记录联系人。</p>
      </section>

      <section class="surface detail-section" aria-labelledby="authorization-title">
        <h2 id="authorization-title">授权可见范围</h2>
        <ul v-if="authorizationLabels.length" class="record-list compact-list">
          <li v-for="label in authorizationLabels" :key="label">{{ label }}</li>
        </ul>
        <p v-else class="section-empty">本页仅展示当前角色获准返回的基础字段。</p>
      </section>

      <section v-if="hasTimeline" class="surface detail-section timeline" aria-labelledby="timeline-title">
        <h2 id="timeline-title">近期照料时间线</h2>
        <ol>
          <li v-if="elder.recentStatus">近期状态：{{ elder.recentStatus.state }}</li>
          <li v-if="elder.careEventSummary">
            当前照料事件 {{ elder.careEventSummary.activeCount }} 件
          </li>
          <li v-if="elder.visitSummary">探访摘要已按授权返回</li>
          <li v-if="elder.reminderCompletion">
            今日提醒完成 {{ elder.reminderCompletion.completedToday }} / {{ elder.reminderCompletion.totalToday }}
          </li>
        </ol>
      </section>
    </div>
  </template>
</template>

<style scoped>
.detail-heading,
.name-line,
.heading-actions {
  display: flex;
  align-items: center;
}

.detail-heading {
  justify-content: space-between;
  gap: var(--space-5);
}

.name-line,
.heading-actions {
  gap: var(--space-3);
}

.name-line h1 {
  margin-bottom: 0;
}

.detail-heading p {
  margin: var(--space-2) 0 0;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-5);
}

.detail-section {
  padding: var(--space-5);
}

.detail-section h2 {
  margin-bottom: var(--space-5);
  font: var(--text-title);
}

.fact-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-5);
  margin: 0;
}

.fact-grid dt {
  margin-bottom: var(--space-1);
  color: var(--ink-muted);
  font-size: 14px;
}

.fact-grid dd {
  margin: 0;
  color: var(--ink-strong);
  font-weight: 700;
}

.record-list,
.timeline ol {
  display: grid;
  gap: var(--space-3);
  padding-left: 22px;
  margin: 0;
}

.compact-list {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.contact-list {
  display: grid;
  gap: var(--space-3);
}

.contact-list article {
  display: grid;
  grid-template-columns: minmax(100px, 1fr) 100px minmax(140px, 1fr);
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--line);
}

.contact-list article:last-child {
  border-bottom: 0;
}

.section-empty {
  margin-bottom: 0;
  color: var(--ink-muted);
}

.timeline {
  grid-column: 1 / -1;
}

@media (max-width: 1023px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .timeline {
    grid-column: auto;
  }
}

@media (max-width: 767px) {
  .detail-heading,
  .heading-actions {
    align-items: stretch;
    flex-direction: column;
  }

  .fact-grid,
  .compact-list {
    grid-template-columns: 1fr;
  }

  .contact-list article {
    grid-template-columns: 1fr;
  }
}
</style>
