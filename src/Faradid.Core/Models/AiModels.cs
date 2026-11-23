using System;
using System.Collections.Generic;

namespace Faradid.Core.Models;

public sealed class EditorialArticleRecommendation
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public List<string> SecondaryKeywords { get; set; } = new();
    public string UserSearchIntent { get; set; } = string.Empty; // e.g. "Transactional", "Informational", "Commercial"
    public int TargetWordCount { get; set; } = 2500;
    public int PriorityScore { get; set; } = 90;
    public string StrategicReasoning { get; set; } = string.Empty;
    public List<string> ContentPillars { get; set; } = new();
}

public sealed class AiStrategicReport
{
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string DeepDiveAnalysis { get; set; } = string.Empty;
    public string TacticalRoadmap { get; set; } = string.Empty;
    public List<EditorialArticleRecommendation> EditorialCalendar { get; set; } = new();
}
