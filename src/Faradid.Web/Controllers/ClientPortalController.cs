using System;
using System.Linq;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Controllers;

[AllowAnonymous]
public sealed class ClientPortalController : Controller
{
    private readonly IProjectRepository _repo;
    private readonly ILocalRankTrackerService _localRankTracker;
    private readonly ISiteCrawlerService _crawler;
    private readonly ISeoAlarmEngine _alarmEngine;
    private readonly ILogger<ClientPortalController> _logger;

    public ClientPortalController(
        IProjectRepository repo,
        ILocalRankTrackerService localRankTracker,
        ISiteCrawlerService crawler,
        ISeoAlarmEngine alarmEngine,
        ILogger<ClientPortalController> logger)
    {
        _repo = repo;
        _localRankTracker = localRankTracker;
        _crawler = crawler;
        _alarmEngine = alarmEngine;
        _logger = logger;
    }

    [HttpGet("/ClientPortal/{id:guid}")]
    [HttpGet("/ClientPortal/View/{id:guid}")]
    public async Task<IActionResult> Index(Guid id)
    {
        var project = await _repo.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound("پروژه درخواستی یافت نشد.");
        }

        var reports = await _repo.GetReportsByProjectIdAsync(id);
        var latestReport = reports.OrderByDescending(r => r.GeneratedAtUtc).FirstOrDefault();

        var localRanks = await _localRankTracker.GetOrUpdateLocalRanksAsync(project);
        var crawlResult = await _crawler.GetLatestCrawlResultAsync(id);
        var alarms = await _alarmEngine.GetAlarmsByProjectAsync(id);

        var vm = new ClientPortalViewModel
        {
            Project = project,
            LatestReport = latestReport,
            LocalRanks = localRanks,
            CrawlResult = crawlResult,
            Alarms = alarms.Take(5).ToList()
        };

        return View(vm);
    }
}
