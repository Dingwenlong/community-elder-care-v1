import { cleanup, render, screen } from '@testing-library/vue'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

import LoginPage from '@/pages/LoginPage.vue'

afterEach(cleanup)

describe('LoginPage', () => {
  it('shows a normal product login without internal test copy', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/login', component: LoginPage }],
    })
    await router.push('/login')
    await router.isReady()

    render(LoginPage, { global: { plugins: [pinia, router] } })

    expect(screen.getByRole('heading', { name: '登录工作区' })).toBeTruthy()
    expect(screen.getByLabelText('账号')).toBeTruthy()
    expect(screen.getByLabelText('密码')).toBeTruthy()
    expect(screen.getByRole('button', { name: '登录' })).toBeTruthy()
    expect(screen.getByText('电话、短信与设备均为模拟记录')).toBeTruthy()
    expect(screen.queryByText(/演示数据|参赛演示|演示账号|账号示例|虚构档案|运行环境/)).toBeNull()
  })
})
