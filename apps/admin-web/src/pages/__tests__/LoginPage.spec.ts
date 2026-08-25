import { cleanup, render, screen } from '@testing-library/vue'
import { createPinia } from 'pinia'
import { afterEach, describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

import LoginPage from '@/pages/LoginPage.vue'

afterEach(cleanup)

describe('LoginPage', () => {
  it('presents a normal product login without internal demo guidance', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/login', component: LoginPage },
        { path: '/dashboard', component: { template: '<h1>工作台</h1>' } },
      ],
    })
    await router.push('/login')
    await router.isReady()

    render(LoginPage, { global: { plugins: [createPinia(), router] } })

    expect(screen.getByRole('heading', { name: '社区独居老人照料系统' })).toBeTruthy()
    expect(screen.getByLabelText('账号')).toBeTruthy()
    expect(screen.getByLabelText('密码')).toBeTruthy()
    expect(screen.getByRole('button', { name: '登录' })).toBeTruthy()
    expect(document.body.textContent).not.toMatch(/演示|参赛|虚构|运行环境|账号示例/)
  })
})
