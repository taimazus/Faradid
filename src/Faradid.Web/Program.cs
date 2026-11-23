using Faradid.AI;
using Faradid.Core.Interfaces;
using Faradid.Engine;
using Faradid.Web.Authentication;
using Faradid.Web.Repositories;
using Faradid.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers and Razor Views
builder.Services.AddControllersWithViews();

// Session Support (for SSO state and navigation context)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication via Cookies (Irres Gate SSO)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Faradid.Auth";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Central SSO Configuration
var centralAuthConfig = builder.Configuration.GetSection("CentralAuth").Get<CentralAuthConfig>() ?? new CentralAuthConfig();
builder.Services.AddSingleton(centralAuthConfig);
builder.Services.AddHttpClient<ICentralAuthClient, CentralAuthClient>();

// Domain & Engine Services
builder.Services.AddSingleton<IProjectRepository, JsonFileProjectRepository>();
builder.Services.AddScoped<ILocalSourceAuditor, LocalSourceAuditor>();
builder.Services.AddScoped<IGscDataParser, GscDataParser>();
builder.Services.AddScoped<IComparativeAnalyticsEngine, ComparativeAnalyticsEngine>();
builder.Services.AddScoped<ICompetitorBenchmarkingEngine, CompetitorBenchmarkingEngine>();
builder.Services.AddScoped<IAiCopilotService, FaradidAiCopilot>();
builder.Services.AddScoped<IReportExporter, HtmlReportExporter>();

// Google Search Console API Client & Scheduled Background Automation
builder.Services.AddHttpClient<IGscApiClient, GoogleSearchConsoleApiClient>();
builder.Services.AddSingleton<ScheduledReportBackgroundService>();
builder.Services.AddSingleton<IScheduledReportService>(sp => sp.GetRequiredService<ScheduledReportBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduledReportBackgroundService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
