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
