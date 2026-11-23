using System;
using System.Collections.Generic;
using Faradid.Core.Enums;

namespace Faradid.Core.Models;

public sealed class CompetitorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public long EstimatedMonthlyVisits { get; set; }
    public int EstimatedKeywordsRanked { get; set; }
    public double AveragePosition { get; set; }
    public double DomainAuthority { get; set; }
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.Moderate;
}

public sealed class CompetitorComparisonAxis
{
    public string AxisName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public double OurScore { get; set; }
    public Dictionary<string, double> CompetitorScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string GapAnalysis { get; set; } = string.Empty;
    public string TacticalCounterAction { get; set; } = string.Empty;
}

public sealed class CompetitorBenchmarkReport
{
    public List<CompetitorProfile> Competitors { get; set; } = new();
    public List<CompetitorComparisonAxis> ComparisonAxes { get; set; } = new();
    public double OurCompositeScore { get; set; }
    public double LeaderCompositeScore { get; set; }
    public string MarketPositionSummary { get; set; } = string.Empty;
    public List<string> OffensiveCounterPlaybook { get; set; } = new();
}
