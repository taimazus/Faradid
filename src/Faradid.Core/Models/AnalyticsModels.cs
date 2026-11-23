using System;
using System.Collections.Generic;
using Faradid.Core.Enums;

namespace Faradid.Core.Models;

public sealed class MetricDelta
{
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public double AbsoluteDelta { get; set; }
    public double PercentageDelta { get; set; }
    public TrendDirection Trend { get; set; }

    public static MetricDelta Create(double current, double previous, bool higherIsBetter = true)
    {
        var abs = current - previous;
        var pct = previous != 0 ? (abs / previous) * 100.0 : 0.0;
        
        TrendDirection trend;
        if (Math.Abs(pct) < 1.0)
        {
            trend = TrendDirection.Stable;
        }
        else if (abs > 0)
        {
            trend = higherIsBetter ? TrendDirection.Up : TrendDirection.Down;
        }
        else
        {
            trend = higherIsBetter ? TrendDirection.Down : TrendDirection.Up;
        }

        return new MetricDelta
        {
            CurrentValue = current,
            PreviousValue = previous,
            AbsoluteDelta = abs,
            PercentageDelta = pct,
            Trend = trend
        };
    }
}

public sealed class TrafficAnomalyEvent
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AnomalyType { get; set; } = string.Empty; // e.g. "InternetBlackout", "VpnSpike", "AlgorithmUpdate"
    public AuditSeverity Severity { get; set; } = AuditSeverity.Medium;
    public long EstimatedLostClicks { get; set; }
}

public sealed class ComparativeMetrics
{
    public MetricDelta ClicksDelta { get; set; } = new();
    public MetricDelta ImpressionsDelta { get; set; } = new();
    public MetricDelta CtrDelta { get; set; } = new();
    public MetricDelta PositionDelta { get; set; } = new();

    public double GermanyVpnSharePercent { get; set; }
    public double IranDirectSharePercent { get; set; }
    public double MobileTrafficSharePercent { get; set; }
    public double DesktopTrafficSharePercent { get; set; }

    public bool HasSignificantAnomaly { get; set; }
    public string AnomalyExecutiveSummary { get; set; } = string.Empty;
    public List<TrafficAnomalyEvent> Anomalies { get; set; } = new();
}
