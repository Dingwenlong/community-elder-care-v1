import { cleanup, render, screen } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { createPinia, setActivePinia } from 'pinia'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'

import ServiceWorkerTasksPage from '@/pages/ServiceWorkerTasksPage.vue'
import VisitListPage from '@/pages/VisitListPage.vue'
import { useAuthStore, type DemoRole } from '@/stores/auth'

const visitId = '55555555-5555-5555-5555-555555555501'

const server = setupServer(
  http.get('*/api/v1/visits', () =>
    HttpResponse.json([
      {
        visitId,
        careEventId: '22222222-2222-2222-2222-222222222201',
        elderDisplayName: '李秀兰',
        assignedStaffUserId: '33333333-3333-3333-3333-333333333301',
        scheduledStartAt: '2026-08-24T09:00:00Z',
        scheduledEndAt: '2026-08-24T09:30:00Z',
        startedAt: '2026-08-24T09:01:00Z',
        completedAt: null,
        confirmedSummary: null,
        result: null,
        status: 'InProgress',
        isMandatory: true,
        isDemoData: true,
      },
    ]),
  ),
  http.get('*/api/v1/service-orders/my-tasks', () =>
    HttpResponse.json([
      {
        orderId: '66666666-6666-6666-6666-666666666601',
        elderDisplayName: '李秀兰',
        serviceType: '助餐配送',
        scheduledWindow: '今天 11:00—11:30',
        contactInstruction: '到门口后敲门三次',
        status: 'Assigned',
      },
    ]),
  ),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  window.sessionStorage.clear()
})
afterAll(() => server.close())

function setSession(role: DemoRole) {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().setSession({
    token: 'test-token',
    role,
    shell: role === 'ServiceWorker' ? 'service' : 'community',
    isDemoMode: true,
  })
  return pinia
}

describe('VisitListPage', () => {
  it('keeps raw staff notes separate from the confirmed result', async () => {
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.post(`*/api/v1/visits/${visitId}/complete`, async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ status: 'Completed' })
      }),
    )
    const pinia = setSession('CommunityStaff')
    render(VisitListPage, { global: { plugins: [pinia] } })
    const user = userEvent.setup()

    expect(await screen.findByText('李秀兰')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: '完成探访' }))
    await user.type(screen.getByLabelText('内部原始记录'), '老人说昨夜睡眠较差')
    await user.type(screen.getByLabelText('对外确认摘要'), '老人意识清楚，已确认安全')
    await user.type(screen.getByLabelText('探访结果'), '本次探访已完成')
    await user.click(screen.getByRole('button', { name: '提交探访结果' }))

    expect(requestBody).toEqual({
      rawStaffNote: '老人说昨夜睡眠较差',
      confirmedSummary: '老人意识清楚，已确认安全',
      result: '本次探访已完成',
    })
    expect(screen.queryByText('老人说昨夜睡眠较差')).toBeNull()
  })
})

describe('ServiceWorkerTasksPage', () => {
  it('renders only the minimal assigned-order fields', async () => {
    const pinia = setSession('ServiceWorker')
    render(ServiceWorkerTasksPage, { global: { plugins: [pinia] } })

    expect(await screen.findByText('李秀兰')).toBeTruthy()
    expect(screen.getByText('助餐配送')).toBeTruthy()
    expect(screen.getByText('今天 11:00—11:30')).toBeTruthy()
    expect(screen.getByText('到门口后敲门三次')).toBeTruthy()
    expect(screen.queryByText('健康风险')).toBeNull()
    expect(screen.queryByText('家属')).toBeNull()
    expect(screen.queryByText('社区备注')).toBeNull()
  })
})
