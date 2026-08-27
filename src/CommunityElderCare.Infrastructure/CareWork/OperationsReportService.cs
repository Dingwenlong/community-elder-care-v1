using System.Globalization;
using System.Text;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareWork;

public sealed record OperationsDateRange(DateOnly From, DateOnly To)
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(8);
    public DateTimeOffset Start => new(From.ToDateTime(TimeOnly.MinValue), Offset);
    public DateTimeOffset End => new(To.AddDays(1).ToDateTime(TimeOnly.MinValue), Offset);
    public bool Contains(DateTimeOffset value) => value >= Start && value < End;
    public static DateOnly Day(DateTimeOffset value) => DateOnly.FromDateTime(value.ToOffset(Offset).DateTime);
    public static OperationsDateRange? Parse(string? from, string? to, DateTimeOffset now)
    {
        var today = Day(now);
        var end = today;
        var start = today.AddDays(-29);
        if (to != null && !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end)) return null;
        if (from == null && end.DayNumber < 29) return null;
        if (from == null) start = end.AddDays(-29);
        else if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start)) return null;
        return end < start || start == DateOnly.MinValue || end.DayNumber - start.DayNumber >= 90 || end == DateOnly.MaxValue ? null : new(start, end);
    }
}

public sealed record ReportSummary(int NewEventCount, int ClosedEventCount, int CompletedVisitCount,
    int CompletedOrderCount, int CompletedFollowUpCount, int VisitedElderCount,
    double? AverageAcceptanceMinutes, int CurrentOpenTaskCount, int CurrentOverdueTaskCount);
public sealed record DailyOperations(DateOnly Date, int NewEventCount, int ClosedEventCount,
    int CompletedVisitCount, int CompletedOrderCount, int CompletedFollowUpCount);
public sealed record PersonnelOperations(Guid UserId, string DisplayName, DemoRole Role, string? AreaCode,
    int CompletedVisitCount, int CompletedOrderCount, int CompletedFollowUpCount, int PendingCount, int OverdueCount);
public sealed record OperationsReport(DateOnly From, DateOnly To, string TimeZone, DateTimeOffset GeneratedAt,
    string AreaLabel, ReportSummary Summary, IReadOnlyList<DailyOperations> Daily, IReadOnlyList<PersonnelOperations> Personnel);

public sealed class OperationsReportService(CommunityCareDbContext db, OperationsQuery query, TimeProvider clock)
{
    public async Task<OperationsReport> BuildAsync(ActorContext actor, OperationsDateRange range, string? areaCode, CancellationToken ct)
    {
        // Scope is applied in SQL before any time comparisons unsupported by SQLite.
        var elderIds = query.Elders(actor, areaCode).Select(e => e.Id);
        var events = await db.CareEvents.AsNoTracking().Where(e => elderIds.Contains(e.ElderId))
            .Select(e => new { e.Id, e.CreatedAt }).ToListAsync(ct);
        var eventIds = events.Select(e => e.Id).ToArray();
        var transitions = await db.CareEventTransitions.AsNoTracking()
            .Where(t => eventIds.Contains(t.CareEventId) &&
                (t.ToStatus == CareEventStatus.Accepted || t.ToStatus == CareEventStatus.Closed))
            .Select(t => new { t.CareEventId, t.ToStatus, t.OccurredAt }).ToListAsync(ct);
        var firstAccepted = transitions.Where(t => t.ToStatus == CareEventStatus.Accepted)
            .GroupBy(t => t.CareEventId).ToDictionary(g => g.Key, g => g.Min(t => t.OccurredAt));
        var closed = transitions.Where(t => t.ToStatus == CareEventStatus.Closed)
            .GroupBy(t => t.CareEventId).Select(g => g.Min(t => t.OccurredAt)).ToList();
        var times = events.Where(e => firstAccepted.TryGetValue(e.Id, out var at) && range.Contains(at) && at >= e.CreatedAt)
            .Select(e => (firstAccepted[e.Id] - e.CreatedAt).TotalMinutes).ToList();
        var tasks = await query.TasksAsync(actor, ct, areaCode);
        var completed = tasks.Where(t => t.Status == WorkStatus.Completed && t.CompletedAt.HasValue && range.Contains(t.CompletedAt.Value)).ToList();
        var summary = new ReportSummary(events.Count(e => range.Contains(e.CreatedAt)), closed.Count(range.Contains),
            completed.Count(t => t.TaskType == "Visit"), completed.Count(t => t.TaskType == "ServiceOrder"),
            completed.Count(t => t.TaskType == "FollowUp"), completed.Where(t => t.TaskType == "Visit").Select(t => t.ElderId).Distinct().Count(),
            times.Count == 0 ? null : Math.Round(times.Average(), 1),
            tasks.Count(t => t.Status is not WorkStatus.Completed and not WorkStatus.Cancelled), tasks.Count(t => t.IsOverdue));
        var daily = Enumerable.Range(0, range.To.DayNumber - range.From.DayNumber + 1).Select(i =>
        {
            var day = range.From.AddDays(i);
            return new DailyOperations(day, events.Count(e => OperationsDateRange.Day(e.CreatedAt) == day),
                closed.Count(at => OperationsDateRange.Day(at) == day),
                CountDay("Visit"), CountDay("ServiceOrder"), CountDay("FollowUp"));
            int CountDay(string type) => completed.Count(t => t.TaskType == type && OperationsDateRange.Day(t.CompletedAt!.Value) == day);
        }).ToList();
        var people = (await query.PersonnelAsync(actor, ct, areaCode)).Select(p => new PersonnelOperations(
            p.UserId, p.DisplayName, p.Role, p.AreaCode,
            completed.Count(t => t.AssignedUserId == p.UserId && t.TaskType == "Visit"),
            completed.Count(t => t.AssignedUserId == p.UserId && t.TaskType == "ServiceOrder"),
            completed.Count(t => t.AssignedUserId == p.UserId && t.TaskType == "FollowUp"), p.PendingCount, p.OverdueCount)).ToList();
        return new(range.From, range.To, "Asia/Shanghai", clock.GetUtcNow(),
            areaCode ?? (actor.Role == DemoRole.Administrator ? "全部片区" : actor.AreaCode!),
            summary, daily, people);
    }

    public static byte[] ExportCsv(OperationsReport report, string section)
    {
        var rows = new List<string[]>();
        var s = report.Summary;
        rows.Add(["统计开始", "统计结束", "片区", "时区", "生成时间"]);
        rows.Add([report.From.ToString("yyyy-MM-dd"), report.To.ToString("yyyy-MM-dd"), report.AreaLabel,
            report.TimeZone, report.GeneratedAt.ToOffset(OperationsDateRange.Offset).ToString("yyyy-MM-dd HH:mm:ss")]);
        if (section == "summary")
        {
            rows.Add(["新增事件", "结案事件", "完成探访", "完成工单", "完成回访", "探访覆盖人数", "平均首次接单分钟", "当前未结任务", "当前逾期任务"]);
            rows.Add([N(s.NewEventCount), N(s.ClosedEventCount), N(s.CompletedVisitCount), N(s.CompletedOrderCount),
                N(s.CompletedFollowUpCount), N(s.VisitedElderCount),
                s.AverageAcceptanceMinutes?.ToString(CultureInfo.InvariantCulture) ?? "暂无数据",
                N(s.CurrentOpenTaskCount), N(s.CurrentOverdueTaskCount)]);
        }
        else if (section == "daily")
        {
            rows.Add(["日期", "新增事件", "结案事件", "完成探访", "完成工单", "完成回访"]);
            rows.AddRange(report.Daily.Select(d => new[] { d.Date.ToString("yyyy-MM-dd"), N(d.NewEventCount), N(d.ClosedEventCount),
                N(d.CompletedVisitCount), N(d.CompletedOrderCount), N(d.CompletedFollowUpCount) }));
        }
        else if (section == "personnel")
        {
            rows.Add(["人员", "角色", "片区", "完成探访", "完成工单", "完成回访", "当前未结任务", "当前逾期任务"]);
            rows.AddRange(report.Personnel.Select(p => new[] { p.DisplayName,
                p.Role == DemoRole.CommunityStaff ? "社区人员" : "服务人员", p.AreaCode ?? "",
                N(p.CompletedVisitCount), N(p.CompletedOrderCount), N(p.CompletedFollowUpCount), N(p.PendingCount), N(p.OverdueCount) }));
        }
        else throw new ArgumentOutOfRangeException(nameof(section));
        var text = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(EscapeCsv)))) + "\r\n";
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    }

    public static string EscapeCsv(string value)
    {
        var trimmed = value.TrimStart();
        if ((trimmed.Length > 0 && "=+-@＝＋－＠".Contains(trimmed[0])) ||
            value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
}
