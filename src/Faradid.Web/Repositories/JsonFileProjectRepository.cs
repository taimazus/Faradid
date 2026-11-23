using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
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
    }

    private void InitializeSeedProjects()
    {
        if (File.Exists(_projectsFilePath)) return;

        var seedProjects = new List<ProjectProfile>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "راهکار الکترونیک سهند",
                Domain = "irres.ir",
                ClientName = "شرکت راهکار الکترونیک سهند",
                Industry = "طراحی نرم‌افزار سازمانی و هوشمندسازی صنعتی",
                LocalSourcePath = @"F:\Projects\Irres.ir",
                TargetReportPath = @"F:\SEO-Reports\irres",
                Notes = "سامانه مرکزی و اتوماسیون تردد پرسنلی و سیستم‌های مدیریت پسماند",
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
                TargetReportPath = @"F:\SEO-Reports\yonarak",
                Notes = "خدمات صنعتی راپل و ساب سنگ در منطقه شمال‌غرب",
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
                TargetReportPath = @"F:\SEO-Reports\asazist",
                Notes = "قراردادهای بیمارستانی و سلامت محیط زیست",
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
                TargetReportPath = @"F:\SEO-Reports\spinora",
                Notes = "فروشگاه آنلاین محصولات لوکس و استاندارد مادر و کودک",
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
                TargetReportPath = @"F:\SEO-Reports\tbztvrepair",
                Notes = "سرویس فوری در محل و گارانتی تعویض قطعات اصلی",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(seedProjects, JsonOptions);
            File.WriteAllText(_projectsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize seed projects in {Path}", _projectsFilePath);
        }
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
