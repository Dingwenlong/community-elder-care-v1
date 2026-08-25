import { cleanup, render, screen, waitFor, within } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'

import DeviceSimulator from '@/components/DeviceSimulator.vue'

const receivedTypes: string[] = []
const eventId = '22222222-2222-2222-2222-222222222299'
const routerLinkStub = {
  props: ['to'],
  template: '<a :href="to"><slot /></a>',
}
const server = setupServer(
  http.post('*/api/v1/demo/device-signals', async ({ request }) => {
    const body = (await request.json()) as { signalType: string }
    receivedTypes.push(body.signalType)
    return HttpResponse.json({
      signalId: crypto.randomUUID(),
      careEventId: eventId,
      receivedAt: '2026-08-24T08:00:00Z',
      isDuplicate: false,
      isSimulation: true,
    })
  }),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  receivedTypes.length = 0
  server.resetHandlers()
})
afterAll(() => server.close())

describe('DeviceSimulator', () => {
  it('sends all demo signals through the simulator endpoint and links the event', async () => {
    const user = userEvent.setup()
    render(DeviceSimulator, { global: { stubs: { RouterLink: routerLinkStub } } })

    await user.click(screen.getByRole('button', { name: '模拟 SOS' }))
    await user.click(screen.getByRole('button', { name: '模拟长时间无用水' }))
    await user.click(screen.getByRole('button', { name: '模拟设备离线' }))

    await waitFor(() => {
      expect(receivedTypes).toEqual(['SosButton', 'NoWaterActivity', 'DeviceOffline'])
    })
    expect(within(screen.getByRole('status')).getByText('模拟信号')).toBeTruthy()
    expect(screen.getByRole('link', { name: '查看照料事件' }).getAttribute('href')).toBe(
      `/care-events/${eventId}`,
    )
  })

  it('shows a pending state until the persisted response arrives', async () => {
    server.use(
      http.post('*/api/v1/demo/device-signals', async () => {
        await new Promise((resolve) => setTimeout(resolve, 120))
        return HttpResponse.json({
          signalId: crypto.randomUUID(),
          careEventId: eventId,
          receivedAt: '2026-08-24T08:00:00Z',
          isDuplicate: false,
          isSimulation: true,
        })
      }),
    )
    const user = userEvent.setup()
    render(DeviceSimulator, { global: { stubs: { RouterLink: routerLinkStub } } })

    await user.click(screen.getByRole('button', { name: '模拟 SOS' }))

    expect(screen.getByText('模拟信号发送中')).toBeTruthy()
    expect(screen.getAllByRole('button').every((button) => button.hasAttribute('disabled'))).toBe(
      true,
    )
    await waitFor(() => {
      expect(within(screen.getByRole('status')).getByText('模拟信号')).toBeTruthy()
    })
  })
})
