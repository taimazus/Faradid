using System;
using System.Collections.Generic;
using System.Linq;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;

namespace Faradid.Engine;

public sealed class ComparativeAnalyticsEngine : IComparativeAnalyticsEngine
{
    public ComparativeMetrics ComputeComparison(GscPeriodSummary current, GscPeriodSummary? previous)
    {
        var metrics = new ComparativeMetrics();

        // 1. Calculate Core Metric Deltas
        if (previous != null && previous.TotalClicks > 0)
        {
            metrics.ClicksDelta = MetricDelta.Create(current.TotalClicks, previous.TotalClicks, higherIsBetter: true);
            metrics.ImpressionsDelta = MetricDelta.Create(current.TotalImpressions, previous.TotalImpressions, higherIsBetter: true);
            metrics.CtrDelta = MetricDelta.Create(current.AverageCtr * 100.0, previous.AverageCtr * 100.0, higherIsBetter: true);
            metrics.PositionDelta = MetricDelta.Create(current.AveragePosition, previous.AveragePosition, higherIsBetter: false); // Lower position rank is better
        }
        else
        {
            metrics.ClicksDelta = MetricDelta.Create(current.TotalClicks, 0, higherIsBetter: true);
            metrics.ImpressionsDelta = MetricDelta.Create(current.TotalImpressions, 0, higherIsBetter: true);
            metrics.CtrDelta = MetricDelta.Create(current.AverageCtr * 100.0, 0, higherIsBetter: true);
            metrics.PositionDelta = MetricDelta.Create(current.AveragePosition, 0, higherIsBetter: false);
        }

        // 2. Compute Geolocation & VPN Distribution
        if (current.CountryBreakdown.Count > 0)
        {
            var totalCountryClicks = current.CountryBreakdown.Sum(c => c.Clicks);
            if (totalCountryClicks > 0)
            {
                var germanyClicks = current.CountryBreakdown.FirstOrDefault(c => c.Key.Equals("DEU", StringComparison.OrdinalIgnoreCase))?.Clicks ?? 0;
                var iranClicks = current.CountryBreakdown.FirstOrDefault(c => c.Key.Equals("IRN", StringComparison.OrdinalIgnoreCase))?.Clicks ?? 0;

                metrics.GermanyVpnSharePercent = Math.Round((double)germanyClicks / totalCountryClicks * 100.0, 1);
                metrics.IranDirectSharePercent = Math.Round((double)iranClicks / totalCountryClicks * 100.0, 1);
            }
        }

        // 3. Compute Device Split
        if (current.DeviceBreakdown.Count > 0)
        {
            var totalDeviceClicks = current.DeviceBreakdown.Sum(d => d.Clicks);
            if (totalDeviceClicks > 0)
            {
                var mobClicks = current.DeviceBreakdown.FirstOrDefault(d => d.Key.Equals("MOBILE", StringComparison.OrdinalIgnoreCase))?.Clicks ?? 0;
                var deskClicks = current.DeviceBreakdown.FirstOrDefault(d => d.Key.Equals("DESKTOP", StringComparison.OrdinalIgnoreCase))?.Clicks ?? 0;

                metrics.MobileTrafficSharePercent = Math.Round((double)mobClicks / totalDeviceClicks * 100.0, 1);
                metrics.DesktopTrafficSharePercent = Math.Round((double)deskClicks / totalDeviceClicks * 100.0, 1);
            }
        }

        // 4. Anomaly Detection & Crisis Impact
        DetectTrafficAnomalies(current, metrics);

        return metrics;
    }

    private static void DetectTrafficAnomalies(GscPeriodSummary current, ComparativeMetrics metrics)
    {
        if (current.DailyMetrics.Count == 0) return;

        // Group daily metrics by date
        var dailyTotals = current.DailyMetrics
            .GroupBy(d => d.Date.Date)
            .Select(g => new { Date = g.Key, Clicks = g.Sum(x => x.Clicks), Impressions = g.Sum(x => x.Impressions) })
            .OrderBy(d => d.Date)
            .ToList();

        if (dailyTotals.Count < 5) return;

        double baselineClicks = dailyTotals.Average(d => d.Clicks);

        foreach (var day in dailyTotals)
        {
            // If clicks dropped more than 40% below baseline
            if (baselineClicks > 10 && day.Clicks < (baselineClicks * 0.55))
            {
                long lost = Math.Max(0, (long)(baselineClicks - day.Clicks));
                metrics.Anomalies.Add(new TrafficAnomalyEvent
                {
                    Date = day.Date,
                    Title = "افت مقطعی شدید ناشی از اختلال اینترنت بین‌الملل",
                    Description = $"در تاریخ {day.Date:yyyy-MM-dd} حجم کلیک‌ها با افت {Math.Round((1.0 - (double)day.Clicks / baselineClicks) * 100)} درصدی مواجه شد که ناشی از اختلالات سراسری شبکه و فیلترینگ گسترده بوده است.",
                    AnomalyType = "InternetBlackout",
                    Severity = AuditSeverity.High,
                    EstimatedLostClicks = lost
                });
            }
        }

        // Check VPN anomaly
        if (metrics.GermanyVpnSharePercent > 35.0)
        {
            metrics.Anomalies.Add(new TrafficAnomalyEvent
            {
                Date = DateTime.UtcNow.Date,
                Title = "پدیده ترافیک ارجاعی فیلترشکن (VPN Anomaly)",
                Description = $"سهم {metrics.GermanyVpnSharePercent} درصدی ترافیک از مبدأ کشور آلمان (DEU) نشان‌دهنده استفاده اکثریت کاربران ایرانی از ابزارهای ضد فیلترینگ و سرورهای پروکسی اروپایی است و مخاطب واقعی کاملاً بومی است.",
                AnomalyType = "VpnTrafficShift",
                Severity = AuditSeverity.Medium,
                EstimatedLostClicks = 0
            });
        }

        metrics.HasSignificantAnomaly = metrics.Anomalies.Count > 0;
        if (metrics.HasSignificantAnomaly)
        {
            long totalLostClicks = metrics.Anomalies.Sum(a => a.EstimatedLostClicks);
            metrics.AnomalyExecutiveSummary = $"در طول این دوره، تعداد {metrics.Anomalies.Count} رویداد غیرعادی و اختلال شبکه‌ای شناسایی شد. برآورد مدل حاکی از دست رفتن حداقل {totalLostClicks:N0} کلیک ارگانیک به دلیل افت پایداری پروتکل‌های اینترنتی است.";
        }
    }
}
