<script setup lang="ts">
import { onBeforeUnmount, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    title?: string
    width?: 'small' | 'medium' | 'large'
    /** 破坏性操作弹窗设为 true，禁用 Esc / 遮罩点击关闭 */
    persistent?: boolean
  }>(),
  { width: 'medium', persistent: false },
)

const emit = defineEmits<{ close: [] }>()

function onBackdropClick() {
  if (!props.persistent) emit('close')
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && !props.persistent) emit('close')
}

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown)
  if (props.open) document.body.style.overflow = ''
})

watch(
  () => props.open,
  (open) => {
    if (open) {
      document.addEventListener('keydown', onKeydown)
      document.body.style.overflow = 'hidden'
    } else {
      document.removeEventListener('keydown', onKeydown)
      document.body.style.overflow = ''
    }
  },
  { immediate: true },
)
</script>

<template>
  <Teleport to="body">
    <Transition name="app-modal">
      <div
        v-if="open"
        class="app-modal__backdrop"
        role="presentation"
        @click.self="onBackdropClick"
      >
        <div
          class="app-modal__dialog"
          :class="`app-modal__dialog--${width}`"
          role="dialog"
          aria-modal="true"
          :aria-label="title"
        >
          <header v-if="title || !persistent" class="app-modal__header">
            <h2 class="app-modal__title">{{ title }}</h2>
            <button
              v-if="!persistent"
              class="app-modal__close"
              type="button"
              aria-label="关闭"
              @click="emit('close')"
            >
              ×
            </button>
          </header>
          <div class="app-modal__body">
            <slot />
          </div>
          <footer v-if="$slots.footer" class="app-modal__footer">
            <slot name="footer" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.app-modal__backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-5);
  background: var(--overlay-modal);
  backdrop-filter: blur(4px);
}

.app-modal__dialog {
  display: flex;
  flex-direction: column;
  max-height: calc(100vh - var(--space-7) * 2);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: var(--shadow-lg);
  width: 100%;
}

.app-modal__dialog--small {
  max-width: 400px;
}

.app-modal__dialog--medium {
  max-width: 560px;
}

.app-modal__dialog--large {
  max-width: 720px;
}

.app-modal__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-5) var(--space-5) var(--space-3);
}

.app-modal__title {
  margin: 0;
  font: var(--text-title);
}

.app-modal__close {
  display: inline-flex;
  width: 36px;
  height: 36px;
  min-height: 36px;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-sm);
  color: var(--ink-muted);
  background: transparent;
  font-size: 20px;
  cursor: pointer;
}

.app-modal__close:hover {
  color: var(--ink);
  background: var(--surface-muted);
}

.app-modal__body {
  overflow-y: auto;
  padding: var(--space-3) var(--space-5) var(--space-5);
}

.app-modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding: var(--space-4) var(--space-5);
  border-top: 1px solid var(--line);
}

.app-modal-enter-active,
.app-modal-leave-active {
  transition: opacity var(--duration-normal) var(--ease-standard);
}

.app-modal-enter-active .app-modal__dialog,
.app-modal-leave-active .app-modal__dialog {
  transition:
    transform var(--duration-normal) var(--ease-standard),
    opacity var(--duration-normal) var(--ease-standard);
}

.app-modal-leave-active,
.app-modal-leave-active .app-modal__dialog {
  transition-duration: var(--duration-fast);
}

.app-modal-enter-from,
.app-modal-leave-to {
  opacity: 0;
}

.app-modal-enter-from .app-modal__dialog,
.app-modal-leave-to .app-modal__dialog {
  opacity: 0;
  transform: translateY(12px);
}
</style>
