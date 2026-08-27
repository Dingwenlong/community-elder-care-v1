import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { readFile } from 'node:fs/promises'

const api = process.env.COMMUNITYCARE_API_URL ?? 'http://127.0.0.1:5180'
const password = process.env.COMMUNITYCARE_DEMO_PASSWORD
const staffId = '11111111-1111-1111-1111-111111111103'
const secondStaffId = '11111111-1111-1111-1111-111111111106'
const workerId = '11111111-1111-1111-1111-111111111104'
const secondWorkerId = '11111111-1111-1111-1111-111111111107'

async function loginApi(request: APIRequestContext, username: string) {
  if (!password) throw new Error('COMMUNITYCARE_DEMO_PASSWORD is required.')
  const response = await request.post(api + '/api/v1/auth/login', { data: { username, password } })
  expect(response.ok()).toBeTruthy()
  return { Authorization: 'Bearer ' + (await response.json()).accessToken }
}
async function loginUi(page: Page, username: string) {
  await page.goto('/login')
  await page.getByLabel('账号', { exact: true }).fill(username)
  await page.getByLabel('密码', { exact: true }).fill(password!)
  await page.getByRole('button', { name: '登录', exact: true }).click()
  await expect(page.getByRole('button', { name: '退出', exact: true })).toBeVisible()
}
async function transition(page: Page, target: string) {
  await page.getByRole('button', { name: '转为' + target, exact: true }).click()
  await page.getByLabel('判断或处理依据').fill('已核对照料任务与处理记录')
  await page.getByLabel('处理结果', { exact: true }).fill('本次照料任务已完成')
  await page.getByRole('button', { name: '确认提交' }).click()
  await expect(page.getByText(target, { exact: true }).first()).toBeVisible()
}

test('community operations creates, reassigns and completes all three task types', async ({ page, request, browser }, testInfo) => {
  test.setTimeout(150_000)
  const admin = await loginApi(request, 'admin.demo')
  const staff = await loginApi(request, 'community.demo')
  const oldWorker = await loginApi(request, 'service.demo')
  const reset = await request.post(api + '/api/v1/demo/reset', { headers: { ...admin, 'X-Confirm-Demo-Reset': 'RESET-20' } })
  expect(reset.ok()).toBeTruthy()
  const { mainElderId } = await reset.json()
  const created = await request.post(api + '/api/v1/care-events', { headers: staff, data: {
    clientRequestId: crypto.randomUUID(), elderId: mainElderId, trigger: 'LifeServiceNeed', summary: '助餐与上门关怀安排', occurredAt: new Date().toISOString(),
  } })
  expect(created.ok()).toBeTruthy()
  const eventId = (await created.json()).id as string
  expect((await request.post(api + '/api/v1/care-events/' + eventId + '/accept', { headers: staff })).ok()).toBeTruthy()
  const workerContext = await browser.newContext({ baseURL: testInfo.project.use.baseURL })
  const secondWorker = await workerContext.newPage()
  const visitContext = await browser.newContext({ baseURL: testInfo.project.use.baseURL })
  const visitor = await visitContext.newPage()
  try {
    await loginUi(secondWorker, 'service.second')
    await loginUi(page, 'community.demo')
    await page.goto('/care-events/' + eventId)
    await page.getByRole('button', { name: '安排探访', exact: true }).click()
    await page.getByLabel('任务负责人').selectOption(secondStaffId)
    await page.getByRole('button', { name: '保存任务' }).click()
    await expect(page.getByLabel('任务负责人')).toHaveCount(0)
    await page.getByRole('button', { name: '创建工单', exact: true }).click()
    await page.getByLabel('任务负责人').selectOption(workerId)
    await page.getByLabel('联系说明').fill('到达后联系社区工作人员')
    await page.getByRole('button', { name: '保存任务' }).click()
    await expect(page.getByLabel('任务负责人')).toHaveCount(0)
    const orders = await (await request.get(api + '/api/v1/service-orders?careEventId=' + eventId, { headers: staff })).json()
    expect(orders).toHaveLength(1)
    expect(orders[0].dueAt).toBeTruthy()
    const orderId = orders[0].orderId as string
    await page.goto('/operations')
    await page.locator('[data-task-id="' + orderId + '"]').getByRole('button', { name: '转派', exact: true }).click()
    await page.getByLabel('新负责人').selectOption(secondWorkerId)
    await page.getByLabel('转派原因').fill('调整助餐服务安排')
    await page.getByRole('button', { name: '确认保存' }).click()
    await expect(page.getByRole('dialog')).toHaveCount(0)
    expect((await request.post(api + '/api/v1/service-orders/' + orderId + '/accept', { headers: oldWorker })).status()).toBe(403)
    await secondWorker.getByRole('button', { name: '刷新我的任务' }).click()
    await secondWorker.getByRole('button', { name: '接收任务' }).click()
    await secondWorker.getByRole('button', { name: '填写完成结果' }).click()
    await secondWorker.getByLabel('服务完成结果').fill('已完成助餐配送')
    await secondWorker.getByRole('button', { name: '提交完成结果' }).click()
    await expect(secondWorker.getByText('已完成', { exact: true })).toBeVisible()

    await loginUi(visitor, 'community.second')
    await visitor.goto('/visits')
    await visitor.getByRole('button', { name: '开始探访' }).click()
    await visitor.getByRole('button', { name: '完成探访' }).click()
    await visitor.getByLabel('内部原始记录').fill('内部走访记录，不得导出')
    await visitor.getByLabel('对外确认摘要').fill('已完成上门关怀')
    await visitor.getByRole('textbox', { name: '探访结果', exact: true }).fill('已确认服务需求')
    await visitor.getByRole('button', { name: '提交探访结果' }).click()
    await expect(visitor.getByText('已完成', { exact: true })).toBeVisible()
    await page.goto('/care-events/' + eventId)
    await transition(page, '已解决')
    await page.getByRole('button', { name: '安排回访', exact: true }).click()
    await page.getByLabel('任务负责人').selectOption(staffId)
    await page.getByRole('button', { name: '保存任务' }).click()
    await expect(page.getByLabel('任务负责人')).toHaveCount(0)
    const followups = await (await request.get(api + '/api/v1/follow-ups?careEventId=' + eventId, { headers: staff })).json()
    expect(followups).toHaveLength(1)
    await page.goto('/operations')
    await page.locator('[data-task-id="' + followups[0].followUpId + '"]').getByRole('button', { name: '完成回访' }).click()
    await page.getByLabel('回访结果', { exact: true }).fill('已确认服务结果')
    await page.getByRole('button', { name: '确认保存' }).click()
    await expect(page.getByRole('dialog')).toHaveCount(0)
    await page.goto('/care-events/' + eventId)
    await transition(page, '已结案')
    const persisted = await (await request.get(api + '/api/v1/care-events/' + eventId, { headers: staff })).json()
    expect(persisted.status).toBe('Closed')
    expect(persisted.evidence.some((e: { kind: string }) => e.kind === 'TaskReassigned')).toBeTruthy()
    await testInfo.attach('operations-persistent-readback', { body: JSON.stringify({
      eventId, status: persisted.status, orderId, followUpId: followups[0].followUpId,
      taskReassigned: true, oldWorkerAccess: 403,
    }), contentType: 'application/json' })
  } finally { await workerContext.close(); await visitContext.close() }
})

test('device ledger, scenario reports and CSV use persisted records', async ({ page, request }, testInfo) => {
  const admin = await loginApi(request, 'admin.demo')
  const reset = await request.post(api + '/api/v1/demo/reset', { headers: { ...admin, 'X-Confirm-Demo-Reset': 'RESET-20' } })
  expect(reset.ok()).toBeTruthy()
  await loginUi(page, 'admin.demo')
  await page.goto('/settings')
  page.once('dialog', dialog => dialog.accept())
  await page.getByRole('button', { name: '加载运营演示场景' }).click()
  await expect(page.getByRole('status')).toContainText('运营演示场景已加载')
  await page.goto('/device-signals')
  await expect(page.getByRole('heading', { name: '设备台账与信号' })).toBeVisible()
  const row = page.getByRole('row').filter({ hasText: '客厅 SOS 设备' })
  await row.getByRole('button', { name: '停用', exact: true }).click()
  await page.getByLabel('启停原因').fill('检查按钮接线')
  await page.getByRole('button', { name: '确认修改' }).click()
  await expect(row).toContainText('已停用')
  await row.getByRole('button', { name: '启用', exact: true }).click()
  await page.getByLabel('启停原因').fill('检查完成')
  await page.getByRole('button', { name: '确认修改' }).click()
  await expect(row).toContainText('已启用')
  await page.getByLabel('查看设备').selectOption({ label: '客厅 SOS 设备' })
  await page.getByRole('button', { name: '查询记录' }).click()
  await page.getByRole('button', { name: '模拟 SOS', exact: true }).click()
  await expect(page.getByText('服务端已保存，并关联到照料事件。')).toBeVisible()
  const devices = await (await request.get(api + '/api/v1/devices', { headers: admin })).json()
  expect(devices.find((d: { displayName: string }) => d.displayName === '客厅 SOS 设备').lastSimulationSignalAt).toBeTruthy()

  await page.goto('/reports')
  await expect(page.getByRole('heading', { name: '社区照料报告' })).toBeVisible()
  await expect(page.locator('.report-metrics')).toContainText('完成探访')
  const downloaded = page.waitForEvent('download')
  await page.getByRole('button', { name: '导出汇总 CSV' }).click()
  const download = await downloaded
  expect(download.suggestedFilename()).toMatch(/\.csv$/)
  const path = await download.path()
  expect(path).toBeTruthy()
  const csv = await readFile(path!)
  expect([...csv.subarray(0, 3)]).toEqual([239, 187, 191])
  expect(csv.toString('utf8')).toContain('"完成探访"')
  expect(csv.toString('utf8')).not.toContain('内部探访记录')
  const report = await (await request.get(api + '/api/v1/reports/operations', { headers: admin })).json()
  expect(report.summary.completedVisitCount).toBe(9)
  await page.emulateMedia({ media: 'print' })
  await expect(page.locator('.community-sidebar')).toBeHidden()
  await expect(page.getByRole('heading', { name: '社区照料报告' })).toBeVisible()
  const pdf = await page.pdf({ format: 'A4', printBackground: true })
  expect(pdf.subarray(0, 4).toString()).toBe('%PDF')
  await testInfo.attach('operations-report-print', { body: pdf, contentType: 'application/pdf' })
  await page.emulateMedia({ media: 'screen' })
  await testInfo.attach('operations-report-readback', { body: JSON.stringify(report), contentType: 'application/json' })
  await testInfo.attach('operations-report-screen', { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' })
})
