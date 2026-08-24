<template>
  <main class="app-shell">
    <section class="product-heading" aria-labelledby="product-title">
      <p class="eyebrow">社区照护 · 参赛演示</p>
      <h1 id="product-title">社区独居老人照料系统</h1>
      <span class="demo-badge">演示数据</span>
    </section>
  </main>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 32px;
  background: #f4f7fb;
}

.product-heading {
  width: min(720px, 100%);
  padding: 48px;
  border: 1px solid #d9e2ef;
  border-radius: 16px;
  background: #ffffff;
  box-shadow: 0 16px 40px rgb(15 47 87 / 8%);
}

.eyebrow {
  margin: 0 0 12px;
  color: #2f6fb2;
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0.08em;
}

h1 {
  margin: 0;
  color: #173b67;
  font-size: clamp(32px, 5vw, 48px);
  line-height: 1.2;
}

.demo-badge {
  display: inline-flex;
  margin-top: 24px;
  padding: 6px 12px;
  border-radius: 999px;
  background: #e8f1fb;
  color: #245d9c;
  font-size: 14px;
  font-weight: 700;
}

@media (max-width: 600px) {
  .app-shell {
    padding: 20px;
  }

  .product-heading {
    padding: 32px 24px;
  }
}
</style>
