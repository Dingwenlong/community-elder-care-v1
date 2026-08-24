# Community Elder Care v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 10 周内交付一个可在 Android 手机和比赛笔记本上稳定演示的社区独居老人照料闭环系统。

**Architecture:** 采用 ASP.NET Core 模块化单体和 SQLite，向同一套版本化 HTTP API 提供 Flutter 老人/家属 App、Vue 社区后台及 ESP32/设备模拟器接入。告警和状态流转由确定性规则控制，云端大模型只生成草稿，所有外部通知和救援动作均为醒目标记的演示模拟。

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core SQLite / xUnit，Vue 3 / TypeScript / Vite / Pinia / Vue Router / Vitest / Playwright，Flutter 3.47.1 / Dart 3.13.1 / Android SDK 36，PlatformIO Core 6.1.19 / ESP32 Arduino。

**Spec:** `docs/superpowers/specs/2026-08-24-community-elder-care-v1-design.md`

## Global Constraints

- 项目为“1 人 + Codex、10 周”的参赛演示，不按生产系统宣称能力。
- 主要老人角色为 75 岁及以上、基本自理，但存在慢性病、跌倒或弱社会支持风险的独居老人。
- 默认生成 20 份、允许 15～25 份完全合成档案；界面持续显示“演示数据”。
- 老人与家属共用一个 Flutter APK，但登录后使用完全独立的路由壳、首页和字段权限。
- 社区、服务人员和管理员使用 Vue Web；服务人员只能读取当前任务所需字段。
- 不接入真实 120、短信、电话、医院、支付或政府平台；所有外部动作保存为模拟记录。
- AI 不诊断、不改药、不判断“没有危险”、不直接联系外部人员、不修改风险等级、不关单。
- 明确危险表达先走本地/服务端固定规则，再决定是否调用云端模型；云端不可用时核心闭环继续运行。
- ESP32 是可替换演示增强项；设备模拟器必须覆盖同一接入契约和完整业务流程。
- 后端保持模块化单体，不引入微服务、消息队列、Kubernetes、复杂排班、支付或服务商城。
- 使用 SQLite 和 ASP.NET Core 托管后台任务；真实试点前再评估 PostgreSQL、TLS、数据库加密、密钥管理和高可用。
- 页面、提示、报告和 AI 回复使用直接、具体、自然的中文。
- 项目锁定稳定 `.NET SDK 10.0.302`，不得落到本机 `10.0.400-preview`；`global.json` 设置 `allowPrerelease: false`。
- Node 使用 `24.16.0`、npm `11.13.0`；Flutter 使用 `3.47.1`，Dart `3.13.1`，Android SDK 36。工具路径由当前 PowerShell 进程或忽略的本机配置提供。
- `scripts/dev-env.ps1` 只修改当前进程环境，不写用户级或系统级 Path、JAVA_HOME、ANDROID_HOME。
- 本机 Windows `flutter_tester` 曾有间歇性原生崩溃；移动端验收必须在 Android 模拟器运行 `integration_test`，出现宿主崩溃时先用官方空项目复核，不改业务代码掩盖工具故障。
- 每个任务先得到失败测试或失败检查，再写最小实现；任务结束运行定向测试和相关全量测试并提交。

---

## 1. Locked File Structure

```text
community-elder-care-v1/
├─ .github/workflows/
├─ apps/
│  ├─ admin-web/
│  │  └─ src/{api,components,layouts,pages,router,stores,styles}/
│  └─ mobile/
│     └─ lib/{app,auth,core,elder,family,ai}/
├─ firmware/esp32-sos/
├─ src/
│  ├─ CommunityElderCare.Api/
│  │  ├─ Contracts/
│  │  ├─ Endpoints/
│  │  └─ Program.cs
│  ├─ CommunityElderCare.Core/
│  │  ├─ Common/
│  │  ├─ Identity/
│  │  ├─ Elders/
│  │  ├─ CheckIns/
│  │  ├─ CareEvents/
│  │  ├─ CareWork/
│  │  ├─ Ai/
│  │  └─ Devices/
│  └─ CommunityElderCare.Infrastructure/
│     ├─ Persistence/
│     ├─ Identity/
│     ├─ Elders/
│     ├─ CheckIns/
│     ├─ CareEvents/
│     ├─ CareWork/
│     ├─ Background/
│     ├─ Ai/
│     ├─ Devices/
│     ├─ Demo/
│     └─ Notifications/
├─ tests/
│  ├─ CommunityElderCare.UnitTests/
│  ├─ CommunityElderCare.IntegrationTests/
│  └─ e2e/
├─ scripts/
├─ docs/{demo,progress,superpowers}/
├─ CommunityElderCare.sln
├─ Directory.Build.props
├─ Directory.Packages.props
└─ global.json
```

### Stable cross-task contracts

The following names are fixed for all tasks:

```csharp
public enum DemoRole { Elder, Family, CommunityStaff, ServiceWorker, Administrator }

public sealed record ActorContext(
    Guid UserId,
    DemoRole Role,
    Guid? ElderId,
    string? AreaCode,
    Guid? AssignedTaskId);

public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ApiError(string Code, string Message);
```

API errors use RFC 7807 `ProblemDetails`; the stable machine-readable error code is stored in `extensions.code`. JSON uses camelCase and UTC ISO-8601 timestamps. All IDs are `Guid`; all domain times are `DateTimeOffset` supplied through `TimeProvider`.

## 2. Delivery Order

1. Foundation and fail-closed tooling.
2. Synthetic elder data and SQLite persistence.
3. Identity, field-level consent and authorization.
4. Check-in and reminder flow.
5. Care-event rules, state machine and idempotency.
6. Visits, service work orders and follow-up closure.
7. Community Web shell and elder records.
8. Community Web event operations.
9. Flutter role shells, API client and offline outbox.
10. Elder Android flows.
11. Family Android flows.
12. AI safety, chat and visit-summary drafts.
13. Device gateway, simulator and ESP32 firmware.
14. Audit, reporting, demo reset and one-click startup.
15. Full acceptance, accessibility, packaging and competition evidence.

Although the system has API, Web, Android and device surfaces, they are not delivered as separate subprojects. Each task extends the same elder-to-community care loop and lands only after its neighboring contracts and regression tests pass, which keeps integration work inside the ten-week path instead of postponing it to the end.

---

### Task 1: Establish the runnable foundation and fail-closed preflight

**Files:**
- Create: `.gitignore`
- Create: `.config/dotnet-tools.json`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `CommunityElderCare.sln`
- Create: `src/CommunityElderCare.Api/CommunityElderCare.Api.csproj`
- Create: `src/CommunityElderCare.Api/Program.cs`
- Create: `src/CommunityElderCare.Core/CommunityElderCare.Core.csproj`
- Create: `src/CommunityElderCare.Core/Common/OperationResult.cs`
- Create: `src/CommunityElderCare.Core/Identity/DemoRole.cs`
- Create: `src/CommunityElderCare.Core/Identity/ActorContext.cs`
- Create: `src/CommunityElderCare.Infrastructure/CommunityElderCare.Infrastructure.csproj`
- Create: `src/CommunityElderCare.Infrastructure/Persistence/CommunityCareDbContext.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Common/ApiError.cs`
- Create: `tests/CommunityElderCare.UnitTests/CommunityElderCare.UnitTests.csproj`
- Create: `tests/CommunityElderCare.IntegrationTests/CommunityElderCare.IntegrationTests.csproj`
- Create: `tests/CommunityElderCare.IntegrationTests/CommunityCareWebFactory.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/HealthEndpointTests.cs`
- Create: `apps/admin-web/**` using `create-vue`
- Create: `apps/admin-web/src/App.spec.ts`
- Create: `apps/mobile/**` using `flutter create`
- Create: `apps/mobile/integration_test/app_shell_test.dart`
- Create: `scripts/dev-env.ps1`
- Create: `scripts/preflight.ps1`
- Create: `scripts/run-mobile-test.ps1`
- Create: `README.md`

**Interfaces:**
- Produces: `GET /health/live` returning `200 { "status": "live" }`.
- Produces: `GET /health/ready` returning `200` only after SQLite can execute `SELECT 1`.
- Produces: process-local toolchain setup through `scripts/dev-env.ps1`.
- Produces: exact `DemoRole`, `ActorContext`, `OperationResult<T>`, and `ApiError` contracts shown above.

- [ ] **Step 1: Pin the stable toolchains and create the solution skeleton**

Create `global.json` with:

```json
{
  "sdk": {
    "version": "10.0.302",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

Run:

```powershell
dotnet new sln -n CommunityElderCare
dotnet new webapi -n CommunityElderCare.Api -o src/CommunityElderCare.Api --no-https --no-openapi
dotnet new classlib -n CommunityElderCare.Core -o src/CommunityElderCare.Core
dotnet new classlib -n CommunityElderCare.Infrastructure -o src/CommunityElderCare.Infrastructure
dotnet new xunit -n CommunityElderCare.UnitTests -o tests/CommunityElderCare.UnitTests
dotnet new xunit -n CommunityElderCare.IntegrationTests -o tests/CommunityElderCare.IntegrationTests
dotnet sln CommunityElderCare.sln add src/CommunityElderCare.Api src/CommunityElderCare.Core src/CommunityElderCare.Infrastructure tests/CommunityElderCare.UnitTests tests/CommunityElderCare.IntegrationTests
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.10
```

Set `TargetFramework` to `net10.0`, enable nullable and warnings as errors, and add project references `Api → Core + Infrastructure`, `Infrastructure → Core`, and tests to the projects they exercise.

Enable central package management and use this exact package set in `Directory.Packages.props`; remove version attributes generated in individual project files:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.10" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
</Project>
```

Remove the generated WeatherForecast endpoint and add these delivery exclusions to `.gitignore`: `.run/`, `.tools/`, `artifacts/`, `**/bin/`, `**/obj/`, `**/node_modules/`, `**/dist/`, `**/.dart_tool/`, `**/build/`, `**/.pio/`, `*.db`, `*.db-shm`, `*.db-wal`, `.env*`, and `firmware/esp32-sos/include/demo_config.h`. Do not ignore EF migrations, package lockfiles, public demo documentation or synthetic seed definitions.

- [ ] **Step 2: Write and run the failing API health test**

```csharp
[Fact]
public async Task Live_health_returns_stable_payload()
{
    await using var app = new CommunityCareWebFactory();
    using var client = app.CreateClient();
    var response = await client.GetAsync("/health/live");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("{\"status\":\"live\"}", await response.Content.ReadAsStringAsync());
}
```

Run:

```powershell
dotnet test tests/CommunityElderCare.IntegrationTests --filter FullyQualifiedName~Live_health_returns_stable_payload
```

Expected: FAIL because `/health/live` and `CommunityCareWebFactory` do not exist.

- [ ] **Step 3: Implement the API host and health endpoints**

`Program.cs` must expose a partial `Program` for `WebApplicationFactory`, configure camelCase JSON, register `TimeProvider.System`, map the two health routes, and return the stable JSON payload. Add `CommunityCareWebFactory` with a temporary SQLite file deleted during disposal.

```csharp
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CommunityCareDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: 503, title: "Database unavailable"));
```

Run the filtered test again and expect PASS.

- [ ] **Step 4: Scaffold the Vue shell with a failing smoke test**

Run `npm create vue@latest apps/admin-web` and choose TypeScript, Router, Pinia, Vitest and ESLint; choose no JSX and no Cypress. Write this test before replacing the generated screen:

```ts
it('shows the community-care product name', () => {
  render(App)
  expect(screen.getByRole('heading', { name: '社区独居老人照料系统' })).toBeTruthy()
  expect(screen.getByText('演示数据')).toBeTruthy()
})
```

Run `npm test -- --run` and expect FAIL, replace the generated app with the two visible labels, then rerun and expect PASS. Do not add a component library in this task.

- [ ] **Step 5: Scaffold the Flutter Android shell with an emulator test**

Run with the exact Flutter executable before the environment script exists:

```powershell
flutter create --platforms=android --org com.dingwenlong.communitycare apps/mobile
```

Write `integration_test/app_shell_test.dart`:

```dart
testWidgets('shows the elder-care demo identity', (tester) async {
  app.main();
  await tester.pumpAndSettle();
  expect(find.text('社区独居老人照料系统'), findsOneWidget);
  expect(find.text('演示数据'), findsOneWidget);
});
```

Run it on the single supported Android emulator and expect FAIL, replace the counter app with `CommunityCareApp`, then rerun and expect PASS.

- [ ] **Step 6: Add process-local environment and fail-closed preflight scripts**

`scripts/dev-env.ps1` must set only process variables:

```powershell
$taskFlutterRoot = $env:COMMUNITYCARE_FLUTTER_ROOT
$taskAndroidSdk = $env:COMMUNITYCARE_ANDROID_SDK_ROOT
$taskJavaHome = $env:COMMUNITYCARE_JAVA_HOME
$env:FLUTTER_ROOT = $taskFlutterRoot
$env:ANDROID_HOME = $taskAndroidSdk
$env:ANDROID_SDK_ROOT = $taskAndroidSdk
$env:JAVA_HOME = $taskJavaHome
$env:Path = "$taskFlutterRoot\bin;$taskJavaHome\bin;$taskAndroidSdk\platform-tools;$taskAndroidSdk\cmdline-tools\latest\bin;$env:Path"
```

`scripts/preflight.ps1` must check native exit codes immediately and verify `.NET 10.0.302`, Node `v24.16.0`, Flutter `3.47.1`, Dart `3.13.1`, Java, ADB, `android-36/android.jar`, build-tools `36.0.0`, SQLite write access and exactly one supported online Android emulator. A canary replacing `dotnet` with a temporary command that exits `73` must make preflight exit non-zero and must not print a success line.

For example, `scripts/run-mobile-test.ps1 -TestPath 'integration_test/role_shell_test.dart'` must parse `flutter devices --machine`, require exactly one supported Android emulator, and run that integration test from `apps/mobile`. It must fail when zero or multiple matching emulators are online and must never select Windows desktop.

- [ ] **Step 7: Run the complete foundation gate**

```powershell
. .\scripts\dev-env.ps1
.\scripts\preflight.ps1
dotnet test CommunityElderCare.sln
npm --prefix apps/admin-web test -- --run
npm --prefix apps/admin-web run build
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/app_shell_test.dart'
Push-Location apps/mobile
flutter analyze
flutter build apk --debug --dart-define=API_BASE_URL=http://10.0.2.2:5180
Pop-Location
```

Expected: every command exits `0`, the Web build exists, and `apps/mobile/build/app/outputs/flutter-apk/app-debug.apk` is non-empty. If the emulator ID differs, select the sole supported Android emulator from `flutter devices --machine`; never silently choose Windows desktop.

- [ ] **Step 8: Commit the foundation**

```powershell
git add .gitignore .config global.json Directory.Build.props Directory.Packages.props CommunityElderCare.sln src tests apps scripts README.md
git commit -m "chore: establish community care foundation"
```

---

### Task 2: Add deterministic synthetic elder data and SQLite persistence

**Files:**
- Create: `src/CommunityElderCare.Core/Elders/CareAttentionLevel.cs`
- Create: `src/CommunityElderCare.Core/Elders/ElderProfile.cs`
- Create: `src/CommunityElderCare.Core/Elders/HealthRisk.cs`
- Create: `src/CommunityElderCare.Core/Elders/ServiceNeed.cs`
- Create: `src/CommunityElderCare.Core/Elders/EmergencyContact.cs`
- Create: `src/CommunityElderCare.Core/Elders/IElderProfileQuery.cs`
- Create: `src/CommunityElderCare.Infrastructure/Elders/ElderProfileQuery.cs`
- Modify: `src/CommunityElderCare.Infrastructure/Persistence/CommunityCareDbContext.cs`
- Create: `src/CommunityElderCare.Infrastructure/Persistence/Configurations/ElderProfileConfiguration.cs`
- Create: `src/CommunityElderCare.Infrastructure/Persistence/DemoSeedBuilder.cs`
- Create: `src/CommunityElderCare.Infrastructure/Persistence/Migrations/**`
- Create: `src/CommunityElderCare.Api/Contracts/Elders/ElderSummaryResponse.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/ElderEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/Elders/DemoSeedBuilderTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/ElderEndpointTests.cs`

**Interfaces:**
- Produces: `CareAttentionLevel { Routine, Priority, High }`.
- Produces: `DemoSeedBuilder.Build(int count, int seed, DateTimeOffset baseTime)` returning exactly `count` synthetic profiles with schedules relative to `baseTime`.
- Produces: `GET /api/v1/elders?attentionLevel={level}`.
- Produces: `GET /api/v1/elders/{elderId}`.

- [ ] **Step 1: Write the failing deterministic-seed test**

```csharp
[Fact]
public void Build_creates_twenty_repeatable_synthetic_profiles()
{
    var baseTime = new DateTimeOffset(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);
    var first = DemoSeedBuilder.Build(20, 20260824, baseTime);
    var second = DemoSeedBuilder.Build(20, 20260824, baseTime);
    Assert.Equal(20, first.Elders.Count);
    Assert.Equal(first.Elders.Select(x => x.Id), second.Elders.Select(x => x.Id));
    Assert.All(first.Elders, x => Assert.True(x.IsDemoData));
    Assert.Contains(first.Elders, x => x.AttentionLevel == CareAttentionLevel.High);
}
```

Run `dotnet test tests/CommunityElderCare.UnitTests --filter FullyQualifiedName~Build_creates_twenty` and expect FAIL.

- [ ] **Step 2: Implement the elder aggregate and seed algorithm**

Use separate `HealthRisk` and `ServiceNeed` collections. `ElderProfile` must expose `Id`, `DemoDisplayName`, `BirthDate`, `AreaCode`, `AttentionLevel`, `IsDemoData`, `HealthRisks`, `ServiceNeeds`, and `EmergencyContacts`. Generate names from a fixed Chinese demo-name array, fake phones in the reserved visual pattern `1990000####`, three area codes, and deterministic risk/service combinations. Reject seed counts outside 15～25. Set the main elder's check-in due time before `baseTime` so the worker can deterministically create the opening event after reset; other schedules are fixed offsets from `baseTime`.

```csharp
public enum CareAttentionLevel { Routine, Priority, High }

public sealed class ElderProfile
{
    public Guid Id { get; private set; }
    public string DemoDisplayName { get; private set; } = string.Empty;
    public DateOnly BirthDate { get; private set; }
    public string AreaCode { get; private set; } = string.Empty;
    public CareAttentionLevel AttentionLevel { get; private set; }
    public bool IsDemoData { get; private set; } = true;
}
```

Run the seed test and expect PASS.

- [ ] **Step 3: Configure SQLite and create the first migration**

Map collection children with foreign keys, store enums as strings, require `IsDemoData = true` in the demo seed, and add indexes on `AreaCode`, `AttentionLevel`, and emergency-contact order. Run:

```powershell
dotnet ef migrations add InitialElderData --project src/CommunityElderCare.Infrastructure --startup-project src/CommunityElderCare.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/CommunityElderCare.Infrastructure --startup-project src/CommunityElderCare.Api
```

- [ ] **Step 4: Write the failing elder-list integration test**

```csharp
[Fact]
public async Task High_attention_filter_returns_demo_profiles_only()
{
    using var client = Factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");
    var elders = await client.GetFromJsonAsync<List<ElderSummaryResponse>>(
        "/api/v1/elders?attentionLevel=High");
    Assert.NotNull(elders);
    Assert.NotEmpty(elders);
    Assert.All(elders, x => Assert.True(x.IsDemoData));
    Assert.All(elders, x => Assert.Equal("High", x.AttentionLevel));
}
```

Run the filtered test and expect FAIL because the endpoint is missing.

- [ ] **Step 5: Implement read-only elder endpoints**

Return summary fields from the list and full health/service/contact fields only from the detail endpoint. Apply area filtering in the query service, not in the Vue client. Include `isDemoData` in every response. Keep this task read-only at the HTTP boundary; the authorized care-profile update endpoint is added only after Task 3 has a working access policy. Run unit and integration suites and expect PASS.

- [ ] **Step 6: Commit the synthetic data slice**

```powershell
git add src tests
git commit -m "feat: add synthetic elder records"
```

---

### Task 3: Add local identity, field-level consent and authorization

**Files:**
- Modify: `src/CommunityElderCare.Core/Identity/DemoRole.cs`
- Create: `src/CommunityElderCare.Core/Identity/UserAccount.cs`
- Create: `src/CommunityElderCare.Core/Identity/ConsentGrant.cs`
- Create: `src/CommunityElderCare.Core/Identity/ConsentField.cs`
- Modify: `src/CommunityElderCare.Core/Identity/ActorContext.cs`
- Create: `src/CommunityElderCare.Core/Identity/BreakGlassGrant.cs`
- Create: `src/CommunityElderCare.Core/Identity/IAccessPolicy.cs`
- Create: `src/CommunityElderCare.Infrastructure/Identity/AccessPolicy.cs`
- Create: `src/CommunityElderCare.Infrastructure/Identity/JwtTokenService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Auth/LoginRequest.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Auth/LoginResponse.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Elders/UpdateElderCareProfileRequest.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/AuthEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/ConsentEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/BreakGlassEndpoints.cs`
- Modify: `src/CommunityElderCare.Api/Endpoints/ElderEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/Identity/AccessPolicyTests.cs`
- Modify: `tests/CommunityElderCare.IntegrationTests/ElderEndpointTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/ConsentEndpointTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/BreakGlassEndpointTests.cs`

**Interfaces:**
- Produces: `POST /api/v1/auth/login` returning `accessToken`, `expiresAt`, `role`, `shell`, and `isDemoMode`.
- Produces: `GET /api/v1/elders/{elderId}/consents`.
- Produces: `PUT /api/v1/elders/{elderId}/consents/{granteeUserId}` with an explicit field list and expiry.
- Produces: `DELETE /api/v1/elders/{elderId}/consents/{granteeUserId}` with immediate read denial.
- Produces: `POST /api/v1/elders/{elderId}/break-glass` requiring a reason and granting at most 15 minutes of emergency access.
- Produces: `PUT /api/v1/elders/{elderId}/care-profile` for a correct-area community-staff actor with an explicit reason.
- Produces: `IAccessPolicy.CanReadAsync(ActorContext actor, Guid elderId, ConsentField field, CancellationToken ct)`.

- [ ] **Step 1: Write the failing consent-policy tests**

```csharp
[Fact]
public async Task Revoked_family_consent_denies_the_next_read()
{
    var grant = TestConsent.Active(ElderId, FamilyUserId, ConsentField.RecentStatus);
    Store.Add(grant);
    Assert.True(await Policy.CanReadAsync(FamilyActor, ElderId, ConsentField.RecentStatus, Ct));
    grant.Revoke(FixedNow, ElderActor.UserId);
    Assert.False(await Policy.CanReadAsync(FamilyActor, ElderId, ConsentField.RecentStatus, Ct));
}
```

Also test community area isolation, service-worker task isolation, elder self-access, administrator denial of raw AI text, and break-glass expiry. Run the unit tests and expect FAIL.

- [ ] **Step 2: Implement role and consent rules**

Use these field values exactly:

```csharp
public enum ConsentField
{
    RecentStatus,
    CareEventSummary,
    VisitSummary,
    ReminderCompletion,
    HealthRiskSummary,
    EmergencyContact
}
```

`AccessPolicy` must evaluate role, area/task scope, active grant, field, expiry and revocation on every request. It must not cache a positive family decision across requests. Return `CONSENT_REQUIRED` or `FORBIDDEN_SCOPE`, never a generic success with hidden fields.

`BreakGlassGrant` is available only to a community-staff actor handling an emergency event, requires a non-empty reason, expires within 15 minutes, and is always auditable. It never grants access to raw AI messages.

- [ ] **Step 3: Add seeded demo accounts and JWT login**

Seed one account per role linked to the main story. Hash the process-local `COMMUNITYCARE_DEMO_PASSWORD` with ASP.NET `PasswordHasher<UserAccount>`. Generate the JWT signing key in `scripts/start-demo.ps1`; integration tests inject a fixed test key. Do not store either value in source, database migrations, logs or the browser bundle.

```csharp
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    DemoRole Role,
    string Shell,
    bool IsDemoMode);
```

- [ ] **Step 4: Write and run the failing authorization integration test**

```csharp
[Fact]
public async Task Family_detail_omits_ungranted_health_fields()
{
    using var client = Factory.CreateAuthenticatedClient(DemoRole.Family, familyFor: MainElderId);
    var response = await client.GetAsync($"/api/v1/elders/{MainElderId}");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.False(json.TryGetProperty("healthRisks", out _));
    Assert.True(json.TryGetProperty("recentStatus", out _));
}
```

Expect FAIL before endpoint projection uses `IAccessPolicy`.

Add update tests before exposing the care-profile route: a family actor receives `403/FORBIDDEN_SCOPE`, a community-staff actor from another area receives `403/FORBIDDEN_SCOPE`, a missing reason receives `400/REASON_REQUIRED`, and a correct-area community-staff actor replaces attention level, health risks, service needs and ordered emergency contacts in one transaction. Assert the successful change creates an audit-ready record containing actor, elder, reason and time.

- [ ] **Step 5: Apply field projections and consent endpoints**

Build response DTOs from allowed fields rather than serializing entities and deleting properties. Require the elder actor for grant/revoke. Implement break-glass creation and expiry checks. Expose the care-profile update only through `IAccessPolicy`; preserve health risks and service needs as separate collections and reject the whole transaction if any child value is invalid. Record grant, revoke, care-profile update, break-glass reason, actor, time and field list in audit-ready records. Add the `AddIdentityAndConsent` EF migration, then run identity unit tests and integration tests from an empty SQLite file and expect PASS.

- [ ] **Step 6: Commit identity and consent**

```powershell
git add src tests
git commit -m "feat: enforce demo role and consent access"
```

---

### Task 4: Implement reminders, safe check-in and idempotent submission

**Files:**
- Create: `src/CommunityElderCare.Core/CheckIns/Reminder.cs`
- Create: `src/CommunityElderCare.Core/CheckIns/CheckIn.cs`
- Create: `src/CommunityElderCare.Core/CheckIns/IdempotencyRecord.cs`
- Create: `src/CommunityElderCare.Core/CheckIns/ICheckInService.cs`
- Create: `src/CommunityElderCare.Infrastructure/CheckIns/CheckInService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/CheckIns/RecordCheckInRequest.cs`
- Create: `src/CommunityElderCare.Api/Contracts/CheckIns/TodayResponse.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/CheckInEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/CheckIns/CheckInServiceTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/CheckInEndpointTests.cs`

**Interfaces:**
- Produces: `ReminderType { Medication, FollowUpAppointment, CommunityActivity, VisitSchedule }`.
- Produces: `ICheckInService.RecordAsync(Guid elderId, Guid requestId, DateTimeOffset clientTime, ActorContext actor, CancellationToken ct)`.
- Produces: `POST /api/v1/elders/{elderId}/check-ins`.
- Produces: `GET /api/v1/elders/{elderId}/today`.
- Produces: `POST /api/v1/reminders/{reminderId}/complete` with `requestId`.
- Produces: `POST /api/v1/reminders/{reminderId}/snooze` with `requestId` and a bounded next-reminder time.
- Produces: `GetOverdueCheckInsAsync(DateTimeOffset now, CancellationToken ct)` for Task 5.

- [ ] **Step 1: Write the failing idempotency test**

```csharp
[Fact]
public async Task Same_request_id_returns_the_original_check_in()
{
    var first = await Service.RecordAsync(ElderId, RequestId, ClientTime, ElderActor, Ct);
    var second = await Service.RecordAsync(ElderId, RequestId, ClientTime, ElderActor, Ct);
    Assert.True(first.IsSuccess);
    Assert.Equal(first.Value!.Id, second.Value!.Id);
    Assert.Single(await Db.CheckIns.ToListAsync(Ct));
}
```

Run the test and expect FAIL.

- [ ] **Step 2: Implement check-in and reminder aggregates**

Add a unique database index on `(ElderId, RequestId, Kind)`. A duplicate request returns the original result with `200`, not `409`. Seed medication, follow-up appointment, community activity and visit-schedule reminders using synthetic labels and fixed times; a medication reminder stores no dose recommendation or treatment advice. A reminder completion stores completion time and actor; “稍后提醒” stores a bounded next due time and cannot silently mark completion.

- [ ] **Step 3: Write the failing endpoint tests**

Cover elder self-check-in, family forbidden submission, community staff manual confirmation with a reason, and a network retry using the same request ID. Verify `TodayResponse` includes reminder states and persisted receive times. The mobile client derives `已送达` or `尚未送达` by reconciling the response with its local outbox; the server does not claim delivery for a request it never received.

- [ ] **Step 4: Implement and authorize the endpoints**

```csharp
public sealed record RecordCheckInRequest(Guid RequestId, DateTimeOffset ClientTime);
public sealed record CheckInResponse(Guid Id, Guid RequestId, DateTimeOffset ReceivedAt, bool IsDuplicate);
```

Use server `ReceivedAt` for ordering and keep `ClientTime` only as evidence. Apply elder-self or community-manual-confirmation policies. Run unit and integration tests and expect PASS.

Add the `AddCheckInsAndReminders` EF migration before the integration run.

- [ ] **Step 5: Commit check-in and reminders**

```powershell
git add src tests
git commit -m "feat: add safe check-in and reminders"
```

---

### Task 5: Build the care-event rules, state machine and correlation engine

**Files:**
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventEnums.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEvent.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventEvidence.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventTransition.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/ContactAttempt.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/EscalationPolicy.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventStateMachine.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventClassifier.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/CareEventCorrelator.cs`
- Create: `src/CommunityElderCare.Core/CareEvents/ICareEventService.cs`
- Create: `src/CommunityElderCare.Infrastructure/CareEvents/CareEventService.cs`
- Create: `src/CommunityElderCare.Infrastructure/Background/MissedCheckInWorker.cs`
- Create: `src/CommunityElderCare.Infrastructure/Background/ContactEscalationWorker.cs`
- Create: `src/CommunityElderCare.Api/Contracts/CareEvents/CareEventResponses.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/CareEventEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/CareEvents/CareEventStateMachineTests.cs`
- Create: `tests/CommunityElderCare.UnitTests/CareEvents/CareEventCorrelationTests.cs`
- Create: `tests/CommunityElderCare.UnitTests/CareEvents/EscalationPolicyTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/CareEventEndpointTests.cs`

**Interfaces:**
- Produces: exact enums below.
- Produces: `ICareEventService.CreateAsync`, `AcceptAsync`, `TransitionAsync`, `EscalateAsync`, and `AddEvidenceAsync`.
- Produces: `POST /api/v1/care-events` for elder help, family report and staff-created events.
- Produces: `GET /api/v1/care-events` and `GET /api/v1/care-events/{eventId}`.
- Produces: `POST /api/v1/care-events/{eventId}/accept` and `/transitions`.
- Produces: care-event responses containing persisted evidence, transition history and `allowedTransitions`.

```csharp
public enum CareEventCategory { SafetyHealth, GeneralService }
public enum CareEventLevel { GeneralService, NeedsConfirmation, Emergency }
public enum CareEventStatus
{
    PendingConfirmation, Accepted, InProgress, Resolved,
    FollowUpPending, Closed, FalseAlarm, UnableToConfirm
}
public enum CareEventSource { CheckIn, ElderHelp, FamilyReport, StaffVisit, Device, AiCue }
```

- [ ] **Step 1: Write the failing state-transition matrix tests**

```csharp
[Theory]
[InlineData(CareEventStatus.PendingConfirmation, CareEventStatus.Accepted, true)]
[InlineData(CareEventStatus.Accepted, CareEventStatus.InProgress, true)]
[InlineData(CareEventStatus.InProgress, CareEventStatus.Resolved, true)]
[InlineData(CareEventStatus.Resolved, CareEventStatus.FollowUpPending, true)]
[InlineData(CareEventStatus.FollowUpPending, CareEventStatus.Closed, true)]
[InlineData(CareEventStatus.UnableToConfirm, CareEventStatus.Closed, false)]
[InlineData(CareEventStatus.PendingConfirmation, CareEventStatus.Closed, false)]
public void Transition_matrix_is_enforced(CareEventStatus from, CareEventStatus to, bool allowed)
{
    Assert.Equal(allowed, CareEventStateMachine.CanTransition(from, to));
}
```

Add tests that `FalseAlarm` requires a reason, `UnableToConfirm` is not terminal, and AI/device/background actors cannot close. Run and expect FAIL.

- [ ] **Step 2: Implement the state machine and close guards**

Allowed transitions are:

```text
PendingConfirmation -> Accepted | FalseAlarm | UnableToConfirm
Accepted            -> InProgress | FalseAlarm | UnableToConfirm
UnableToConfirm     -> Accepted
InProgress          -> Resolved | UnableToConfirm
Resolved            -> FollowUpPending | Closed
FollowUpPending     -> Closed
```

Closing requires one current staff owner, a non-empty resolution, no incomplete mandatory task, and a completed follow-up when required. Every accepted event has exactly one current owner; pending events have one responsibility queue.

- [ ] **Step 3: Write the failing classification and correlation tests**

```csharp
[Fact]
public void Device_signal_joins_recent_open_safety_event_only()
{
    var existing = TestEvent.SafetyHealth(lastActivity: Now.AddMinutes(-10));
    var match = Correlator.FindMatch(existing.Yield(), ElderId, Now);
    Assert.Equal(existing.Id, match);
    Assert.Null(Correlator.FindMatch(TestEvent.GeneralService().Yield(), ElderId, Now));
}
```

Test that the window is exactly 30 minutes, general-service events never absorb safety signals, and duplicate source-event IDs return the original event.

- [ ] **Step 4: Implement classifier, correlator and missed-check worker**

Explicit SOS and a structured `DangerCue` trigger produce `Emergency`; missed check-in and device anomaly produce `NeedsConfirmation`; life-service needs produce `GeneralService`. Arbitrary-text scanning is added in Task 12 and feeds the structured trigger into this classifier. `MissedCheckInWorker` uses `TimeProvider`, queries Task 4 overdue items, and calls `ICareEventService.CreateAsync` with a deterministic source ID so worker reruns are idempotent.

`EscalationPolicy` uses configurable demo intervals: create the elder reminder immediately, create a simulated phone-confirmation attempt after 2 minutes, add the next emergency-contact attempt after 5 minutes, and move an unconfirmed item to `UnableToConfirm` plus reassignment after 10 minutes. Emergency events create community and emergency-contact simulation attempts immediately. `ContactEscalationWorker` uses deterministic attempt IDs so reruns never duplicate an attempt.

- [ ] **Step 5: Add event API integration tests before endpoints**

Test acceptance ownership, illegal transition returning `409` with `INVALID_TRANSITION`, duplicate create returning the original event, emergency level independent of process status, and all external actions represented as simulation records. For a family report, assert the server always creates `source=FamilyReport`, `level=NeedsConfirmation`, `status=PendingConfirmation`; the request contract exposes no level/status/owner fields, and a retry with the same client request ID returns the original event. Expect FAIL, then implement the endpoint group and expect PASS.

Add the `AddCareEvents` EF migration and run the integration factory from an empty SQLite file to prove the migration creates events, transition history, evidence and contact attempts.

- [ ] **Step 6: Run the care-event regression gate**

```powershell
dotnet test tests/CommunityElderCare.UnitTests --filter FullyQualifiedName~CareEvent
dotnet test tests/CommunityElderCare.IntegrationTests --filter FullyQualifiedName~CareEvent
dotnet test CommunityElderCare.sln
```

Expected: PASS with no SQLite unique-constraint leak and no duplicate event rows.

- [ ] **Step 7: Commit the event engine**

```powershell
git add src tests
git commit -m "feat: add care event workflow"
```

---

### Task 6: Add visits, service work orders and follow-up closure

**Files:**
- Create: `src/CommunityElderCare.Core/CareWork/VisitTask.cs`
- Create: `src/CommunityElderCare.Core/CareWork/ServiceOrder.cs`
- Create: `src/CommunityElderCare.Core/CareWork/FollowUp.cs`
- Create: `src/CommunityElderCare.Core/CareWork/WorkStatus.cs`
- Create: `src/CommunityElderCare.Core/CareWork/IVisitService.cs`
- Create: `src/CommunityElderCare.Core/CareWork/IServiceOrderService.cs`
- Create: `src/CommunityElderCare.Infrastructure/CareWork/VisitService.cs`
- Create: `src/CommunityElderCare.Infrastructure/CareWork/ServiceOrderService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/CareWork/CareWorkContracts.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/VisitEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/ServiceOrderEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/CareWork/CareWorkTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/CareWorkEndpointTests.cs`

**Interfaces:**
- Produces: `WorkStatus { Unassigned, Assigned, InProgress, Completed, Cancelled }`.
- Produces: `POST /api/v1/care-events/{eventId}/visits`.
- Produces: `POST /api/v1/care-events/{eventId}/service-orders`.
- Produces: `POST /api/v1/visits/{visitId}/start` and `/complete`.
- Produces: `POST /api/v1/service-orders/{orderId}/accept` and `/complete`.
- Produces: `POST /api/v1/care-events/{eventId}/follow-ups` and `/follow-ups/{followUpId}/complete`.

- [ ] **Step 1: Write failing work-assignment unit tests**

```csharp
[Fact]
public void Service_worker_can_only_complete_the_assigned_order()
{
    var order = TestOrder.AssignedTo(ServiceWorkerId);
    var denied = order.Complete(OtherWorkerActor, "已完成送餐", Now);
    var allowed = order.Complete(ServiceWorkerActor, "已完成送餐", Now);
    Assert.Equal("FORBIDDEN_SCOPE", denied.ErrorCode);
    Assert.True(allowed.IsSuccess);
}
```

Also test visit start/complete order, cancellation reason, follow-up due time and prevention of closing an event with unfinished mandatory work. Run and expect FAIL.

- [ ] **Step 2: Implement work aggregates and minimal task views**

`VisitTask` stores event, assigned staff, scheduled/start/end times, raw staff note, confirmed summary and result. `ServiceOrder` stores service type, minimum contact fields, assignee and result. Service-worker DTOs must omit health risks, family fields, other orders and community notes.

```csharp
public sealed record ServiceWorkerOrderResponse(
    Guid OrderId,
    string ElderDisplayName,
    string ServiceType,
    string ScheduledWindow,
    string ContactInstruction,
    WorkStatus Status);
```

- [ ] **Step 3: Write the failing full-closure integration test**

Create an event, accept it, start a visit, complete the visit, resolve the event, schedule and complete follow-up, then close. Assert every state and actor. Add a negative test where closure returns `409 CLOSE_GUARD_FAILED` while follow-up is incomplete.

- [ ] **Step 4: Implement endpoints and event linkage**

All mutations call domain services and write a transaction containing the work change, event evidence and audit-ready transition. Completing a visit must not automatically close the event. Creating a mandatory follow-up transitions `Resolved → FollowUpPending`; completing it allows a community staff actor to request `Closed`. Add the `AddCareWork` EF migration before integration tests.

- [ ] **Step 5: Run care-work and event regression tests**

```powershell
dotnet test tests/CommunityElderCare.UnitTests --filter "FullyQualifiedName~CareWork|FullyQualifiedName~CareEvent"
dotnet test tests/CommunityElderCare.IntegrationTests --filter "FullyQualifiedName~CareWork|FullyQualifiedName~CareEvent"
```

Expected: PASS, with service-worker response snapshots containing no sensitive fields.

- [ ] **Step 6: Commit visits and work orders**

```powershell
git add src tests
git commit -m "feat: add visits and service work orders"
```

---

### Task 7: Build the community Web shell and elder-record workspace

**Files:**
- Create: `apps/admin-web/src/api/apiClient.ts`
- Create: `apps/admin-web/src/api/contracts.ts`
- Create: `apps/admin-web/src/stores/auth.ts`
- Create: `apps/admin-web/src/router/index.ts`
- Create: `apps/admin-web/src/layouts/CommunityLayout.vue`
- Create: `apps/admin-web/src/components/DemoDataBadge.vue`
- Create: `apps/admin-web/src/components/StatusNotice.vue`
- Create: `apps/admin-web/src/pages/LoginPage.vue`
- Create: `apps/admin-web/src/pages/DashboardPage.vue`
- Create: `apps/admin-web/src/pages/ElderListPage.vue`
- Create: `apps/admin-web/src/pages/ElderDetailPage.vue`
- Create: `apps/admin-web/src/pages/ElderEditPage.vue`
- Create: `apps/admin-web/src/pages/NotAuthorizedPage.vue`
- Create: `apps/admin-web/src/styles/tokens.css`
- Create: `apps/admin-web/src/styles/base.css`
- Create: `apps/admin-web/src/pages/__tests__/CommunityLayout.spec.ts`
- Create: `apps/admin-web/src/pages/__tests__/ElderPages.spec.ts`

**Interfaces:**
- Consumes: Task 2 elder endpoints and Task 3 login/field authorization.
- Produces: `apiClient.request<T>(path, options): Promise<T>`.
- Produces: routes `/login`, `/dashboard`, `/elders`, `/elders/:elderId`, `/elders/:elderId/edit`.
- Produces: Pinia auth state `{ token, role, shell, isDemoMode }`.

- [ ] **Step 1: Write the failing navigation and dashboard tests**

```ts
it('keeps dashboard concise and exposes elder records as a separate route', async () => {
  renderWithRouter(CommunityLayout, '/dashboard', { role: 'CommunityStaff' })
  expect(screen.getByRole('link', { name: '老人档案' })).toBeTruthy()
  expect(screen.getByRole('heading', { name: '待处理事项' })).toBeTruthy()
  expect(screen.queryByText('全部老人档案')).toBeNull()
})
```

Test that service workers cannot see elder-record navigation and every authenticated page shows `演示数据`. Run `npm test -- --run` and expect FAIL.

- [ ] **Step 2: Implement the visual foundation without a generic dashboard kit**

Use a fixed sidebar, content header and main region. `tokens.css` defines neutral surfaces, one blue action color, emergency red only for emergency states, 16px body text, clear focus rings and 44px minimum controls. Do not use gradients, glass cards, decorative KPI grids or icon-only actions.

- [ ] **Step 3: Implement typed API and login state**

Install the test-only Web dependencies:

```powershell
npm --prefix apps/admin-web install --save-dev @testing-library/vue @testing-library/user-event msw
```

```ts
export type DemoRole = 'Elder' | 'Family' | 'CommunityStaff' | 'ServiceWorker' | 'Administrator'

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  role: DemoRole
  shell: 'elder' | 'family' | 'community' | 'service' | 'admin'
  isDemoMode: true
}
```

Keep the token in memory plus session storage, clear it on `401`, map `ProblemDetails.extensions.code` to natural Chinese, and never store AI text or elder records in browser persistence.

- [ ] **Step 4: Write failing elder-list and detail tests with MSW**

Test attention filters, area-safe results, loading, empty and error states. In detail tests, verify `healthRisks` is rendered for community staff, absent fields are not replaced with “暂无” for family/service roles, and the demo badge remains visible.

- [ ] **Step 5: Implement elder list/detail and consent display**

The list columns are name, age, attention level, latest status, next visit and current open event. The detail uses separate sections for basic information, health risks, service needs, contacts, authorizations and recent care timeline. Put editing on the separate `/edit` route; require a change reason and keep health risks and service needs in visibly separate form sections. Do not place editing forms in the list page.

- [ ] **Step 6: Verify Web accessibility and build**

```powershell
npm --prefix apps/admin-web test -- --run
npm --prefix apps/admin-web run lint
npm --prefix apps/admin-web run build
```

Expected: PASS with no missing accessible names and a non-empty `dist` directory.

- [ ] **Step 7: Commit the community Web foundation**

```powershell
git add apps/admin-web
git commit -m "feat: add community elder workspace"
```

---

### Task 8: Add Web event operations, visits, service work and timeline

**Files:**
- Create: `apps/admin-web/src/pages/CareEventListPage.vue`
- Create: `apps/admin-web/src/pages/CareEventDetailPage.vue`
- Create: `apps/admin-web/src/pages/VisitListPage.vue`
- Create: `apps/admin-web/src/pages/ServiceOrderListPage.vue`
- Create: `apps/admin-web/src/pages/ServiceWorkerTasksPage.vue`
- Create: `apps/admin-web/src/components/EventLevelBadge.vue`
- Create: `apps/admin-web/src/components/EventTimeline.vue`
- Create: `apps/admin-web/src/components/TransitionDialog.vue`
- Create: `apps/admin-web/src/components/BreakGlassDialog.vue`
- Create: `apps/admin-web/src/components/SimulationActionPanel.vue`
- Create: `apps/admin-web/src/pages/__tests__/CareEventPages.spec.ts`
- Create: `apps/admin-web/src/pages/__tests__/CareWorkPages.spec.ts`
- Modify: `apps/admin-web/src/router/index.ts`
- Modify: `apps/admin-web/src/layouts/CommunityLayout.vue`

**Interfaces:**
- Consumes: Task 5 care-event endpoints and Task 6 care-work endpoints.
- Produces: routes `/care-events`, `/care-events/:eventId`, `/visits`, `/service-orders`, `/my-tasks`.
- Produces: `submitTransition(eventId, targetStatus, reason)` and `completeVisit(visitId, note)` Web actions.

- [ ] **Step 1: Write the failing event-list behavior tests**

Verify sorting by emergency first then waiting time, columns for level/status/elder/owner/next action, and a visible `模拟` label for external actions. Verify level and status are rendered separately. Run and expect FAIL.

- [ ] **Step 2: Implement event list and detail timeline**

Use server-provided timestamps and statuses. The detail page shows evidence, contact attempts, assignments, transitions, visits, service orders, follow-ups and simulation records in chronological order. It must never infer “已联系” from a button click; it displays the persisted attempt result.

- [ ] **Step 3: Write failing transition-dialog tests**

```ts
it('requires a reason before marking an event as a false alarm', async () => {
  render(TransitionDialog, { props: { target: 'FalseAlarm' } })
  await user.click(screen.getByRole('button', { name: '确认提交' }))
  expect(screen.getByText('请填写判断依据')).toBeTruthy()
  expect(mockSubmit).not.toHaveBeenCalled()
})
```

Also test `UnableToConfirm` shows “将进入联系升级，不能直接关单”. Test that break-glass cannot submit without a reason, shows a 15-minute expiry, and is absent for non-emergency events.

- [ ] **Step 4: Implement event and care-work actions**

Disable illegal transitions from the server-provided `allowedTransitions`; still handle server `409 INVALID_TRANSITION`. Visit completion separates raw note from confirmed result. Service-worker route renders only the Task 6 minimal DTO and no global sidebar items. Emergency break-glass is available only on an emergency event, requires a typed reason, displays the 15-minute expiry and never exposes raw AI content.

- [ ] **Step 5: Add the main-story Web component test**

With MSW, simulate missed check-in, device evidence merge, staff acceptance, visit completion, emergency escalation, simulated family/120 actions, resolution, follow-up and closure. Assert the timeline contains each persisted step once.

- [ ] **Step 6: Run Web and backend regression gates**

```powershell
npm --prefix apps/admin-web test -- --run
npm --prefix apps/admin-web run build
dotnet test CommunityElderCare.sln
```

Expected: PASS.

- [ ] **Step 7: Commit Web operations**

```powershell
git add apps/admin-web
git commit -m "feat: add community event operations"
```

---

### Task 9: Build Flutter role shells, authenticated API access and offline outbox

**Files:**
- Modify: `apps/mobile/pubspec.yaml`
- Create: `apps/mobile/lib/app/community_care_app.dart`
- Create: `apps/mobile/lib/app/app_router.dart`
- Create: `apps/mobile/lib/core/api/api_client.dart`
- Create: `apps/mobile/lib/core/api/api_problem.dart`
- Create: `apps/mobile/lib/core/api/contracts.dart`
- Create: `apps/mobile/lib/core/storage/secure_session_store.dart`
- Create: `apps/mobile/lib/core/outbox/outbox_entry.dart`
- Create: `apps/mobile/lib/core/outbox/outbox_repository.dart`
- Create: `apps/mobile/lib/core/outbox/outbox_sync_service.dart`
- Create: `apps/mobile/lib/auth/login_page.dart`
- Create: `apps/mobile/lib/auth/session_controller.dart`
- Create: `apps/mobile/lib/elder/elder_shell.dart`
- Create: `apps/mobile/lib/family/family_shell.dart`
- Create: `apps/mobile/integration_test/role_shell_test.dart`
- Create: `apps/mobile/integration_test/offline_outbox_test.dart`

**Interfaces:**
- Consumes: Task 3 login endpoint and RFC 7807 error contract.
- Produces: `ApiClient.get/post/put/delete<T>()` with bearer token and stable error mapping.
- Produces: `OutboxRepository.enqueue(OutboxEntry)`, `pending()`, `markSent(id)`, and `markFailed(id, message)`.
- Produces: routes under `/elder/*` and `/family/*`; role is selected only from authenticated session.
- Produces: `ElderChatGateway.send(String text)` initially backed by the fixed unavailable response, replaced by Task 12 HTTP integration.

- [ ] **Step 1: Add focused mobile dependencies**

Add `flutter_riverpod`, `go_router`, `http`, `flutter_secure_storage`, `sqflite`, `path`, `uuid`, `flutter_tts`, and `integration_test` from the SDK. Run `flutter pub get` and commit `pubspec.lock`. Do not add analytics, Firebase, background location, camera or accessibility-service packages.

- [ ] **Step 2: Write the failing role-separation integration test**

```dart
testWidgets('family session cannot navigate to elder routes', (tester) async {
  await launchWithSession(tester, role: DemoRole.family);
  expect(find.text('家属首页'), findsOneWidget);
  expect(find.text('我今天平安'), findsNothing);
  await expectRouteDenied(tester, '/elder/home');
});
```

Run on the Android emulator and expect FAIL.

- [ ] **Step 3: Implement session, router guards and separate shells**

```dart
enum DemoRole { elder, family, communityStaff, serviceWorker, administrator }

enum ConsentField {
  recentStatus,
  careEventSummary,
  visitSummary,
  reminderCompletion,
  healthRiskSummary,
  emergencyContact,
}

class SessionState {
  const SessionState({required this.token, required this.role, required this.isDemoMode});
  final String token;
  final DemoRole role;
  final bool isDemoMode;
}
```

Only elder and family roles may enter the App. Route guards redirect every other role to a clear “请使用社区管理端” page. Normal UI has no role dropdown; a demo-account switch is placed in a guarded settings screen and always shows `演示模式`.

- [ ] **Step 4: Write the failing offline-outbox integration test**

Queue an emergency request while the fake API is offline. Assert the page shows `尚未送达`, the SQLite outbox contains one high-priority entry with a UUID request ID, and reconnecting sends it once and changes the display to `已送达`.

- [ ] **Step 5: Implement SQLite outbox and sync**

Use table columns `id`, `request_id`, `kind`, `payload_json`, `priority`, `created_at`, `attempt_count`, `last_error`, and `state`. Flush emergency entries before normal ones. Never delete a failed entry; mark it sent only after a successful idempotent API response.

- [ ] **Step 6: Run mobile foundation gates**

```powershell
. .\scripts\dev-env.ps1
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/role_shell_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/offline_outbox_test.dart'
Push-Location apps/mobile
flutter analyze
flutter build apk --debug --dart-define=API_BASE_URL=http://10.0.2.2:5180
Pop-Location
```

Expected: PASS and a non-empty APK.

- [ ] **Step 7: Commit the role shells and outbox**

```powershell
git add apps/mobile
git commit -m "feat: add mobile role shells and offline outbox"
```

---

### Task 10: Implement the elder Android experience

**Files:**
- Create: `apps/mobile/lib/elder/home/elder_home_page.dart`
- Create: `apps/mobile/lib/elder/home/elder_today_controller.dart`
- Create: `apps/mobile/lib/elder/help/help_category_page.dart`
- Create: `apps/mobile/lib/elder/help/help_request_controller.dart`
- Create: `apps/mobile/lib/elder/reminders/reminder_page.dart`
- Create: `apps/mobile/lib/elder/chat/elder_chat_page.dart`
- Create: `apps/mobile/lib/elder/chat/elder_chat_controller.dart`
- Create: `apps/mobile/lib/elder/settings/elder_settings_page.dart`
- Create: `apps/mobile/lib/core/widgets/large_action_button.dart`
- Create: `apps/mobile/lib/core/widgets/delivery_status_banner.dart`
- Create: `apps/mobile/integration_test/elder_check_in_test.dart`
- Create: `apps/mobile/integration_test/elder_help_test.dart`
- Create: `apps/mobile/integration_test/elder_accessibility_test.dart`

**Interfaces:**
- Consumes: Task 4 today/check-in/reminder APIs and Task 9 outbox.
- Produces: elder routes `/elder/home`, `/elder/reminders`, `/elder/chat`, `/elder/settings`, `/elder/help`.
- Produces: help categories `Emergency`, `Unwell`, `LifeService`, `WantToTalk`.

- [ ] **Step 1: Write the failing check-in flow test**

```dart
testWidgets('elder can confirm safety with one primary action', (tester) async {
  await launchElderHome(tester);
  expect(find.text('我今天平安'), findsOneWidget);
  await tester.tap(find.text('我今天平安'));
  await tester.pumpAndSettle();
  expect(find.text('签到已送达'), findsOneWidget);
  expect(find.text('今天已签到'), findsOneWidget);
});
```

Run on Android emulator and expect FAIL.

- [ ] **Step 2: Implement home and reminders**

Home displays date, seeded/cached weather, today reminders, one safety button and one help button. Use at least 18sp body text, 24sp primary action text, 56dp minimum main controls, high contrast and visible focus/semantics. Reminder actions are exactly `已完成` and `稍后提醒`.

- [ ] **Step 3: Write the failing help and offline emergency tests**

Test that tapping `我需要帮助` opens the four explicit categories, `紧急情况` requires confirmation, an offline request immediately shows guidance plus `尚未送达`, and reconnect sends the same request ID once. Ensure no real dial intent is launched.

- [ ] **Step 4: Implement help categories and local safety copy**

For emergency and explicit danger cues show:

```text
如果能够操作，请立即呼叫身边的人。
系统正在把演示求助发送给社区；当前不会真实拨打 120。
```

Life-service and want-to-talk requests create normal drafts. The App must not display “已经联系” before a persisted simulation record is returned.

- [ ] **Step 5: Implement chat fallback and settings**

Before Task 12 connects the cloud endpoint, `ElderChatGateway` returns fixed FAQ and the explicit banner `AI 当前不可用，核心求助功能仍可使用`. Settings exposes contacts, consent summary, AI memory list, font size and text-to-speech toggle. `flutter_tts` reads reminder and chat text only after an explicit tap and stops on page exit. No raw audio is captured or persisted.

- [ ] **Step 6: Run elder flow and semantics tests**

```powershell
. .\scripts\dev-env.ps1
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/elder_check_in_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/elder_help_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/elder_accessibility_test.dart'
Push-Location apps/mobile
flutter analyze
Pop-Location
```

Expected: PASS; accessibility test finds semantic labels for every primary action and no control smaller than 44dp.

- [ ] **Step 7: Commit the elder experience**

```powershell
git add apps/mobile
git commit -m "feat: add elder check-in and help flows"
```

---

### Task 11: Implement the authorized family Android experience

**Files:**
- Create: `apps/mobile/lib/family/home/family_home_page.dart`
- Create: `apps/mobile/lib/family/home/family_status_controller.dart`
- Create: `apps/mobile/lib/family/events/family_event_list_page.dart`
- Create: `apps/mobile/lib/family/events/family_event_detail_page.dart`
- Create: `apps/mobile/lib/family/events/family_report_controller.dart`
- Create: `apps/mobile/lib/family/records/family_care_records_page.dart`
- Create: `apps/mobile/lib/family/settings/family_settings_page.dart`
- Create: `apps/mobile/lib/family/widgets/consent_scope_card.dart`
- Create: `apps/mobile/integration_test/family_authorization_test.dart`
- Create: `apps/mobile/integration_test/family_report_test.dart`
- Create: `apps/mobile/integration_test/family_revocation_test.dart`
- Modify: `apps/mobile/lib/app/app_router.dart`

**Interfaces:**
- Consumes: Task 3 field-filtered elder responses, Task 5 event summaries and Task 6 visit/service summaries.
- Produces: family routes `/family/home`, `/family/events`, `/family/events/:eventId`, `/family/records`, `/family/settings`.
- Produces: no family mutation of elder risk, event status, visits, work orders or AI memory.
- Produces: a single `报告联系不上老人` action that creates a `FamilyReport`/`NeedsConfirmation` event but cannot choose the final level or status.

- [ ] **Step 1: Write the failing family authorization test**

```dart
testWidgets('family sees authorized summaries but no raw AI or internal notes', (tester) async {
  await launchFamilyHome(tester, grantedFields: {
    ConsentField.recentStatus,
    ConsentField.careEventSummary,
    ConsentField.visitSummary,
  });
  expect(find.text('最近状态'), findsOneWidget);
  expect(find.text('照料进展'), findsOneWidget);
  expect(find.text('AI 原始对话'), findsNothing);
  expect(find.text('社区内部备注'), findsNothing);
});
```

Run on the Android emulator and expect FAIL.

In `family_report_test.dart`, submit `报告联系不上老人`, retry once with the same client request ID, and assert both responses reference one event whose source is `FamilyReport`, level is `NeedsConfirmation` and status is `PendingConfirmation`. Assert the UI offers no level, assignment, transition, escalation or close control.

- [ ] **Step 2: Implement family home and event summaries**

Family home shows latest check-in, reminder-completion summary, last community confirmation and active-event summary only when granted. Event detail uses natural summaries such as `社区正在电话确认` and `已安排次日回访`; it does not expose exact address, raw staff note, raw AI text or internal responsibility-queue data.

The events page may submit `报告联系不上老人` with a client request ID and optional short note. A duplicate retry returns the original event. The family actor cannot assign, transition, escalate or close it.

- [ ] **Step 3: Write the failing revocation test**

Open a granted summary, revoke consent through the test fixture, refresh the page, and assert the content disappears and the message becomes `老人已撤回此项授权`. Also assert cached HTTP data is not shown after logout or role switch.

- [ ] **Step 4: Implement revocation-safe controllers**

Controllers must refetch on resume and pull-to-refresh, treat `CONSENT_REQUIRED` as a state change rather than a generic network error, clear protected state immediately, and never persist elder summaries in the mobile SQLite outbox or secure session store.

- [ ] **Step 5: Implement care records and settings**

Care records group authorized visit, service and follow-up summaries by date. Settings shows the exact granted field list, expiry and notification preferences. It cannot grant itself additional scope.

- [ ] **Step 6: Run family and cross-role tests**

```powershell
. .\scripts\dev-env.ps1
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/family_authorization_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/family_report_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/family_revocation_test.dart'
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/role_shell_test.dart'
Push-Location apps/mobile
flutter analyze
Pop-Location
```

Expected: PASS with no protected data after revocation.

- [ ] **Step 7: Commit the family experience**

```powershell
git add apps/mobile
git commit -m "feat: add authorized family experience"
```

---

### Task 12: Add deterministic AI safety, cloud chat and confirmed drafts

**Files:**
- Create: `src/CommunityElderCare.Core/Ai/DangerCueScanner.cs`
- Create: `src/CommunityElderCare.Core/Ai/AiDraft.cs`
- Create: `src/CommunityElderCare.Core/Ai/MemoryCandidate.cs`
- Create: `src/CommunityElderCare.Core/Ai/IAiCareService.cs`
- Create: `src/CommunityElderCare.Infrastructure/Ai/CloudLlmOptions.cs`
- Create: `src/CommunityElderCare.Infrastructure/Ai/ICloudLlmClient.cs`
- Create: `src/CommunityElderCare.Infrastructure/Ai/OpenAiCompatibleLlmClient.cs`
- Create: `src/CommunityElderCare.Infrastructure/Ai/FixedContentFallback.cs`
- Create: `src/CommunityElderCare.Infrastructure/Ai/AiCareService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Ai/AiContracts.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/AiEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/Ai/DangerCueScannerTests.cs`
- Create: `tests/CommunityElderCare.UnitTests/Ai/AiCareServiceTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/AiEndpointTests.cs`
- Create: `apps/mobile/lib/ai/ai_api_gateway.dart`
- Create: `apps/mobile/lib/ai/ai_draft_confirmation_card.dart`
- Create: `apps/mobile/lib/ai/ai_memory_controller.dart`
- Create: `apps/mobile/lib/ai/local_danger_cue_scanner.dart`
- Modify: `apps/mobile/lib/elder/chat/elder_chat_controller.dart`
- Modify: `apps/mobile/lib/elder/settings/elder_settings_page.dart`
- Create: `apps/mobile/integration_test/ai_chat_safety_test.dart`
- Create: `apps/admin-web/src/components/AiVisitSummaryDraft.vue`
- Create: `apps/admin-web/src/components/__tests__/AiVisitSummaryDraft.spec.ts`

**Interfaces:**
- Produces: `DangerCueScanner.Scan(string input): DangerCueResult`.
- Produces: `IAiCareService.ChatAsync`, `DraftServiceRequestAsync`, `SummarizeVisitAsync`, `ConfirmDraftAsync`, `ConfirmMemoryAsync`, and `DeleteMemoryAsync`.
- Produces: `POST /api/v1/ai/elder-chat`, `/service-request-drafts`, `/visit-summary-drafts`.
- Produces: `POST /api/v1/ai/drafts/{draftId}/confirm` and `/memory-candidates/{id}/confirm`.
- Produces: `DELETE /api/v1/ai/memories/{memoryId}`.

- [ ] **Step 1: Write the failing danger-cue tests**

```csharp
[Theory]
[InlineData("我摔倒了，起不来", "FALL_CANNOT_STAND")]
[InlineData("胸口很痛", "CHEST_PAIN")]
[InlineData("我喘不上气", "BREATHING_DIFFICULTY")]
[InlineData("我不想活了", "SELF_HARM")]
public void Explicit_danger_cues_bypass_the_model(string input, string code)
{
    var result = DangerCueScanner.Scan(input);
    Assert.True(result.IsEmergency);
    Assert.Equal(code, result.Code);
}
```

Add neutral cases such as `昨天差点摔倒，想看看防滑垫` that return `NeedsConfirmation`, not `Emergency`. Run and expect FAIL.

- [ ] **Step 2: Implement deterministic input and output safety**

Keep the fixed phrase/rule table in code with named test cases. Mirror the same named cases in `local_danger_cue_scanner.dart` so the Android App can show immediate offline guidance; the delivered request is rescanned by the server, whose result is authoritative. The server scanner creates a Task 5 event before any model call. Output validation rejects diagnosis, medicine dosage/change, “没有危险”, external-action claims and instructions to bypass human confirmation. Rejected output uses fixed safe copy and records only the rejection code.

- [ ] **Step 3: Write failing AI service tests with a fake cloud client**

Cover successful chat, timeout, malformed JSON, prompt-injection text, cross-elder context request and forbidden medical output. Assert timeout returns `usedFallback: true`, does not create duplicate events, and never persists raw input after the session.

```csharp
public interface ICloudLlmClient
{
    Task<string> CompleteJsonAsync(
        IReadOnlyList<LlmMessage> messages,
        string schemaName,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Implement the cloud adapter and fixed fallback**

`CloudLlmOptions` requires `BaseUrl`, `Model` and process environment key `COMMUNITYCARE_LLM_API_KEY`. `OpenAiCompatibleLlmClient` posts to `{BaseUrl}/chat/completions`, sends no tool definitions, sets a 15-second timeout and requests JSON. The server validates the returned structure before constructing an `AiDraft`. Missing configuration marks AI as degraded but does not fail application readiness.

- [ ] **Step 5: Implement draft and memory confirmation**

Persist only session metadata, generated draft, confirmation actor/time and confirmed memory text. Raw elder messages and raw audio are not stored after the request. A visit-summary draft cannot replace the original visit note; confirmation writes a separate confirmed summary. Add the `AddAiDraftsAndMemory` EF migration.

- [ ] **Step 6: Add API and client tests before integration**

Test elder chat, staff visit summary, forbidden family raw-text access, memory confirmation/deletion and AI timeout. On mobile, verify a danger cue shows emergency guidance even with the fake cloud client offline, a service-request draft is not saved before tapping `确认提交`, and a memory candidate can be confirmed then deleted from settings. On Web, verify the draft is visibly labeled `AI 草稿` and requires staff confirmation.

- [ ] **Step 7: Run AI safety gates**

```powershell
dotnet test tests/CommunityElderCare.UnitTests --filter FullyQualifiedName~Ai
dotnet test tests/CommunityElderCare.IntegrationTests --filter FullyQualifiedName~Ai
npm --prefix apps/admin-web test -- --run
. .\scripts\dev-env.ps1
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/ai_chat_safety_test.dart'
```

Expected: PASS with the cloud client fully replaced by a fake in automated tests.

- [ ] **Step 8: Commit AI capabilities**

```powershell
git add src tests apps
git commit -m "feat: add safe AI care drafts"
```

---

### Task 13: Add the device gateway, Web simulator and ESP32 SOS firmware

**Files:**
- Create: `src/CommunityElderCare.Core/Devices/Device.cs`
- Create: `src/CommunityElderCare.Core/Devices/DeviceSignal.cs`
- Create: `src/CommunityElderCare.Core/Devices/DeviceSignalType.cs`
- Create: `src/CommunityElderCare.Core/Devices/IDeviceSignalService.cs`
- Create: `src/CommunityElderCare.Infrastructure/Devices/DeviceTokenValidator.cs`
- Create: `src/CommunityElderCare.Infrastructure/Devices/DeviceSignalService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Devices/DeviceSignalRequest.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/DeviceEndpoints.cs`
- Create: `tests/CommunityElderCare.UnitTests/Devices/DeviceSignalServiceTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/DeviceEndpointTests.cs`
- Create: `apps/admin-web/src/pages/DeviceSignalPage.vue`
- Create: `apps/admin-web/src/components/DeviceSimulator.vue`
- Create: `apps/admin-web/src/components/__tests__/DeviceSimulator.spec.ts`
- Create: `firmware/esp32-sos/platformio.ini`
- Create: `firmware/esp32-sos/src/main.cpp`
- Create: `firmware/esp32-sos/include/demo_config.example.h`
- Create: `firmware/esp32-sos/README.md`
- Create: `scripts/setup-platformio.ps1`

**Interfaces:**
- Produces: `POST /api/v1/device-signals` authenticated by `X-Device-Token`.
- Produces: `POST /api/v1/demo/device-signals` for administrator simulator use.
- Produces: signal types `SosButton`, `NoWaterActivity`, `DeviceOffline`.
- Produces: request fields `deviceId`, `eventId`, `deviceTime`, `signalType`, and `buttonState`.
- Consumes: Task 5 care-event correlation and idempotent source IDs.

- [ ] **Step 1: Write failing protocol and idempotency tests**

```csharp
[Fact]
public async Task Duplicate_device_event_is_stored_and_correlated_once()
{
    var request = TestDeviceSignal.Sos(EventId);
    var first = await Service.ReceiveAsync(request, DeviceActor, Ct);
    var second = await Service.ReceiveAsync(request, DeviceActor, Ct);
    Assert.Equal(first.Value!.SignalId, second.Value!.SignalId);
    Assert.Single(await Db.DeviceSignals.ToListAsync(Ct));
    Assert.Single(await Db.CareEventEvidence.ToListAsync(Ct));
}
```

Also test invalid token, unknown device, server receive-time ordering, 30-minute safety-event merge and no-water mapping to `NeedsConfirmation`. Run and expect FAIL.

- [ ] **Step 2: Implement device storage and gateway**

Hash each process-local device token and bind it to one device ID; do not store or log the raw value. Add a unique index on `(DeviceId, EventId)`. Return the original response for duplicate delivery. Device time is diagnostic only; `ReceivedAt` controls ordering.

```csharp
public sealed record DeviceSignalRequest(
    Guid DeviceId,
    Guid EventId,
    DateTimeOffset DeviceTime,
    DeviceSignalType SignalType,
    string? ButtonState);
```

Add the `AddDevicesAndSignals` EF migration before endpoint integration tests.

- [ ] **Step 3: Write and implement the Web simulator test**

Test buttons `模拟 SOS`, `模拟长时间无用水`, and `模拟设备离线`. Each request must go through the same `IDeviceSignalService`; the admin endpoint may supply authenticated device identity but cannot insert events directly. Render the returned event link and `模拟信号` label.

- [ ] **Step 4: Create isolated PlatformIO tooling**

`scripts/setup-platformio.ps1` creates `.tools/platformio`, installs `platformio==6.1.19`, and invokes the executable by full path. `.tools/` is ignored by Git and never appended to persistent Path.

```powershell
python -m venv .tools\platformio
.\.tools\platformio\Scripts\python.exe -m pip install --disable-pip-version-check platformio==6.1.19
```

- [ ] **Step 5: Implement and compile the ESP32 firmware**

Use this pinned `platformio.ini`:

```ini
[env:esp32dev]
platform = espressif32@7.0.1
board = esp32dev
framework = arduino
monitor_speed = 115200
build_flags = -DCORE_DEBUG_LEVEL=0
```

`main.cpp` must debounce the button, require a 2-second hold, generate a UUID-compatible event ID, POST the exact JSON contract, retry with bounded backoff, and drive LED states for sending/success/failure. Commit `demo_config.example.h` with visibly non-working compile-only values `COMPILE_ONLY_WIFI`, `COMPILE_ONLY_PASSWORD`, `http://192.0.2.1:5180`, and `COMPILE_ONLY_DEVICE_TOKEN`; `192.0.2.0/24` is used only as documentation address space. By default, `setup-platformio.ps1` copies those values to ignored `demo_config.h` only to prove compilation. Its explicit `-Physical` mode instead requires process-local `COMMUNITYCARE_WIFI_SSID`, `COMMUNITYCARE_WIFI_PASSWORD`, `COMMUNITYCARE_API_BASE_URL`, and `COMMUNITYCARE_DEVICE_TOKEN`, rewrites the ignored header without echoing values, and fails if any value is absent. Physical setup instructions state that no public compile-only value may protect a real device or service.

- [ ] **Step 6: Run device and firmware gates**

```powershell
dotnet test tests/CommunityElderCare.UnitTests --filter FullyQualifiedName~Device
dotnet test tests/CommunityElderCare.IntegrationTests --filter FullyQualifiedName~Device
npm --prefix apps/admin-web test -- --run
.\scripts\setup-platformio.ps1
.\.tools\platformio\Scripts\platformio.exe run --project-dir firmware/esp32-sos
```

Expected: PASS and a non-empty firmware binary under `.pio/build/esp32dev`; `.pio` and `.tools` remain ignored.

- [ ] **Step 7: Commit device support**

```powershell
git add src tests apps/admin-web firmware scripts .gitignore
git commit -m "feat: add SOS device and simulator"
```

---

### Task 14: Add audit, reports, reset, diagnostics and one-click demo operation

**Files:**
- Create: `src/CommunityElderCare.Core/Common/AuditEntry.cs`
- Create: `src/CommunityElderCare.Core/Common/BackgroundJobRun.cs`
- Create: `src/CommunityElderCare.Infrastructure/Persistence/AuditSaveChangesInterceptor.cs`
- Create: `src/CommunityElderCare.Core/Common/NotificationAttempt.cs`
- Create: `src/CommunityElderCare.Infrastructure/Notifications/SimulationNotificationService.cs`
- Create: `src/CommunityElderCare.Infrastructure/Background/BackgroundJobRecorder.cs`
- Create: `src/CommunityElderCare.Infrastructure/Demo/DemoResetService.cs`
- Create: `src/CommunityElderCare.Api/Contracts/Notifications/SimulationAttemptContracts.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/AuditEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/ReportEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/NotificationSimulationEndpoints.cs`
- Create: `src/CommunityElderCare.Api/Endpoints/DemoEndpoints.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/AuditEndpointTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/NotificationSimulationEndpointTests.cs`
- Create: `tests/CommunityElderCare.IntegrationTests/DemoResetTests.cs`
- Create: `apps/admin-web/src/pages/AuditPage.vue`
- Create: `apps/admin-web/src/pages/ReportPage.vue`
- Create: `apps/admin-web/src/pages/SettingsPage.vue`
- Create: `apps/admin-web/src/pages/__tests__/AuditAndReset.spec.ts`
- Modify: `apps/admin-web/src/components/SimulationActionPanel.vue`
- Modify: `apps/admin-web/src/layouts/CommunityLayout.vue`
- Modify: `apps/admin-web/src/router/index.ts`
- Create: `scripts/start-demo.ps1`
- Create: `scripts/stop-demo.ps1`
- Create: `scripts/reset-demo.ps1`
- Create: `scripts/verify-all.ps1`

**Interfaces:**
- Produces: `GET /api/v1/audit?entityType=&entityId=`.
- Produces: `GET /api/v1/reports/demo-summary`.
- Produces: `POST /api/v1/care-events/{eventId}/simulation-attempts` for `InAppNotification`, `Sms`, `Phone`, `HomeVisit`, and `EmergencyTransport` channels; every response is marked `isSimulation=true`.
- Produces: `POST /api/v1/demo/reset` requiring administrator role and header `X-Confirm-Demo-Reset: RESET-20`.
- Produces: readiness components `database`, `backgroundJobs`, `ai`, `deviceGateway`, `localNetwork` with independent status.
- Produces: process manifest `.run/demo-processes.json`, ignored by Git.

- [ ] **Step 1: Write the failing audit-completeness test**

Run the main backend story and assert audit entries for event creation, evidence merge, acceptance, visit result, emergency escalation, simulated contacts, resolution, follow-up and closure. Each entry must include actor, action, entity, time, reason and before/after status where applicable. Add notification tests proving an HTTP retry with the same request ID returns one attempt, while an operator retry after a recorded failure uses a new request ID and preserves both attempts.

- [ ] **Step 2: Implement audit interception and simulation records**

The interceptor captures business mutations in the same transaction. It must redact passwords, JWTs, device tokens, LLM keys, raw AI messages and raw visit notes. `SimulationNotificationService` records request ID, channel, recipient role, attempt time, outcome and `IsSimulation = true`; it never performs network calls. Only a correct-area community-staff actor handling the event may invoke the endpoint. The Web panel shows `模拟发送中` until the persisted response arrives and never says `已联系` after a failed request. `BackgroundJobRecorder` stores job name, run ID, start/end time, result, retry count and a sanitized error code for missed-check and contact-escalation workers.

- [ ] **Step 3: Write the failing reset determinism test**

```csharp
[Fact]
public async Task Reset_restores_the_same_twenty_profile_story()
{
    var baseTime = new DateTimeOffset(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);
    Factory.SetTime(baseTime);
    var before = await SnapshotStableSeedFieldsAsync();
    await MutateMainStoryAsync();
    await ResetAsync(confirmHeader: "RESET-20");
    var after = await SnapshotStableSeedFieldsAsync();
    Assert.Equal(before, after);
    Assert.Equal(20, after.ElderCount);
    Assert.Equal(0, after.OpenEventCountAtStart);
    await Factory.RunMissedCheckInWorkerOnceAsync();
    Assert.Equal(1, await CountOpenEventsAsync());
}
```

Also verify missing confirmation returns `400 RESET_CONFIRMATION_REQUIRED` and non-admin returns `403`.

- [ ] **Step 4: Implement transactional reset and report endpoints**

Reset stops new demo mutations through an in-process gate, replaces only known demo rows inside one transaction, reseeds with `(20, 20260824, TimeProvider.GetUtcNow())`, and returns elder count, main elder ID, base time and elapsed milliseconds. It never deletes arbitrary filesystem paths. The main elder is immediately overdue; the idempotent worker creates exactly one opening event after reset. The report returns synthetic aggregate counts only. Add the `AddAuditAndDemoOperations` EF migration.

- [ ] **Step 5: Implement audit/report/settings pages**

Audit filters by entity and time, report clearly states `基于演示数据`, and settings shows each readiness component separately. Complete the community navigation with exactly these destinations: `工作台`、`老人档案`、`照料事件`、`探访任务`、`服务工单`、`设备信号`、`报告与审计`、`系统设置`; add a component assertion for the complete label set and role-based omissions. The reset control requires typing `RESET-20` and a second confirmation; success refetches counts from the API.

- [ ] **Step 6: Implement safe demo process scripts**

`start-demo.ps1` runs preflight, creates process-local demo password and JWT values, and uses `COMMUNITYCARE_DEVICE_TOKEN` when the operator supplied it for a physical ESP32; otherwise it creates a random simulator-only device token. It starts API and Web with `Start-Process -WindowStyle Hidden`, records exact PIDs and executable paths under `.run`, then polls readiness. The documented physical sequence sets the four process variables once, runs `setup-platformio.ps1 -Physical`, flashes the board, and invokes `start-demo.ps1` from the same PowerShell process so firmware and API use the same token. `stop-demo.ps1` reads the manifest and stops a PID only when its executable and command-line path still match this repository. `reset-demo.ps1` calls the authenticated API and reads back 20 elders.

- [ ] **Step 7: Make `verify-all.ps1` fail closed**

It must run, in order: preflight, .NET tests, Web tests/lint/build, Android integration tests, Flutter analyze, debug APK build, PlatformIO compile and main-story API test. After every native command, capture `$LASTEXITCODE` immediately. A temporary canary command exiting `73` must make the whole script exit non-zero without a success footer.

- [ ] **Step 8: Run operations regression gates**

```powershell
dotnet test tests/CommunityElderCare.IntegrationTests --filter "FullyQualifiedName~Audit|FullyQualifiedName~DemoReset"
npm --prefix apps/admin-web test -- --run
.\scripts\verify-all.ps1
```

Expected: PASS; reset completes under 60 seconds and readiness reports each component.

- [ ] **Step 9: Commit demo operations**

```powershell
git add src tests apps/admin-web scripts .gitignore
git commit -m "feat: add auditable demo operations"
```

---

### Task 15: Complete end-to-end acceptance, packaging and competition evidence

**Files:**
- Create: `tests/e2e/package.json`
- Create: `tests/e2e/package-lock.json`
- Create: `tests/e2e/tsconfig.json`
- Create: `tests/e2e/playwright.config.ts`
- Create: `tests/e2e/main-story.spec.ts`
- Create: `tests/e2e/authorization.spec.ts`
- Create: `apps/mobile/integration_test/main_story_test.dart`
- Create: `.github/workflows/ci.yml`
- Create: `docs/demo/demo-script.md`
- Create: `docs/demo/acceptance-report.md`
- Create: `docs/demo/failure-drill.md`
- Create: `docs/demo/physical-phone-receipt.md`
- Create: `docs/demo/usability-evidence.md`
- Create: `docs/progress/release-checklist.md`
- Create: `scripts/package-demo.ps1`
- Create: `scripts/verify-physical-phone.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: every prior task.
- Produces: repeatable 5～7 minute main story from reset to closure.
- Produces: packaged APK, Web build, API publish directory, firmware binary, scripts, documentation and SHA-256 manifest under ignored `artifacts/demo-v1/`.
- Produces: GitHub CI for .NET, Web, Flutter analyze/build and PlatformIO compile; Android emulator acceptance remains a recorded local gate.

- [ ] **Step 1: Write the failing Playwright main-story test**

Initialize the isolated package with `@playwright/test`, a `test` script equal to `playwright test`, and a config whose `baseURL` points to the already running local Web app:

```powershell
Push-Location tests/e2e
npm install --save-dev @playwright/test typescript
npx playwright install chromium
Pop-Location
```

The test must reset data, log in as community staff, wait for the single missed-check event, inject a device signal through the simulator, accept the merged event, complete a simulated visit, escalate to emergency, record simulated family/120 actions, resolve, follow up and close. Assert one event ID throughout and one timeline entry per action.

- [ ] **Step 2: Write the failing authorization E2E test**

Cover family field grants and revocation, service-worker task isolation, community area isolation, administrator denial of raw AI text, and emergency break-glass requiring reason, expiry and audit. No E2E test may use real personal data or external messaging.

- [ ] **Step 3: Implement missing wiring only**

Wire routes, API contracts and test selectors required by the E2E tests. Do not add new business modules after this point. Run Playwright until both specifications pass.

- [ ] **Step 4: Add the Android main-story integration test**

On an Android emulator, log in as the main elder, complete check-in, submit help while offline, reconnect, verify one delivered event, open AI chat, trigger a fixed danger cue and verify emergency guidance without a real dial action. Then log in as family and verify only authorized summary fields.

- [ ] **Step 5: Verify the APK on the competition Android phone**

`scripts/verify-physical-phone.ps1` must require exactly one connected non-emulator Android device, select the laptop's explicit LAN IPv4 address, rebuild the APK with that `API_BASE_URL`, install it with `adb install -r`, launch it and confirm the phone can reach `/health/ready`. Manually verify elder login, one check-in, one offline/reconnect help submission, large-font layout and no real dial action. Record redacted device model, Android version, APK SHA-256, API address class and results in `physical-phone-receipt.md`; do not record the full device serial.

- [ ] **Step 6: Add CI without pretending it replaces local Android acceptance**

The workflow checks out the repository, installs `.NET 10.0.302`, Node 24 LTS, Flutter 3.47.1 and Python 3.12, then runs .NET tests, Web tests/build, Flutter analyze/debug APK build and PlatformIO compile. CI uploads non-sensitive build logs and test reports. The acceptance report separately records the exact local emulator run.

- [ ] **Step 7: Run failure drills**

Record outcomes for: cloud AI unavailable, API temporarily offline during SOS, duplicate App request, duplicate device event, ESP32 offline, notification simulation failure, background-worker retry and SQLite write failure. Each drill must show visible user state, retry/idempotency behavior and preserved audit evidence.

- [ ] **Step 8: Perform accessibility and usability evidence collection**

Verify large text, contrast, semantic labels, keyboard focus, no hidden gestures, no color-only status and one primary action per elder page. If five or more volunteers aged 60+ are available, use only synthetic data and record tasks, completion and observed difficulties; label it exploratory and not validation of the 75+ target group. Otherwise write exactly `尚无真实老人可用性证据` in `usability-evidence.md`.

- [ ] **Step 9: Package reproducible demo artifacts**

`scripts/package-demo.ps1` first runs `verify-all.ps1`, then copies only the published API, Web `dist`, APK, firmware binary, safe scripts and public docs into `artifacts/demo-v1`. It rejects logs, `.env`, tokens, database files containing non-seed state, `.run`, `.tools`, caches and source paths outside the repository. Generate `SHA256SUMS.txt` from the packaged files.

- [ ] **Step 10: Rehearse the 5～7 minute story and backup path**

Run one reset, one preflight, one hardware path and one simulator path. Record actual elapsed time, reset time and dependency states. Create a backup screen recording showing the same simulated story; do not edit labels that identify simulation or synthetic data.

- [ ] **Step 11: Run the final acceptance gate**

```powershell
.\scripts\verify-all.ps1
try {
  .\scripts\start-demo.ps1
  .\scripts\verify-physical-phone.ps1
  npm --prefix tests/e2e test
}
finally {
  .\scripts\stop-demo.ps1
}
.\scripts\package-demo.ps1
git status --short
```

Expected: all gates pass, the working tree contains only intended evidence changes before commit, the package has a SHA-256 manifest, and no real data or secret scan finding exists.

- [ ] **Step 12: Commit acceptance evidence**

```powershell
git add .github tests apps/mobile/integration_test docs README.md scripts/package-demo.ps1
git commit -m "test: complete competition acceptance evidence"
```

Do not commit `artifacts/`, `.run/`, `.tools/`, test recordings containing local paths, tokens, caches or generated databases.

---

## 3. Ten-Week Mapping

| Week | Tasks | Exit gate |
|---|---|---|
| 1 | Task 1 | API、Web、Android 三端可启动；预检失败关闭 |
| 2 | Tasks 2–3 | 20 份确定性合成档案；角色与字段授权通过 |
| 3 | Tasks 4–5 | 签到、提醒、漏签和事件状态机通过 |
| 4 | Task 6 | 探访、服务、回访和关单闭环通过 |
| 5 | Tasks 7–8 | 社区后台完成档案与事件操作 |
| 6 | Tasks 9–11 | 老人/家属独立 App 流程和离线队列通过 |
| 7 | Task 12 | AI 安全、草稿确认和断网降级通过 |
| 8 | Task 13 | 设备模拟器稳定；ESP32 固件可编译 |
| 9 | Task 14 | 审计、报告、重置、诊断和一键运行通过 |
| 10 | Task 15 | 全链路、权限、适老化、故障演练和打包通过 |

## 4. Mandatory Review Gates

After every task:

1. Read the exact diff; reject unrelated files, generated caches, secrets, real personal data and backup copies.
2. Verify the task's focused tests fail before implementation and pass afterward.
3. Run the named regression tests for neighboring interfaces.
4. Confirm public API names and enum values still match the stable contracts in this plan.
5. Commit only after tests pass; do not combine two task commits to hide a red gate.

At the end of Weeks 2, 4, 6, 8 and 10, run `scripts/verify-all.ps1`. A tooling crash, missing emulator, missing cloud configuration or hardware problem is reported as its own blocker; it is never converted into a green business result.

## 5. Completion Definition

v1 is complete only when all of the following are true:

- Reset returns the same 20-profile synthetic dataset and main-story IDs in under 60 seconds.
- Main story completes in 5～7 minutes and retains one event ID through merged evidence.
- Pending events have one responsibility queue; accepted events have exactly one current owner.
- Illegal transitions, unauthorized reads and revoked consent fail with stable error codes.
- Duplicate App and device submissions create one check-in/signal/event result.
- Core care flow works with cloud AI absent and ESP32 disconnected.
- Android 模拟器集成测试、比赛 Android 真机验收、Web 测试与构建、.NET 测试、PlatformIO 编译和 Playwright 端到端测试全部通过。
- Every external contact is visibly simulated; no real external action or personal data appears.
- Package contents and SHA-256 manifest are reproducible from a clean checkout with the documented toolchain.
- Claims in the presentation match the evidence report, including any missing target-user usability evidence.
