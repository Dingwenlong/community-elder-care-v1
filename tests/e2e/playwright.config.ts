import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: '.',
  testMatch: ['**/*.spec.ts'],
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 90_000,
  expect: { timeout: 10_000 },
  reporter: [['line']],
  outputDir: 'test-results',
  use: {
    baseURL: process.env.COMMUNITYCARE_WEB_URL ?? 'http://127.0.0.1:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: process.env.COMMUNITYCARE_RECORD_DEMO === '1' ? 'on' : 'off',
    ...devices['Desktop Chrome'],
  },
})
