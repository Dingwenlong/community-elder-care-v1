import { cleanup, render, screen, within } from '@testing-library/vue'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, describe, expect, it } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'

import CommunityLayout from '@/layouts/CommunityLayout.vue'
import DashboardPage from '@/pages/DashboardPage.vue'
import { useAuthStore, type DemoRole } from '@/stores/auth'

afterEach(cleanup)

async function renderWithRouter(path: string, role: DemoRole) {
  const pinia = createPinia()
  setActivePinia(pinia)
  const auth = useAuthStore()
  auth.setSession({
    token: 'test-token',
    role,
    shell: role === 'ServiceWorker' ? 'service' : 'community',
    isDemoMode: true,
  })
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: CommunityLayout,
        children: [
          { path: 'dashboard', component: DashboardPage },
          { path: 'elders', component: { template: '<h1>老人档案</h1>' } },
        ],
      },
    ],
  })
  await router.push(path)
  await router.isReady()

  render(defineComponent({ template: '<RouterView />' }), {
    global: { plugins: [pinia, router] },
  })
}

describe('CommunityLayout', () => {
  it('keeps dashboard concise and exposes elder records as a separate route', async () => {
    await renderWithRouter('/dashboard', 'CommunityStaff')

    expect(screen.getByRole('link', { name: '老人档案' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: '待处理事项' })).toBeTruthy()
    expect(screen.queryByText('全部老人档案')).toBeNull()
  })

  it('hides elder-record navigation from service workers', async () => {
    await renderWithRouter('/dashboard', 'ServiceWorker')

    expect(screen.queryByRole('link', { name: '老人档案' })).toBeNull()
    expect(screen.queryByRole('link', { name: '工作台' })).toBeNull()
    expect(screen.queryByRole('link', { name: '照料事件' })).toBeNull()
    expect(screen.queryByRole('link', { name: '探访任务' })).toBeNull()
    expect(screen.queryByRole('link', { name: '服务工单' })).toBeNull()
    expect(screen.getByRole('link', { name: '我的任务' })).toBeTruthy()
  })

  it('shows the demo-data marker on every authenticated route', async () => {
    await renderWithRouter('/elders', 'CommunityStaff')

    expect(screen.getByText('演示数据')).toBeTruthy()
    expect(screen.getByRole('heading', { name: '老人档案' })).toBeTruthy()
  })

  it('shows the complete eight-destination navigation only to administrators', async () => {
    await renderWithRouter('/dashboard', 'Administrator')

    const sidebar = screen.getByRole('complementary', { name: '社区工作区导航' })
    expect(within(sidebar).getAllByRole('link').map((link) => link.textContent)).toEqual([
      '工作台',
      '老人档案',
      '照料事件',
      '探访任务',
      '服务工单',
      '设备信号',
      '报告与审计',
      '系统设置',
    ])

    cleanup()
    await renderWithRouter('/dashboard', 'CommunityStaff')
    expect(screen.queryByRole('link', { name: '设备信号' })).toBeNull()
    expect(screen.queryByRole('link', { name: '系统设置' })).toBeNull()
    expect(screen.getByRole('link', { name: '报告与审计' })).toBeTruthy()
  })
})
