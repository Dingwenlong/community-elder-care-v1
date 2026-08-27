import { cleanup, render, screen, waitFor } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { createPinia, setActivePinia } from 'pinia'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'

import OperationsPage from '@/pages/OperationsPage.vue'
import CareTaskComposer from '@/components/CareTaskComposer.vue'
import type { CareEvent } from '@/api/contracts'
import ServiceWorkerTasksPage from '@/pages/ServiceWorkerTasksPage.vue'
import ServiceOrderListPage from '@/pages/ServiceOrderListPage.vue'
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
    token: 'header.' + btoa(JSON.stringify({ sub: '33333333-3333-3333-3333-333333333301' })) + '.signature',
    role,
    shell: role === 'ServiceWorker' ? 'service' : 'community',
    isDemoMode: true,
  })
  return pinia
}

describe('VisitListPage', () => {
  it('uses the care-work illustration when there are no visit tasks', async () => {
    server.use(http.get('*/api/v1/visits', () => HttpResponse.json([])))

    const pinia = setSession('CommunityStaff')
    render(VisitListPage, { global: { plugins: [pinia] } })

    const title = await screen.findByText('当前没有探访任务')
    expect(title.closest('.status-notice')?.querySelector('img')?.getAttribute('src')).toContain(
      'care-work-empty.webp',
    )
  })

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

describe('ServiceOrderListPage', () => {
  it('uses the care-work illustration when there are no service orders', async () => {
    server.use(http.get('*/api/v1/service-orders', () => HttpResponse.json([])))

    const pinia = setSession('CommunityStaff')
    render(ServiceOrderListPage, { global: { plugins: [pinia] } })

    const title = await screen.findByText('当前没有服务工单')
    expect(title.closest('.status-notice')?.querySelector('img')?.getAttribute('src')).toContain(
      'care-work-empty.webp',
    )
  })
})

describe('ServiceWorkerTasksPage', () => {
  it('uses the care-work illustration when there are no assigned tasks', async () => {
    server.use(http.get('*/api/v1/service-orders/my-tasks', () => HttpResponse.json([])))

    const pinia = setSession('ServiceWorker')
    render(ServiceWorkerTasksPage, { global: { plugins: [pinia] } })

    const title = await screen.findByText('当前没有获派任务')
    expect(title.closest('.status-notice')?.querySelector('img')?.getAttribute('src')).toContain(
      'care-work-empty.webp',
    )
  })

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


describe('OperationsPage', () => {
  const people = [
    { userId: 'owner', displayName: '周敏', role: 'CommunityStaff', areaCode: 'A01', pendingCount: 1, overdueCount: 0 },
    { userId: 'other', displayName: '陈佳', role: 'CommunityStaff', areaCode: 'A01', pendingCount: 0, overdueCount: 0 },
    { userId: 'worker', displayName: '王芳', role: 'ServiceWorker', areaCode: 'A01', pendingCount: 0, overdueCount: 0 },
  ]
  const task = { taskId: visitId, taskType: 'Visit', careEventId: 'event', elderDisplayName: '李秀兰', assignedUserId: 'owner', assignedDisplayName: '周敏', areaCode: 'A01', status: 'Assigned', dueAt: null, version: 'version-1', eventOwnerUserId: 'owner', isOverdue: false }
  function setup() {
    const pinia = setSession('CommunityStaff')
    useAuthStore().setSession({ token: 'header.' + btoa(JSON.stringify({ sub: 'owner' })) + '.signature', role: 'CommunityStaff', shell: 'community', isDemoMode: true })
    server.use(http.get('*/api/v1/operations/personnel', () => HttpResponse.json(people)),
      http.get('*/api/v1/operations/tasks', () => HttpResponse.json([task])))
    return { global: { plugins: [pinia], stubs: { RouterLink: { template: '<a><slot /></a>' } } } }
  }
  it('submits the current version and preserves unknown deadlines', async () => {
    let body: Record<string, unknown> | undefined
    const options = setup()
    server.use(http.post('*/api/v1/visits/:id/reassign', async ({ request }) => { body = await request.json() as Record<string, unknown>; return HttpResponse.json({ taskId: visitId }) }))
    render(OperationsPage, options)
    expect(await screen.findByText('未设截止时间')).toBeTruthy()
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: '转派' }))
    await user.selectOptions(screen.getByLabelText('新负责人'), 'other')
    expect(screen.getByLabelText('新负责人').textContent).not.toContain('王芳')
    await user.type(screen.getByLabelText('转派原因'), '调整上门安排')
    await user.click(screen.getByRole('button', { name: '确认保存' }))
    expect(await screen.findByText('任务已转派，原工作人员不能再操作此任务。')).toBeTruthy()
    expect(body).toEqual({ assignedUserId: 'other', reason: '调整上门安排', expectedVersion: 'version-1' })
  })
  it('keeps a conflict visible without claiming successful reassignment', async () => {
    render(OperationsPage, setup())
    server.use(http.post('*/api/v1/visits/:id/reassign', () => HttpResponse.json({ code: 'CONCURRENT_CHANGE' }, { status: 409 })))
    const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: '转派' }))
    await user.selectOptions(screen.getByLabelText('新负责人'), 'other')
    await user.type(screen.getByLabelText('转派原因'), '调整上门安排')
    await user.click(screen.getByRole('button', { name: '确认保存' }))
    expect(await screen.findByRole('alert')).toHaveProperty('textContent', '资料已被更新，请刷新后重试。')
    expect(screen.getByRole('dialog')).toBeTruthy()
  })
  it('creates an order with an actual deadline and a selected worker', async () => {
    let body: Record<string, unknown> | undefined
    const options = setup()
    server.use(http.post('*/api/v1/care-events/event/service-orders', async ({ request }) => { body = await request.json() as Record<string, unknown>; return HttpResponse.json({}) }))
    render(CareTaskComposer, { ...options, props: { careEvent: { id: 'event', currentOwnerUserId: 'owner', status: 'Accepted' } as CareEvent } })
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: '创建工单' }))
    await user.selectOptions(await screen.findByLabelText('任务负责人'), 'worker')
    await user.type(screen.getByLabelText('联系说明'), '到达后联系社区')
    await user.click(screen.getByRole('button', { name: '保存任务' }))
    await waitFor(() => expect(body?.assignedWorkerUserId).toBe('worker'))
    expect(body?.dueAt).toMatch(/Z$/)
    expect(body?.isMandatory).toBe(true)
  })
})
