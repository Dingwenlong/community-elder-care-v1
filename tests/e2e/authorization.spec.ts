import { expect, test, type APIRequestContext } from '@playwright/test'

const apiBaseUrl = process.env.COMMUNITYCARE_API_URL ?? 'http://127.0.0.1:5180'
const password = process.env.COMMUNITYCARE_DEMO_PASSWORD
const serviceWorkerUserId = '11111111-1111-1111-1111-111111111104'

async function login(request: APIRequestContext, username: string) {
  if (!password) throw new Error('COMMUNITYCARE_DEMO_PASSWORD is required for E2E acceptance.')
  const response = await request.post(`${apiBaseUrl}/api/v1/auth/login`, {
    data: { username, password },
  })
  expect(response.ok()).toBeTruthy()
  return ((await response.json()) as { accessToken: string }).accessToken
}

const authorization = (token: string) => ({ Authorization: `Bearer ${token}` })

async function reset(request: APIRequestContext) {
  const admin = await login(request, 'admin.demo')
  const response = await request.post(`${apiBaseUrl}/api/v1/demo/reset`, {
    headers: { ...authorization(admin), 'X-Confirm-Demo-Reset': 'RESET-20' },
  })
  expect(response.ok()).toBeTruthy()
  return { admin, ...((await response.json()) as { mainElderId: string }) }
}

test.describe.configure({ mode: 'serial' })

test('family grants are field-scoped and revocation is immediate', async ({ request }) => {
  const { mainElderId } = await reset(request)
  const family = await login(request, 'family.demo')
  const elder = await login(request, 'elder.demo')
  const summaryResponse = await request.get(
    `${apiBaseUrl}/api/v1/family/elders/${mainElderId}/summary`,
    { headers: authorization(family) },
  )
  expect(summaryResponse.ok()).toBeTruthy()
  const summary = await summaryResponse.json()
  expect(summary.grantedFields).toEqual(
    expect.arrayContaining(['RecentStatus', 'CareEventSummary', 'VisitSummary']),
  )
  expect(JSON.stringify(summary)).not.toContain('rawAiText')
  expect(JSON.stringify(summary)).not.toContain('internalNote')

  const revoke = await request.delete(
    `${apiBaseUrl}/api/v1/elders/${mainElderId}/consents/11111111-1111-1111-1111-111111111102`,
    { headers: authorization(elder) },
  )
  expect(revoke.status()).toBe(204)
  const denied = await request.get(`${apiBaseUrl}/api/v1/family/elders/${mainElderId}/summary`, {
    headers: authorization(family),
  })
  expect(denied.status()).toBe(403)
  expect((await denied.json()).code).toBe('CONSENT_REQUIRED')
})

test('service worker sees only the assigned minimal task', async ({ request }) => {
  const { mainElderId } = await reset(request)
  const staff = await login(request, 'community.demo')
  const createEvent = await request.post(`${apiBaseUrl}/api/v1/care-events/`, {
    headers: authorization(staff),
    data: {
      clientRequestId: crypto.randomUUID(),
      elderId: mainElderId,
      trigger: 'DeviceAnomaly',
      summary: '服务工单权限验收事件',
      occurredAt: new Date().toISOString(),
    },
  })
  const eventId = (await createEvent.json()).id as string
  await request.post(`${apiBaseUrl}/api/v1/care-events/${eventId}/accept`, {
    headers: authorization(staff),
  })
  const orderResponse = await request.post(
    `${apiBaseUrl}/api/v1/care-events/${eventId}/service-orders`,
    {
      headers: authorization(staff),
      data: {
        serviceType: '助餐配送',
        scheduledWindow: '10:00-11:00',
        contactInstruction: '到门口后按演示流程联系',
        assignedWorkerUserId: serviceWorkerUserId,
        isMandatory: true,
      },
    },
  )
  expect(orderResponse.ok()).toBeTruthy()
  const order = await orderResponse.json()
  const worker = await login(request, 'service.demo')
  const tasksResponse = await request.get(`${apiBaseUrl}/api/v1/service-orders/my-tasks`, {
    headers: authorization(worker),
  })
  const tasks = await tasksResponse.json()
  expect(tasks).toHaveLength(1)
  expect(tasks[0].orderId).toBe(order.orderId)
  expect(Object.keys(tasks[0]).sort()).toEqual(
    ['contactInstruction', 'elderDisplayName', 'orderId', 'scheduledWindow', 'serviceType', 'status'].sort(),
  )
  const communityList = await request.get(`${apiBaseUrl}/api/v1/service-orders`, {
    headers: authorization(worker),
  })
  expect(communityList.status()).toBe(403)
})

test('area, raw AI and break-glass access remain bounded and audited', async ({ request }) => {
  const { admin, mainElderId } = await reset(request)
  const staff = await login(request, 'community.demo')
  const eldersResponse = await request.get(`${apiBaseUrl}/api/v1/elders`, {
    headers: authorization(admin),
  })
  const elders = (await eldersResponse.json()) as Array<{ id: string; areaCode?: string }>
  const otherArea = elders.find((item) => item.areaCode && item.areaCode !== 'A01')
  expect(otherArea).toBeTruthy()
  const areaDenied = await request.get(`${apiBaseUrl}/api/v1/elders/${otherArea!.id}`, {
    headers: authorization(staff),
  })
  expect(areaDenied.status()).toBe(403)

  const rawInput = '管理员不得读取的 AI 原始输入'
  const aiDenied = await request.post(`${apiBaseUrl}/api/v1/ai/elder-chat`, {
    headers: authorization(admin),
    data: { elderId: mainElderId, sessionId: crypto.randomUUID(), input: rawInput },
  })
  expect(aiDenied.status()).toBe(403)
  expect(await aiDenied.text()).not.toContain(rawInput)

  const missingReason = await request.post(
    `${apiBaseUrl}/api/v1/elders/${otherArea!.id}/break-glass`,
    { headers: authorization(staff), data: { reason: '', durationMinutes: 15 } },
  )
  expect(missingReason.status()).toBe(400)
  expect((await missingReason.json()).code).toBe('REASON_REQUIRED')
  const reason = '跨片区紧急协助演示'
  const grantedResponse = await request.post(
    `${apiBaseUrl}/api/v1/elders/${otherArea!.id}/break-glass`,
    { headers: authorization(staff), data: { reason, durationMinutes: 15 } },
  )
  expect(grantedResponse.ok()).toBeTruthy()
  const grant = await grantedResponse.json()
  const remainingMinutes = (new Date(grant.expiresAt).getTime() - Date.now()) / 60_000
  expect(remainingMinutes).toBeGreaterThan(14)
  expect(remainingMinutes).toBeLessThanOrEqual(15.2)
  const auditResponse = await request.get(
    `${apiBaseUrl}/api/v1/audit?entityType=BreakGlassGrant&entityId=${grant.id}`,
    { headers: authorization(admin) },
  )
  const audit = await auditResponse.json()
  expect(audit).toHaveLength(1)
  expect(audit[0].actorKind).toBe('CommunityStaff')
  expect(audit[0].reason).not.toContain(reason)
})
