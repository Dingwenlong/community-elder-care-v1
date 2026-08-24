const problemMessages: Record<string, string> = {
  INVALID_CREDENTIALS: '演示账号或密码不正确。',
  FORBIDDEN_SCOPE: '当前账号没有访问这项资料的权限。',
  CONSENT_REQUIRED: '老人尚未授权查看这项资料。',
  NOT_FOUND: '没有找到对应资料。',
  REASON_REQUIRED: '请填写本次修改原因。',
  INVALID_TRANSITION: '当前状态不能执行这项操作。',
  INVALID_WORK_STATUS: '当前任务状态不能执行这项操作。',
  INVALID_EVENT_STATUS: '当前事件状态不能执行这项任务。',
  RESULT_REQUIRED: '请完整填写处理结果。',
  INVALID_BREAK_GLASS_DURATION: '临时授权时长不符合规则。',
  CLOSE_GUARD_FAILED: '仍有必做任务或随访未完成，暂时不能结案。',
  REQUEST_FAILED: '请求未完成，请稍后重试。',
}

interface ProblemPayload {
  title?: string
  detail?: string
  code?: string
  extensions?: { code?: string }
}

let accessToken: string | null = null
let unauthorizedHandler: (() => void) | null = null

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export function configureApiAuthorization(token: string | null, onUnauthorized?: () => void) {
  accessToken = token
  unauthorizedHandler = onUnauthorized ?? null
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }
  if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const configuredBase = import.meta.env.VITE_API_BASE_URL as string | undefined
  const requestUrl = configuredBase
    ? `${configuredBase.replace(/\/$/, '')}${path}`
    : new URL(path, window.location.origin).toString()
  const response = await fetch(requestUrl, {
    ...options,
    headers,
  })
  if (response.status === 401) {
    unauthorizedHandler?.()
  }
  if (!response.ok) {
    const payload = (await response.json().catch(() => ({}))) as ProblemPayload
    const code = payload.code ?? payload.extensions?.code ?? 'REQUEST_FAILED'
    const message = problemMessages[code] ?? payload.detail ?? payload.title ?? '请求未完成，请稍后重试。'
    throw new ApiError(response.status, code, message)
  }
  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

export const apiClient = { request }
