using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.CareWork;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityElderCare.IntegrationTests;

public sealed class OperationsReportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    [Fact]
    public async Task Scenario_is_confirmed_idempotent_scoped_and_resettable()
    {
        await using var factory = new CommunityCareWebFactory();
        using var admin = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/v1/demo/operations-scenario", null)).StatusCode);
        admin.DefaultRequestHeaders.Add("X-Confirm-Operations-Scenario", "LOAD-OPERATIONS");
        var loaded = await admin.PostAsync("/api/v1/demo/operations-scenario", null);
        loaded.EnsureSuccessStatusCode();
        Assert.False((await loaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("alreadyLoaded").GetBoolean());
        var repeated = await admin.PostAsync("/api/v1/demo/operations-scenario", null);
        repeated.EnsureSuccessStatusCode();
        Assert.True((await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("alreadyLoaded").GetBoolean());
        var report = await admin.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions);
        Assert.NotNull(report);
        Assert.Equal(12, report.Summary.NewEventCount);
        Assert.Equal(9, report.Summary.ClosedEventCount);
        Assert.Equal(9, report.Summary.CompletedVisitCount);
        Assert.Equal(9, report.Summary.CompletedOrderCount);
        Assert.Equal(9, report.Summary.CompletedFollowUpCount);
        Assert.Equal(10, report.Summary.AverageAcceptanceMinutes);
        Assert.Equal(6, report.Summary.CurrentOpenTaskCount);
        Assert.Equal(4, report.Summary.CurrentOverdueTaskCount);
        Assert.Equal(report.Summary.NewEventCount, report.Daily.Sum(d => d.NewEventCount));
        using var area2 = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A02");
        var other = await area2.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions);
        Assert.Equal(0, other!.Summary.NewEventCount);
        Assert.Empty(other.Personnel);
        Assert.Equal(HttpStatusCode.Forbidden, (await area2.GetAsync("/api/v1/reports/operations?areaCode=A01")).StatusCode);
        var legacy = await area2.GetFromJsonAsync<JsonElement>("/api/v1/reports/demo-summary");
        Assert.Equal(0, legacy.GetProperty("completedVisitCount").GetInt32());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
            Assert.Equal(await db.ElderProfiles.CountAsync(e => e.AreaCode == "A02"), legacy.GetProperty("elderCount").GetInt32());
            Assert.Equal(12, await db.CareEvents.CountAsync(e => e.SourceEventId.StartsWith("operations-scenario:v1:")));
        }
        admin.DefaultRequestHeaders.Add("X-Confirm-Demo-Reset", "RESET-20");
        (await admin.PostAsync("/api/v1/demo/reset", null)).EnsureSuccessStatusCode();
        var reset = await admin.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions);
        Assert.Equal(0, reset!.Summary.NewEventCount);
        (await admin.PostAsync("/api/v1/demo/operations-scenario", null)).EnsureSuccessStatusCode();
        Assert.Equal(12, (await admin.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions))!.Summary.NewEventCount);
    }

    [Fact]
    public async Task Csv_is_authorized_escaped_audited_and_matches_report()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        await OperationsEndpointTests.CreateOrder(staff, factory.MainElderId);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
            var person = new UserAccount(Guid.NewGuid(), "csv.test", DemoRole.ServiceWorker, null, "A01", null);
            person.InitializeOperationsProfile(" =SUM(1,2)\"\n第二行", "A01");
            db.UserAccounts.Add(person);
            await db.SaveChangesAsync();
        }
        var report = await staff.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions);
        Assert.Equal(1, report!.Summary.NewEventCount);
        Assert.Equal(1, report.Summary.CurrentOpenTaskCount);
        foreach (var section in new[] { "summary", "daily", "personnel" })
        {
            var response = await staff.GetAsync("/api/v1/reports/operations.csv?section=" + section);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(new byte[] { 239, 187, 191 }, bytes.Take(3));
            var text = Encoding.UTF8.GetString(bytes);
            Assert.Contains("统计开始", text);
            Assert.DoesNotContain("healthRisks", text);
            Assert.DoesNotContain("contactInstruction", text);
            if (section == "personnel") Assert.Contains("\"' =SUM(1,2)\"\"\n第二行\"", text);
            if (section == "daily") Assert.Equal(report.Daily.Count + 3, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        }
        using var worker = factory.CreateAuthenticatedClient(DemoRole.ServiceWorker);
        Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/v1/reports/operations.csv?section=summary")).StatusCode);
        using var check = factory.Services.CreateScope();
        var dbCheck = check.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Equal(3, await dbCheck.AuditEntries.CountAsync(a => a.Action == "OperationsReportExported"));
    }

    [Theory]
    [InlineData("2026-01-01", "2026-04-01")]
    [InlineData("2026-08-27", "2026-08-26")]
    [InlineData("bad-date", "2026-08-27")]
    [InlineData(null, "0001-01-01")]
    [InlineData("0001-01-01", "0001-01-02")]
    public async Task Invalid_date_ranges_are_rejected(string? from, string to)
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var response = await client.GetAsync("/api/v1/reports/operations?to=" + to + (from == null ? "" : "&from=" + from));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Beijing_days_use_creation_acceptance_and_completion_independently_and_deduplicate_elders()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var midnight = DateTimeOffset.Parse("2026-08-27T00:00:00+08:00");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
            var actor = new ActorContext(DemoIdentitySeed.CommunityUserId, DemoRole.CommunityStaff, null, "A01", null);
            for (var i = 0; i < 2; i++)
            {
                var created = midnight.AddMinutes(i == 0 ? -1 : 0);
                var evt = CareEvent.Create(Guid.NewGuid(), factory.MainElderId, CareEventCategory.GeneralService,
                    CareEventLevel.GeneralService, CareEventSource.StaffVisit, "report-test:" + i,
                    "社区探访", created, "A01:care", created);
                Assert.True(evt.Accept(actor.UserId, midnight.AddMinutes(i == 0 ? 1 : 4)).IsAllowed);
                Assert.True(evt.TryTransition(CareEventStatus.InProgress, CareEventActorKind.Staff,
                    actor.UserId, "开始探访", null, midnight.AddMinutes(5)).IsAllowed);
                for (var j = 0; j < (i == 0 ? 2 : 1); j++)
                {
                    var end = i == 0 ? midnight.AddMinutes(10 + j) : midnight.AddDays(1);
                    var visit = VisitTask.Create(Guid.NewGuid(), evt.Id, evt.ElderId, actor.UserId,
                        midnight.AddMinutes(5), end, false, created).Value!;
                    Assert.True(visit.Start(actor, midnight.AddMinutes(5)).IsSuccess);
                    Assert.True(visit.Complete(actor, "内部探访记录", "已确认服务需求", "完成", end).IsSuccess);
                    db.VisitTasks.Add(visit);
                }
                if (i == 0)
                {
                    Assert.True(evt.TryTransition(CareEventStatus.Resolved, CareEventActorKind.Staff,
                        actor.UserId, "探访完成", "已确认", midnight.AddHours(1)).IsAllowed);
                    Assert.True(evt.TryTransition(CareEventStatus.Closed, CareEventActorKind.Staff,
                        actor.UserId, "完成后结案", null, midnight.AddHours(2)).IsAllowed);
                }
                db.CareEvents.Add(evt);
            }
            await db.SaveChangesAsync();
        }
        var report = await staff.GetFromJsonAsync<OperationsReport>(
            "/api/v1/reports/operations?from=2026-08-27&to=2026-08-27", JsonOptions);
        Assert.Equal(1, report!.Summary.NewEventCount);
        Assert.Equal(1, report.Summary.ClosedEventCount);
        Assert.Equal(2, report.Summary.CompletedVisitCount);
        Assert.Equal(1, report.Summary.VisitedElderCount);
        Assert.Equal(3, report.Summary.AverageAcceptanceMinutes);
        Assert.Equal(2, Assert.Single(report.Daily).CompletedVisitCount);
        Assert.Equal(2, report.Personnel.Sum(p => p.CompletedVisitCount));
        var previous = await staff.GetFromJsonAsync<OperationsReport>(
            "/api/v1/reports/operations?from=2026-08-26&to=2026-08-26", JsonOptions);
        Assert.Equal(1, previous!.Summary.NewEventCount);
        Assert.Equal(0, previous.Summary.CompletedVisitCount);
        Assert.Null(previous.Summary.AverageAcceptanceMinutes);
        var next = await staff.GetFromJsonAsync<OperationsReport>(
            "/api/v1/reports/operations?from=2026-08-28&to=2026-08-28", JsonOptions);
        Assert.Equal(1, next!.Summary.CompletedVisitCount);
        Assert.Equal(0, next.Summary.NewEventCount);
    }

    [Fact]
    public async Task Missing_deadline_is_not_invented_and_cancelled_tasks_are_not_open()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var oldId = await OperationsEndpointTests.CreateOrder(staff, factory.MainElderId);
        var lateId = await OperationsEndpointTests.CreateOrder(staff, factory.MainElderId, DateTimeOffset.UtcNow.AddDays(-1));
        var tasks = await staff.GetFromJsonAsync<List<OperationsTask>>("/api/v1/operations/tasks", JsonOptions);
        Assert.NotNull(tasks);
        Assert.Null(tasks.Single(t => t.TaskId == oldId).DueAt);
        Assert.False(tasks.Single(t => t.TaskId == oldId).IsOverdue);
        Assert.True(tasks.Single(t => t.TaskId == lateId).IsOverdue);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
            var task = await db.ServiceOrders.SingleAsync(t => t.Id == lateId);
            task.Cancel(new(Guid.NewGuid(), DemoRole.CommunityStaff, null, "A01", null), "老人已取消预约", DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        var report = await staff.GetFromJsonAsync<OperationsReport>("/api/v1/reports/operations", JsonOptions);
        Assert.Equal(1, report!.Summary.CurrentOpenTaskCount);
        Assert.Equal(0, report.Summary.CurrentOverdueTaskCount);
        Assert.Equal(0, report.Summary.CompletedOrderCount);
    }
}
