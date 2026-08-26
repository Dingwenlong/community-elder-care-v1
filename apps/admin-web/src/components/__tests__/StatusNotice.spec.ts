import { cleanup, render, screen } from '@testing-library/vue'
import { afterEach, describe, expect, it } from 'vitest'

import StatusNotice from '@/components/StatusNotice.vue'

afterEach(cleanup)

describe('StatusNotice', () => {
  it('shows the matching decorative illustration for an empty state', () => {
    render(StatusNotice, {
      props: { kind: 'empty', title: '暂无待处理事项', illustration: 'care-events' },
    })

    const image = screen.getByRole('status').querySelector('img')
    expect(image?.getAttribute('src')).toContain('care-events-empty.webp')
    expect(image?.getAttribute('alt')).toBe('')
    expect(image?.getAttribute('aria-hidden')).toBe('true')
  })

  it('does not show a decorative illustration outside an empty state', () => {
    render(StatusNotice, {
      props: { kind: 'loading', title: '正在载入', illustration: 'care-work' },
    })

    expect(screen.getByRole('status').querySelector('img')).toBeNull()
  })
})
