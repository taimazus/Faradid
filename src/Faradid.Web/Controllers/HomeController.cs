using System;
using System.Linq;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Web.Authentication;
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
        // Intercept incoming SSO authorization codes redirected to site root
        if (Request.Query.ContainsKey("code"))
        {
            var code = Request.Query["code"].ToString();
            var state = Request.Query["state"].ToString();
            var error = Request.Query["error"].ToString();
            return RedirectToAction("SsoCallback", "Account", new { code, state, error });
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        var allProjects = await _repo.GetAllProjectsAsync();
        var userProjects = UserAccessHelper.FilterProjects(User, allProjects);

        ViewBag.TotalProjects = allProjects.Count;
        ViewBag.UserProjectsCount = userProjects.Count;
        ViewBag.IsAdmin = UserAccessHelper.IsAdmin(User);

        return View(userProjects);
    }

    public IActionResult About()
    {
        return View();
    }
}
