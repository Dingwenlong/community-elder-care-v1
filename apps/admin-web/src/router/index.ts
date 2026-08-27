import { createRouter, createWebHistory } from 'vue-router'

import CommunityLayout from '@/layouts/CommunityLayout.vue'
import CareEventDetailPage from '@/pages/CareEventDetailPage.vue'
import CareEventListPage from '@/pages/CareEventListPage.vue'
import DashboardPage from '@/pages/DashboardPage.vue'
import DeviceSignalPage from '@/pages/DeviceSignalPage.vue'
import AuditPage from '@/pages/AuditPage.vue'
import ReportPage from '@/pages/ReportPage.vue'
import OperationsPage from '@/pages/OperationsPage.vue'
import SettingsPage from '@/pages/SettingsPage.vue'
import ElderDetailPage from '@/pages/ElderDetailPage.vue'
import ElderEditPage from '@/pages/ElderEditPage.vue'
import ElderListPage from '@/pages/ElderListPage.vue'
import LoginPage from '@/pages/LoginPage.vue'
import NotAuthorizedPage from '@/pages/NotAuthorizedPage.vue'
import ServiceOrderListPage from '@/pages/ServiceOrderListPage.vue'
import ServiceWorkerTasksPage from '@/pages/ServiceWorkerTasksPage.vue'
import VisitListPage from '@/pages/VisitListPage.vue'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/login', component: LoginPage, meta: { public: true } },
    { path: '/not-authorized', component: NotAuthorizedPage },
    {
      path: '/',
      component: CommunityLayout,
      children: [
        {
          path: 'dashboard',
          component: DashboardPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'elders',
          component: ElderListPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'elders/:elderId',
          component: ElderDetailPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'elders/:elderId/edit',
          component: ElderEditPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'care-events',
          component: CareEventListPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'care-events/:eventId',
          component: CareEventDetailPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'operations',
          component: OperationsPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'visits',
          component: VisitListPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'service-orders',
          component: ServiceOrderListPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'device-signals',
          component: DeviceSignalPage,
          meta: { roles: ['Administrator'] },
        },
        {
          path: 'reports',
          component: ReportPage,
          meta: { roles: ['CommunityStaff', 'Administrator'] },
        },
        {
          path: 'audit',
          component: AuditPage,
          meta: { roles: ['Administrator'] },
        },
        {
          path: 'settings',
          component: SettingsPage,
          meta: { roles: ['Administrator'] },
        },
        {
          path: 'my-tasks',
          component: ServiceWorkerTasksPage,
          meta: { roles: ['ServiceWorker'] },
        },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!auth.token) auth.restore()
  if (to.meta.public) {
    if (!auth.isAuthenticated) return true
    return auth.shell === 'service' ? '/my-tasks' : '/dashboard'
  }
  if (!auth.isAuthenticated) return { path: '/login', query: { redirect: to.fullPath } }
  if (to.path === '/dashboard' && auth.shell === 'service') return '/my-tasks'
  const roles = to.meta.roles as string[] | undefined
  if (roles && (!auth.role || !roles.includes(auth.role))) return '/not-authorized'
  return true
})

export default router
