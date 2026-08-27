import { defineStore } from 'pinia'

import { apiClient, configureApiAuthorization } from '@/api/apiClient'
import type { AppShell, DemoRole, LoginResponse, ServerLoginResponse } from '@/api/contracts'

export type { DemoRole } from '@/api/contracts'

const storageKey = 'community-care-demo-session'

export interface AuthSession {
  token: string
  role: DemoRole
  shell: AppShell
  isDemoMode: true
}

function normalizedShell(role: DemoRole): AppShell {
  const shells: Record<DemoRole, AppShell> = {
    Elder: 'elder',
    Family: 'family',
    CommunityStaff: 'community',
    ServiceWorker: 'service',
    Administrator: 'admin',
  }
  return shells[role]
}

function readSession(): AuthSession | null {
  if (typeof window === 'undefined') return null
  const stored = window.sessionStorage.getItem(storageKey)
  if (!stored) return null
  try {
    const session = JSON.parse(stored) as Partial<AuthSession>
    if (!session.token || !session.role || !session.shell || session.isDemoMode !== true) return null
    return session as AuthSession
  } catch {
    window.sessionStorage.removeItem(storageKey)
    return null
  }
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: null as string | null,
    role: null as DemoRole | null,
    shell: null as AppShell | null,
    isDemoMode: false,
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token),
    userId: (state): string | null => {
      try {
        const payload = state.token?.split('.')[1]
        if (!payload) return null
        return (JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as { sub?: string }).sub ?? null
      } catch { return null }
    },
  },
  actions: {
    restore() {
      const session = readSession()
      if (session) this.setSession(session)
    },
    setSession(session: AuthSession) {
      this.token = session.token
      this.role = session.role
      this.shell = session.shell
      this.isDemoMode = session.isDemoMode
      if (typeof window !== 'undefined') {
        window.sessionStorage.setItem(storageKey, JSON.stringify(session))
      }
      configureApiAuthorization(session.token, () => this.clearSession())
    },
    clearSession() {
      this.$reset()
      if (typeof window !== 'undefined') window.sessionStorage.removeItem(storageKey)
      configureApiAuthorization(null)
    },
    async login(username: string, password: string): Promise<LoginResponse> {
      const response = await apiClient.request<ServerLoginResponse>('/api/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
      })
      const normalized: LoginResponse = {
        ...response,
        shell: normalizedShell(response.role),
      }
      this.setSession({
        token: normalized.accessToken,
        role: normalized.role,
        shell: normalized.shell,
        isDemoMode: normalized.isDemoMode,
      })
      return normalized
    },
  },
})
