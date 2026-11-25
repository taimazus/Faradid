using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Faradid.AI;
using Faradid.Core.Models;
using Faradid.Engine;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Faradid.Tests;

public sealed class AdvancedPhasesTests
{
    private readonly ProjectProfile _sampleProject = new()
    {
        Id = Guid.NewGuid(),
        Name = "شرکت راهکار الکترونیک سهند",
        Domain = "irres.ir",
        ClientName = "مهندس سهند",
        Industry = "تعمیر تلویزیون و طراحی وب‌سایت"
    };

    [Fact]
    public async Task SeoAlarmEngine_DetectsTrafficDrop_AndCreatesCriticalAlert()
    {
        var engine = new SeoAlarmEngine(NullLogger<SeoAlarmEngine>.Instance);

        var current = new GscPeriodSummary
        {
            PeriodName = "دوره اخیر",
            StartDate = DateTime.UtcNow.AddDays(-28),
            EndDate = DateTime.UtcNow,
            TotalClicks = 70, // 30% drop from 100
            TotalImpressions = 5000,
            AveragePosition = 12.0
        };

        var previous = new GscPeriodSummary
        {
            PeriodName = "دوره قبل",
            StartDate = DateTime.UtcNow.AddDays(-56),
            EndDate = DateTime.UtcNow.AddDays(-28),
            TotalClicks = 100,
            TotalImpressions = 6000,
            AveragePosition = 11.5
        };

        var alerts = await engine.EvaluateAlarmsAsync(_sampleProject, current, previous, null);

        Assert.NotEmpty(alerts);
        var trafficAlert = alerts.FirstOrDefault(a => a.Severity == SeoAlertSeverity.Critical);
        Assert.NotNull(trafficAlert);
        Assert.Contains("افت محسوس ترافیک", trafficAlert.Title);
    }

    [Fact]
    public async Task SeoAlarmEngine_DetectsVpnDeuAnomaly_CreatesInfoAlert()
    {
        var engine = new SeoAlarmEngine(NullLogger<SeoAlarmEngine>.Instance);

        var current = new GscPeriodSummary
        {
            PeriodName = "دوره اخیر",
            StartDate = DateTime.UtcNow.AddDays(-28),
            EndDate = DateTime.UtcNow,
            TotalClicks = 500,
            CountryBreakdown = new List<GscDimensionAggregate>
            {
                new() { Key = "DEU", Label = "آلمان (DEU / VPN)", Clicks = 260, SharePercentage = 52.0 },
                new() { Key = "IRN", Label = "ایران", Clicks = 240, SharePercentage = 48.0 }
            }
        };

        var alerts = await engine.EvaluateAlarmsAsync(_sampleProject, current, null, null);

        var vpnAlert = alerts.FirstOrDefault(a => a.Title.Contains("فیلترشکن") || a.Title.Contains("DEU"));
        Assert.NotNull(vpnAlert);
        Assert.Equal(SeoAlertSeverity.Info, vpnAlert.Severity);
    }

    [Fact]
    public async Task AiArticleWriter_GeneratesComprehensiveDraft_WithSchemaAndFaq()
    {
        var writer = new AiArticleWriter(NullLogger<AiArticleWriter>.Instance);

        var plan = new EditorialArticleRecommendation
        {
            Order = 1,
            Title = "راهنمای عیب‌یابی و تعمیر تلویزیون در تبریز",
            TargetKeyword = "تعمیر تلویزیون در تبریز",
            UserSearchIntent = "تراکنشی (Transactional)"
        };

        var draft = await writer.GenerateFullArticleAsync(_sampleProject, plan);

        Assert.NotNull(draft);
        Assert.Equal("تعمیر تلویزیون در تبریز", draft.TargetKeyword);
        Assert.True(draft.WordCount >= 2500, "Draft should exceed 2500 words for deep-dive SEO authority");
        Assert.Contains("<h1", draft.HtmlContent);
        Assert.Contains("جدول برآورد هزینه‌ها", draft.HtmlContent);
        Assert.Contains("سوالات متداول", draft.HtmlContent);
        Assert.Contains("041-33360852", draft.HtmlContent); // Direct contact
        Assert.Contains("schema.org", draft.SchemaJsonLd);
        Assert.Contains("FAQPage", draft.SchemaJsonLd);
    }

    [Fact]
    public async Task LocalRankTracker_InitializesTabrizKeywords_AndComputesLocalScore()
    {
        var tracker = new LocalRankTrackerService(NullLogger<LocalRankTrackerService>.Instance);

        var summary = await tracker.GetOrUpdateLocalRanksAsync(_sampleProject);

        Assert.NotNull(summary);
        Assert.Equal("تبریز", summary.City);
        Assert.True(summary.Keywords.Count >= 3);
        Assert.True(summary.OverallLocalVisibilityScore > 50);

        var mapsPackKws = summary.Keywords.Where(k => k.IsInGoogleMapsPack).ToList();
        Assert.NotEmpty(mapsPackKws);
    }

    [Fact]
    public async Task LocalRankTracker_YonarakCom_NeverShowsHoneyOrRopeAccess_EvenWithLegacyCache()
    {
        var yonarakCom = new ProjectProfile
        {
            Id = Guid.NewGuid(),
            Name = "یونارک",
            Domain = "yonarak.com",
            ClientName = "تولیدی صنعتی یونارک",
            Industry = "اولین تولیدکننده دیسک تراش در ایران"
        };

        var cacheDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "local_ranks");
        Directory.CreateDirectory(cacheDir);
        var cacheFile = Path.Combine(cacheDir, $"{yonarakCom.Id}.json");

        try
        {
            // Simulate poisoned legacy cache on disk
            await File.WriteAllTextAsync(cacheFile, """
                {
                  "ProjectId": "%ID%",
                  "Domain": "yonarak.com",
                  "City": "تبریز",
                  "Keywords": [
                    { "Keyword": "خرید عسل طبیعی تبریز", "CurrentRank": 2.2 },
                    { "Keyword": "سوغات آذربایجان و تبریز", "CurrentRank": 3.0 }
                  ]
                }
                """.Replace("%ID%", yonarakCom.Id.ToString()));

            var tracker = new LocalRankTrackerService(NullLogger<LocalRankTrackerService>.Instance);
            var summary = await tracker.GetOrUpdateLocalRanksAsync(yonarakCom);

            Assert.DoesNotContain(summary.Keywords, k => k.Keyword.Contains("عسل") || k.Keyword.Contains("سوغات") || k.Keyword.Contains("کفسابی"));
            Assert.Contains(summary.Keywords, k => k.Keyword.Contains("دیسک تراش") || k.Keyword.Contains("یونارک"));
        }
        finally
        {
            if (File.Exists(cacheFile)) File.Delete(cacheFile);
        }
    }
}
