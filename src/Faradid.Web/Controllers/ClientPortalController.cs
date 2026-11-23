using System;
using System.Linq;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Web.Authentication;
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
    public async Task<IActionResult> Index(Guid id, [FromQuery] string? token = null)
    {
        var project = await _repo.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound("پروژه درخواستی یافت نشد.");
        }

        // 1. Check if Client Portal is enabled
        if (!project.IsClientPortalEnabled)
        {
            ViewBag.ErrorMessage = "دسترسی عمومی به این پورتال توسط مدیریت سامانه موقتاً غیرفعال شده است.";
            return View("AccessDeniedPortal", project);
        }

        // 2. Security Check: Allow if authenticated user has access, OR if valid secret token provided
        var isAuthenticatedStaff = User.Identity?.IsAuthenticated == true && UserAccessHelper.CanAccessProject(User, project);
        var isValidToken = !string.IsNullOrWhiteSpace(token) && 
                           !string.IsNullOrWhiteSpace(project.ClientPortalToken) && 
                           string.Equals(token.Trim(), project.ClientPortalToken.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isAuthenticatedStaff && !isValidToken)
        {
            ViewBag.ErrorMessage = "کلید دسترسی امن (Token) نامعتبر است، منقضی شده یا ارائه نگردیده است. جهت دریافت لینک دسترسی اختصاصی با شرکت راهکار الکترونیک سهند تماس حاصل فرمایید.";
            return View("AccessDeniedPortal", project);
        }

        var reports = await _repo.GetReportsByProjectIdAsync(id);
        var latestReport = reports.OrderByDescending(r => r.GeneratedAtUtc).FirstOrDefault();

        var localRanks = await _localRankTracker.GetOrUpdateLocalRanksAsync(project);
        var crawlResult = await _crawler.GetLatestCrawlResultAsync(id);
        var alarms = await _alarmEngine.GetAlarmsByProjectAsync(id);

        ViewBag.Token = token ?? project.ClientPortalToken;

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
