import { expect, test } from '@playwright/test'

test('the demo web origin returns API readiness JSON', async ({ request }) => {
  const response = await request.get('/health/ready')

  expect(response.ok()).toBeTruthy()
  expect(response.headers()['content-type']).toContain('application/json')
  expect(await response.json()).toMatchObject({ status: 'ready' })
})
