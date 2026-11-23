using System;
using System.Collections.Generic;
using Faradid.Core.Enums;

namespace Faradid.Core.Models;

public sealed class FaradidMasterReport
{
    public Guid ReportId { get; set; } = Guid.NewGuid();
    public ProjectProfile Project { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public PersonaView ActivePersona { get; set; } = PersonaView.Executive;

    public GscPeriodSummary CurrentPeriod { get; set; } = new();
    public GscPeriodSummary? PreviousPeriod { get; set; }

    public ComparativeMetrics Comparative { get; set; } = new();
    public CodebaseAuditResult? CodeAudit { get; set; }
    public CompetitorBenchmarkReport Competitors { get; set; } = new();
    public AiStrategicReport AiSynthesis { get; set; } = new();

    public string ExportPath { get; set; } = string.Empty;
}
