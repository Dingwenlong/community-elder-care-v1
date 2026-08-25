# 社区独居老人照料系统 UI 美化方案

> 版本：v1.0｜日期：2026-08-25
> 适用范围：`apps/mobile/`（Flutter 老人端 + 家属端）、`apps/admin-web/`（Vue 3 管理端）
> 约束：本方案只做视觉与交互层优化，不改动业务逻辑、API 契约与现有测试选择器（`data-testid`、Semantics label 保持不变），确保 `verify-all.ps1`、Vitest、Playwright、flutter test 全部继续通过。

---

## 0. 现状评估

| 端 | 技术栈 | 样式现状 | 主要问题 |
|---|---|---|---|
| 管理端 admin-web | Vue 3 + Vite + Pinia，无 UI 框架，手写 CSS | 已有 `styles/tokens.css`（配色 + 间距令牌）和 `base.css` | 令牌体系不完整（无圆角/阴影/字号/动效令牌）；组件样式散落在各页面 scoped 块中，同类组件（按钮、卡片、徽章）写法不一；无响应式断点体系 |
| 客户端 mobile | Flutter Material 3 + Riverpod + go_router | 主题内联在 `app/community_care_app.dart`，仅定义了 ColorScheme 和少量 ButtonTheme | 主题未抽取为独立文件；色值与管理端令牌存在偏差（见下）；老人端适老化有余而视觉层次不足（纯文字堆叠）；家属端缺卡片化与状态可视化 |
| 两端一致性 | — | 各自为政 | 管理端 `--emergency: #C92A2A` 与移动端 `error: #B42318` 不一致；管理端 `--navy: #0B315D` 与移动端 `secondary: #173B67` 不一致；圆角体系（4px 直角系）过于生硬 |

**方案核心动作**：建立一份「单一事实来源」的设计令牌（Design Tokens），两端各自实现但数值完全一致；在令牌之上沉淀两端的组件样式库；再按优先级逐页落地。

---

## 1. 整体视觉设计规范

### 1.1 配色方案

品牌基调：**安心蓝 + 暖光橙**。蓝色传递社区服务的专业与可信赖，暖橙色（源自现有 `--focus: #FFB000`）作为老人端的温暖点缀与全局焦点色。两端共用同一套色值。

#### 品牌与功能色（两端统一，加粗为对现状的修正）

| 令牌 | 色值 | 用途 | 备注 |
|---|---|---|---|
| `brand/primary` | `#0969DA` | 主操作按钮、链接、选中态 | 沿用现状，两端已一致 |
| `brand/primary-hover` | `#0758B8` | 主色悬停/按压 | 沿用 |
| `brand/primary-soft` | `#E8F1FC` | 主色浅底（标签、选中行） | 沿用 |
| `brand/navy` | `#0B315D` | 管理端侧边栏、页眉标题、图表主色 | **移动端 secondary 由 `#173B67` 改为 `#0B315D`** |
| `brand/navy-deep` | `#082747` | 侧边栏 active 项、深色强调 | 沿用 |
| `accent/warm` | `#FFB000` | 焦点框、老人端问候语强调、待办提醒角标 | 沿用 focus 色升格为品牌辅助色 |
| `accent/warm-soft` | `#FFF5E6` | 提醒类浅底 | 沿用 warning-soft |
| `danger` | **`#C92A2A`** | 紧急求助、错误、一级事件 | **移动端 error 由 `#B42318` 统一为 `#C92A2A`** |
| `danger-soft` | `#FFF0F0` | 危险浅底、一级事件行底 | 沿用 |
| `warning` | `#A15C00` | 二级事件、待跟进 | 沿用 |
| `success` | `#216E4E` | 已结案、已签到、正常 | 沿用 |
| `success-soft` | `#E9F7F0` | 成功浅底 | 沿用 |

#### 中性色（文字 / 背景 / 线条，沿用现有令牌，两端对齐）

| 令牌 | 色值 | 用途 |
|---|---|---|
| `ink/strong` | `#10253F` | 标题、关键数字 |
| `ink/default` | `#263D57` | 正文 |
| `ink/muted` | `#61738A` | 次要说明、占位符 |
| `paper` | `#F5F7FA` | 页面背景 |
| `surface` | `#FFFFFF` | 卡片、表格、弹窗 |
| `surface/muted` | `#EEF2F6` | 斑马纹、分组底 |
| `line` | `#D7DEE7` | 分隔线、输入框边 |
| `line/strong` | `#B9C5D2` | 悬停边、表头下边 |

**无障碍对比度**：管理端正文 ≥ 4.5:1（WCAG AA）；**老人端所有文字 ≥ 7:1（WCAG AAA）**，`ink` 系三色对白色背景分别为 12.6:1 / 9.4:1 / 5.9:1，老人端正文禁用 `ink/muted` 单独承载关键信息。

**事件等级配色**（与管理端 `EventLevelBadge` 对齐，客户端家属端复用同一语义）：

| 等级 | 文字色 | 底色 | 语义 |
|---|---|---|---|
| L1 紧急 | `#C92A2A` | `#FFF0F0` | SOS、长时间无活动 |
| L2 重要 | `#A15C00` | `#FFF5E6` | 未签到超时、求助 |
| L3 常规 | `#0B315D` | `#E8F1FC` | 探访、随访 |
| 已结案 | `#216E4E` | `#E9F7F0` | 闭环 |

### 1.2 字体体系

| 角色 | 管理端（CSS） | 客户端（Flutter） |
|---|---|---|
| 展示字体 `font-display` | `"Microsoft YaHei UI", "Noto Sans SC", sans-serif` | `fontFamilyFallback: ["Microsoft YaHei UI", "Noto Sans SC", "PingFang SC"]`（Android 实际命中系统思源黑体） |
| 正文字体 `font-body` | `"Noto Sans SC", "Microsoft YaHei", sans-serif` | 同上 |
| 数字字体 `font-numeric` | `Bahnschrift, "DIN Alternate", sans-serif` | 同 fallback；用于时钟、统计数字，开启 `fontFeatures: tabularNums` 保证表格数字等宽对齐 |

字号阶梯（管理端基准 / 老人端基准，老人端经由现有 `elderFontScaleProvider` 提供 1.0× / 1.3× / 1.6× 三档）：

| 令牌 | 管理端 | 老人端 | 家属端 | 用途 |
|---|---|---|---|---|
| `text/display` | 28px/700 | 32px/700 | 28px/700 | 页面大标题、问候语 |
| `text/title` | 20px/600 | 24px/700 | 20px/600 | 卡片标题 |
| `text/body` | 16px/400 | 20px/400 | 16px/400 | 正文 |
| `text/secondary` | 14px/400 | 18px/400 | 14px/400 | 辅助说明 |
| `text/caption` | 12px/400 | 不使用 | 12px/400 | 表格辅助列、时间戳 |
| `text/action` | 16px/600 | 24px/700 | 16px/600 | 按钮文字（沿用 `LargeActionButton` 现状） |

行高统一 1.5（标题 1.3），管理端正文最小 14px，老人端正文最小 18px。

### 1.3 图标风格

- **统一线性图标，2px 描边、圆角端点**，不用面性/多色图标混用。
- 客户端：使用 Flutter 内置 `Icons.*_outlined` 系列（Material Symbols 风格），尺寸档位 20 / 24 / 32 / 48（老人端首页主功能用 48）。
- 管理端：新建 `components/ui/AppIcon.vue`，以内联 SVG 注册 24 个以内业务图标（仪表盘、老人、事件、探访、设备、报表、审计、设置、退出、搜索、筛选、电话、定位、告警、时钟、勾选、关闭、箭头方向×4），统一 `viewBox="0 0 24 24"`、`stroke-width="2"`、尺寸档位 16 / 20 / 24。
- 语义规则：图标不单独承载含义，必须配文字标签（老人端）或 tooltip（管理端）；紧急类图标固定 `danger` 色。

### 1.4 间距、圆角与阴影标准

**间距**：沿用现有 4px 基网，两端使用同一阶梯，禁止出现阶梯外数值：

`4 / 8 / 12 / 16 / 20 / 24 / 32 / 48`（管理端已有 `--space-1..7`，补 `--space-45: 20px`；Flutter 新建 `AppSpacing` 常量类：`xs=4, sm=8, md=12, lg=16, xl=20, xxl=24, xxxl=32, huge=48`）。

**圆角**（新增令牌，修正当前全 4px 的生硬感）：

| 令牌 | 值 | 用途 |
|---|---|---|
| `radius/sm` | 6px | 输入框、标签、小徽章 |
| `radius/md` | 10px | 按钮、下拉、表格容器 |
| `radius/lg` | 14px | 卡片、弹窗 |
| `radius/xl` | 20px | 老人端大按钮、首页状态大卡 |
| `radius/pill` | 999px | 胶囊标签、头像 |

**阴影**（新增令牌，仅浅色主题）：

| 令牌 | 值 | 用途 |
|---|---|---|
| `shadow/sm` | `0 1px 2px rgba(16,37,63,.06), 0 1px 3px rgba(16,37,63,.08)` | 卡片常态 |
| `shadow/md` | `0 4px 12px rgba(16,37,63,.10)` | 悬停卡片、下拉菜单 |
| `shadow/lg` | `0 12px 32px rgba(16,37,63,.16)` | 弹窗、抽屉 |

Flutter 侧对应 `BoxShadow` 常量；卡片统一用 `shadow/sm` + `elevation: 0`（用阴影而非 Material 默认 elevation 染色）。

### 1.5 令牌落地方式（单一事实来源）

- 新建 `docs/ui/design-tokens.json` 存放全部令牌数值（机器可读）。
- 管理端：扩展 `apps/admin-web/src/styles/tokens.css`，按上表补齐圆角、阴影、字号、动效、断点令牌。
- 客户端：新建 `apps/mobile/lib/core/theme/app_theme.dart`（`AppColors` / `AppTextStyles` / `AppSpacing` / `AppRadius` / `AppShadows` 常量类 + `buildAppTheme()`），把 `community_care_app.dart` 内联主题迁移过去并修正两处色差。

---

## 2. 客户端界面优化（Flutter：老人端 + 家属端）

### 2.1 老人端首页（`elder/home/elder_home_page.dart`）——本方案第一优先级页面

现状为纯文字纵向堆叠，优化为**三层视觉层次**：

1. **顶部问候区**：日期（`text/secondary`）+「李奶奶，早上好」（`text/display`，问候语中的称呼用 `accent/warm` 深一度 `#8A5A00` 强调）+ 天气行。背景用 `surface` 白卡改为页面直出，减少卡片嵌套。
2. **平安签到状态大卡**（全宽，`radius/xl`，`shadow/sm`）：
   - 未签到：`danger` 系浅底 `#FFF0F0` + 大字「今天还没报平安」+ 卡内 64px 主按钮「我很好，确认平安」；
   - 已签到：`success` 系浅底 + 大号对勾图标（48px）+「今天已签到，社区知道了」。
   - 状态切换时播放对勾绘制动画（见 §6）。
3. **今日待办列表**：卡片化（白底、`radius/lg`、左侧 4px 色条标识类型：服药 `accent/warm`、探访 `primary`、随访 `success`），每条右侧 32px 箭头图标；空态显示插画位 +「今天没有待办，好好休息」。
4. **SOS 求助**：固定于底部安全区上方的全宽大按钮（高 72px、`danger` 实色、`radius/xl`、24px 加粗白字 + 24px 警铃图标），滚动时始终可见；点击进入 `help_category_page` 二次确认（现状流程保留）。
5. **导航**：`elder_shell` 底部导航加高至 80px，图标 28px + 文字 18px，最多 3 项（首页 / 求助 / 我的），选中态用 `primary-soft` 底 + `primary` 图标文字。

### 2.2 老人端其他页面

- **求助分类页**：分类项改为 2 列大网格卡片（每卡高 ≥ 96px，图标 48px + 文字 20px），替代列表，减少误触。
- **聊天页**（`elder_chat_page`）：气泡圆角 `radius/lg`，自己 `primary` 实色白字，对方白底；输入框加高至 56px，发送按钮 56×56 圆形。
- **设置页**：字号档位用三段式分段控件（「标准 / 大 / 特大」）实时预览；每行设置项高 ≥ 64px。

### 2.3 家属端（`family/`）

- **首页**：顶部「老人今日状态」汇总卡（签到状态 + 未结事件数 + 最近一次探访时间，三个数字用 `font-numeric` 大字）；下方「最近动态」时间线卡片流，复用事件等级配色（§1.1）。
- **事件页 / 记录页**：列表项卡片化，等级徽章居左、时间戳 `text/caption` 居右；详情页顶部用等级色横幅条。
- **导航**：标准 BottomNavigationBar（4 项以内），选中态与老人端一致。

---

## 3. 管理端界面优化（Vue 3 admin-web）

### 3.1 全局框架（`layouts/CommunityLayout.vue`）

- **侧边栏**（宽 `--sidebar-width: 232px`，`navy` 深底白字）：菜单项高 44px、左 4px 透明指示条，active 项指示条 `accent/warm` + 背景 `navy-deep`；图标 20px 与文字 14px 居中对齐；折叠态 64px 仅图标（见 §5 响应式）。
- **顶栏**（高 `--header-height: 72px`，白底 + 底部 `line` 细线）：左为当前页标题（`text/title`），右为全局搜索框（宽 280px）、通知铃铛（带未读角标）、用户头像 + 角色名；顶栏 sticky。
- **内容区**：`paper` 底，最大宽度 1440px 居中，内边距 `space-5`(24px)。

### 3.2 Dashboard 首页（`DashboardPage.vue`）

- 第一行 **KPI 卡片**（4 张：`今日待处理事件` / `未签到老人` / `进行中服务单` / `设备离线数`）：白卡 `radius/lg` + `shadow/sm`，左上 40px 图标方块（语义浅底 + 深色图标），右侧数字 32px `font-numeric` 加粗 + 环比小字；异常指标数字用 `danger`。
- 第二行左 **事件等级分布**（横向堆叠条 + 图例，用事件等级四色），右 **今日待办 Top5** 紧凑列表。
- 第三行 **最新事件时间线**，复用 `EventTimeline` 组件并统一样式（见 §4）。

### 3.3 列表页（事件 / 老人 / 探访 / 服务单 / 审计）

统一表格规范（新建 `AppTable`，§4）：

- 表头：`surface/muted` 底、`text/secondary` 600 字重、sticky；行高 48px，斑马纹 `#FAFBFC`；行悬停 `primary-soft` 40% 透明度底；点击整行进详情。
- 状态列一律用徽章组件（`EventLevelBadge` 扩展为通用 `AppBadge`），禁止裸文字状态。
- 数字列右对齐 + 等宽数字；时间列格式统一 `MM-DD HH:mm`。
- 筛选栏：卡片化工具条（搜索框 + 下拉筛选 + 「重置」文字按钮），与表格间距 `space-4`。
- 空态：居中插画位 + 「暂无数据」+ 引导操作按钮；加载用骨架行（§6），不用全屏 loading。
- 分页：右下，「共 N 条」居左。

### 3.4 详情页与表单页

- **事件详情**（`CareEventDetailPage`）：顶部等级色横幅（4px 高色条 + 等级徽章 + 事件标题 + 关键时间）；主体左 2/3 时间线 + 右 1/3 信息面板（老人摘要卡、设备信号卡、操作按钮组）；危险操作（如破窗查看 `BreakGlassDialog`）固定 `danger` 描边按钮样式。
- **老人编辑**（`ElderEditPage`）：表单分组卡片（基本信息 / 健康信息 / 联系人 / 设备绑定），≥1024px 两列排布，标签顶置、必填星号 `danger`、错误提示内联红字 + 输入框 `danger` 边；底部操作条 sticky（右对齐：取消文字按钮 + 保存主按钮）。
- **登录页**：居中 400px 卡片（`shadow/md`），品牌区顶部 `navy` 色块 + 系统名；演示账号提示用 `accent/warm-soft` 提示条。

---

## 4. 组件样式统一（可复用组件体系）

### 4.1 管理端：新建 `src/components/ui/` 基础组件库

| 组件 | 规格要点 |
|---|---|
| `AppButton` | 四种变体：primary（`primary` 实底白字）/ secondary（白底 `line-strong` 边）/ danger（`danger` 实底，仅破坏性操作）/ ghost（纯文字）。高度：默认 36px、大 44px；圆角 `radius/md`；按压 `transform: scale(.97)`；loading 态内置 spinner 禁用；图标按钮 36×36 |
| `AppInput` / `AppSelect` | 高 36px（表格筛选）/ 40px（表单）；边 `line`，悬停 `line-strong`，聚焦 `primary` 边 + 3px `primary-soft` 外发光；错误态 `danger` 边 + 下方 12px 红字 |
| `AppCard` | 白底 `radius/lg` + `shadow/sm`，内边距 24px；可选标题栏（标题 + 右侧操作区）；悬停可交互卡片升 `shadow/md` |
| `AppModal` | 宽度档 400 / 560 / 720px；遮罩 `rgba(8,39,71,.45)` + 4px 背景模糊；圆角 `radius/lg`、阴影 `shadow/lg`；标题栏 + 内容 + 底部按钮条三段；进场 200ms 上浮淡入；Esc 关闭（破坏性操作除外，`BreakGlassDialog` 迁移到此组件） |
| `AppBadge` | 由 `EventLevelBadge` 泛化：等级/状态语义色（§1.1 表），`radius/pill`、12px 字、左右 8px 内边距 |
| `AppMenu` / 下拉 | 白底 `shadow/md`、`radius/md`，项高 36px，悬停 `surface/muted` |
| `AppToast` | 右上角滑入，成功/警告/错误三语义色左图标，4s 自动消失 |
| `AppSkeleton` / `AppEmpty` | 骨架屏（呼吸动画）与空态统一组件 |
| `AppIcon` | 见 §1.3 |

现有 `EventTimeline`、`StatusNotice`、`DemoDataBadge` 迁入 ui 目录并改用令牌重写样式；页面内 scoped 样式只保留布局，颜色/字号/间距一律引用令牌。

### 4.2 客户端：扩展 `lib/core/widgets/`

保留并升级 `LargeActionButton`（圆角升 `radius/xl`，加按压缩放动效）；新增 `StatusCard`（签到/事件状态大卡）、`TimelineTile`（家属端动态）、`AppBadge`、`LevelColorBar`（4px 等级色条）、`SkeletonBox`；全部样式取自 `app_theme.dart` 常量，页面内禁止硬编码色值。

### 4.3 交互效果统一

- 可点元素：hover 有反馈（变色/阴影/上浮 1px），active `scale(.97)`，disabled 透明度 45% 且禁手势。
- 焦点：全局 `:focus-visible` 3px `accent/warm` 描边（已有，保留）；Flutter 侧 `focusColor` 同色。
- 所有颜色过渡 120ms、位移/阴影过渡 200ms，曲线统一 `cubic-bezier(0.2, 0, 0, 1)`。

---

## 5. 响应式适配

### 5.1 管理端断点（新增令牌）

| 断点 | 范围 | 布局策略 |
|---|---|---|
| `--bp-desktop` | ≥ 1280px | 完整侧边栏 232px + 内容最大宽 1440px；表单两列 |
| `--bp-laptop` | 1024–1279px | 侧边栏 232px；KPI 4→2×2；详情页左右栏保持 |
| `--bp-tablet` | 768–1023px | 侧边栏折叠为 64px 图标栏（hover 展开浮层）；表单单列；表格隐藏次要列（优先级低的数据属性标注 `data-col-priority`） |
| `--bp-mobile` | < 768px | 侧边栏改顶部栏 + 抽屉菜单；表格转卡片列表（每行变一张卡，关键字段前两行，其余展开）；操作条全宽 sticky 底部 |

实现：CSS 容器查询优先（内容区宽度驱动），媒体查询兜底；不引入新依赖。

### 5.2 客户端（Flutter）

- **手机（<600dp）**：现状布局，老人端锁竖屏。
- **平板（≥600dp）**：家属端用 `NavigationRail` 左侧导航 + 内容双栏（列表 + 详情同屏，`LayoutBuilder` 判断）；老人端保持单列居中、内容最大宽 560dp，按钮与字号不因大屏缩小。
- 横屏：家属端可用，老人端不强制适配但不允许布局溢出（全部页面 `SafeArea` + 可滚动）。

---

## 6. 交互动效设计

| 场景 | 规格 |
|---|---|
| 页面过渡（管理端） | 路由切换 160ms 淡入 + 8px 上移；`<router-view v-slot>` + `<Transition>`，禁用花哨翻转 |
| 页面过渡（客户端） | go_router 统一 `CustomTransitionPage`：fade + 4dp 上移 200ms；老人端主流程（签到/求助）页间用 250ms，给老人感知缓冲 |
| 加载状态 | 列表/表格用骨架屏（`AppSkeleton` / `SkeletonBox`，1.2s 呼吸透明度 .5↔.9）；按钮提交用按钮内 spinner；禁止全屏菊花超过 1 次 |
| 操作反馈 | 成功：`AppToast` + 老人端签到卡对勾路径绘制动画 500ms + `HapticFeedback.mediumImpact`；失败：Toast + 表单字段抖动 300ms（仅老人端大表单，管理端用红字内联） |
| 按钮按压 | `scale(.97)` 120ms；老人端大按钮额外加深底色 8% |
| 卡片交互 | 可点卡片 hover 上浮 -2px + `shadow/md`，200ms |
| 下拉/弹窗 | 菜单 150ms 展开（透明度 + 4px 位移）；弹窗 200ms 上浮淡入；关闭 120ms |
| 数字变化 | KPI 卡数字 300ms 滚动计数（`font-numeric` 等宽防跳动） |
| 降级 | 管理端 `@media (prefers-reduced-motion: reduce)` 关闭一切非必要动画；Flutter 读取 `MediaQuery.disableAnimations`，命中时所有动画时长归零 |

---

## 7. 优先级与实施顺序

| 阶段 | 内容 | 涉及文件 | 验收标准 |
|---|---|---|---|
| **P0 地基（第 1 周）** | ① `design-tokens.json` 定稿；② 扩展 `tokens.css`（圆角/阴影/字号/动效/断点）；③ 新建 Flutter `app_theme.dart` 并修正两处色差（error→`#C92A2A`、secondary→`#0B315D`）；④ 管理端 `ui/` 组件库 6 件（Button/Input/Card/Modal/Badge/Toast） | `docs/ui/`、`styles/tokens.css`、`core/theme/`、`components/ui/` | 两端色值完全一致；现有页面无视觉回归；Vitest / flutter test 全绿 |
| **P1 核心页面（第 2 周）** | ① 老人端首页 + 签到状态卡 + SOS 底栏；② 管理端 Dashboard KPI + 布局框架（侧边栏/顶栏）；③ `AppTable` 落地事件列表页 | `elder/home/`、`CommunityLayout.vue`、`DashboardPage.vue`、`CareEventListPage.vue` | 老人端首页三层层次达成；Dashboard 一屏看全关键指标；表格规范在事件列表页跑通 |
| **P2 全面铺开（第 3 周）** | ① 其余列表页接入 `AppTable`；② 事件详情 + 老人编辑表单；③ 家属端首页/事件/记录；④ 老人端求助/聊天/设置；⑤ 弹窗体系迁移（含 `BreakGlassDialog`） | 其余 pages、`family/`、`elder/` | 所有页面无硬编码色值/间距；组件复用率 ≥ 80% |
| **P3 体验收尾（第 4 周）** | ① 响应式四断点 + 平板适配；② 全套动效 + reduced-motion 降级；③ 骨架屏/空态/Toast 全覆盖；④ 走查与对比度验收 | 全局 | 1280/1024/768/375 四档截图走查通过；老人端文字对比度全量 ≥ 7:1；`verify-all.ps1` + Playwright e2e 全绿 |

**贯穿原则**：
1. 每阶段结束跑一次 `verify-all.ps1` 与 e2e，样式改动不允许破坏 `data-testid` 与 Semantics label。
2. 先做令牌和组件，后改页面——页面改动只引用令牌，新增硬编码值一律视为返工。
3. 老人端的每一次视觉调整以「更大、更清晰、更少步骤」为裁决标准，美观让位于可读性。
