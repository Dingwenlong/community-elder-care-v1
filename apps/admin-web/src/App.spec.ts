import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

import App from './App.vue'

describe('App', () => {
  it('renders the active route', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        {
          path: '/',
          component: {
            template: '<main><h1>社区独居老人照料系统</h1><span>社区工作台</span></main>',
          },
        },
      ],
    })
    await router.push('/')
    await router.isReady()
    render(App, { global: { plugins: [router] } })

    expect(screen.getByRole('heading', { name: '社区独居老人照料系统' })).toBeTruthy()
    expect(screen.getByText('社区工作台')).toBeTruthy()
  })
})
