import { render, screen } from '@testing-library/vue'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'

import AiVisitSummaryDraft from '@/components/AiVisitSummaryDraft.vue'

describe('AiVisitSummaryDraft', () => {
  it('marks generated content as a draft and waits for explicit confirmation', async () => {
    const user = userEvent.setup()
    const view = render(AiVisitSummaryDraft, {
      props: {
        draft: {
          id: 'draft-1',
          generatedText: '老人精神状态平稳，已完成本次安全确认。',
        },
      },
    })

    expect(screen.getByText('AI 草稿')).toBeTruthy()
    expect(screen.getByText('老人精神状态平稳，已完成本次安全确认。')).toBeTruthy()
    expect(view.emitted().confirm).toBeUndefined()

    await user.click(screen.getByRole('button', { name: '确认摘要' }))

    expect(view.emitted().confirm).toEqual([['draft-1']])
  })

  it('disables confirmation while the request is being submitted', () => {
    render(AiVisitSummaryDraft, {
      props: {
        draft: { id: 'draft-2', generatedText: '待确认摘要' },
        loading: true,
      },
    })

    expect(screen.getByRole('button', { name: '正在确认' }).getAttribute('disabled')).not.toBeNull()
  })
})
