import { createPinia } from 'pinia'
import { cleanup, render, screen, waitFor } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest'

import AuditPage from '@/pages/AuditPage.vue'
import ReportPage from '@/pages/ReportPage.vue'
import SettingsPage from '@/pages/SettingsPage.vue'
import SimulationActionPanel from '@/components/SimulationActionPanel.vue'

let reportReads = 0
const server = setupServer(
  http.get('*/api/v1/reports/operations', () => HttpResponse.json({
    from: '2026-08-01', to: '2026-08-27', timeZone: 'Asia/Shanghai', generatedAt: '2026-08-27T10:00:00Z', areaLabel: 'A01',
    summary: { newEventCount: 20, closedEventCount: 2, completedVisitCount: 3, completedOrderCount: 4, completedFollowUpCount: 5,
      visitedElderCount: 3, averageAcceptanceMinutes: null, currentOpenTaskCount: 1, currentOverdueTaskCount: 0 },
    daily: [], personnel: [],
  })),
  http.get('*/health/ready', () =>
    HttpResponse.json({
      status: 'ready',
      components: [
        { name: 'database', status: 'ready', detail: 'SQLite connected' },
        { name: 'backgroundJobs', status: 'ready', detail: 'workers registered' },
        { name: 'ai', status: 'degraded', detail: 'fixed fallback active' },
        { name: 'deviceGateway', status: 'ready', detail: 'simulator only' },
        { name: 'localNetwork', status: 'degraded', detail: 'loopback binding' },
      ],
    }),
  ),
  http.get('*/api/v1/reports/demo-summary', () => {
    reportReads++
    return HttpResponse.json({
      label: '当前数据',
      elderCount: 20,
      openEventCount: 1,
      completedVisitCount: 2,
      activeServiceOrderCount: 3,
      simulationAttemptCount: 4,
      deviceSignalCount: 5,
      confirmedMemoryCount: 1,
    })
  }),
  http.get('*/api/v1/audit', () =>
    HttpResponse.json([
      {
        id: crypto.randomUUID(),
        actorKind: 'CommunityStaff',
        action: 'EventAccepted',
        entityType: 'CareEvent',
        entityId: crypto.randomUUID(),
        occurredAt: '2026-08-24T08:00:00Z',
        reason: '工作人员接单',
        beforeStatus: 'PendingConfirmation',
        afterStatus: 'Accepted',
        isDemoData: true,
      },
    ]),
  ),
  http.post('*/api/v1/demo/reset', ({ request }) => {
    if (request.headers.get('X-Confirm-Demo-Reset') !== 'RESET-20') {
      return HttpResponse.json({ code: 'RESET_CONFIRMATION_REQUIRED' }, { status: 400 })
    }
    return HttpResponse.json({
      elderCount: 20,
      mainElderId: crypto.randomUUID(),
      baseTime: '2026-08-24T08:00:00Z',
      elapsedMilliseconds: 120,
    })
  }),
)

const routerLinkStub = { props: ['to'], template: '<a :href="to"><slot /></a>' }
const renderOptions = { global: { plugins: [createPinia()], stubs: { RouterLink: routerLinkStub } } }

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  reportReads = 0
  server.resetHandlers()
  vi.restoreAllMocks()
})
afterAll(() => server.close())

describe('audit and data operations pages', () => {
  it('labels reports with normal product copy and renders an audit trail', async () => {
    render(ReportPage, renderOptions)
    expect(await screen.findByText('暂无数据')).toBeTruthy()
    expect(screen.getByRole('heading', { name: '社区照料报告' })).toBeTruthy()
    expect(screen.queryByText(/演示数据|演示运行/)).toBeNull()
    expect(await screen.findByText('20')).toBeTruthy()
    cleanup()

    render(AuditPage)
    expect(await screen.findByText('EventAccepted')).toBeTruthy()
    expect(screen.getByText('PendingConfirmation → Accepted')).toBeTruthy()
  })

  it('requires exact reset text and a second confirmation, then refetches counts', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const user = userEvent.setup()
    render(SettingsPage)

    expect(await screen.findByText('database')).toBeTruthy()
    expect(screen.getByRole('button', { name: '恢复 20 人初始数据' }).hasAttribute('disabled')).toBe(
      true,
    )
    await user.type(screen.getByLabelText('输入 RESET-20'), 'RESET-20')
    await user.click(screen.getByRole('button', { name: '恢复 20 人初始数据' }))

    expect(window.confirm).toHaveBeenCalledExactlyOnceWith(
      '再次确认：恢复初始数据会清除当前业务记录，是否继续？',
    )
    expect(await screen.findByText('重置完成：20 份老人档案')).toBeTruthy()
    await waitFor(() => expect(reportReads).toBeGreaterThanOrEqual(2))
  })

  it('shows persisted simulation progress and never claims contact after failure', async () => {
    server.use(
      http.post('*/api/v1/care-events/:eventId/simulation-attempts', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({
          attemptId: crypto.randomUUID(),
          careEventId: crypto.randomUUID(),
          requestId: crypto.randomUUID(),
          channel: 'Phone',
          recipientRole: 'Family',
          attemptedAt: '2026-08-24T08:00:00Z',
          outcome: '模拟失败',
          isSimulation: true,
          isDuplicate: false,
        })
      }),
    )
    const user = userEvent.setup()
    render(SimulationActionPanel, {
      props: { attempts: [], eventId: crypto.randomUUID() },
    })

    await user.click(screen.getByRole('button', { name: '模拟电话' }))
    expect(screen.getByText('模拟发送中')).toBeTruthy()
    expect(await screen.findByText('模拟失败')).toBeTruthy()
    expect(screen.queryByText(/已联系/)).toBeNull()
  })
})
