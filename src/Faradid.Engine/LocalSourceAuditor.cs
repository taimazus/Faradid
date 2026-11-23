using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;

namespace Faradid.Engine;

public sealed class LocalSourceAuditor : ILocalSourceAuditor
{
    public async Task<CodebaseAuditResult> AuditPathAsync(string rootPath, Guid projectId, CancellationToken ct = default)
    {
        var result = new CodebaseAuditResult
        {
            ProjectId = projectId,
            AnalyzedPath = rootPath,
            TechnicalHealthScore = 100
        };

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "پیکربندی مسیر",
                RuleName = "بررسی دسترسی‌پذیری مسیر پروژه",
                Severity = AuditSeverity.Info,
                Description = $"مسیر محلی مشخص‌شده یافت نشد یا در دسترس نیست: {rootPath}",
                Recommendation = "در صورت تمایل، مسیر فیزیکی پروژه روی سرور یا رایانه محلی را وارد فرمایید.",
                Passed = false
            });
            result.IssuesCount = 1;
            result.TechnicalHealthScore = 80;
            result.TotalRulesEvaluated = 1;
            return result;
        }

        // 1. Audit web.config
        await AuditWebConfigAsync(rootPath, result, ct);

        // 2. Audit robots.txt
        await AuditRobotsTxtAsync(rootPath, result, ct);

        // 3. Audit sitemap.xml
        AuditSitemapXml(rootPath, result);

        // 4. Audit View / Razor / HTML Templates
        await AuditRazorViewsAsync(rootPath, result, ct);

        // Calculate score
        result.TotalRulesEvaluated = result.Items.Count;
        result.PassedChecksCount = result.Items.Count(i => i.Passed);
        result.IssuesCount = result.Items.Count(i => !i.Passed);
        result.CriticalIssuesCount = result.Items.Count(i => !i.Passed && i.Severity >= AuditSeverity.High);

        int deductions = result.Items.Where(i => !i.Passed).Sum(i => i.Severity switch
        {
            AuditSeverity.Critical => 25,
            AuditSeverity.High => 15,
            AuditSeverity.Medium => 8,
            AuditSeverity.Low => 3,
            _ => 0
        });

        result.TechnicalHealthScore = Math.Max(10, 100 - deductions);

        return result;
    }

    private static async Task AuditWebConfigAsync(string rootPath, CodebaseAuditResult result, CancellationToken ct)
    {
        var webConfigFiles = Directory.GetFiles(rootPath, "web.config", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin", StringComparison.OrdinalIgnoreCase) && !f.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (webConfigFiles.Count == 0)
        {
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "IIS / سرور وب",
                RuleName = "فایل پیکربندی web.config",
                Severity = AuditSeverity.Medium,
                Description = "فایل web.config در ریشه پروژه یافت نشد.",
                Recommendation = "برای استقرار در IIS یا Plesk و مدیریت کدهای وضعیت HTTP، وجود web.config توصیه می‌شود.",
                Passed = false
            });
            return;
        }

        foreach (var file in webConfigFiles.Take(2))
        {
            var content = await File.ReadAllTextAsync(file, ct);

            // Rule: Custom Error Handling
            bool hasCustomErrors = content.Contains("httpErrors", StringComparison.OrdinalIgnoreCase) || content.Contains("customErrors", StringComparison.OrdinalIgnoreCase);
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "مدیریت خطاهای HTTP",
                RuleName = "پیکربندی صفحات خطای اختصاصی (404/500)",
                Severity = AuditSeverity.High,
                FilePath = file,
                Description = hasCustomErrors 
                    ? "قوانین مدیریت خطاهای سرور و سفارشی‌سازی 404 در web.config تعریف شده است."
                    : "بخش httpErrors در web.config پیکربندی نشده است. ممکن است خطاهای خام سرور برای کاربران نمایش یابند.",
                Recommendation = "تگ <httpErrors errorMode=\"Custom\" existingResponse=\"Replace\"> را همراه با مسیردهی صفحات خطای 404 و 500 تعریف نمایید.",
                Passed = hasCustomErrors
            });

            // Rule: HTTPS Redirect / Canonical Rule
            bool hasHttpsRedirect = content.Contains("Redirect to HTTPS", StringComparison.OrdinalIgnoreCase) || content.Contains("HTTPS", StringComparison.OrdinalIgnoreCase);
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "امنیت و ریدایرکت",
                RuleName = "ریدایرکت خودکار به پروتکل امن HTTPS",
                Severity = AuditSeverity.High,
                FilePath = file,
                Description = hasHttpsRedirect
                    ? "قوانین URL Rewrite برای ارجاع دائمی (301) به پروتکل HTTPS وجود دارد."
                    : "قانون صریح ریدایرکت به HTTPS در web.config یافت نشد.",
                Recommendation = "قانون دائمی ۳۰۱ برای انتقال کلیه ترافیک HTTP به HTTPS در بخش rewrite/rules قرار گیرد.",
                Passed = hasHttpsRedirect
            });

            // Rule: Static Content Caching
            bool hasClientCache = content.Contains("clientCache", StringComparison.OrdinalIgnoreCase);
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "کارایی و کش",
                RuleName = "تنظیم هدرهای کش مرورگر برای فایل‌های استاتیک",
                Severity = AuditSeverity.Low,
                FilePath = file,
                Description = hasClientCache
                    ? "کش مرورگر برای فایل‌های استاتیک در staticContent/clientCache تنظیم شده است."
                    : "کش مرورگر (clientCache) در web.config صراحتاً تعیین نشده است.",
                Recommendation = "برای افزایش نمره Core Web Vitals، کش ۳۰ روزه برای فونت‌ها و تصاویر تنظیم شود.",
                Passed = hasClientCache
            });
        }
    }

    private static async Task AuditRobotsTxtAsync(string rootPath, CodebaseAuditResult result, CancellationToken ct)
    {
        var robotsFiles = Directory.GetFiles(rootPath, "robots.txt", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        if (robotsFiles.Count == 0)
        {
            result.Items.Add(new CodebaseAuditItem
            {
                Category = "خزنده‌ها و ربات‌ها",
                RuleName = "وجود فایل robots.txt",
                Severity = AuditSeverity.High,
                Description = "فایل robots.txt در ساختار پروژه یافت نشد.",
                Recommendation = "فایل robots.txt را در ریشه وب (wwwroot) ایجاد کرده و مسیر sitemap.xml را به خزنده‌های موتور جستجو معرفی نمایید.",
                Passed = false
            });
        }
        else
        {
            var robotsFile = robotsFiles.First();
            var content = await File.ReadAllTextAsync(robotsFile, ct);
            bool hasSitemap = content.Contains("Sitemap:", StringComparison.OrdinalIgnoreCase);

            result.Items.Add(new CodebaseAuditItem
            {
                Category = "خزنده‌ها و ربات‌ها",
                RuleName = "معرفی سایت‌مپ در robots.txt",
                Severity = AuditSeverity.Medium,
                FilePath = robotsFile,
                Description = hasSitemap
                    ? "دستور Sitemap در فایل robots.txt وجود دارد."
                    : "خط معرفی Sitemap: https://domain.ir/sitemap.xml در robots.txt درج نشده است.",
                Recommendation = "آدرس مطلق sitemap.xml سایت را در انتهای فایل robots.txt اضافه نمایید.",
                Passed = hasSitemap
            });
        }
    }

    private static void AuditSitemapXml(string rootPath, CodebaseAuditResult result)
    {
        var sitemapFiles = Directory.GetFiles(rootPath, "sitemap*.xml", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        result.Items.Add(new CodebaseAuditItem
        {
            Category = "نقشه سایت (Sitemap)",
            RuleName = "بررسی وجود نقشه سایت XML",
            Severity = AuditSeverity.Medium,
            Description = sitemapFiles.Count > 0
                ? $"تعداد {sitemapFiles.Count} فایل نقشه سایت XML در پروژه شناسایی شد."
                : "هیچ فایل sitemap.xml در ریشه استاتیک پروژه پیدا نشد (احتمال تولید پویا توسط کنترلر).",
            Recommendation = "اطمینان حاصل کنید نقشه سایت XML در دسترس خزنده‌های گوگل قرار دارد و صفحات ایندکس‌شدنی را لیست می‌کند.",
            Passed = sitemapFiles.Count > 0
        });
    }

    private static async Task AuditRazorViewsAsync(string rootPath, CodebaseAuditResult result, CancellationToken ct)
    {
        var viewFiles = Directory.GetFiles(rootPath, "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootPath, "*.html", SearchOption.AllDirectories))
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .Take(15)
            .ToList();

        if (viewFiles.Count == 0) return;

        bool hasViewport = false;
        bool hasCanonical = false;
        bool hasOpenGraph = false;
        bool hasSchema = false;

        foreach (var file in viewFiles)
        {
            var text = await File.ReadAllTextAsync(file, ct);
            if (text.Contains("name=\"viewport\"", StringComparison.OrdinalIgnoreCase)) hasViewport = true;
            if (text.Contains("rel=\"canonical\"", StringComparison.OrdinalIgnoreCase) || text.Contains("canonical", StringComparison.OrdinalIgnoreCase)) hasCanonical = true;
            if (text.Contains("og:title", StringComparison.OrdinalIgnoreCase) || text.Contains("og:image", StringComparison.OrdinalIgnoreCase)) hasOpenGraph = true;
            if (text.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase) || text.Contains("schema.org", StringComparison.OrdinalIgnoreCase)) hasSchema = true;
        }

        result.Items.Add(new CodebaseAuditItem
        {
            Category = "بهینه‌سازی موبایل",
            RuleName = "برچسب متای Viewport در لایوت صفحات",
            Severity = AuditSeverity.Critical,
            Description = hasViewport
                ? "تگ ضروری viewport در ساختار قالب یا لایوت شناسایی شد."
                : "تگ <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"> در فایل‌های بررسی‌شده یافت نشد.",
            Recommendation = "برای تطابق با الگوریتم Mobile-First Indexing گوگل، تگ viewport را در _Layout.cshtml درج فرمایید.",
            Passed = hasViewport
        });

        result.Items.Add(new CodebaseAuditItem
        {
            Category = "تگ‌های استاندارد سئو",
            RuleName = "تگ لینک کانونیکال (Canonical Tag)",
            Severity = AuditSeverity.High,
            Description = hasCanonical
                ? "مکانیزم تولید لینک کانونیکال در صفحات مشاهده شد."
                : "تگ کانونیکال جهت جلوگیری از محتوای تکراری (Duplicate Content) یافت نشد.",
            Recommendation = "تگ <link rel=\"canonical\" href=\"...\" /> را در بخش <head> تمام صفحات به صورت داینامیک تزریق کنید.",
            Passed = hasCanonical
        });

        result.Items.Add(new CodebaseAuditItem
        {
            Category = "شبکه‌های اجتماعی و پیش‌نمایش",
            RuleName = "پروتکل OpenGraph و کارت‌های توییتر",
            Severity = AuditSeverity.Low,
            Description = hasOpenGraph
                ? "متادیتاهای استاندارد OpenGraph (og:title, og:image) پیاده‌سازی شده‌اند."
                : "متادیتاهای OpenGraph برای پیش‌نمایش جذاب در شبکه‌های اجتماعی و پیام‌رسان‌ها یافت نشد.",
            Recommendation = "تگ‌های og:title، og:description و og:image را به لایوت وب‌سایت اضافه نمایید.",
            Passed = hasOpenGraph
        });

        result.Items.Add(new CodebaseAuditItem
        {
            Category = "داده‌های ساختاریافته (Schema Markup)",
            RuleName = "اسکیما و میکرودیتای JSON-LD",
            Severity = AuditSeverity.Medium,
            Description = hasSchema
                ? "اسکریپت‌های اسکیما بر پایه استانداردهای schema.org شناسایی شدند."
                : "اسکریپت‌های JSON-LD (مانند Organization, WebSite, Article یا LocalBusiness) مشاهده نشدند.",
            Recommendation = "اسکیمای متناسب با نوع کسب‌وکار را برای غنی‌سازی نتایج جستجو (Rich Snippets) تزریق کنید.",
            Passed = hasSchema
        });
    }
}
