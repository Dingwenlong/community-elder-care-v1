import { reactive, readonly } from 'vue'

export type ToastTone = 'success' | 'warning' | 'error'

export interface ToastItem {
  id: number
  tone: ToastTone
  message: string
}

const toasts = reactive<ToastItem[]>([])
let nextId = 1

const DURATION_MS = 4000

function push(tone: ToastTone, message: string) {
  const id = nextId++
  toasts.push({ id, tone, message })
  window.setTimeout(() => dismiss(id), DURATION_MS)
}

function dismiss(id: number) {
  const index = toasts.findIndex((toast) => toast.id === id)
  if (index >= 0) toasts.splice(index, 1)
}

export function useToast() {
  return {
    toasts: readonly(toasts),
    dismiss,
    success: (message: string) => push('success', message),
    warning: (message: string) => push('warning', message),
    error: (message: string) => push('error', message),
  }
}
