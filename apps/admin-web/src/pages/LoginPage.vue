<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { ApiError } from '@/api/apiClient'
import loginCommunityVisit from '@/assets/illustrations/login-community-visit.webp'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const username = ref('')
const password = ref('')
const submitting = ref(false)
const errorMessage = ref('')

async function submit() {
  errorMessage.value = ''
  submitting.value = true
  try {
    const session = await auth.login(username.value, password.value)
    if (session.shell === 'service') {
      await router.replace('/my-tasks')
    } else if (session.shell === 'community' || session.shell === 'admin') {
      await router.replace('/dashboard')
    } else {
      await router.replace('/not-authorized')
    }
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '登录未完成，请稍后重试。'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="login-shell">
    <section class="login-context" aria-labelledby="product-name">
      <img class="login-context__cover" :src="loginCommunityVisit" alt="" aria-hidden="true" />
      <div class="login-context__scrim" aria-hidden="true"></div>
      <div class="login-context__content">
        <h1 id="product-name">社区独居老人照料系统</h1>
        <p>社区工作人员在这里处理安全确认、探访和服务记录。</p>
      </div>
    </section>

    <section class="login-panel" aria-labelledby="login-title">
      <div class="login-form-wrap">
        <p class="login-context-label">工作人员入口</p>
        <h2 id="login-title">登录工作区</h2>
        <p>请使用已经开通的社区、服务或管理员账号登录。</p>
        <form @submit.prevent="submit">
          <label for="username">账号</label>
          <input id="username" v-model="username" autocomplete="username" required />
          <label for="password">密码</label>
          <input
            id="password"
            v-model="password"
            type="password"
            autocomplete="current-password"
            required
          />
          <p v-if="errorMessage" class="login-error" role="alert">{{ errorMessage }}</p>
          <button class="primary-button" type="submit" :disabled="submitting">
            {{ submitting ? '正在登录' : '登录' }}
          </button>
        </form>
      </div>
    </section>
  </main>
</template>

<style scoped>
.login-shell {
  display: grid;
  min-height: 100vh;
  grid-template-columns: minmax(340px, 0.85fr) minmax(480px, 1.15fr);
  background: var(--surface);
}

.login-context {
  position: relative;
  overflow: hidden;
  background: var(--navy-deep);
}

.login-context__cover,
.login-context__scrim {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.login-context__cover {
  object-fit: cover;
}

.login-context__scrim {
  background: rgb(10 35 66 / 78%);
}

.login-context__content {
  display: flex;
  position: relative;
  z-index: 1;
  min-height: 100%;
  flex-direction: column;
  justify-content: center;
  padding: clamp(40px, 7vw, 96px);
  color: #e8f1fb;
}

.login-context__content h1 {
  max-width: 540px;
  margin: 0;
  color: white;
  font-size: clamp(34px, 4vw, 54px);
}

.login-panel {
  display: grid;
  place-items: center;
  padding: var(--space-6);
  background: var(--paper);
}

.login-form-wrap {
  width: min(460px, 100%);
}

.login-form-wrap h2 {
  margin-bottom: var(--space-6);
  font-size: 30px;
}

form {
  display: grid;
}

label {
  margin-bottom: var(--space-2);
  color: var(--ink-strong);
  font-weight: 700;
}

input {
  width: 100%;
  padding: 10px 12px;
  margin-bottom: var(--space-4);
  border: 1px solid var(--line-strong);
  border-radius: 2px;
  background: var(--surface);
}

.login-error {
  padding: var(--space-3);
  margin-bottom: var(--space-4);
  color: var(--emergency);
  background: var(--emergency-soft);
}

@media (max-width: 820px) {
  .login-shell {
    grid-template-columns: 1fr;
  }

  .login-context {
    height: 220px;
    min-height: 0;
  }

  .login-context__content {
    box-sizing: border-box;
    height: 100%;
    min-height: 0;
    padding: var(--space-6) var(--space-5);
  }

}
</style>
