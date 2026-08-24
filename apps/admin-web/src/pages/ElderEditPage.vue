<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { apiClient, ApiError } from '@/api/apiClient'
import type { ElderDetail, EmergencyContact, LabelValue } from '@/api/contracts'
import StatusNotice from '@/components/StatusNotice.vue'

const route = useRoute()
const elderId = computed(() => route.params.elderId as string)
const elderName = ref('')
const attentionLevel = ref<ElderDetail['attentionLevel']>('Routine')
const healthRisks = ref<LabelValue[]>([])
const serviceNeeds = ref<LabelValue[]>([])
const emergencyContacts = ref<EmergencyContact[]>([])
const reason = ref('')
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const validationMessage = ref('')
const successMessage = ref('')

async function loadElder() {
  loading.value = true
  try {
    const elder = await apiClient.request<ElderDetail>(`/api/v1/elders/${elderId.value}`)
    elderName.value = elder.demoDisplayName
    attentionLevel.value = elder.attentionLevel ?? 'Routine'
    healthRisks.value = elder.healthRisks?.map((item) => ({ ...item })) ?? []
    serviceNeeds.value = elder.serviceNeeds?.map((item) => ({ ...item })) ?? []
    emergencyContacts.value = elder.emergencyContacts?.map((item) => ({ ...item })) ?? []
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '请求未完成，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function addRisk() {
  healthRisks.value.push({ code: '', demoLabel: '' })
}

function addNeed() {
  serviceNeeds.value.push({ code: '', demoLabel: '' })
}

function addContact() {
  emergencyContacts.value.push({
    demoName: '',
    relationship: '',
    phoneNumber: '',
    contactOrder: emergencyContacts.value.length + 1,
  })
}

function removeContact(index: number) {
  emergencyContacts.value.splice(index, 1)
  emergencyContacts.value.forEach((contact, itemIndex) => {
    contact.contactOrder = itemIndex + 1
  })
}

async function submit() {
  validationMessage.value = ''
  successMessage.value = ''
  if (!reason.value.trim()) {
    validationMessage.value = '请填写本次修改原因。'
    return
  }
  if (!healthRisks.value.length || !serviceNeeds.value.length || !emergencyContacts.value.length) {
    validationMessage.value = '健康风险、服务需求和联系人都至少保留一项。'
    return
  }

  saving.value = true
  try {
    await apiClient.request<ElderDetail>(`/api/v1/elders/${elderId.value}/care-profile`, {
      method: 'PUT',
      body: JSON.stringify({
        attentionLevel: attentionLevel.value,
        healthRisks: healthRisks.value,
        serviceNeeds: serviceNeeds.value,
        emergencyContacts: emergencyContacts.value,
        reason: reason.value.trim(),
      }),
    })
    successMessage.value = '照料档案已更新，并记录本次修改原因。'
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '请求未完成，请稍后重试。'
  } finally {
    saving.value = false
  }
}

onMounted(loadElder)
</script>

<template>
  <StatusNotice v-if="loading" title="正在载入可编辑档案" />
  <StatusNotice v-else-if="errorMessage && !elderName" kind="error" title="档案载入失败" :message="errorMessage" />

  <template v-else>
    <header class="page-heading edit-heading">
      <div>
        <h1>编辑{{ elderName }}</h1>
        <p>风险、需求与联系人分区维护；保存时必须说明原因。</p>
      </div>
      <RouterLink class="secondary-button" :to="`/elders/${elderId}`">取消并返回</RouterLink>
    </header>

    <form class="edit-form" novalidate @submit.prevent="submit">
      <section class="surface form-section" aria-labelledby="attention-title">
        <h2 id="attention-title">关注等级</h2>
        <label for="attention-level">当前等级</label>
        <select id="attention-level" v-model="attentionLevel">
          <option value="Routine">常规关注</option>
          <option value="Priority">重点关注</option>
          <option value="HighAttention">高关注</option>
        </select>
      </section>

      <section class="surface form-section" aria-labelledby="risk-edit-title">
        <div class="form-section-heading">
          <h2 id="risk-edit-title">健康风险</h2>
          <button class="secondary-button" type="button" @click="addRisk">增加风险</button>
        </div>
        <div v-for="(risk, index) in healthRisks" :key="index" class="field-row">
          <label :for="`risk-code-${index}`">风险代码</label>
          <input :id="`risk-code-${index}`" v-model="risk.code" required />
          <label :for="`risk-label-${index}`">风险说明</label>
          <input :id="`risk-label-${index}`" v-model="risk.demoLabel" required />
          <button
            class="text-action"
            type="button"
            :aria-label="`删除健康风险 ${index + 1}`"
            @click="healthRisks.splice(index, 1)"
          >
            删除
          </button>
        </div>
      </section>

      <section class="surface form-section" aria-labelledby="need-edit-title">
        <div class="form-section-heading">
          <h2 id="need-edit-title">服务需求</h2>
          <button class="secondary-button" type="button" @click="addNeed">增加需求</button>
        </div>
        <div v-for="(need, index) in serviceNeeds" :key="index" class="field-row">
          <label :for="`need-code-${index}`">需求代码</label>
          <input :id="`need-code-${index}`" v-model="need.code" required />
          <label :for="`need-label-${index}`">需求说明</label>
          <input :id="`need-label-${index}`" v-model="need.demoLabel" required />
          <button
            class="text-action"
            type="button"
            :aria-label="`删除服务需求 ${index + 1}`"
            @click="serviceNeeds.splice(index, 1)"
          >
            删除
          </button>
        </div>
      </section>

      <section class="surface form-section" aria-labelledby="contact-edit-title">
        <div class="form-section-heading">
          <h2 id="contact-edit-title">联系人</h2>
          <button class="secondary-button" type="button" @click="addContact">增加联系人</button>
        </div>
        <fieldset v-for="(contact, index) in emergencyContacts" :key="index">
          <legend>联系人 {{ index + 1 }}</legend>
          <label :for="`contact-name-${index}`">姓名</label>
          <input :id="`contact-name-${index}`" v-model="contact.demoName" required />
          <label :for="`contact-relation-${index}`">关系</label>
          <input :id="`contact-relation-${index}`" v-model="contact.relationship" required />
          <label :for="`contact-phone-${index}`">演示电话</label>
          <input
            :id="`contact-phone-${index}`"
            v-model="contact.phoneNumber"
            inputmode="tel"
            pattern="1990000[0-9]{4}"
            required
          />
          <button class="text-action" type="button" @click="removeContact(index)">删除联系人</button>
        </fieldset>
      </section>

      <section class="surface reason-section" aria-labelledby="reason-title">
        <h2 id="reason-title">修改原因</h2>
        <label for="change-reason">说明本次调整依据</label>
        <textarea id="change-reason" v-model="reason" rows="3" required />
        <p v-if="validationMessage" class="form-error" role="alert">{{ validationMessage }}</p>
        <p v-if="errorMessage" class="form-error" role="alert">{{ errorMessage }}</p>
        <p v-if="successMessage" class="form-success" role="status">{{ successMessage }}</p>
        <button class="primary-button" type="submit" :disabled="saving">
          {{ saving ? '正在保存' : '保存修改' }}
        </button>
      </section>
    </form>
  </template>
</template>

<style scoped>
.edit-heading,
.form-section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
}

.edit-form {
  display: grid;
  gap: var(--space-5);
}

.form-section,
.reason-section {
  padding: var(--space-5);
}

.form-section h2,
.reason-section h2 {
  margin-bottom: var(--space-5);
  font-size: 19px;
}

.form-section-heading h2 {
  margin-bottom: var(--space-4);
}

label {
  color: var(--ink-strong);
  font-weight: 700;
}

input,
select,
textarea {
  width: 100%;
  padding: 9px 12px;
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  background: var(--surface);
}

.form-section > label,
.reason-section > label {
  display: block;
  margin-bottom: var(--space-2);
}

.form-section > select {
  max-width: 320px;
}

.field-row {
  display: grid;
  grid-template-columns: 110px minmax(140px, 0.7fr) 110px minmax(220px, 1.3fr) 56px;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid var(--line);
}

fieldset {
  display: grid;
  grid-template-columns: 80px 1fr 80px 1fr 100px 1fr auto;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-4) 0;
  border: 0;
  border-top: 1px solid var(--line);
}

legend {
  padding: 0;
  margin-bottom: var(--space-2);
  color: var(--ink-strong);
  font-weight: 700;
}

.text-action {
  border: 0;
  color: var(--action);
  background: transparent;
  cursor: pointer;
}

.text-action:hover {
  text-decoration: underline;
}

.reason-section textarea {
  margin-bottom: var(--space-3);
  resize: vertical;
}

.form-error,
.form-success {
  padding: var(--space-3);
  margin-bottom: var(--space-3);
}

.form-error {
  color: var(--emergency);
  background: var(--emergency-soft);
}

.form-success {
  color: var(--success);
  background: var(--success-soft);
}

@media (max-width: 980px) {
  .field-row,
  fieldset {
    grid-template-columns: 1fr;
  }
}
</style>
