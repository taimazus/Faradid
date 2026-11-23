using System;
using System.Collections.Generic;
using Faradid.Core.Models;

namespace Faradid.Web.Models;

public sealed class ClientPortalViewModel
{
    public required ProjectProfile Project { get; set; }
    public FaradidMasterReport? LatestReport { get; set; }
    public LocalRankTrackingSummary? LocalRanks { get; set; }
    public SiteCrawlResult? CrawlResult { get; set; }
    public IReadOnlyList<SeoAlertLog> Alarms { get; set; } = Array.Empty<SeoAlertLog>();
}
