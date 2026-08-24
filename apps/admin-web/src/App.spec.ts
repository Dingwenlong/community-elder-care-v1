import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'

import App from './App.vue'

describe('App', () => {
  it('shows the community-care product name', () => {
    render(App)

    expect(screen.getByRole('heading', { name: '社区独居老人照料系统' })).toBeTruthy()
    expect(screen.getByText('演示数据')).toBeTruthy()
  })
})
