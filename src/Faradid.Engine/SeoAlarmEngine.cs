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
using Microsoft.Extensions.Logging;

namespace Faradid.Engine;

public sealed class SeoAlarmEngine : ISeoAlarmEngine
{
    private readonly string _storagePath;
    private readonly ILogger<SeoAlarmEngine> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<SeoAlertLog> _cachedAlarms = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SeoAlarmEngine(ILogger<SeoAlarmEngine> logger)
    {
        _logger = logger;
        var appData = Path.Combine(AppContext.BaseDirectory, "App_Data");
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }
        _storagePath = Path.Combine(appData, "seo_alarms.json");
        LoadAlarms();
    }

    private void LoadAlarms()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                var list = JsonSerializer.Deserialize<List<SeoAlertLog>>(json, JsonOptions);
                if (list != null)
                {
                    _cachedAlarms.AddRange(list);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading SEO alarms from storage.");
        }
    }

    private async Task SaveAlarmsAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(_cachedAlarms, JsonOptions);
            await File.WriteAllTextAsync(_storagePath, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<SeoAlertLog>> EvaluateAlarmsAsync(
        ProjectProfile project,
        GscPeriodSummary current,
        GscPeriodSummary? previous,
        CodebaseAuditResult? audit,
        CancellationToken ct = default)
    {
        var newAlarms = new List<SeoAlertLog>();

        // 1. Check Traffic Drops (Clicks / Impressions)
        if (previous != null && previous.TotalClicks > 50)
        {
            var clickChangePct = ((double)(current.TotalClicks - previous.TotalClicks) / previous.TotalClicks) * 100.0;
            if (clickChangePct <= -18.0)
            {
                newAlarms.Add(new SeoAlertLog
                {
                    ProjectId = project.Id,
                    Domain = project.Domain,
                    Title = "هشدار افت محسوس ترافیک ارگانیک ماهانه",
                    Message = $"ترافیک ارگانیک ورودی سایت در دوره اخیر {Math.Abs(clickChangePct):0.1}٪ کاهش داشته است ({current.TotalClicks} کلیک در برابر {previous.TotalClicks} در دوره قبل).",
                    MetricTrigger = $"افت {Math.Abs(clickChangePct):0.1}٪ در کلیک‌ها",
                    RecommendedAction = "بررسی وضعیت ایندکس صفحات اصلی، بررسی اعمال الگوریتم‌های جدید هسته گوگل و پایش ثبات پاسخ‌دهی سرور.",
                    Severity = SeoAlertSeverity.Critical
                });
            }
        }

        // 2. Keyword Rank Drop Check
        if (previous != null && previous.TopQueries.Count > 0)
        {
            foreach (var curQ in current.TopQueries.Take(8))
            {
                var prevQ = previous.TopQueries.FirstOrDefault(q => q.Key.Equals(curQ.Key, StringComparison.OrdinalIgnoreCase));
                if (prevQ != null && curQ.Position - prevQ.Position >= 3.0)
                {
                    newAlarms.Add(new SeoAlertLog
                    {
                        ProjectId = project.Id,
                        Domain = project.Domain,
                        Title = $"هشدار افت رتبه در کوئری کلیدی «{curQ.Key}»",
                        Message = $"رتبه عبارت تجاری «{curQ.Key}» با افت {curQ.Position - prevQ.Position:0.1} پله‌ای از جایگاه {prevQ.Position:0.1} به رتبه {curQ.Position:0.1} تنزل یافته است.",
                        MetricTrigger = $"تنزل {curQ.Position - prevQ.Position:0.1} رتبه در سرپ",
                        RecommendedAction = "بهینه‌سازی تگ عنوان و هدینگ H1، افزایش محتوای عمیق متنی صفحه و دریافت استنادهای جدید رپورتاژی.",
                        Severity = SeoAlertSeverity.Warning
                    });
                }
            }
        }

        // 3. Technical Health Score Alert
        if (audit != null && audit.TechnicalHealthScore < 85)
        {
            newAlarms.Add(new SeoAlertLog
            {
                ProjectId = project.Id,
                Domain = project.Domain,
                Title = "هشدار افت امتیاز سلامت کدهای محلی و زیرساخت",
                Message = $"شاخص ممیزی سلامت سورس‌کد پروژه با کسب امتیاز {audit.TechnicalHealthScore}/100 نیازمند توجه است. {audit.IssuesCount} ایراد معماری ثبت شده است.",
                MetricTrigger = $"نمره فنی {audit.TechnicalHealthScore}/۱۰۰",
                RecommendedAction = "اصلاح قواعد ریدایرکت در web.config، اعتبارسنجی تگ‌های کنونیکال و به‌روزرسانی اسکیماهای ساختاریافته.",
                Severity = SeoAlertSeverity.Warning
            });
        }

        // 4. VPN / DEU Anomaly Detection
        var deuMetric = current.CountryBreakdown.FirstOrDefault(c => c.Key == "DEU" || c.Label.Contains("آلمان") || c.Label.Contains("DEU"));
        if (deuMetric != null && deuMetric.SharePercentage >= 45.0)
        {
            newAlarms.Add(new SeoAlertLog
            {
                ProjectId = project.Id,
                Domain = project.Domain,
                Title = "تشخیص ناهنجاری شدید ترافیک فیلترشکن (IP اروپا/آلمان)",
                Message = $"بیش از {deuMetric.SharePercentage:0.0}٪ از کلیک‌های ثبت‌شده در کنسول با مبدأ سرورهای آلمان/اروپا بوده است که موجب اختلال در سنجش رتبه‌های محلی تبریز می‌شود.",
                MetricTrigger = $"{deuMetric.SharePercentage:0.0}٪ سهم ترافیک DEU",
                RecommendedAction = "تفکیک گزارش رتبه‌بندی کاربران بومی ایران (IRN) در تصمیم‌گیری‌ها و تقویت سیگنال‌های محلی Google Maps.",
                Severity = SeoAlertSeverity.Info
            });
        }

        // Add to cached list avoiding exact duplicates within 24h
        await _lock.WaitAsync(ct);
        try
        {
            foreach (var a in newAlarms)
            {
                var exists = _cachedAlarms.Any(x => x.ProjectId == a.ProjectId && x.Title == a.Title && (DateTime.UtcNow - x.TriggeredAtUtc).TotalHours < 24);
                if (!exists)
                {
                    _cachedAlarms.Insert(0, a);
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        await SaveAlarmsAsync(ct);
        return newAlarms;
    }

    public Task<IReadOnlyList<SeoAlertLog>> GetAlarmsByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var list = _cachedAlarms.Where(a => a.ProjectId == projectId).OrderByDescending(a => a.TriggeredAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<SeoAlertLog>>(list);
    }

    public Task<IReadOnlyList<SeoAlertLog>> GetAllActiveAlarmsAsync(CancellationToken ct = default)
    {
        var list = _cachedAlarms.Where(a => !a.IsAcknowledged).OrderByDescending(a => a.TriggeredAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<SeoAlertLog>>(list);
    }

    public async Task AcknowledgeAlarmAsync(Guid alarmId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var match = _cachedAlarms.FirstOrDefault(a => a.Id == alarmId);
            if (match != null)
            {
                match.IsAcknowledged = true;
            }
        }
        finally
        {
            _lock.Release();
        }

        await SaveAlarmsAsync(ct);
    }
}
