using System;
using System.Threading.Tasks;
using Faradid.AI;
using Faradid.Core.Models;
using Faradid.Engine;
using Xunit;

namespace Faradid.Tests;

public class AiCopilotTests
{
    [Fact]
    public async Task GenerateInsightsAsync_GeneratesExecutiveSynthesisAnd4Articles()
    {
        // Arrange
        var parser = new GscDataParser();
        var gsc = parser.CreatePresetData("asazist.com", "دوره ماهانه", 28);
        var compEngine = new ComparativeAnalyticsEngine();
        var comparative = compEngine.ComputeComparison(gsc, null);
        var competitorEngine = new CompetitorBenchmarkingEngine();
        var project = new ProjectProfile
        {
            Name = "آسا زیست",
            Domain = "asazist.com",
            Industry = "مدیریت پسماند پزشکی"
        };
        var competitors = competitorEngine.Benchmark(project, gsc, Array.Empty<CompetitorProfile>());
        var copilot = new FaradidAiCopilot();

        // Act
        var result = await copilot.GenerateInsightsAsync(project, gsc, comparative, competitors, null);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ExecutiveSummary));
        Assert.False(string.IsNullOrWhiteSpace(result.DeepDiveAnalysis));
        Assert.False(string.IsNullOrWhiteSpace(result.TacticalRoadmap));
        Assert.Equal(4, result.EditorialCalendar.Count);
        Assert.All(result.EditorialCalendar, art =>
        {
            Assert.False(string.IsNullOrWhiteSpace(art.Title));
            Assert.False(string.IsNullOrWhiteSpace(art.TargetKeyword));
            Assert.True(art.TargetWordCount >= 1800);
            Assert.NotEmpty(art.ContentPillars);
        });
    }
}
