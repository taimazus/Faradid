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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DirectLogin(string username, string? role = null, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["ErrorMessage"] = "نام کاربری نمی‌تواند خالی باشد.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        username = username.Trim();
        string displayName;
        string email;
        string assignedRole;

        switch (username.ToLowerInvariant())
        {
            case "admin":
            case "sahand":
            case "مدیر":
            case "مدیر سیستم (سهند)":
            case "کارشناس ارشد سئو (سهند)":
                username = "admin";
                displayName = "مدیر کل راهکار الکترونیک سهند";
                email = "admin@irres.ir";
                assignedRole = "Admin";
                break;

            case "yonarak":
                displayName = "مدیریت یونارک (Yonarak)";
                email = "manager@yonarak.ir";
                assignedRole = "Client";
                break;

            case "asazist":
                displayName = "مدیریت آسا زیست تبریز";
                email = "info@asazist.com";
                assignedRole = "Client";
                break;

            case "spinora":
                displayName = "مدیریت کالای نوزاد اسپینورا";
                email = "support@spinora.ir";
                assignedRole = "Client";
                break;

            case "tbztv":
            case "tabriztv":
                displayName = "مدیریت تعمیر تلویزیون تبریز";
                email = "service@tbztvrepair.ir";
                assignedRole = "Client";
                break;

            case "irres":
                displayName = "پرتال مرکزی راهکار الکترونیک سهند";
                email = "info@irres.ir";
                assignedRole = "Client";
                break;

            default:
                displayName = username;
                email = $"{username}@irres.ir";
                assignedRole = !string.IsNullOrWhiteSpace(role) && role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Client";
                break;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, assignedRole),
            new("Username", username)
        };

        if (assignedRole == "Admin")
        {
            claims.Add(new(ClaimTypes.Role, "SeoSpecialist"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        TempData["SuccessMessage"] = $"خوش آمدید، {displayName}. وارد سامانه فَرادید شدید.";
        return LocalRedirect(!string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/");
    }

    [HttpPost]
    public async Task<IActionResult> DevBypassLogin(string username = "مدیر سیستم (سهند)", string? returnUrl = null)
    {
        return await DirectLogin(username, "Admin", returnUrl);
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "با موفقیت از سامانه فَرادید خارج شدید.";
        return RedirectToAction(nameof(Login));
    }
}
