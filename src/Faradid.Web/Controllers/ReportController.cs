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
    public async Task<IActionResult> ViewReport(Guid id, string? persona = null)
    {
        var report = await _repo.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        if (!UserAccessHelper.CanAccessProject(User, report.Project))
        {
            TempData["ErrorMessage"] = "شما مجوز مشاهده این کارنامه راهبردی را ندارید.";
            return RedirectToAction("Index", "Project");
        }

        if (!string.IsNullOrWhiteSpace(persona) && Enum.TryParse<PersonaView>(persona, true, out var p))
        {
            report.ActivePersona = p;
        }

        ViewBag.ActivePersona = persona?.ToLowerInvariant() ?? "all";
        ViewBag.IsAdmin = UserAccessHelper.IsAdmin(User);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, string? persona = null)
    {
        var report = await _repo.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        if (!UserAccessHelper.CanAccessProject(User, report.Project))
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
