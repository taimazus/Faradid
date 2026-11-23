using System;
using System.Linq;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Faradid.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IProjectRepository _repo;

    public HomeController(IProjectRepository repo)
    {
        _repo = repo;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _repo.GetAllProjectsAsync();
        ViewBag.TotalProjects = projects.Count;
        return View(projects);
    }

    public IActionResult About()
    {
        return View();
    }
}
