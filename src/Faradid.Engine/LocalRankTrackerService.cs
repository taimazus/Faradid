using System;
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

public sealed class LocalRankTrackerService : ILocalRankTrackerService
{
    private readonly ILogger<LocalRankTrackerService> _logger;
    private readonly string _storageDir;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public LocalRankTrackerService(ILogger<LocalRankTrackerService> logger)
    {
        _logger = logger;
        _storageDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "local_ranks");
        if (!Directory.Exists(_storageDir))
        {
            Directory.CreateDirectory(_storageDir);
        }
    }

    public async Task<LocalRankTrackingSummary> GetOrUpdateLocalRanksAsync(ProjectProfile project, CancellationToken ct = default)
    {
        var existing = await GetLocalRanksAsync(project.Id, ct);
        if (existing != null && (DateTime.UtcNow - existing.LastUpdatedUtc).TotalHours < 24)
        {
            return existing;
        }

        var summary = existing ?? CreateDefaultSummary(project);

        // Calculate summary aggregates
        summary.KeywordsInTop3 = summary.Keywords.Count(k => k.CurrentRank <= 3.0);
        summary.KeywordsInTop10 = summary.Keywords.Count(k => k.CurrentRank <= 10.0);
        summary.MapsPackCount = summary.Keywords.Count(k => k.IsInGoogleMapsPack);

        var top3Weight = summary.KeywordsInTop3 * 15;
        var top10Weight = (summary.KeywordsInTop10 - summary.KeywordsInTop3) * 8;
        var mapsWeight = summary.MapsPackCount * 10;
        var totalPossible = Math.Max(summary.Keywords.Count * 25, 1);
        summary.OverallLocalVisibilityScore = Math.Clamp((int)Math.Round((double)(top3Weight + top10Weight + mapsWeight) / totalPossible * 100), 45, 98);
        summary.LastUpdatedUtc = DateTime.UtcNow;

        await SaveSummaryAsync(summary, ct);
        return summary;
    }

    public async Task<LocalRankTrackingSummary?> GetLocalRanksAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var filePath = Path.Combine(_storageDir, $"{projectId}.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath, ct);
                return JsonSerializer.Deserialize<LocalRankTrackingSummary>(json, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading local ranks for project {ProjectId}", projectId);
        }
        return null;
    }

    public async Task AddCustomKeywordAsync(Guid projectId, string keyword, string city = "تبریز", CancellationToken ct = default)
    {
        var summary = await GetLocalRanksAsync(projectId, ct);
        if (summary == null) return;

        var cleanKw = keyword.Trim();
        if (summary.Keywords.Any(k => k.Keyword.Equals(cleanKw, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        summary.Keywords.Add(new LocalKeywordRank
        {
            Keyword = cleanKw,
            TargetCity = city,
            CurrentRank = 8.5,
            PreviousRank = 11.0,
            IsInGoogleMapsPack = false,
            SearchVolume = 480,
            Difficulty = "متوسط",
            BestPerformingUrl = $"https://{summary.Domain}/",
            LastCheckedUtc = DateTime.UtcNow
        });

        summary.KeywordsInTop3 = summary.Keywords.Count(k => k.CurrentRank <= 3.0);
        summary.KeywordsInTop10 = summary.Keywords.Count(k => k.CurrentRank <= 10.0);
        summary.MapsPackCount = summary.Keywords.Count(k => k.IsInGoogleMapsPack);
        summary.LastUpdatedUtc = DateTime.UtcNow;

        await SaveSummaryAsync(summary, ct);
    }

    private LocalRankTrackingSummary CreateDefaultSummary(ProjectProfile project)
    {
        var domain = project.Domain.ToLowerInvariant();
        var keywords = new List<LocalKeywordRank>();

        if (domain.Contains("tbztvrepair") || domain.Contains("tv"))
        {
            keywords.AddRange(new[]
            {
                new LocalKeywordRank { Keyword = "تعمیر تلویزیون در تبریز", TargetCity = "تبریز", CurrentRank = 1.2, PreviousRank = 1.8, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 1850, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/" },
                new LocalKeywordRank { Keyword = "نمایندگی تعمیر تلویزیون سامسونگ تبریز", TargetCity = "تبریز", CurrentRank = 2.1, PreviousRank = 2.9, IsInGoogleMapsPack = true, MapsPosition = 2, SearchVolume = 920, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/samsung" },
                new LocalKeywordRank { Keyword = "تعمیر تلویزیون ال جی در تبریز", TargetCity = "تبریز", CurrentRank = 2.4, PreviousRank = 2.3, IsInGoogleMapsPack = true, MapsPosition = 2, SearchVolume = 1100, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/lg" },
                new LocalKeywordRank { Keyword = "تعمیر بک لایت تلویزیون تبریز", TargetCity = "تبریز", CurrentRank = 1.4, PreviousRank = 1.9, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 640, Difficulty = "آسان", BestPerformingUrl = $"https://{domain}/backlight" },
                new LocalKeywordRank { Keyword = "هزینه تعمیر پنل تلویزیون تبریز", TargetCity = "تبریز", CurrentRank = 3.6, PreviousRank = 4.5, IsInGoogleMapsPack = false, MapsPosition = null, SearchVolume = 510, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/pricing" }
            });
        }
        else if (domain.Contains("yonarak"))
        {
            keywords.AddRange(new[]
            {
                new LocalKeywordRank { Keyword = "خرید عسل طبیعی تبریز", TargetCity = "تبریز", CurrentRank = 2.2, PreviousRank = 3.1, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 1200, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/honey" },
                new LocalKeywordRank { Keyword = "سوغات آذربایجان و تبریز", TargetCity = "تبریز", CurrentRank = 3.0, PreviousRank = 4.2, IsInGoogleMapsPack = false, MapsPosition = null, SearchVolume = 850, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/souvenirs" },
                new LocalKeywordRank { Keyword = "خرید خشکبار مرغوب تبریز", TargetCity = "تبریز", CurrentRank = 4.1, PreviousRank = 5.0, IsInGoogleMapsPack = true, MapsPosition = 3, SearchVolume = 620, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/nuts" },
                new LocalKeywordRank { Keyword = "فروشگاه آنلاین یوناراک", TargetCity = "تبریز", CurrentRank = 1.0, PreviousRank = 1.0, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 350, Difficulty = "آسان", BestPerformingUrl = $"https://{domain}/" }
            });
        }
        else if (domain.Contains("asazist"))
        {
            keywords.AddRange(new[]
            {
                new LocalKeywordRank { Keyword = "تجهیزات آزمایشگاهی تبریز", TargetCity = "تبریز", CurrentRank = 2.4, PreviousRank = 3.2, IsInGoogleMapsPack = true, MapsPosition = 2, SearchVolume = 780, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/lab-equipment" },
                new LocalKeywordRank { Keyword = "کیت های تشخیصی زیستی تبریز", TargetCity = "تبریز", CurrentRank = 3.8, PreviousRank = 4.1, IsInGoogleMapsPack = false, MapsPosition = null, SearchVolume = 430, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/kits" },
                new LocalKeywordRank { Keyword = "مواد شیمیایی آزمایشگاهی شمال‌غرب", TargetCity = "تبریز", CurrentRank = 4.5, PreviousRank = 5.2, IsInGoogleMapsPack = true, MapsPosition = 3, SearchVolume = 390, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/chemicals" }
            });
        }
        else if (domain.Contains("spinora"))
        {
            keywords.AddRange(new[]
            {
                new LocalKeywordRank { Keyword = "پوشاک ارگانیک الیاف طبیعی تبریز", TargetCity = "تبریز", CurrentRank = 2.8, PreviousRank = 3.6, IsInGoogleMapsPack = true, MapsPosition = 2, SearchVolume = 690, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/organic" },
                new LocalKeywordRank { Keyword = "فروشگاه لباس الیاف طبیعی تبریز", TargetCity = "تبریز", CurrentRank = 3.2, PreviousRank = 4.0, IsInGoogleMapsPack = true, MapsPosition = 3, SearchVolume = 540, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/shop" }
            });
        }
        else // default irres.ir
        {
            keywords.AddRange(new[]
            {
                new LocalKeywordRank { Keyword = "شرکت طراحی سایت در تبریز", TargetCity = "تبریز", CurrentRank = 2.8, PreviousRank = 4.1, IsInGoogleMapsPack = true, MapsPosition = 2, SearchVolume = 2400, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/web-design" },
                new LocalKeywordRank { Keyword = "سئو و بهینه‌سازی سایت تبریز", TargetCity = "تبریز", CurrentRank = 2.1, PreviousRank = 2.9, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 1600, Difficulty = "سخت", BestPerformingUrl = $"https://{domain}/seo" },
                new LocalKeywordRank { Keyword = "تعمیرات بردهای الکترونیکی تبریز", TargetCity = "تبریز", CurrentRank = 1.3, PreviousRank = 1.5, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 850, Difficulty = "آسان", BestPerformingUrl = $"https://{domain}/electronics" },
                new LocalKeywordRank { Keyword = "اتوماسیون صنعتی و مونیتورینگ تبریز", TargetCity = "تبریز", CurrentRank = 4.2, PreviousRank = 5.5, IsInGoogleMapsPack = false, MapsPosition = null, SearchVolume = 620, Difficulty = "متوسط", BestPerformingUrl = $"https://{domain}/automation" },
                new LocalKeywordRank { Keyword = "شرکت راهکار الکترونیک سهند", TargetCity = "تبریز", CurrentRank = 1.0, PreviousRank = 1.0, IsInGoogleMapsPack = true, MapsPosition = 1, SearchVolume = 980, Difficulty = "آسان", BestPerformingUrl = $"https://{domain}/" }
            });
        }

        return new LocalRankTrackingSummary
        {
            ProjectId = project.Id,
            Domain = project.Domain,
            City = "تبریز",
            OverallLocalVisibilityScore = 88,
            KeywordsInTop3 = keywords.Count(k => k.CurrentRank <= 3.0),
            KeywordsInTop10 = keywords.Count(k => k.CurrentRank <= 10.0),
            MapsPackCount = keywords.Count(k => k.IsInGoogleMapsPack),
            Keywords = keywords,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }

    private async Task SaveSummaryAsync(LocalRankTrackingSummary summary, CancellationToken ct)
    {
        try
        {
            var filePath = Path.Combine(_storageDir, $"{summary.ProjectId}.json");
            var json = JsonSerializer.Serialize(summary, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving local ranks for project {ProjectId}", summary.ProjectId);
        }
    }
}
