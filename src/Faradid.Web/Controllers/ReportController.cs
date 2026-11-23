using System;
using System.IO;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Engine;
using Faradid.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faradid.Web.Controllers;

[Authorize]
public sealed class ReportController : Controller
{
    private readonly IProjectRepository _repo;

    public ReportController(IProjectRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ViewReport(Guid id, string? persona = null, string? token = null)
    {
        var report = await _repo.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        var isStaff = User.Identity?.IsAuthenticated == true && UserAccessHelper.CanAccessProject(User, report.Project);
        var isValidToken = !string.IsNullOrWhiteSpace(token) &&
                           !string.IsNullOrWhiteSpace(report.Project.ClientPortalToken) &&
                           report.Project.IsClientPortalEnabled &&
                           string.Equals(token.Trim(), report.Project.ClientPortalToken.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isStaff && !isValidToken)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }
            TempData["ErrorMessage"] = "شما مجوز مشاهده این کارنامه راهبردی را ندارید.";
            return RedirectToAction("Index", "Project");
        }

        if (!string.IsNullOrWhiteSpace(persona) && Enum.TryParse<PersonaView>(persona, true, out var p))
        {
            report.ActivePersona = p;
        }

        ViewBag.ActivePersona = persona?.ToLowerInvariant() ?? "all";
        ViewBag.IsAdmin = UserAccessHelper.IsAdmin(User);
        ViewBag.Token = token;
        return View(report);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Download(Guid id, string? persona = null, string? token = null)
    {
        var report = await _repo.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        var isStaff = User.Identity?.IsAuthenticated == true && UserAccessHelper.CanAccessProject(User, report.Project);
        var isValidToken = !string.IsNullOrWhiteSpace(token) &&
                           !string.IsNullOrWhiteSpace(report.Project.ClientPortalToken) &&
                           report.Project.IsClientPortalEnabled &&
                           string.Equals(token.Trim(), report.Project.ClientPortalToken.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isStaff && !isValidToken)
        {
            return Forbid();
        }

        var safePersona = !string.IsNullOrWhiteSpace(persona) ? persona.ToLowerInvariant() : "all";
        var html = HtmlReportExporter.GenerateHtmlContent(report);
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        var fileName = $"Faradid-Report-{report.Project.Domain.Replace(".", "-")}-{safePersona}.html";
        return File(bytes, "text/html; charset=utf-8", fileName);
    }
}
