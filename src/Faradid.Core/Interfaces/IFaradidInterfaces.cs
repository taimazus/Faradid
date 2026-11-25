using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Models;

namespace Faradid.Core.Interfaces;

public interface IGscDataParser
{
    Task<GscPeriodSummary> ParseCsvAsync(Stream csvStream, string periodName, CancellationToken ct = default);
    Task<GscPeriodSummary> ParseJsonAsync(string jsonContent, string periodName, CancellationToken ct = default);
    GscPeriodSummary CreatePresetData(string domain, string periodName, int days = 28);
}

public interface ILocalSourceAuditor
{
    Task<CodebaseAuditResult> AuditPathAsync(string rootPath, Guid projectId, CancellationToken ct = default);
}

public interface IComparativeAnalyticsEngine
{
    ComparativeMetrics ComputeComparison(GscPeriodSummary current, GscPeriodSummary? previous);
}

public interface ICompetitorBenchmarkingEngine
{
    CompetitorBenchmarkReport Benchmark(ProjectProfile project, GscPeriodSummary currentGsc, IEnumerable<CompetitorProfile> competitors);
}

public interface IAiCopilotService
{
    Task<AiStrategicReport> GenerateInsightsAsync(
        ProjectProfile project,
        GscPeriodSummary currentGsc,
        ComparativeMetrics comparative,
        CompetitorBenchmarkReport competitors,
        CodebaseAuditResult? auditResult,
        CancellationToken ct = default);
}

public interface IReportExporter
{
    Task<string> ExportHtmlAsync(FaradidMasterReport report, string destinationPath, CancellationToken ct = default);
}

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectProfile>> GetAllProjectsAsync(CancellationToken ct = default);
    Task<ProjectProfile?> GetProjectByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveProjectAsync(ProjectProfile project, CancellationToken ct = default);
    Task DeleteProjectAsync(Guid id, CancellationToken ct = default);

    Task SaveReportAsync(FaradidMasterReport report, CancellationToken ct = default);
    Task<FaradidMasterReport?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default);
    Task<IReadOnlyList<FaradidMasterReport>> GetReportsByProjectIdAsync(Guid projectId, CancellationToken ct = default);
}

public interface IGscApiClient
{
    Task<GscPeriodSummary> FetchSearchAnalyticsAsync(string siteUrl, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<bool> VerifySiteAccessAsync(string siteUrl, CancellationToken ct = default);
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);
}

public interface IScheduledReportService
{
    Task<int> ExecuteScheduledCycleAsync(CancellationToken ct = default);
}

public interface ISeoAlarmEngine
{
    Task<IReadOnlyList<SeoAlertLog>> EvaluateAlarmsAsync(
        ProjectProfile project,
        GscPeriodSummary current,
        GscPeriodSummary? previous,
        CodebaseAuditResult? audit,
        CancellationToken ct = default);

    Task<IReadOnlyList<SeoAlertLog>> GetAlarmsByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<SeoAlertLog>> GetAllActiveAlarmsAsync(CancellationToken ct = default);
    Task AcknowledgeAlarmAsync(Guid alarmId, CancellationToken ct = default);
}

public interface IAiArticleWriter
{
    Task<GeneratedArticleDraft> GenerateFullArticleAsync(
        ProjectProfile project,
        EditorialArticleRecommendation articlePlan,
        CancellationToken ct = default);
}

public interface ISiteCrawlerService
{
    Task<SiteCrawlResult> CrawlDomainAsync(ProjectProfile project, int maxPages = 20, CancellationToken ct = default);
    Task<SiteCrawlResult?> GetLatestCrawlResultAsync(Guid projectId, CancellationToken ct = default);
}

public interface ILocalRankTrackerService
{
    Task<LocalRankTrackingSummary> GetOrUpdateLocalRanksAsync(ProjectProfile project, CancellationToken ct = default);
    Task<LocalRankTrackingSummary> RefreshLocalRanksAsync(ProjectProfile project, CancellationToken ct = default);
    Task<LocalRankTrackingSummary?> GetLocalRanksAsync(Guid projectId, CancellationToken ct = default);
    Task AddCustomKeywordAsync(Guid projectId, string keyword, string city = "ایران", CancellationToken ct = default);
    Task RemoveCustomKeywordAsync(Guid projectId, string keyword, CancellationToken ct = default);
}
