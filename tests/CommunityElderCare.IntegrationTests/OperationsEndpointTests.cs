using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class OperationsEndpointTests
{
    [Fact]
    public async Task Personnel_and_tasks_are_scoped_and_workers_cannot_read_operations()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var response = await staff.GetAsync("/api/v1/operations/personnel");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var personnel = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(personnel.EnumerateArray().Count(p => p.GetProperty("role").GetString() == "CommunityStaff") >= 2);
        Assert.True(personnel.EnumerateArray().Count(p => p.GetProperty("role").GetString() == "ServiceWorker") >= 2);
        Assert.All(personnel.EnumerateArray(), p => Assert.Equal("A01", p.GetProperty("areaCode").GetString()));
        using var worker = factory.CreateAuthenticatedClient(DemoRole.ServiceWorker);
        Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/v1/operations/tasks")).StatusCode);
    }

    [Fact]
    public async Task A_worker_can_handle_multiple_orders_and_reassignment_revokes_old_access()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        using var worker = factory.CreateAuthenticatedClient(DemoRole.ServiceWorker);
        var first = await CreateOrder(staff, factory.MainElderId);
        Guid otherElder;
        using (var scope = factory.Services.CreateScope())
            otherElder = await scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>().ElderProfiles
                .Where(e => e.AreaCode == "A01" && e.Id != factory.MainElderId).Select(e => e.Id).FirstAsync();
        var second = await CreateOrder(staff, otherElder);
        using var newWorker = factory.CreateAuthenticatedClient(DemoRole.ServiceWorker,
            userId: DemoIdentitySeed.SecondServiceWorkerUserId);
        var tasks = await worker.GetFromJsonAsync<JsonElement>("/api/v1/service-orders/my-tasks");
        Assert.Equal(2, tasks.GetArrayLength());
        var rows = await staff.GetFromJsonAsync<JsonElement>("/api/v1/operations/tasks");
        var row = rows.EnumerateArray().Single(p => p.GetProperty("taskId").GetGuid() == first);
        var version = row.GetProperty("version").GetGuid();
        var reassign = await staff.PostAsJsonAsync($"/api/v1/service-orders/{first}/reassign", new
        {
            assignedUserId = Guid.Parse("11111111-1111-1111-1111-111111111107"),
            reason = "调整服务人员",
            expectedVersion = version,
        });
        Assert.Equal(HttpStatusCode.OK, reassign.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await worker.PostAsync($"/api/v1/service-orders/{first}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await worker.PostAsync($"/api/v1/service-orders/{second}/accept", null)).StatusCode);
        var stale = await staff.PostAsJsonAsync($"/api/v1/service-orders/{first}/reassign", new
        {
            assignedUserId = DemoIdentitySeed.ServiceWorkerUserId,
            reason = "再次调整",
            expectedVersion = version,
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await newWorker.PostAsync($"/api/v1/service-orders/{first}/accept", null)).StatusCode);
        var startedRow = (await staff.GetFromJsonAsync<JsonElement>("/api/v1/operations/tasks"))
            .EnumerateArray().Single(p => p.GetProperty("taskId").GetGuid() == second);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync($"/api/v1/service-orders/{second}/reassign", new
        {
            assignedUserId = Guid.Parse("11111111-1111-1111-1111-111111111107"),
            reason = "已经开始不能转派",
            expectedVersion = startedRow.GetProperty("version").GetGuid(),
        })).StatusCode);
    }

    [Fact]
    public async Task Stale_start_cannot_overwrite_reassignment_or_leave_audit_rows()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var id = await CreateOrder(staff, factory.MainElderId);
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var first = await db1.ServiceOrders.SingleAsync(t => t.Id == id);
        var stale = await db2.ServiceOrders.SingleAsync(t => t.Id == id);
        first.Reassign(DemoIdentitySeed.SecondServiceWorkerUserId);
        await db1.SaveChangesAsync();
        var auditCount = await db1.AuditEntries.CountAsync();
        Assert.True(stale.Accept(new(DemoIdentitySeed.ServiceWorkerUserId, DemoRole.ServiceWorker, null, null, null), DateTimeOffset.UtcNow).IsSuccess);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        Assert.Equal(auditCount, await db1.AuditEntries.CountAsync());
        var persisted = await db1.ServiceOrders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(WorkStatus.Assigned, persisted.Status);
        Assert.Equal(DemoIdentitySeed.SecondServiceWorkerUserId, persisted.AssignedWorkerUserId);
    }

    [Fact]
    public async Task Migration_preserves_existing_order_text_and_leaves_unknown_deadline_empty()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var id = await CreateOrder(staff, factory.MainElderId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var before = await db.ServiceOrders.AsNoTracking().SingleAsync(t => t.Id == id);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260824142456_AddAuditAndDemoOperations");
        await migrator.MigrateAsync();
        var after = await db.ServiceOrders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(before.ContactInstruction, after.ContactInstruction);
        Assert.Equal(before.ScheduledWindow, after.ScheduledWindow);
        Assert.Equal(before.AssignedWorkerUserId, after.AssignedWorkerUserId);
        Assert.Null(after.DueAt);
        await migrator.MigrateAsync();
        Assert.Equal(1, await db.ServiceOrders.CountAsync(t => t.Id == id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Community_task_reassignment_preserves_schedule_and_owner_and_enforces_scope(bool followUp)
    {
        await using var factory = new CommunityCareWebFactory();
        using var owner = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        using var next = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, userId: DemoIdentitySeed.SecondCommunityUserId);
        using var crossArea = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A02");
        using var admin = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        var created = await owner.PostAsJsonAsync("/api/v1/care-events", new
        {
            clientRequestId = Guid.NewGuid(), elderId = factory.MainElderId, trigger = "LifeServiceNeed",
            summary = "安排社区关怀", occurredAt = DateTimeOffset.UtcNow,
        });
        created.EnsureSuccessStatusCode();
        var eventId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await owner.PostAsync($"/api/v1/care-events/{eventId}/accept", null)).EnsureSuccessStatusCode();
        if (followUp)
        {
            (await owner.PostAsJsonAsync($"/api/v1/care-events/{eventId}/transitions",
                new { toStatus = "InProgress", reason = "开始电话确认" })).EnsureSuccessStatusCode();
            (await owner.PostAsJsonAsync($"/api/v1/care-events/{eventId}/transitions",
                new { toStatus = "Resolved", reason = "已完成电话确认", resolution = "安排后续回访" })).EnsureSuccessStatusCode();
        }
        var route = followUp ? "follow-ups" : "visits";
        var due = DateTimeOffset.UtcNow.AddDays(1);
        var taskResponse = await owner.PostAsJsonAsync($"/api/v1/care-events/{eventId}/{route}", new
        {
            assignedStaffUserId = DemoIdentitySeed.CommunityUserId, dueAt = due,
            scheduledStartAt = due.AddHours(-1), scheduledEndAt = due, isMandatory = true,
        });
        taskResponse.EnsureSuccessStatusCode();
        var taskId = (await taskResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty(followUp ? "followUpId" : "visitId").GetGuid();
        var before = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/operations/tasks"))
            .EnumerateArray().Single(t => t.GetProperty("taskId").GetGuid() == taskId);
        var body = new { assignedUserId = DemoIdentitySeed.SecondCommunityUserId,
            reason = "调整社区人员安排", expectedVersion = before.GetProperty("version").GetGuid() };
        foreach (var forbidden in new[] { next, crossArea, admin })
            Assert.Equal(HttpStatusCode.Forbidden, (await forbidden.PostAsJsonAsync($"/api/v1/{route}/{taskId}/reassign", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/v1/{route}/{taskId}/reassign",
            new { body.assignedUserId, reason = " ", body.expectedVersion })).StatusCode);
        (await owner.PostAsJsonAsync($"/api/v1/{route}/{taskId}/reassign", body)).EnsureSuccessStatusCode();
        var after = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/operations/tasks"))
            .EnumerateArray().Single(t => t.GetProperty("taskId").GetGuid() == taskId);
        Assert.Equal(before.GetProperty("dueAt").GetDateTimeOffset(), after.GetProperty("dueAt").GetDateTimeOffset());
        Assert.True(after.GetProperty("isMandatory").GetBoolean());
        Assert.Equal(DemoIdentitySeed.CommunityUserId, after.GetProperty("eventOwnerUserId").GetGuid());
        var action = $"/api/v1/{route}/{taskId}/" + (followUp ? "complete" : "start");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync(action, new { result = "关怀已完成" })).StatusCode);
        (await next.PostAsJsonAsync(action, new { result = "关怀已完成" })).EnsureSuccessStatusCode();
        var started = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/operations/tasks"))
            .EnumerateArray().Single(t => t.GetProperty("taskId").GetGuid() == taskId);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/{route}/{taskId}/reassign",
            new { assignedUserId = DemoIdentitySeed.CommunityUserId, reason = "不能再次转派",
                expectedVersion = started.GetProperty("version").GetGuid() })).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var history = await db.TaskReassignments.SingleAsync(r => r.TaskId == taskId);
        Assert.Equal(DemoIdentitySeed.CommunityUserId, history.FromUserId);
        Assert.Equal(DemoIdentitySeed.SecondCommunityUserId, history.ToUserId);
        Assert.Equal(DemoIdentitySeed.CommunityUserId, history.ActorUserId);
        Assert.Equal(1, await db.AuditEntries.CountAsync(a => a.Action == "TaskReassigned" && a.EntityId == taskId));
    }

    internal static async Task<Guid> CreateOrder(HttpClient staff, Guid elderId, DateTimeOffset? dueAt = null)
    {
        var created = await staff.PostAsJsonAsync("/api/v1/care-events", new
        {
            clientRequestId = Guid.NewGuid(), elderId, trigger = "LifeServiceNeed",
            summary = "助餐安排", occurredAt = DateTimeOffset.UtcNow,
        });
        created.EnsureSuccessStatusCode();
        var evt = await created.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = evt.GetProperty("id").GetGuid();
        (await staff.PostAsync($"/api/v1/care-events/{eventId}/accept", null)).EnsureSuccessStatusCode();
        var order = await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/service-orders", new
        {
            serviceType = "助餐配送", scheduledWindow = "10:00—11:00", contactInstruction = "到达后联系社区",
            assignedWorkerUserId = DemoIdentitySeed.ServiceWorkerUserId, isMandatory = true, dueAt,
        });
        order.EnsureSuccessStatusCode();
        return (await order.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetGuid();
    }
}
