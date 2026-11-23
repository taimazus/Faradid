using System;
using System.Collections.Generic;

namespace Faradid.Core.Models;

public sealed class LocalKeywordRank
{
    public string Keyword { get; set; } = string.Empty;
    public string TargetCity { get; set; } = "تبریز";
    public double CurrentRank { get; set; }
    public double PreviousRank { get; set; }
    public double Change => PreviousRank > 0 ? (PreviousRank - CurrentRank) : 0; // Positive means rank improved (e.g. from 5 to 3 is +2)
    public bool IsInGoogleMapsPack { get; set; }
    public int? MapsPosition { get; set; }
    public int SearchVolume { get; set; }
    public string Difficulty { get; set; } = "متوسط";
    public string BestPerformingUrl { get; set; } = string.Empty;
    public DateTime LastCheckedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LocalRankTrackingSummary
{
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string City { get; set; } = "تبریز";
    public int OverallLocalVisibilityScore { get; set; } = 85; // 0-100
    public int KeywordsInTop3 { get; set; }
    public int KeywordsInTop10 { get; set; }
    public int MapsPackCount { get; set; }
    public List<LocalKeywordRank> Keywords { get; set; } = new();
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
