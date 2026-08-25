<template>
  <main class="not-authorized">
    <div class="surface notice-panel">
      <p>访问范围受限</p>
      <h1>当前账号不能打开这个工作区</h1>
      <span>老人端和家属端请使用移动端；服务人员只能查看分配给自己的任务。</span>
      <RouterLink class="primary-button" to="/dashboard">返回可用工作台</RouterLink>
    </div>
  </main>
</template>

<style scoped>
.not-authorized {
  display: grid;
  min-height: 100vh;
  place-items: center;
  padding: var(--space-6);
}

.notice-panel {
  width: min(620px, 100%);
  padding: var(--space-7);
}

.notice-panel p {
  color: var(--emergency);
  font-weight: 700;
}

.notice-panel span {
  display: block;
  margin-bottom: var(--space-5);
  color: var(--ink-muted);
}
</style>
