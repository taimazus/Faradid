using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Services;

public sealed class ScheduledReportBackgroundService : BackgroundService, IScheduledReportService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<ScheduledReportBackgroundService> _logger;

    public ScheduledReportBackgroundService(
        IServiceProvider services,
        IConfiguration config,
        ILogger<ScheduledReportBackgroundService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Faradid Automated GSC Sync & Scheduled Report Background Service started.");

        // Initial brief delay to let application start completely
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var autoEnabled = _config.GetValue<bool>("GoogleSearchConsole:AutoGenerateMonthlyReports", true);
                if (autoEnabled)
                {
                    await ExecuteScheduledCycleAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Faradid scheduled report generation cycle.");
            }

            // Run check once every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    public async Task<int> ExecuteScheduledCycleAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var gscClient = scope.ServiceProvider.GetRequiredService<IGscApiClient>();
        var gscParser = scope.ServiceProvider.GetRequiredService<IGscDataParser>();
        var auditor = scope.ServiceProvider.GetRequiredService<ILocalSourceAuditor>();
        var comparativeEngine = scope.ServiceProvider.GetRequiredService<IComparativeAnalyticsEngine>();
        var competitorEngine = scope.ServiceProvider.GetRequiredService<ICompetitorBenchmarkingEngine>();
        var aiCopilot = scope.ServiceProvider.GetRequiredService<IAiCopilotService>();
        var exporter = scope.ServiceProvider.GetRequiredService<IReportExporter>();

        var projects = await repo.GetAllProjectsAsync(ct);
        int generatedCount = 0;

        foreach (var project in projects)
        {
            try
            {
                var reports = await repo.GetReportsByProjectIdAsync(project.Id, ct);
                var latestReport = reports.OrderByDescending(r => r.GeneratedAtUtc).FirstOrDefault();

                // If no report or latest report is older than 7 days, generate an updated one
                bool shouldGenerate = latestReport == null || (DateTime.UtcNow - latestReport.GeneratedAtUtc).TotalDays >= 7;

                if (shouldGenerate)
                {
                    _logger.LogInformation("Auto-generating scheduled report for project {ProjectName} ({Domain})", project.Name, project.Domain);

                    var endDate = DateTime.UtcNow;
                    var startDate = endDate.AddDays(-28);

                    var currentGsc = await gscClient.FetchSearchAnalyticsAsync(project.Domain, startDate, endDate, ct);
                    var previousGsc = gscParser.CreatePresetData(project.Domain, "دوره مشابه پیشین (۲۸ روز قبل)", 28);

                    CodebaseAuditResult? codeAudit = null;
                    if (!string.IsNullOrWhiteSpace(project.LocalSourcePath) && Directory.Exists(project.LocalSourcePath))
                    {
                        codeAudit = await auditor.AuditPathAsync(project.LocalSourcePath, project.Id, ct);
                    }

                    var comparative = comparativeEngine.ComputeComparison(currentGsc, previousGsc);
                    var competitors = competitorEngine.Benchmark(project, currentGsc, Array.Empty<CompetitorProfile>());
                    var aiReport = await aiCopilot.GenerateInsightsAsync(project, currentGsc, comparative, competitors, codeAudit, ct);

                    var masterReport = new FaradidMasterReport
                    {
                        ReportId = Guid.NewGuid(),
                        Project = project,
                        ActivePersona = PersonaView.Comprehensive,
                        GeneratedAtUtc = DateTime.UtcNow,
                        CurrentPeriod = currentGsc,
                        PreviousPeriod = previousGsc,
                        Comparative = comparative,
                        CodeAudit = codeAudit,
                        Competitors = competitors,
                        AiSynthesis = aiReport
                    };

                    var targetDir = !string.IsNullOrWhiteSpace(project.TargetReportPath) && !project.TargetReportPath.StartsWith("F:", StringComparison.OrdinalIgnoreCase)
                        ? (Path.IsPathRooted(project.TargetReportPath) ? project.TargetReportPath : Path.Combine(AppContext.BaseDirectory, project.TargetReportPath))
                        : Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", project.Domain);

                    var exportFile = await exporter.ExportHtmlAsync(masterReport, targetDir, ct);
                    masterReport.ExportPath = exportFile;

                    await repo.SaveReportAsync(masterReport, ct);
                    generatedCount++;
                    _logger.LogInformation("Scheduled report generated successfully for {Domain}. ReportId: {ReportId}", project.Domain, masterReport.ReportId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-generate report for {Domain}", project.Domain);
            }
        }

        return generatedCount;
    }
}
