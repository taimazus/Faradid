using System;
using System.Collections.Generic;

namespace Faradid.Core.Models;

public sealed class GscMetricRecord
{
    public DateTime Date { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double Position { get; set; }
}

public sealed class GscDimensionAggregate
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double Position { get; set; }
    public double SharePercentage { get; set; }
}

public sealed class GscPeriodSummary
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long TotalClicks { get; set; }
    public long TotalImpressions { get; set; }
    public double AverageCtr { get; set; }
    public double AveragePosition { get; set; }

    public List<GscDimensionAggregate> TopQueries { get; set; } = new();
    public List<GscDimensionAggregate> TopPages { get; set; } = new();
    public List<GscDimensionAggregate> CountryBreakdown { get; set; } = new();
    public List<GscDimensionAggregate> DeviceBreakdown { get; set; } = new();
    public List<GscMetricRecord> DailyMetrics { get; set; } = new();
}
