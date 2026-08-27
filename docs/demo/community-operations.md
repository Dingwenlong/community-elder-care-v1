# 社区运营管理

## 使用入口

“人员与任务”统一查看探访、服务工单和回访，可按类型、负责人、状态和逾期筛选。“照料事件”详情中的当前负责人可以创建任务；只有尚未开始的任务允许转派，并须填写原因。转派不改变事件负责人、预约时间或必须完成的属性。

预置社区人员为 `community.demo`（周敏）、`community.second`（陈佳），服务人员为 `service.demo`（王芳）、`service.second`（刘志远），均负责 A01。原有老人、家属和管理员账号保留，密码仍来自演示运行环境。管理员可以查看任务，但不能代替事件负责人分派或处理。服务人员进入“我的任务”后点击“刷新我的任务”，即可看到当前分配，不需要重新登录。

“设备信号”现在提供设备台账、启停和历史筛选。管理员填写原因后停用设备，硬件与模拟新增上报均被拒绝；历史信号和关联事件保留。启用后恢复上报。页面分别展示硬件和模拟上报时间，不推断实时在线状态。

“演示设置”中的“加载运营演示场景”需要确认，新增 12 条近期虚构事件及相关任务、模拟信号。首次加载包含 9 次完成探访、9 张完成工单和 9 次完成回访；重复加载不会覆盖已经处理的任务。默认重置会清除场景，之后可以重新加载。主故事使用的老人和 SOS 设备不被场景换绑。

## 报告口径

报告默认最近 30 天，可选最近 7 天、本月或自选范围，首尾日期均包含，最多 90 天。按北京时间划分日期。

| 指标 | 统计方式 |
| --- | --- |
| 新增事件 | 创建时间落在期间内 |
| 结案事件 | 首次结案时间落在期间内 |
| 完成探访、工单、回访 | 完成时间落在期间内，取消任务不算完成 |
| 探访覆盖人数 | 期间完成探访的老人去重 |
| 平均首次接单时长 | 期间首次接单事件的首次接单时间减创建时间，无样本显示“暂无数据” |
| 当前未结任务、当前逾期任务 | 报告生成时的当前任务，不受日期筛选限制；仍受片区限制 |

探访按预约结束时间、回访按到期时间、工单按可选截止时间判断逾期。旧工单没有截止时间时显示“未设截止时间”，不补造时间，也不计逾期。新建工单表单要求填写时间。逾期不会自动升级或关单，原强制任务与回访关单校验保留。

汇总、每日趋势和人员统计分别提供 CSV。导出使用页面最近一次成功查询的日期和片区，不采用尚未查询的表单修改。下载时重新读取当前数据库，因此任务正在被其他人员更新时，数值可能比页面快照更新。CSV 包含 UTF-8 BOM、引号与换行转义、公式注入保护，不含健康资料、联系方式和内部探访记录。打印仅输出当前页面已授权报告，可使用浏览器另存 PDF。

这些指标只用于项目内部运营演示，不代表政策达标率或真实救援效果。设计背景参考[探访关爱指导意见](https://www.gov.cn/zhengce/zhengceku/2022-10/13/content_5718017.htm)。

## API 与兼容

全部沿用 JWT、`/api/v1` 和现有 Problem 错误格式。

| 接口 | 参数或说明 |
| --- | --- |
| `GET /operations/personnel` | 管理员查看全部；社区人员仅本片区 |
| `GET /operations/tasks` | `taskType=Visit/ServiceOrder/FollowUp`、`assignedUserId`、`status`、`overdueOnly` |
| `POST /visits/{id}/reassign` | `assignedUserId`、`reason`、`expectedVersion` |
| `POST /service-orders/{id}/reassign` | 同上 |
| `POST /follow-ups/{id}/reassign` | 同上 |
| `GET /operations/tasks/{id}/reassignments` | 受任务片区权限限制的转派记录 |
| `GET /devices` | 管理员台账，无设备令牌 |
| `GET /devices/{id}/signals` | `from`、`to`、`signalType`、`isSimulation`，日期按接收时间筛选 |
| `PATCH /devices/{id}/enabled` | `enabled`、`reason`、`expectedVersion` |
| `GET /reports/operations` | `from`、`to`（YYYY-MM-DD）、`areaCode`（管理员可选） |
| `GET /reports/operations.csv` | 同报告，另加 `section=summary/daily/personnel` |
| `POST /demo/operations-scenario` | 管理员；请求头 `X-Confirm-Operations-Scenario: LOAD-OPERATIONS` |

转派、开始和完成共享数据库应用维护的并发版本；过期版本或数据库并发冲突返回 409。服务人员权限取数据库当前工单归属，旧 JWT 中的单任务标识不再授予任务权限，也不授予档案读取权限。转派、设备启停及 CSV 导出写入审计。

`AddCommunityOperations` 迁移只增加字段和转派表，不重置业务库。旧截止时间保持空值。启动时补齐已知演示账号缺失的姓名及服务片区，不覆盖已有显示名。日期比较在数据库先限定片区后进行，避免 SQLite 的 DateTimeOffset 比较限制。

## 复验

2026-08-27 本地验证结果：.NET 136 项（单元 66、集成 70）通过，Web 38 项通过；oxlint、ESLint、Web 类型检查与构建、EF 迁移模型一致性及 PowerShell 脚本解析通过。7 条浏览器用例已通过类型检查与发现，尚未执行。以上是工作区验证，不是提交、发布或实物验收回执。

依赖已按锁文件安装时，从仓库根目录运行：

```powershell
.\scripts\verify-operations.ps1
```

默认执行 .NET、Web 测试、静态检查、类型检查、构建和浏览器测试发现；不启动服务、不执行浏览器、不涉及移动端和固件。

浏览器验收需由操作人员在允许启动服务的环境中，先启动使用**独立可重置数据库**的 API 与 Web。API 使用 Development 环境，在启动前设置进程变量 `ConnectionStrings__CommunityCare` 指向专用 SQLite 文件。Web 的 `VITE_API_PROXY_TARGET` 必须指向该 API。不要使用正在演示或需要保留数据的数据库。

在测试进程设置 `COMMUNITYCARE_API_URL`、`COMMUNITYCARE_WEB_URL` 和与 API 一致的 `COMMUNITYCARE_DEMO_PASSWORD`。地址必须为本机回环地址。然后运行：

```powershell
.\scripts\verify-operations.ps1 -Browser -ConfirmDemoReset
```

浏览器套件包含原主故事、原授权边界和两条新增运营流程。它会重置演示资料，并在成功步骤后通过 API 回读任务、事件和报表。新增用例保存安全的 JSON 回读附件、报表截图和打印 PDF。测试附件位于忽略目录 `tests/e2e/test-results/`；失败 trace 可能含测试会话信息，不应直接作为公开交付附件。

本轮浏览器执行因测试服务启动被运行环境策略阻止而未完成。用例发现或类型检查不代表浏览器验收通过。打印分页、真实浏览器交互以及新增流程与主故事的连续运行仍待上述复验；本轮未执行移动端或实物硬件验收。
