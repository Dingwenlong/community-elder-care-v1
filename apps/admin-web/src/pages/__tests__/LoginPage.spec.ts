import { cleanup, render, screen } from '@testing-library/vue'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

import LoginPage from '@/pages/LoginPage.vue'

afterEach(cleanup)

describe('LoginPage', () => {
  it('shows the illustrated product login without internal test copy', async () => {
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
    const cover = document.querySelector<HTMLImageElement>('.login-context img')
    expect(cover?.src).toContain('login-community-visit.webp')
    expect(cover?.getAttribute('alt')).toBe('')
    expect(cover?.getAttribute('aria-hidden')).toBe('true')
    expect(screen.getByText('社区工作人员在这里处理安全确认、探访和服务记录。')).toBeTruthy()
    expect(screen.queryByText('20 位老人档案')).toBeNull()
    expect(screen.queryByText('电话、短信与设备均为模拟记录')).toBeNull()
    expect(screen.queryByText(/演示数据|参赛演示|演示账号|账号示例|虚构档案|运行环境/)).toBeNull()
  })
})
