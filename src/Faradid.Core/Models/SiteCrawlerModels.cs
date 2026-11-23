using System;
using System.Collections.Generic;

namespace Faradid.Core.Models;

public sealed class CrawlUrlFinding
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public string? Title { get; set; }
    public int TitleLength => Title?.Length ?? 0;
    public string? MetaDescription { get; set; }
    public int MetaDescriptionLength => MetaDescription?.Length ?? 0;
    public int H1Count { get; set; }
    public int MissingAltImagesCount { get; set; }
    public int TotalImagesCount { get; set; }
    public bool HasCanonical { get; set; }
    public string? CanonicalUrl { get; set; }
    public long ResponseTimeMs { get; set; }
    public List<string> Issues { get; set; } = new();
}

public sealed class SiteCrawlResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public DateTime CrawledAtUtc { get; set; } = DateTime.UtcNow;
    public int TotalPagesCrawled { get; set; }
    public int BrokenLinksCount { get; set; }
    public int MissingAltCount { get; set; }
    public int MissingMetaDescCount { get; set; }
    public int MultipleH1Count { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int OverallHealthScore { get; set; } = 100; // 0-100
    public List<CrawlUrlFinding> Findings { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
}
