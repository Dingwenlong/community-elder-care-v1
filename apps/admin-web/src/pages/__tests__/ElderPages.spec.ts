import { cleanup, render, screen, waitFor } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { createPinia, setActivePinia } from 'pinia'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'

import ElderDetailPage from '@/pages/ElderDetailPage.vue'
import ElderEditPage from '@/pages/ElderEditPage.vue'
import ElderListPage from '@/pages/ElderListPage.vue'
import { useAuthStore, type DemoRole } from '@/stores/auth'

const elderId = '11111111-1111-1111-1111-111111111101'
const listFixture = [
  {
    id: elderId,
    demoDisplayName: '李秀兰',
    areaCode: 'A01',
    attentionLevel: 'Priority',
    nextCheckInDueAt: '2026-08-24T09:30:00Z',
    isDemoData: true,
    latestStatus: '等待今日平安确认',
    nextVisit: '今天 14:00',
    currentOpenEvent: '需确认',
  },
]

const server = setupServer(
  http.get('*/api/v1/elders', () => HttpResponse.json(listFixture)),
  http.get(`*/api/v1/elders/${elderId}`, () =>
    HttpResponse.json({
      id: elderId,
      demoDisplayName: '李秀兰',
      birthDate: '1948-03-12',
      areaCode: 'A01',
      attentionLevel: 'Priority',
      nextCheckInDueAt: '2026-08-24T09:30:00Z',
      isDemoData: true,
      healthRisks: [{ code: 'FALL', demoLabel: '跌倒风险' }],
      serviceNeeds: [{ code: 'MEAL', demoLabel: '助餐协助' }],
      emergencyContacts: [
        { demoName: '李家属', relationship: '女儿', phoneNumber: '19900000001', contactOrder: 1 },
      ],
    }),
  ),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  window.sessionStorage.clear()
})
afterAll(() => server.close())

async function renderPage(
  component: object,
  path: string,
  role: DemoRole = 'CommunityStaff',
) {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().setSession({
    token: 'test-token',
    role,
    shell: role === 'Family' ? 'family' : role === 'ServiceWorker' ? 'service' : 'community',
    isDemoMode: true,
  })
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/elders', component: ElderListPage },
      { path: '/elders/:elderId', component: ElderDetailPage },
      { path: '/elders/:elderId/edit', component: ElderEditPage },
      { path: '/dashboard', component: { template: '<h1>工作台</h1>' } },
    ],
  })
  await router.push(path)
  await router.isReady()
  render(defineComponent({ template: '<RouterView />' }), {
    global: { plugins: [pinia, router] },
  })
}

describe('ElderListPage', () => {
  it('filters by attention level and renders only area-safe results', async () => {
    let requestedAttention = ''
    server.use(
      http.get('*/api/v1/elders', ({ request }) => {
        requestedAttention = new URL(request.url).searchParams.get('attentionLevel') ?? ''
        return HttpResponse.json(listFixture)
      }),
    )
    await renderPage(ElderListPage, '/elders')
    const user = userEvent.setup()

    await screen.findByText('李秀兰')
    await user.selectOptions(screen.getByRole('combobox', { name: '关注等级' }), 'Priority')

    await waitFor(() => expect(requestedAttention).toBe('Priority'))
    expect(screen.getByText('A01')).toBeTruthy()
    expect(screen.queryByText('A02')).toBeNull()
  })

  it('shows loading, empty and error states explicitly', async () => {
    server.use(
      http.get('*/api/v1/elders', async () => {
        await delay(100)
        return HttpResponse.json([])
      }),
    )
    await renderPage(ElderListPage, '/elders')

    expect(screen.getByText('正在载入老人档案')).toBeTruthy()
    const emptyTitle = await screen.findByText('当前筛选条件下没有老人档案')
    expect(emptyTitle.closest('.status-notice')?.querySelector('img')?.getAttribute('src')).toContain(
      'elder-records-empty.webp',
    )

    cleanup()
    server.use(
      http.get('*/api/v1/elders', () =>
        HttpResponse.json({ title: '失败', code: 'REQUEST_FAILED' }, { status: 500 }),
      ),
    )
    await renderPage(ElderListPage, '/elders')
    expect((await screen.findByRole('alert')).textContent).toContain('请求未完成，请稍后重试。')
  })
})

describe('ElderDetailPage', () => {
  it('shows authorized community fields without an internal data marker', async () => {
    await renderPage(ElderDetailPage, `/elders/${elderId}`)

    expect(await screen.findByRole('heading', { name: '李秀兰' })).toBeTruthy()
    expect(screen.getByText('跌倒风险')).toBeTruthy()
    expect(screen.getByText('助餐协助')).toBeTruthy()
    expect(screen.queryByText('演示数据')).toBeNull()
  })

  it.each<DemoRole>(['Family', 'ServiceWorker'])(
    'does not invent placeholders for fields omitted from the %s projection',
    async (role) => {
      server.use(
        http.get(`*/api/v1/elders/${elderId}`, () =>
          HttpResponse.json({
            id: elderId,
            demoDisplayName: '李秀兰',
            isDemoData: true,
            recentStatus: { state: 'AwaitingDemoCheckIn', latestCheckInAt: null },
          }),
        ),
      )

      await renderPage(ElderDetailPage, `/elders/${elderId}`, role)

      expect(await screen.findByRole('heading', { name: '李秀兰' })).toBeTruthy()
      expect(screen.queryByRole('heading', { name: '健康风险' })).toBeNull()
      expect(screen.queryByText('暂无')).toBeNull()
      expect(screen.queryByText('演示数据')).toBeNull()
    },
  )
})

describe('ElderEditPage', () => {
  it('keeps risks and service needs separate and blocks submission without a reason', async () => {
    let updateCount = 0
    server.use(
      http.put(`*/api/v1/elders/${elderId}/care-profile`, () => {
        updateCount += 1
        return HttpResponse.json({})
      }),
    )
    await renderPage(ElderEditPage, `/elders/${elderId}/edit`)
    const user = userEvent.setup()

    expect(await screen.findByRole('heading', { name: '编辑李秀兰' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: '健康风险' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: '服务需求' })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: '保存修改' }))

    expect(screen.getByText('请填写本次修改原因。')).toBeTruthy()
    expect(updateCount).toBe(0)
  })
})
