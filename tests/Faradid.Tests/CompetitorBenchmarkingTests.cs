using System;
using Faradid.Core.Models;
using Faradid.Engine;
using Xunit;

namespace Faradid.Tests;

public class CompetitorBenchmarkingTests
{
    [Fact]
    public void Benchmark_Generates6AxesAndCompositeScores()
    {
        // Arrange
        var parser = new GscDataParser();
        var gsc = parser.CreatePresetData("yonarak.ir", "دوره آزمون", 28);
        var project = new ProjectProfile
        {
            Name = "یونارک",
            Domain = "yonarak.ir",
            Industry = "خدمات ساختمانی و نما"
        };
        var engine = new CompetitorBenchmarkingEngine();

        // Act
        var report = engine.Benchmark(project, gsc, Array.Empty<CompetitorProfile>());

        // Assert
        Assert.NotNull(report);
        Assert.Equal(6, report.ComparisonAxes.Count);
        Assert.True(report.OurCompositeScore > 0);
        Assert.True(report.LeaderCompositeScore > 0);
        Assert.NotEmpty(report.OffensiveCounterPlaybook);
        Assert.False(string.IsNullOrWhiteSpace(report.MarketPositionSummary));
    }
}
