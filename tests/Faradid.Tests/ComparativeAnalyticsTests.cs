using System;
using Faradid.Core.Enums;
using Faradid.Engine;
using Xunit;

namespace Faradid.Tests;

public class ComparativeAnalyticsTests
{
    [Fact]
    public void ComputeComparison_WithPreviousPeriod_ComputesDeltasAndTrendsCorrectly()
    {
        // Arrange
        var parser = new GscDataParser();
        var current = parser.CreatePresetData("irres.ir", "دوره جاری", 28);
        var previous = parser.CreatePresetData("irres.ir", "دوره قبل", 28);
        var engine = new ComparativeAnalyticsEngine();

        // Act
        var metrics = engine.ComputeComparison(current, previous);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.GermanyVpnSharePercent > 30.0);
        Assert.True(metrics.IranDirectSharePercent > 20.0);
        Assert.True(metrics.HasSignificantAnomaly);
        Assert.NotEmpty(metrics.Anomalies);
        Assert.False(string.IsNullOrWhiteSpace(metrics.AnomalyExecutiveSummary));
    }
}
