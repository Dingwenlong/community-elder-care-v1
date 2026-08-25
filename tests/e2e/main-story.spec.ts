import { expect, test, type APIRequestContext } from '@playwright/test'

const apiBaseUrl = process.env.COMMUNITYCARE_API_URL ?? 'http://127.0.0.1:5180'
const password = process.env.COMMUNITYCARE_DEMO_PASSWORD
const communityUserId = '11111111-1111-1111-1111-111111111103'
const demoDeviceId = '77777777-7777-7777-7777-777777777701'

type LoginResponse = { accessToken: string }
type ResetResponse = { mainElderId: string }
type CareEvent = {
  id: string
  elderId: string
  status: string
  evidence: Array<{ kind: string }>
  transitions: Array<{ toStatus: string }>
  contactAttempts: Array<{ targetLabel: string; isSimulation: boolean }>
}

async function login(request: APIRequestContext, username: string) {
  if (!password) throw new Error('COMMUNITYCARE_DEMO_PASSWORD is required for E2E acceptance.')
  const response = await request.post(`${apiBaseUrl}/api/v1/auth/login`, {
    data: { username, password },
  })
  expect(response.ok()).toBeTruthy()
  return ((await response.json()) as LoginResponse).accessToken
}

const authorization = (token: string) => ({ Authorization: `Bearer ${token}` })

test('reset to closure keeps one event and one auditable timeline', async ({ page, request }) => {
  const adminToken = await login(request, 'admin.demo')
  const staffToken = await login(request, 'community.demo')
  const resetResponse = await request.post(`${apiBaseUrl}/api/v1/demo/reset`, {
    headers: { ...authorization(adminToken), 'X-Confirm-Demo-Reset': 'RESET-20' },
  })
  expect(resetResponse.ok()).toBeTruthy()
  const reset = (await resetResponse.json()) as ResetResponse

  let openingEvent: CareEvent | undefined
  await expect
    .poll(async () => {
      const response = await request.get(`${apiBaseUrl}/api/v1/care-events`, {
        headers: authorization(staffToken),
      })
      const events = (await response.json()) as CareEvent[]
      openingEvent = events.find((item) => item.status !== 'Closed' && item.status !== 'FalseAlarm')
      return events.length
    }, { timeout: 35_000 })
    .toBe(1)
  expect(openingEvent).toBeTruthy()
  const eventId = openingEvent!.id

  const signalResponse = await request.post(`${apiBaseUrl}/api/v1/demo/device-signals`, {
    headers: authorization(adminToken),
    data: {
      deviceId: demoDeviceId,
      eventId: crypto.randomUUID(),
      deviceTime: new Date().toISOString(),
      signalType: 'NoWaterActivity',
      buttonState: null,
    },
  })
  expect(signalResponse.ok()).toBeTruthy()
  expect((await signalResponse.json()).careEventId).toBe(eventId)

  await page.goto('/login')
  await page.getByLabel('账号').fill('community.demo')
  await page.getByLabel('密码').fill(password!)
  await page.getByRole('button', { name: '登录' }).click()
  await expect(page.getByRole('heading', { name: '社区工作台' })).toBeVisible()
  await page.goto(`/care-events/${eventId}`)
  await page.getByRole('button', { name: '受理事件' }).click()
  await expect(page.getByText('已受理', { exact: true }).first()).toBeVisible()

  const visitResponse = await request.post(`${apiBaseUrl}/api/v1/care-events/${eventId}/visits`, {
    headers: authorization(staffToken),
    data: {
      assignedStaffUserId: communityUserId,
      scheduledStartAt: new Date(Date.now() + 3_600_000).toISOString(),
      scheduledEndAt: new Date(Date.now() + 7_200_000).toISOString(),
      isMandatory: true,
    },
  })
  expect(visitResponse.ok()).toBeTruthy()
  const visitId = (await visitResponse.json()).visitId as string

  await page.goto('/visits')
  await page.getByRole('button', { name: '开始探访' }).click()
  await page.getByRole('button', { name: '完成探访' }).click()
  await page.getByLabel('内部原始记录').fill('演示现场记录，仅社区内部可见')
  await page.getByLabel('对外确认摘要').fill('已当面确认老人状态')
  await page.getByRole('textbox', { name: '探访结果' }).fill('完成演示探访并确认安全')
  await page.getByRole('button', { name: '提交探访结果' }).click()
  await expect(page.getByText('Completed', { exact: true })).toBeVisible()

  await page.goto(`/care-events/${eventId}`)
  await page.getByRole('button', { name: '模拟电话' }).click()
  await expect(page.getByRole('status')).toContainText('模拟送达')
  await page.getByRole('button', { name: '模拟急救转运' }).click()
  await expect(page.getByRole('status')).toContainText('模拟送达')

  await page.getByRole('button', { name: '转为已解决' }).click()
  await page.getByLabel('判断或处理依据').fill('现场探访与模拟联系已完成')
  await page.getByLabel('处理结果').fill('确认老人安全，安排后续随访')
  await page.getByRole('button', { name: '确认提交' }).click()
  await expect(page.getByText('已解决', { exact: true }).first()).toBeVisible()

  const followUpResponse = await request.post(`${apiBaseUrl}/api/v1/care-events/${eventId}/follow-ups`, {
    headers: authorization(staffToken),
    data: {
      assignedStaffUserId: communityUserId,
      dueAt: new Date(Date.now() + 86_400_000).toISOString(),
    },
  })
  expect(followUpResponse.ok()).toBeTruthy()
  const followUpId = (await followUpResponse.json()).followUpId as string
  const followUpComplete = await request.post(
    `${apiBaseUrl}/api/v1/follow-ups/${followUpId}/complete`,
    { headers: authorization(staffToken), data: { result: '随访完成，状态稳定' } },
  )
  expect(followUpComplete.ok()).toBeTruthy()

  await page.reload()
  await page.getByRole('button', { name: '转为已结案' }).click()
  await page.getByLabel('判断或处理依据').fill('随访完成后结案')
  await page.getByLabel('处理结果').fill('演示闭环已完成')
  await page.getByRole('button', { name: '确认提交' }).click()
  await expect(page.getByText('已结案', { exact: true }).first()).toBeVisible()

  const eventResponse = await request.get(`${apiBaseUrl}/api/v1/care-events/${eventId}`, {
    headers: authorization(staffToken),
  })
  const closed = (await eventResponse.json()) as CareEvent
  expect(closed.id).toBe(eventId)
  expect(closed.status).toBe('Closed')
  expect(closed.evidence.filter((item) => item.kind === 'VisitCompleted')).toHaveLength(1)
  expect(closed.transitions.filter((item) => item.toStatus === 'Accepted')).toHaveLength(1)
  expect(closed.transitions.filter((item) => item.toStatus === 'Resolved')).toHaveLength(1)
  expect(closed.transitions.filter((item) => item.toStatus === 'Closed')).toHaveLength(1)
  expect(closed.contactAttempts.filter((item) => item.targetLabel === 'Family 模拟电话')).toHaveLength(1)
  expect(
    closed.contactAttempts.filter((item) => item.targetLabel === '120 模拟急救转运'),
  ).toHaveLength(1)
  expect(visitId).toMatch(/^[0-9a-f-]{36}$/)

  await page.reload()
  await expect(page.locator('.event-timeline li')).toHaveCount(
    closed.evidence.length + closed.transitions.length + closed.contactAttempts.length,
  )
})
