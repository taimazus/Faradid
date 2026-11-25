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
    public IActionResult Create([FromServices] Microsoft.Extensions.Configuration.IConfiguration config)
    {
        ViewBag.ServiceAccountEmail = config["GoogleSearchConsole:ServiceAccountEmail"] ?? "indexing-bot@still-manifest-501707-f2.iam.gserviceaccount.com";
        return View(new ProjectProfile());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProjectProfile model,
        [FromServices] IGscApiClient gscClient,
        [FromServices] ISiteCrawlerService crawler,
        [FromServices] ILocalRankTrackerService localRankTracker)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Domain))
        {
            ModelState.AddModelError("", "نام پروژه و دامنه وب‌سایت الزامی هستند.");
            return View(model);
        }

        // Clean domain
        model.Domain = model.Domain.Trim().ToLowerInvariant()
            .Replace("https://", "")
            .Replace("http://", "")
            .TrimEnd('/');
        if (model.Domain.StartsWith("www."))
        {
            model.Domain = model.Domain.Substring(4);
        }

        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;

        // Auto-assign owner to current user only if Admin didn't specify a client username
        if (string.IsNullOrWhiteSpace(model.OwnerUsername) || !UserAccessHelper.IsAdmin(User))
        {
            model.OwnerUsername = User.FindFirst("Username")?.Value ?? User.Identity?.Name ?? "admin";
        }
        else
        {
            model.OwnerUsername = model.OwnerUsername.Trim().ToLowerInvariant();
        }
        if (string.IsNullOrWhiteSpace(model.OwnerEmail))
        {
            model.OwnerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "admin@irres.ir";
        }

        if (string.IsNullOrWhiteSpace(model.TargetReportPath) || model.TargetReportPath.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
        {
            model.TargetReportPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", model.Domain);
        }

        await _repo.SaveProjectAsync(model);

        // Immediately initialize real crawl, local SERP, and first live GSC report if accessible
        try
        {
            await crawler.CrawlDomainAsync(model, 10);
            await localRankTracker.RefreshLocalRanksAsync(model);

            if (await gscClient.VerifySiteAccessAsync(model.Domain))
            {
                var report = await BuildAndSaveReportAsync(model, PersonaView.Executive, null, gscClient, localRankTracker);
                TempData["SuccessMessage"] = $"پروژه «{model.Name}» ایجاد و اولین کارنامه واقعی از Google Search Console صادر شد.";
                return RedirectToAction("ViewReport", "Report", new { id = report.ReportId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial auto-sync failed for newly created project {Domain}", model.Domain);
        }

        TempData["SuccessMessage"] = $"پروژه «{model.Name}» با موفقیت ایجاد گردید.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        [FromServices] IGscApiClient gscClient,
        [FromServices] ILocalRankTrackerService localRankTracker,
        [FromServices] ISiteCrawlerService crawler,
        [FromServices] ISeoAlarmEngine alarmEngine,
        [FromServices] Microsoft.Extensions.Configuration.IConfiguration config)
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
        ViewBag.LocalRanks = await localRankTracker.GetOrUpdateLocalRanksAsync(project);
        ViewBag.CrawlResult = await crawler.GetLatestCrawlResultAsync(id);
        ViewBag.ActiveAlarms = await alarmEngine.GetAlarmsByProjectAsync(id);
        ViewBag.IsGscConnected = await gscClient.VerifySiteAccessAsync(project.Domain);
        ViewBag.GscDiagnostic = await gscClient.GetDiagnosticStatusAsync(project.Domain);
        ViewBag.ServiceAccountEmail = config["GoogleSearchConsole:ServiceAccountEmail"] ?? "indexing-bot@still-manifest-501707-f2.iam.gserviceaccount.com";

        return View(project);
    }

    [HttpGet]
    public IActionResult GenerateReport(Guid? projectId)
    {
        if (projectId.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = projectId.Value });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReport(
        Guid projectId,
        [FromServices] IGscApiClient gscClient,
        [FromServices] ILocalRankTrackerService localRankTracker,
        [FromServices] ISiteCrawlerService crawler,
        string? gscCsvContent = null,
        string? persona = "Executive")
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

            // Ensure at least one crawl snapshot exists for real on-page diagnostics
            if (await crawler.GetLatestCrawlResultAsync(project.Id) == null)
            {
                await crawler.CrawlDomainAsync(project, 10);
            }

            var masterReport = await BuildAndSaveReportAsync(project, activePersona, gscCsvContent, gscClient, localRankTracker);

            TempData["SuccessMessage"] = masterReport.CurrentPeriod.IsLiveData
                ? $"کارنامه واقعی فَرادید برای {project.Name} از داده‌های زنده سرچ کنسول گوگل صادر گردید."
                : $"کارنامه جامع فَرادید برای {project.Name} با موفقیت صادر گردید.";
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
    public async Task<IActionResult> SyncGscApi(
        Guid projectId,
        [FromServices] IGscApiClient gscClient,
        [FromServices] ILocalRankTrackerService localRankTracker)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!UserAccessHelper.CanAccessProject(User, project))
            {
                TempData["ErrorMessage"] = "شما مجوز همگام‌سازی این پروژه را ندارید.";
                return RedirectToAction(nameof(Index));
            }

            var masterReport = await BuildAndSaveReportAsync(project, PersonaView.Comprehensive, null, gscClient, localRankTracker);

            if (masterReport.CurrentPeriod.IsLiveData)
            {
                TempData["SuccessMessage"] = $"همگام‌سازی زنده با سرچ کنسول گوگل ({masterReport.CurrentPeriod.ResolvedPropertyUrl}) انجام شد و کارنامه واقعی صادر گردید.";
            }
            else
            {
                TempData["ErrorMessage"] = $"دامنه {project.Domain} هنوز در سرچ کنسول به Service Account دسترسی ندارد؛ کارنامه بر اساس داده‌های برآوردی موضوعی صادر شد.";
            }

            return RedirectToAction("ViewReport", "Report", new { id = masterReport.ReportId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing GSC API for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در اتصال به API سرچ کنسول گوگل: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }

    private async Task<FaradidMasterReport> BuildAndSaveReportAsync(
        ProjectProfile project,
        PersonaView activePersona,
        string? gscCsvContent,
        IGscApiClient gscClient,
        ILocalRankTrackerService localRankTracker)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-28);

        // 1. Ingest live GSC API data (for BOTH current & previous 28-day periods) or CSV
        GscPeriodSummary currentGsc;
        GscPeriodSummary previousGsc;

        if (!string.IsNullOrWhiteSpace(gscCsvContent))
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(gscCsvContent));
            currentGsc = await _gscParser.ParseCsvAsync(stream, "ماه جاری (CSV)");
            currentGsc.IsLiveData = true;
            currentGsc.DataSource = "فایل CSV بارگذاری‌شده از سرچ کنسول";
            previousGsc = await gscClient.FetchSearchAnalyticsAsync(project.Domain, startDate.AddDays(-28), startDate);
        }
        else
        {
            currentGsc = await gscClient.FetchSearchAnalyticsAsync(project.Domain, startDate, endDate);
            previousGsc = await gscClient.FetchSearchAnalyticsAsync(project.Domain, startDate.AddDays(-28), startDate);
        }

        // 2. Perform Codebase Audit if local path provided and exists on this host
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
            ? (Path.IsPathRooted(project.TargetReportPath) ? project.TargetReportPath : Path.Combine(AppContext.BaseDirectory, project.TargetReportPath))
            : Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", project.Domain);

        var exportFile = await _exporter.ExportHtmlAsync(masterReport, targetDir);
        masterReport.ExportPath = exportFile;

        // 8. Save report in repository
        await _repo.SaveReportAsync(masterReport);

        // 9. Keep Local SERP synced and evaluate SEO alarms
        await localRankTracker.RefreshLocalRanksAsync(project);

        var alarmEngine = HttpContext?.RequestServices.GetService<ISeoAlarmEngine>();
        if (alarmEngine != null)
        {
            await alarmEngine.EvaluateAlarmsAsync(project, currentGsc, previousGsc, codeAudit);
        }

        return masterReport;
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

    [HttpPost]
    public async Task<IActionResult> GenerateArticleDraft(Guid projectId, int articleOrder, [FromServices] IAiArticleWriter writer)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            var reports = await _repo.GetReportsByProjectIdAsync(projectId);
            var latestReport = reports.OrderByDescending(r => r.GeneratedAtUtc).FirstOrDefault();

            EditorialArticleRecommendation articlePlan;
            var recommended = latestReport?.AiSynthesis?.EditorialCalendar;
            if (recommended != null && articleOrder >= 1 && articleOrder <= recommended.Count)
            {
                articlePlan = recommended[articleOrder - 1];
            }
            else
            {
                articlePlan = new EditorialArticleRecommendation
                {
                    Title = $"راهنمای جامع و کاربردی {project.Industry} در تبریز",
                    TargetKeyword = project.Industry,
                    UserSearchIntent = "تراکنشی و تجاری (Commercial / Transactional)"
                };
            }

            var draft = await writer.GenerateFullArticleAsync(project, articlePlan);
            return Json(new { success = true, draft });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI article draft for project {ProjectId}", projectId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AcknowledgeAlarm(Guid alarmId, [FromServices] ISeoAlarmEngine alarmEngine)
    {
        try
        {
            await alarmEngine.AcknowledgeAlarmAsync(alarmId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alarm {AlarmId}", alarmId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunSiteCrawl(Guid projectId, [FromServices] ISiteCrawlerService crawler)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!UserAccessHelper.CanAccessProject(User, project))
            {
                TempData["ErrorMessage"] = "شما مجوز اجرای خزش این پروژه را ندارید.";
                return RedirectToAction(nameof(Index));
            }

            var crawlResult = await crawler.CrawlDomainAsync(project, 20);
            TempData["SuccessMessage"] = $"خزش آنلاین وب‌سایت با موفقیت انجام شد. نمره سلامت آن‌پیج: {crawlResult.OverallHealthScore}/۱۰۰ ({crawlResult.TotalPagesCrawled} صفحه پایش شد).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running site crawl for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در خزش آنلاین: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLocalKeyword(Guid projectId, string keyword, string? city, [FromServices] ILocalRankTrackerService tracker)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                await tracker.AddCustomKeywordAsync(projectId, keyword.Trim(), string.IsNullOrWhiteSpace(city) ? "تبریز" : city.Trim());
                TempData["SuccessMessage"] = $"کلیدواژه محلی «{keyword}» به ردیاب رتبه‌های تبریز افزوده شد.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding local keyword for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در افزودن کلیدواژه محلی: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleClientPortal(Guid projectId)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!UserAccessHelper.CanAccessProject(User, project))
            {
                TempData["ErrorMessage"] = "شما مجوز تغییر تنظیمات دسترسی این پروژه را ندارید.";
                return RedirectToAction(nameof(Index));
            }

            project.IsClientPortalEnabled = !project.IsClientPortalEnabled;
            project.UpdatedAtUtc = DateTime.UtcNow;
            await _repo.SaveProjectAsync(project);

            TempData["SuccessMessage"] = project.IsClientPortalEnabled
                ? "دسترسی به پورتال کارفرما با موفقیت فعال گردید."
                : "پورتال کارفرما با موفقیت غیرفعال و دسترسی عمومی به آن مسدود شد.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling client portal for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در تغییر وضعیت پورتال: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateClientPortalToken(Guid projectId)
    {
        try
        {
            var project = await _repo.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            if (!UserAccessHelper.CanAccessProject(User, project))
            {
                TempData["ErrorMessage"] = "شما مجوز تغییر تنظیمات دسترسی این پروژه را ندارید.";
                return RedirectToAction(nameof(Index));
            }

            project.ClientPortalToken = Guid.NewGuid().ToString("N");
            project.UpdatedAtUtc = DateTime.UtcNow;
            await _repo.SaveProjectAsync(project);

            TempData["SuccessMessage"] = "توکن امنیتی پورتال بازنشانی شد. کلیه لینک‌های ارسالی قبلی باطل شدند.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating client portal token for project {ProjectId}", projectId);
            TempData["ErrorMessage"] = $"خطا در بازنشانی توکن: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = projectId });
    }
}
