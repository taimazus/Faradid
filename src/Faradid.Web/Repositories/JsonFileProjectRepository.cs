using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Faradid.AI;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
using Faradid.Engine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Repositories;

public sealed class JsonFileProjectRepository : IProjectRepository
{
    private readonly string _projectsFilePath;
    private readonly string _reportsDirectory;
    private readonly ILogger<JsonFileProjectRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonFileProjectRepository(IWebHostEnvironment env, ILogger<JsonFileProjectRepository> logger)
    {
        _logger = logger;
        var appData = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }

        _projectsFilePath = Path.Combine(appData, "projects.json");
        _reportsDirectory = Path.Combine(appData, "reports");

        if (!Directory.Exists(_reportsDirectory))
        {
            Directory.CreateDirectory(_reportsDirectory);
        }

        InitializeSeedProjects();
        InitializeSeedReports();
    }

    private void InitializeSeedProjects()
    {
        try
        {
            var seedProjects = GetInitialProjectList();

            if (!File.Exists(_projectsFilePath))
            {
                var json = JsonSerializer.Serialize(seedProjects, JsonOptions);
                File.WriteAllText(_projectsFilePath, json);
                return;
            }

            // If file exists, ensure all initial projects have correct owners
            var existingJson = File.ReadAllText(_projectsFilePath);
            var existingList = JsonSerializer.Deserialize<List<ProjectProfile>>(existingJson, JsonOptions) ?? new List<ProjectProfile>();
            var modified = false;

            foreach (var seed in seedProjects)
            {
                var match = existingList.FirstOrDefault(p => p.Id == seed.Id || p.Domain.Equals(seed.Domain, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    existingList.Add(seed);
                    modified = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(match.OwnerUsername))
                    {
                        match.OwnerUsername = seed.OwnerUsername;
                        match.OwnerEmail = seed.OwnerEmail;
                        modified = true;
                    }
                    if (string.IsNullOrWhiteSpace(match.TargetReportPath) || match.TargetReportPath.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
                    {
                        match.TargetReportPath = seed.TargetReportPath;
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                File.WriteAllText(_projectsFilePath, JsonSerializer.Serialize(existingList, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize or update seed projects in {Path}", _projectsFilePath);
        }
    }

    private void InitializeSeedReports()
    {
        try
        {
            var existingFiles = Directory.GetFiles(_reportsDirectory, "*.json");
            if (existingFiles.Length > 0) return;

            var projects = GetInitialProjectList();
            var gscParser = new GscDataParser();
            var compEngine = new ComparativeAnalyticsEngine();
            var compBench = new CompetitorBenchmarkingEngine();
            var copilot = new FaradidAiCopilot();

            foreach (var proj in projects)
            {
                var currentGsc = gscParser.CreatePresetData(proj.Domain, "دوره ۲۸ روزه اخیر", 28);
                var prevGsc = gscParser.CreatePresetData(proj.Domain, "دوره مشابه پیشین", 28);
                var comparative = compEngine.ComputeComparison(currentGsc, prevGsc);
                var competitors = compBench.Benchmark(proj, currentGsc, Array.Empty<CompetitorProfile>());
                
                // Pre-generate standard code audit items
                var audit = new CodebaseAuditResult
                {
                    ProjectId = proj.Id,
                    AnalyzedPath = proj.LocalSourcePath ?? string.Empty,
                    TotalRulesEvaluated = 7,
                    PassedChecksCount = 6,
                    IssuesCount = 1,
                    CriticalIssuesCount = 0,
                    TechnicalHealthScore = 92,
                    Items = new List<CodebaseAuditItem>
                    {
                        new() { Category = "IIS / Web.config", RuleName = "پیکربندی خطاهای سفارشی HTTP (Custom Errors)", Severity = AuditSeverity.High, Passed = true, Description = "تنظیمات اختصاصی صفحات ۴۰۴ و ۵۰۰ رعایت شده است.", Recommendation = "تداوم مانیتورینگ لاگ‌های IIS." },
                        new() { Category = "امنیت و ریدایرکت", RuleName = "ریدایرکت ۳۰۱ اجباری به HTTPS", Severity = AuditSeverity.High, Passed = true, Description = "قانون URL Rewrite با کد ۳۰۱ فعال است.", Recommendation = "اطمینان از فعال بودن HSTS." },
                        new() { Category = "بهینه‌سازی موبایل", RuleName = "تگ متای Viewport در لایوت", Severity = AuditSeverity.Critical, Passed = true, Description = "واکنش‌گرایی در کلیه دستگاه‌ها برقرار است.", Recommendation = "حفظ ساختار ریسپانسیو." },
                        new() { Category = "تگ‌های کانونیکال", RuleName = "لینک کانونیکال یکتا", Severity = AuditSeverity.High, Passed = true, Description = "تگ کانونیکال جهت جلوگیری از محتوای تکراری تزریق شده است.", Recommendation = "بررسی پارامترهای فیلتر در URL." },
                        new() { Category = "اسکیما و میکرودیتا", RuleName = "اسکیمای JSON-LD سازمانی", Severity = AuditSeverity.Medium, Passed = true, Description = "اسکریپت‌های سازمان و وب‌سایت در هدر مستقر هستند.", Recommendation = "افزودن اسکیمای FAQ به صفحات خدمات." },
                        new() { Category = "خزنده‌ها و ربات‌ها", RuleName = "فایل robots.txt و نقشه سایت", Severity = AuditSeverity.Medium, Passed = true, Description = "دستور Sitemap در انتهای robots.txt ثبت شده است.", Recommendation = "به‌روزرسانی پویای نقشه سایت." },
                        new() { Category = "کش مرورگر", RuleName = "تنظیم هدر Cache-Control فایل‌های استاتیک", Severity = AuditSeverity.Low, Passed = false, Description = "کش ۳۰ روزه برای برخی از وب‌فونت‌ها صراحتاً در هدر تعیین نشده است.", Recommendation = "افزودن تگ clientCache در web.config." }
                    }
                };

                var aiReport = copilot.GenerateInsightsAsync(proj, currentGsc, comparative, competitors, audit).GetAwaiter().GetResult();

                var masterReport = new FaradidMasterReport
                {
                    ReportId = Guid.NewGuid(),
                    Project = proj,
                    ActivePersona = PersonaView.Executive,
                    GeneratedAtUtc = DateTime.UtcNow.AddHours(-1),
                    CurrentPeriod = currentGsc,
                    PreviousPeriod = prevGsc,
                    Comparative = comparative,
                    CodeAudit = audit,
                    Competitors = competitors,
                    AiSynthesis = aiReport,
                    ExportPath = Path.Combine(proj.TargetReportPath ?? "App_Data/exports", $"Faradid-Report-{proj.Domain}.html")
                };

                var reportFile = Path.Combine(_reportsDirectory, $"{masterReport.ReportId}.json");
                File.WriteAllText(reportFile, JsonSerializer.Serialize(masterReport, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize seed reports in {Path}", _reportsDirectory);
        }
    }

    private static List<ProjectProfile> GetInitialProjectList()
    {
        return new List<ProjectProfile>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "راهکار الکترونیک سهند",
                Domain = "irres.ir",
                ClientName = "شرکت راهکار الکترونیک سهند",
                Industry = "طراحی نرم‌افزار سازمانی و هوشمندسازی صنعتی",
                LocalSourcePath = @"F:\Projects\Irres.ir",
                TargetReportPath = @"App_Data/exports/irres",
                Notes = "سامانه مرکزی و اتوماسیون تردد پرسنلی و سیستم‌های مدیریت پسماند",
                OwnerUsername = "irres",
                OwnerEmail = "info@irres.ir",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-60),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "یونارک (Yonarak)",
                Domain = "yonarak.ir",
                ClientName = "گروه مهندسی و خدماتی یونارک",
                Industry = "خدمات کفسابی، نماشویی و پیچ‌رولپلاک با طناب",
                LocalSourcePath = @"F:\Projects\yonarak.ir",
                TargetReportPath = @"App_Data/exports/yonarak",
                Notes = "خدمات صنعتی راپل و ساب سنگ در منطقه شمال‌غرب",
                OwnerUsername = "yonarak",
                OwnerEmail = "manager@yonarak.ir",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-45),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "آسا زیست تبریز",
                Domain = "asazist.com",
                ClientName = "شرکت مدیریت پسماند آسا زیست",
                Industry = "بی‌خطرسازی و امحای پسماندهای عفونی و پزشکی",
                LocalSourcePath = @"F:\Projects\Asazist",
                TargetReportPath = @"App_Data/exports/asazist",
                Notes = "قراردادهای بیمارستانی و سلامت محیط زیست",
                OwnerUsername = "asazist",
                OwnerEmail = "info@asazist.com",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "اسپینورا (Spinora)",
                Domain = "spinora.ir",
                ClientName = "مجموعه سیسمونی و کالای نوزاد اسپینورا",
                Industry = "سیسمونی نوزاد، کالسکه و صندلی خودرو کودک",
                LocalSourcePath = @"F:\Projects\Spinora.ir",
                TargetReportPath = @"App_Data/exports/spinora",
                Notes = "فروشگاه آنلاین محصولات لوکس و استاندارد مادر و کودک",
                OwnerUsername = "spinora",
                OwnerEmail = "support@spinora.ir",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "تعمیر تلویزیون تبریز",
                Domain = "tbztvrepair.ir",
                ClientName = "مرکز تخصصی تعمیرات صوتی تصویری تبریز",
                Industry = "تعمیر تخصصی بک‌لایت و پنل تلویزیون",
                LocalSourcePath = @"F:\Projects\TabrizTv",
                TargetReportPath = @"App_Data/exports/tbztvrepair",
                Notes = "سرویس فوری در محل و گارانتی تعویض قطعات اصلی",
                OwnerUsername = "tbztv",
                OwnerEmail = "service@tbztvrepair.ir",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
    }

    public async Task<IReadOnlyList<ProjectProfile>> GetAllProjectsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_projectsFilePath)) return Array.Empty<ProjectProfile>();
            var json = await File.ReadAllTextAsync(_projectsFilePath, ct);
            return JsonSerializer.Deserialize<List<ProjectProfile>>(json, JsonOptions) ?? new List<ProjectProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read projects from {Path}", _projectsFilePath);
            return Array.Empty<ProjectProfile>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProjectProfile?> GetProjectByIdAsync(Guid id, CancellationToken ct = default)
    {
        var all = await GetAllProjectsAsync(ct);
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task SaveProjectAsync(ProjectProfile project, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var list = new List<ProjectProfile>();
            if (File.Exists(_projectsFilePath))
            {
                var currentJson = await File.ReadAllTextAsync(_projectsFilePath, ct);
                list = JsonSerializer.Deserialize<List<ProjectProfile>>(currentJson, JsonOptions) ?? new List<ProjectProfile>();
            }

            var existingIndex = list.FindIndex(p => p.Id == project.Id);
            if (existingIndex >= 0)
            {
                project.UpdatedAtUtc = DateTime.UtcNow;
                list[existingIndex] = project;
            }
            else
            {
                list.Add(project);
            }

            var updatedJson = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(_projectsFilePath, updatedJson, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_projectsFilePath)) return;
            var currentJson = await File.ReadAllTextAsync(_projectsFilePath, ct);
            var list = JsonSerializer.Deserialize<List<ProjectProfile>>(currentJson, JsonOptions) ?? new List<ProjectProfile>();
            list.RemoveAll(p => p.Id == id);
            var updatedJson = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(_projectsFilePath, updatedJson, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveReportAsync(FaradidMasterReport report, CancellationToken ct = default)
    {
        var file = Path.Combine(_reportsDirectory, $"{report.ReportId}.json");
        var json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(file, json, ct);
    }

    public async Task<FaradidMasterReport?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default)
    {
        var file = Path.Combine(_reportsDirectory, $"{reportId}.json");
        if (!File.Exists(file)) return null;

        var json = await File.ReadAllTextAsync(file, ct);
        return JsonSerializer.Deserialize<FaradidMasterReport>(json, JsonOptions);
    }

    public Task<IReadOnlyList<FaradidMasterReport>> GetReportsByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var list = new List<FaradidMasterReport>();
        var files = Directory.GetFiles(_reportsDirectory, "*.json");

        foreach (var f in files)
        {
            try
            {
                var json = File.ReadAllText(f);
                var r = JsonSerializer.Deserialize<FaradidMasterReport>(json, JsonOptions);
                if (r != null && r.Project?.Id == projectId)
                {
                    list.Add(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse report file {File}", f);
            }
        }

        return Task.FromResult<IReadOnlyList<FaradidMasterReport>>(list.OrderByDescending(r => r.GeneratedAtUtc).ToList());
    }
}
