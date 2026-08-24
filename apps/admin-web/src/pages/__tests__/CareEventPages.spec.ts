import { cleanup, render, screen, waitFor, within } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { createPinia, setActivePinia } from 'pinia'
import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'

import BreakGlassDialog from '@/components/BreakGlassDialog.vue'
import EventTimeline from '@/components/EventTimeline.vue'
import TransitionDialog from '@/components/TransitionDialog.vue'
import CareEventDetailPage from '@/pages/CareEventDetailPage.vue'
import CareEventListPage from '@/pages/CareEventListPage.vue'
import { useAuthStore } from '@/stores/auth'

const emergencyId = '22222222-2222-2222-2222-222222222201'
const confirmationId = '22222222-2222-2222-2222-222222222202'
const elderId = '11111111-1111-1111-1111-111111111101'

const eventFixture = [
  {
    id: confirmationId,
    elderId,
    category: 'SafetyHealth',
    level: 'NeedsConfirmation',
    status: 'PendingConfirmation',
    source: 'CheckIn',
    summary: '尚未完成今日平安确认',
    occurredAt: '2026-08-24T08:10:00Z',
    createdAt: '2026-08-24T08:10:00Z',
    lastActivityAt: '2026-08-24T08:10:00Z',
    responsibilityQueue: 'A01-CARE',
    currentOwnerUserId: null,
    resolution: null,
    isDemoData: true,
    isDuplicate: false,
    evidence: [],
    transitions: [],
    contactAttempts: [],
    allowedTransitions: ['Accepted', 'FalseAlarm', 'UnableToConfirm'],
  },
  {
    id: emergencyId,
    elderId,
    category: 'SafetyHealth',
    level: 'Emergency',
    status: 'Accepted',
    source: 'Device',
    summary: '设备检测到紧急信号',
    occurredAt: '2026-08-24T08:20:00Z',
    createdAt: '2026-08-24T08:20:00Z',
    lastActivityAt: '2026-08-24T08:22:00Z',
    responsibilityQueue: 'A01-EMERGENCY',
    currentOwnerUserId: '33333333-3333-3333-3333-333333333301',
    resolution: null,
    isDemoData: true,
    isDuplicate: false,
    evidence: [],
    transitions: [],
    contactAttempts: [
      {
        id: '44444444-4444-4444-4444-444444444401',
        kind: 'EmergencyContact',
        targetLabel: '第一联系人',
        attemptedAt: '2026-08-24T08:21:00Z',
        outcome: '模拟未接通',
        isSimulation: true,
      },
    ],
    allowedTransitions: ['InProgress', 'UnableToConfirm'],
  },
]

const server = setupServer(
  http.get('*/api/v1/care-events', () => HttpResponse.json(eventFixture)),
  http.get('*/api/v1/elders', () =>
    HttpResponse.json([
      {
        id: elderId,
        demoDisplayName: '李秀兰',
        areaCode: 'A01',
        attentionLevel: 'Priority',
        nextCheckInDueAt: '2026-08-24T09:30:00Z',
        isDemoData: true,
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

async function renderEventPage(component: object, path: string) {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().setSession({
    token: 'test-token',
    role: 'CommunityStaff',
    shell: 'community',
    isDemoMode: true,
  })
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/care-events', component: CareEventListPage },
      { path: '/care-events/:eventId', component: CareEventDetailPage },
    ],
  })
  await router.push(path)
  await router.isReady()
  render(defineComponent({ template: '<RouterView />' }), {
    global: { plugins: [pinia, router] },
  })
}

describe('CareEventListPage', () => {
  it('sorts emergencies before waiting time and keeps level separate from status', async () => {
    await renderEventPage(CareEventListPage, '/care-events')

    const rows = await screen.findAllByRole('row')
    expect(rows[1]?.textContent).toContain('紧急')
    expect(rows[1]?.textContent).toContain('已受理')
    expect(rows[1]?.textContent).toContain('李秀兰')
    expect(rows[1]?.textContent).toContain('模拟')
    expect(rows[2]?.textContent).toContain('需确认')
    expect(rows[2]?.textContent).toContain('待确认')
    expect(screen.getByRole('columnheader', { name: '级别' })).toBeTruthy()
    expect(screen.getByRole('columnheader', { name: '状态' })).toBeTruthy()
    expect(screen.getByRole('columnheader', { name: '当前负责人' })).toBeTruthy()
    expect(screen.getByRole('columnheader', { name: '下一步' })).toBeTruthy()
  })
})

describe('TransitionDialog', () => {
  it('requires a reason before marking an event as a false alarm', async () => {
    const submit = vi.fn<(reason: string, resolution: string | null) => void>()
    render(TransitionDialog, { props: { target: 'FalseAlarm', onSubmit: submit } })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: '确认提交' }))

    expect(screen.getByText('请填写判断依据')).toBeTruthy()
    expect(submit).not.toHaveBeenCalled()
  })

  it('warns that unable-to-confirm enters escalation and cannot close directly', () => {
    render(TransitionDialog, {
      props: {
        target: 'UnableToConfirm',
        onSubmit: vi.fn<(reason: string, resolution: string | null) => void>(),
      },
    })

    expect(screen.getByText('将进入联系升级，不能直接关单')).toBeTruthy()
  })
})

describe('BreakGlassDialog', () => {
  it('requires a reason and states the 15-minute expiry for emergencies', async () => {
    const submit = vi.fn<(reason: string) => void>()
    render(BreakGlassDialog, {
      props: { eventLevel: 'Emergency', elderId, onSubmit: submit },
    })
    const user = userEvent.setup()

    expect(screen.getByText('临时授权将在 15 分钟后失效')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: '申请临时授权' }))
    expect(screen.getByText('请填写紧急访问原因')).toBeTruthy()
    expect(submit).not.toHaveBeenCalled()
  })

  it('is absent for non-emergency events', () => {
    render(BreakGlassDialog, {
      props: {
        eventLevel: 'NeedsConfirmation',
        elderId,
        onSubmit: vi.fn<(reason: string) => void>(),
      },
    })

    expect(screen.queryByRole('button', { name: '申请临时授权' })).toBeNull()
  })
})

describe('CareEventDetailPage', () => {
  it('does not render raw AI evidence text', () => {
    render(EventTimeline, {
      props: {
        evidence: [
          {
            id: 'ai-1',
            kind: 'AiCue',
            summary: '未经确认的原始模型文本',
            occurredAt: '2026-08-24T08:00:00Z',
            recordedAt: '2026-08-24T08:00:01Z',
            isSimulation: true,
          },
        ],
        transitions: [],
        contactAttempts: [],
      },
    })

    expect(screen.getByText('AI 已生成结构化风险提示，原始内容不展示。')).toBeTruthy()
    expect(screen.queryByText('未经确认的原始模型文本')).toBeNull()
  })

  it('renders every persisted main-story step exactly once in chronological order', async () => {
    const event = {
      ...eventFixture[1],
      evidence: [
        {
          id: 'ev-1',
          kind: 'MissedCheckIn',
          summary: '漏签生成照料事件',
          occurredAt: '2026-08-24T08:00:00Z',
          recordedAt: '2026-08-24T08:00:01Z',
          isSimulation: false,
        },
        {
          id: 'ev-2',
          kind: 'DeviceSignal',
          summary: '设备证据已合并',
          occurredAt: '2026-08-24T08:01:00Z',
          recordedAt: '2026-08-24T08:01:01Z',
          isSimulation: true,
        },
        {
          id: 'ev-3',
          kind: 'VisitCompleted',
          summary: '探访确认老人意识清楚',
          occurredAt: '2026-08-24T08:04:00Z',
          recordedAt: '2026-08-24T08:04:01Z',
          isSimulation: true,
        },
        {
          id: 'ev-4',
          kind: 'FollowUpCompleted',
          summary: '随访完成',
          occurredAt: '2026-08-24T08:08:00Z',
          recordedAt: '2026-08-24T08:08:01Z',
          isSimulation: true,
        },
      ],
      transitions: [
        {
          id: 'tr-1',
          fromStatus: 'PendingConfirmation',
          toStatus: 'Accepted',
          actorKind: 'Staff',
          actorUserId: 'staff-1',
          reason: '工作人员受理',
          occurredAt: '2026-08-24T08:02:00Z',
          isSimulation: false,
        },
        {
          id: 'tr-2',
          fromStatus: 'InProgress',
          toStatus: 'Resolved',
          actorKind: 'Staff',
          actorUserId: 'staff-1',
          reason: '现场确认无持续危险',
          occurredAt: '2026-08-24T08:06:00Z',
          isSimulation: false,
        },
        {
          id: 'tr-3',
          fromStatus: 'FollowUpPending',
          toStatus: 'Closed',
          actorKind: 'Staff',
          actorUserId: 'staff-1',
          reason: '随访完成后结案',
          occurredAt: '2026-08-24T08:09:00Z',
          isSimulation: false,
        },
      ],
      contactAttempts: [
        {
          id: 'ct-1',
          kind: 'EmergencyContact',
          targetLabel: '第一联系人',
          attemptedAt: '2026-08-24T08:05:00Z',
          outcome: '模拟联系成功',
          isSimulation: true,
        },
        {
          id: 'ct-2',
          kind: 'EmergencyTransport',
          targetLabel: '120',
          attemptedAt: '2026-08-24T08:05:30Z',
          outcome: '模拟已记录',
          isSimulation: true,
        },
      ],
    }
    server.use(
      http.get(`*/api/v1/care-events/${emergencyId}`, () => HttpResponse.json(event)),
      http.get(`*/api/v1/elders/${elderId}`, () =>
        HttpResponse.json({ id: elderId, demoDisplayName: '李秀兰', isDemoData: true }),
      ),
      http.get('*/api/v1/visits', () => HttpResponse.json([])),
      http.get('*/api/v1/service-orders', () => HttpResponse.json([])),
      http.get('*/api/v1/follow-ups', () => HttpResponse.json([])),
    )

    await renderEventPage(CareEventDetailPage, `/care-events/${emergencyId}`)

    const timeline = await screen.findByRole('region', { name: '照料时间线' })

    for (const label of [
      '漏签生成照料事件',
      '设备证据已合并',
      '工作人员受理',
      '探访确认老人意识清楚',
      '模拟联系成功',
      '模拟已记录',
      '现场确认无持续危险',
      '随访完成',
      '随访完成后结案',
    ]) {
      await waitFor(() => expect(within(timeline).getAllByText(label)).toHaveLength(1))
    }
  })
})
