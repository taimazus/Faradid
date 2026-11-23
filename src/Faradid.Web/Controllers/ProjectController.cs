using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
using Faradid.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Controllers;

[Authorize]
public sealed class ProjectController : Controller
{
    private readonly IProjectRepository _repo;
    private readonly ILocalSourceAuditor _auditor;
    private readonly IGscDataParser _gscParser;
    private readonly IComparativeAnalyticsEngine _comparativeEngine;
    private readonly ICompetitorBenchmarkingEngine _competitorEngine;
    private readonly IAiCopilotService _aiCopilot;
    private readonly IReportExporter _exporter;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectRepository repo,
        ILocalSourceAuditor auditor,
        IGscDataParser gscParser,
        IComparativeAnalyticsEngine comparativeEngine,
        ICompetitorBenchmarkingEngine competitorEngine,
        IAiCopilotService aiCopilot,
        IReportExporter exporter,
        ILogger<ProjectController> logger)
    {
        _repo = repo;
        _auditor = auditor;
        _gscParser = gscParser;
        _comparativeEngine = comparativeEngine;
        _competitorEngine = competitorEngine;
        _aiCopilot = aiCopilot;
        _exporter = exporter;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var allProjects = await _repo.GetAllProjectsAsync();
        var userProjects = UserAccessHelper.FilterProjects(User, allProjects);
        ViewBag.IsAdmin = UserAccessHelper.IsAdmin(User);
        return View(userProjects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProjectProfile());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectProfile model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Domain))
        {
            ModelState.AddModelError("", "نام پروژه و دامنه وب‌سایت الزامی هستند.");
            return View(model);
        }

        // Clean domain
        model.Domain = model.Domain.Trim().ToLowerInvariant().Replace("https://", "").Replace("http://", "").TrimEnd('/');
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;

        // Auto-assign owner to current user
        model.OwnerUsername = User.FindFirst("Username")?.Value ?? User.Identity?.Name ?? "admin";
        model.OwnerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "admin@irres.ir";

        if (string.IsNullOrWhiteSpace(model.TargetReportPath) || model.TargetReportPath.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
        {
            model.TargetReportPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", model.Domain);
        }

        await _repo.SaveProjectAsync(model);
        TempData["SuccessMessage"] = $"پروژه «{model.Name}» با موفقیت ایجاد گردید.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var project = await _repo.GetProjectByIdAsync(id);
        if (project == null) return NotFound();

        if (!UserAccessHelper.CanAccessProject(User, project))
        {
            TempData["ErrorMessage"] = "شما مجوز دسترسی به اطلاعات این پروژه را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var reports = await _repo.GetReportsByProjectIdAsync(id);
        ViewBag.Reports = reports;
        ViewBag.IsAdmin = UserAccessHelper.IsAdmin(User);

        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReport(Guid projectId, string? gscCsvContent = null, string? persona = "Executive")
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!UserAccessHelper.CanAccessProject(User, project))
            {
                TempData["ErrorMessage"] = "شما مجوز صدور یا مشاهده کارنامه این پروژه را ندارید.";
                return RedirectToAction(nameof(Index));
            }

            var activePersona = Enum.TryParse<PersonaView>(persona, true, out var parsedPersona) ? parsedPersona : PersonaView.Executive;

            // 1. Ingest or generate GSC period summaries
            GscPeriodSummary currentGsc;
            if (!string.IsNullOrWhiteSpace(gscCsvContent))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(gscCsvContent));
                currentGsc = await _gscParser.ParseCsvAsync(stream, "ماه جاری (Current Month)");
            }
            else
            {
                currentGsc = _gscParser.CreatePresetData(project.Domain, "دوره ۲۸ روزه اخیر", 28);
            }

            var previousGsc = _gscParser.CreatePresetData(project.Domain, "دوره مشابه پیشین", 28);

            // 2. Perform Codebase Audit if local path provided
            CodebaseAuditResult? codeAudit = null;
            if (!string.IsNullOrWhiteSpace(project.LocalSourcePath))
            {
                codeAudit = await _auditor.AuditPathAsync(project.LocalSourcePath, project.Id);
            }

            // 3. Compute comparative metrics & VPN/Crisis anomalies
            var comparative = _comparativeEngine.ComputeComparison(currentGsc, previousGsc);

            // 4. Benchmark competitors
            var competitors = _competitorEngine.Benchmark(project, currentGsc, Array.Empty<CompetitorProfile>());

            // 5. Generate AI Copilot strategic synthesis & 4 editorial articles
            var aiReport = await _aiCopilot.GenerateInsightsAsync(project, currentGsc, comparative, competitors, codeAudit);

            // 6. Build Master Report
            var masterReport = new FaradidMasterReport
            {
                ReportId = Guid.NewGuid(),
                Project = project,
                ActivePersona = activePersona,
                GeneratedAtUtc = DateTime.UtcNow,
                CurrentPeriod = currentGsc,
                PreviousPeriod = previousGsc,
                Comparative = comparative,
                CodeAudit = codeAudit,
                Competitors = competitors,
                AiSynthesis = aiReport
            };

            // 7. Export HTML safely to target destination
            var targetDir = !string.IsNullOrWhiteSpace(project.TargetReportPath) && !project.TargetReportPath.StartsWith("F:", StringComparison.OrdinalIgnoreCase)
                ? project.TargetReportPath
                : Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", project.Domain);

            var exportFile = await _exporter.ExportHtmlAsync(masterReport, targetDir);
            masterReport.ExportPath = exportFile;

            // 8. Save report in repository
            await _repo.SaveReportAsync(masterReport);

            TempData["SuccessMessage"] = $"کارنامه جامع فَرادید برای {project.Name} با موفقیت صادر گردید.";
            return RedirectToAction("ViewReport", "Report", new { id = masterReport.ReportId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در صدور کارنامه: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var project = await _repo.GetProjectByIdAsync(id);
        if (project != null && !UserAccessHelper.CanAccessProject(User, project))
        {
            TempData["ErrorMessage"] = "شما مجوز حذف این پروژه را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        await _repo.DeleteProjectAsync(id);
        TempData["SuccessMessage"] = "پروژه با موفقیت حذف گردید.";
        return RedirectToAction(nameof(Index));
    }
}
