using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Faradid.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly ICentralAuthClient _centralAuth;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ICentralAuthClient centralAuth, ILogger<AccountController> logger)
    {
        _centralAuth = centralAuth;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Config = _centralAuth.GetConfig();
        return View();
    }

    [HttpPost]
    public IActionResult SsoRedirect(string? returnUrl = null)
    {
        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("SSO_STATE", state);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            HttpContext.Session.SetString("SSO_RETURN_URL", returnUrl);
        }

        var redirectUrl = _centralAuth.BuildAuthorizeUrl(state, returnUrl);
        return Redirect(redirectUrl);
    }

    [HttpGet]
    [Route("sso/callback")]
    public async Task<IActionResult> SsoCallback(string? code, string? state, string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            TempData["ErrorMessage"] = $"خطای احراز هویت متمرکز: {error}";
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "کد دسترسی از سرور هویت دریافت نشد.";
            return RedirectToAction(nameof(Login));
        }

        var exchange = await _centralAuth.ExchangeSsoCodeAsync(code);
        if (!exchange.IsSuccess || exchange.Value == null)
        {
            TempData["ErrorMessage"] = exchange.ErrorMessage ?? "تبادل اطلاعات با سرور هویت متمرکز با شکست مواجه شد.";
            return RedirectToAction(nameof(Login));
        }

        var user = exchange.Value.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName ?? user.Username),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("Username", user.Username)
        };

        if (user.Roles != null)
        {
            foreach (var r in user.Roles)
            {
                claims.Add(new(ClaimTypes.Role, r));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var returnUrl = HttpContext.Session.GetString("SSO_RETURN_URL");
        HttpContext.Session.Remove("SSO_RETURN_URL");
        HttpContext.Session.Remove("SSO_STATE");

        return LocalRedirect(!string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/");
    }

    [HttpPost]
    public async Task<IActionResult> DevBypassLogin(string username = "مدیر سیستم (سهند)", string? returnUrl = null)
    {
        var config = _centralAuth.GetConfig();
        if (!config.AllowLocalBypass)
        {
            return Forbid();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, "admin@irres.ir"),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "SeoSpecialist")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
