<script setup lang="ts">
interface VisitSummaryDraft {
  id: string
  generatedText: string
}

const props = withDefaults(
  defineProps<{
    draft: VisitSummaryDraft
    loading?: boolean
  }>(),
  { loading: false },
)

const emit = defineEmits<{
  confirm: [draftId: string]
}>()
</script>

<template>
  <section class="ai-draft" aria-labelledby="ai-draft-title">
    <div class="ai-draft__header">
      <div>
        <p class="ai-draft__label">AI 草稿</p>
        <h2 id="ai-draft-title">探访摘要待确认</h2>
      </div>
      <span class="ai-draft__status">不会自动提交</span>
    </div>
    <p class="ai-draft__content">{{ draft.generatedText }}</p>
    <p class="ai-draft__notice">请工作人员核对事实。确认后只保存摘要，不改写原始探访记录。</p>
    <button type="button" :disabled="loading" @click="emit('confirm', props.draft.id)">
      {{ loading ? '正在确认' : '确认摘要' }}
    </button>
  </section>
</template>

<style scoped>
.ai-draft {
  padding: var(--space-5);
  border: 1px solid var(--line-strong);
  background: var(--surface);
}

.ai-draft__header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: var(--space-4);
}

.ai-draft__label {
  margin: 0 0 var(--space-1);
  color: var(--action);
  font-size: 13px;
  font-weight: 700;
}

h2 {
  margin: 0;
  color: var(--ink-strong);
  font-size: 20px;
}

.ai-draft__status {
  padding-left: var(--space-3);
  border-left: 3px solid var(--warning);
  color: var(--warning);
  font-size: 13px;
  font-weight: 700;
}

.ai-draft__content {
  padding: var(--space-4);
  margin: var(--space-4) 0 var(--space-3);
  border-left: 3px solid var(--action);
  background: var(--action-soft);
  color: var(--ink-strong);
  line-height: 1.7;
}

.ai-draft__notice {
  color: var(--ink-muted);
  font-size: 14px;
}

button {
  min-height: 44px;
  padding: 0 var(--space-4);
  border: 1px solid var(--navy);
  background: var(--navy);
  color: white;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

button:disabled {
  border-color: var(--line-strong);
  background: var(--surface-muted);
  color: var(--ink-muted);
  cursor: wait;
}

button:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

@media (max-width: 640px) {
  .ai-draft__header {
    display: block;
  }

  .ai-draft__status {
    display: block;
    margin-top: var(--space-3);
  }
}
</style>
