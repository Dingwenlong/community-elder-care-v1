<script setup lang="ts">
import { useToast } from './useToast'

const { toasts, dismiss } = useToast()

const icons = {
  success: '✓',
  warning: '!',
  error: '×',
} as const
</script>

<template>
  <Teleport to="body">
    <div class="app-toast-stack" role="status" aria-live="polite">
      <TransitionGroup name="app-toast">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="app-toast"
          :class="`app-toast--${toast.tone}`"
        >
          <span class="app-toast__icon" aria-hidden="true">{{ icons[toast.tone] }}</span>
          <span class="app-toast__message">{{ toast.message }}</span>
          <button
            class="app-toast__close"
            type="button"
            aria-label="关闭提示"
            @click="dismiss(toast.id)"
          >
            ×
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.app-toast-stack {
  position: fixed;
  top: var(--space-4);
  right: var(--space-4);
  z-index: 200;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  max-width: min(360px, calc(100vw - var(--space-6)));
}

.app-toast {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  background: var(--surface);
  box-shadow: var(--shadow-md);
  font: var(--text-secondary);
  color: var(--ink);
}

.app-toast__icon {
  display: inline-flex;
  width: 20px;
  height: 20px;
  flex: none;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-pill);
  color: var(--surface);
  font-size: 12px;
  font-weight: 700;
}

.app-toast--success .app-toast__icon {
  background: var(--success);
}

.app-toast--warning .app-toast__icon {
  background: var(--warning);
}

.app-toast--error .app-toast__icon {
  background: var(--emergency);
}

.app-toast__message {
  flex: 1;
}

.app-toast__close {
  display: inline-flex;
  width: 24px;
  height: 24px;
  min-height: 24px;
  flex: none;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-sm);
  color: var(--ink-muted);
  background: transparent;
  font-size: 14px;
  cursor: pointer;
}

.app-toast__close:hover {
  color: var(--ink);
  background: var(--surface-muted);
}

.app-toast-enter-active,
.app-toast-leave-active {
  transition:
    transform var(--duration-normal) var(--ease-standard),
    opacity var(--duration-normal) var(--ease-standard);
}

.app-toast-enter-from,
.app-toast-leave-to {
  opacity: 0;
  transform: translateX(16px);
}

.app-toast-move {
  transition: transform var(--duration-normal) var(--ease-standard);
}
</style>
