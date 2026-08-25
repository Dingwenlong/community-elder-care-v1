<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const roleLabel = computed(() => {
  const labels = {
    Elder: '老人',
    Family: '家属',
    CommunityStaff: '社区工作人员',
    ServiceWorker: '服务人员',
    Administrator: '系统管理员',
  }
  return auth.role ? labels[auth.role] : '当前用户'
})

const canViewElders = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
const canOperateCare = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
const isServiceWorker = computed(() => auth.role === 'ServiceWorker')
const isAdministrator = computed(() => auth.role === 'Administrator')
const canViewReports = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
const workspaceLabel = computed(() =>
  isServiceWorker.value ? '服务任务工作区' : '社区照料工作区',
)

async function signOut() {
  auth.clearSession()
  await router.replace('/login')
}
</script>

<template>
  <div class="community-shell">
    <aside class="community-sidebar" aria-label="社区工作区导航">
      <div class="brand-block">
        <span class="brand-mark" aria-hidden="true">社</span>
        <span>独居老人照料</span>
      </div>
      <nav class="primary-nav">
        <RouterLink v-if="!isServiceWorker" to="/dashboard">工作台</RouterLink>
        <RouterLink v-if="canViewElders" to="/elders">老人档案</RouterLink>
        <RouterLink v-if="canOperateCare" to="/care-events">照料事件</RouterLink>
        <RouterLink v-if="canOperateCare" to="/visits">探访任务</RouterLink>
        <RouterLink v-if="canOperateCare" to="/service-orders">服务工单</RouterLink>
        <RouterLink v-if="isAdministrator" to="/device-signals">设备信号</RouterLink>
        <RouterLink v-if="canViewReports" to="/reports">报告与审计</RouterLink>
        <RouterLink v-if="isAdministrator" to="/settings">系统设置</RouterLink>
        <RouterLink v-if="isServiceWorker" to="/my-tasks">我的任务</RouterLink>
      </nav>
      <p class="sidebar-note">社区照料工作台<br />外部通知与救援操作均为模拟</p>
    </aside>

    <div class="community-workspace">
      <header class="workspace-header">
        <div>
          <p class="system-name">社区独居老人照料系统</p>
          <p class="workspace-name">{{ workspaceLabel }}</p>
        </div>
        <div class="account-area">
          <span>{{ roleLabel }}</span>
          <button class="text-button" type="button" @click="signOut">退出</button>
        </div>
      </header>
      <main class="workspace-main">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.community-shell {
  min-height: 100vh;
}

.community-sidebar {
  position: fixed;
  inset: 0 auto 0 0;
  z-index: 2;
  display: flex;
  width: var(--sidebar-width);
  flex-direction: column;
  color: #f4f8fd;
  background: var(--navy);
}

.brand-block {
  display: flex;
  min-height: var(--header-height);
  align-items: center;
  gap: var(--space-3);
  padding: 0 var(--space-5);
  border-bottom: 1px solid rgb(255 255 255 / 18%);
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}

.brand-mark {
  display: grid;
  width: 34px;
  height: 34px;
  place-items: center;
  border: 1px solid rgb(255 255 255 / 68%);
  border-radius: var(--radius-sm);
  font-size: 16px;
}

.primary-nav {
  display: grid;
  padding: var(--space-3) 0;
}

.primary-nav a {
  display: flex;
  min-height: 44px;
  align-items: center;
  padding: 0 var(--space-5);
  border-left: 4px solid transparent;
  color: #dfeaf7;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
}

.primary-nav a:hover {
  background: rgb(255 255 255 / 8%);
}

.primary-nav a.router-link-active {
  border-left-color: var(--focus);
  color: white;
  background: var(--navy-deep);
}

.sidebar-note {
  margin: auto var(--space-5) var(--space-5);
  color: #b8cbe0;
  font-size: 13px;
  line-height: 1.7;
}

.community-workspace {
  min-height: 100vh;
  margin-left: var(--sidebar-width);
}

.workspace-header {
  position: sticky;
  top: 0;
  z-index: 1;
  display: flex;
  min-height: var(--header-height);
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  padding: 0 var(--space-6);
  border-bottom: 1px solid var(--line);
  background: var(--surface);
}

.system-name,
.workspace-name {
  margin: 0;
}

.system-name {
  color: var(--ink-strong);
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}

.workspace-name {
  color: var(--ink-muted);
  font-size: 13px;
}

.account-area {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  color: var(--ink-strong);
}

.text-button {
  min-width: 56px;
  border: 0;
  color: var(--action);
  background: transparent;
  cursor: pointer;
}

.text-button:hover {
  text-decoration: underline;
}

.workspace-main {
  max-width: calc(var(--content-max-width) + var(--space-6) * 2);
  margin: 0 auto;
  padding: var(--space-5) var(--space-6);
}

@media (max-width: 1279px) {
  .workspace-main {
    padding: var(--space-5);
  }
}

@media (max-width: 767px) {
  .community-sidebar {
    position: static;
    width: 100%;
  }

  .primary-nav {
    grid-auto-flow: column;
    grid-auto-columns: max-content;
    overflow-x: auto;
  }

  .sidebar-note {
    display: none;
  }

  .community-workspace {
    margin-left: 0;
  }

  .workspace-header {
    position: static;
    padding: var(--space-3) var(--space-4);
  }

  .account-area > span:first-child {
    display: none;
  }

  .workspace-main {
    padding: var(--space-4);
  }
}
</style>
