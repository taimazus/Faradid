using System;
using System.Collections.Generic;

namespace Faradid.Core.Models;

public enum SeoAlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public sealed class SeoAlertLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MetricTrigger { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public SeoAlertSeverity Severity { get; set; } = SeoAlertSeverity.Warning;
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
}

public sealed class GeneratedArticleDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public List<string> SecondaryKeywords { get; set; } = new();
    public string UserSearchIntent { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string SchemaJsonLd { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
