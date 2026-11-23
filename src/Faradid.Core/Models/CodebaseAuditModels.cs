using System;
using System.Collections.Generic;
using Faradid.Core.Enums;

namespace Faradid.Core.Models;

public sealed class CodebaseAuditItem
{
    public string Category { get; set; } = string.Empty; // e.g. "IIS/Web.config", "Redirects", "Metadata", "Canonical", "ErrorHandling"
    public string RuleName { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public bool Passed { get; set; }
}

public sealed class CodebaseAuditResult
{
    public Guid ProjectId { get; set; }
    public string AnalyzedPath { get; set; } = string.Empty;
    public int TotalRulesEvaluated { get; set; }
    public int PassedChecksCount { get; set; }
    public int IssuesCount { get; set; }
    public int CriticalIssuesCount { get; set; }
    public int TechnicalHealthScore { get; set; } // 0-100
    public List<CodebaseAuditItem> Items { get; set; } = new();
}
