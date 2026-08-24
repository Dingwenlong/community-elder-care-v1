<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import DemoDataBadge from '@/components/DemoDataBadge.vue'
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
  return auth.role ? labels[auth.role] : '演示用户'
})

const canViewElders = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
const canOperateCare = computed(() =>
  auth.role === 'CommunityStaff' || auth.role === 'Administrator',
)
const isServiceWorker = computed(() => auth.role === 'ServiceWorker')
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
        <RouterLink v-if="isServiceWorker" to="/my-tasks">我的任务</RouterLink>
      </nav>
      <p class="sidebar-note">参赛演示环境<br />不接入真实设备或电话</p>
    </aside>

    <div class="community-workspace">
      <header class="workspace-header">
        <div>
          <p class="system-name">社区独居老人照料系统</p>
          <p class="workspace-name">{{ workspaceLabel }}</p>
        </div>
        <div class="account-area">
          <span>{{ roleLabel }}</span>
          <DemoDataBadge v-if="auth.isDemoMode" />
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
  font-size: 16px;
}

.primary-nav {
  display: grid;
  padding: var(--space-4) 0;
}

.primary-nav a {
  display: flex;
  min-height: 52px;
  align-items: center;
  padding: 0 var(--space-5);
  border-left: 4px solid transparent;
  color: #dfeaf7;
  font-weight: 700;
  text-decoration: none;
}

.primary-nav a:hover {
  background: rgb(255 255 255 / 8%);
}

.primary-nav a.router-link-active {
  border-left-color: #75b7ff;
  color: white;
  background: #0d4d8c;
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
  padding: var(--space-6);
}

@media (max-width: 760px) {
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
