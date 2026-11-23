using System;
using System.IO;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Faradid.Web.Controllers;

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

        if (!string.IsNullOrWhiteSpace(persona) && Enum.TryParse<PersonaView>(persona, true, out var p))
        {
            report.ActivePersona = p;
        }

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var report = await _repo.GetReportByIdAsync(id);
        if (report == null || string.IsNullOrWhiteSpace(report.ExportPath) || !System.IO.File.Exists(report.ExportPath))
        {
            return NotFound("فایل گزارش یافت نشد.");
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(report.ExportPath);
        var fileName = Path.GetFileName(report.ExportPath);
        return File(bytes, "text/html", fileName);
    }
}
