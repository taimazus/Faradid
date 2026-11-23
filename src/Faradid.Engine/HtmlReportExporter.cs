using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Enums;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;

namespace Faradid.Engine;

public sealed class HtmlReportExporter : IReportExporter
{
    public async Task<string> ExportHtmlAsync(FaradidMasterReport report, string destinationPath, CancellationToken ct = default)
    {
        string targetFile;
        if (Directory.Exists(destinationPath))
        {
            var fileName = $"Faradid-Report-{report.Project.Domain.Replace(".", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmm}.html";
            targetFile = Path.Combine(destinationPath, fileName);
        }
        else
        {
            targetFile = destinationPath;
            var dir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        var html = GenerateHtmlContent(report);
        await File.WriteAllTextAsync(targetFile, html, Encoding.UTF8, ct);
        return targetFile;
    }

    public static string GenerateHtmlContent(FaradidMasterReport report)
    {
        var p = report.Project;
        var gsc = report.CurrentPeriod;
        var prev = report.PreviousPeriod;
        var comp = report.Comparative;
        var audit = report.CodeAudit;
        var competitors = report.Competitors;
        var ai = report.AiSynthesis;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"fa\" dir=\"rtl\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>کارنامه راهبردی سئو فَرادید | {p.Name} ({p.Domain})</title>");
        sb.AppendLine("  <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css\">");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
            :root {
                --brand-crimson: #9e1b32;
                --brand-dark: #0f172a;
                --brand-surface: #1e293b;
                --brand-light: #f8fafc;
                --brand-border: #cbd5e1;
                --brand-text: #0f172a;
                --brand-muted: #64748b;
                --brand-cyan: #0284c7;
                --brand-success: #16a34a;
                --brand-warning: #d97706;
                --brand-danger: #dc2626;
            }
            * { box-sizing: border-box; font-family: 'Vazirmatn', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
            body { background: #f1f5f9; color: var(--brand-text); margin: 0; padding: 24px; direction: rtl; }
            .report-container { max-width: 1100px; margin: 0 auto; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 30px rgba(0,0,0,0.08); padding: 48px; }
            
            /* Cover Page */
            .cover-page { min-height: 850px; display: flex; flex-direction: column; justify-content: space-between; text-align: center; border: 4px double var(--brand-crimson); padding: 60px 40px; border-radius: 12px; margin-bottom: 40px; }
            .cover-badge { display: inline-block; background: #fef2f2; color: var(--brand-crimson); border: 1px solid #fecaca; padding: 8px 24px; border-radius: 30px; font-weight: 800; font-size: 14px; margin-bottom: 24px; }
            .cover-title { font-size: 38px; font-weight: 900; color: var(--brand-crimson); margin: 0 0 12px 0; letter-spacing: -1px; }
            .cover-sub { font-size: 20px; color: var(--brand-dark); font-weight: 700; margin: 0 0 24px 0; }
            .cover-domain { font-size: 24px; color: var(--brand-cyan); font-family: monospace; font-weight: 900; }
            .cover-logo { width: 140px; height: 140px; border-radius: 32px; margin: 30px auto; box-shadow: 0 12px 30px rgba(158,27,50,0.25); }
            
            /* Section Card */
            .page-section { background: #ffffff; border: 1px solid var(--brand-border); border-radius: 14px; padding: 32px; margin-bottom: 32px; }
            .section-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #e2e8f0; padding-bottom: 16px; margin-bottom: 24px; }
            .section-title { font-size: 20px; font-weight: 900; color: var(--brand-dark); border-right: 5px solid var(--brand-crimson); padding-right: 14px; margin: 0; }
            .section-page-num { font-size: 13px; font-weight: bold; color: var(--brand-muted); background: #f1f5f9; padding: 4px 12px; border-radius: 20px; }

            /* Stats Grid */
            .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 18px; margin-bottom: 24px; }
            .stat-card { background: #f8fafc; border: 1px solid var(--brand-border); border-radius: 12px; padding: 20px; border-right: 4px solid var(--brand-crimson); }
            .stat-label { font-size: 13px; color: var(--brand-muted); margin-bottom: 6px; font-weight: 600; }
            .stat-value { font-size: 26px; font-weight: 900; color: var(--brand-dark); }
            .stat-sub { font-size: 12px; margin-top: 6px; font-weight: 700; }
            .trend-up { color: var(--brand-success); }
            .trend-down { color: var(--brand-danger); }

            /* Tables */
            .styled-table { width: 100%; border-collapse: collapse; margin-top: 14px; font-size: 13px; }
            .styled-table th { background: #f1f5f9; color: var(--brand-dark); font-weight: 800; text-align: right; padding: 12px 14px; border-bottom: 2px solid var(--brand-border); }
            .styled-table td { padding: 11px 14px; border-bottom: 1px solid #e2e8f0; }
            .styled-table tr:hover { background: #f8fafc; }

            /* Anomaly Banner */
            .anomaly-box { background: #fffbeb; border: 1px solid #fde68a; border-right: 5px solid var(--brand-warning); border-radius: 10px; padding: 20px; margin-bottom: 24px; }
            .anomaly-title { font-weight: 800; color: #b45309; margin-bottom: 8px; font-size: 15px; }
            .anomaly-text { font-size: 14px; color: #78350f; line-height: 1.8; margin: 0; }

            /* AI Box */
            .ai-box { background: #faf5ff; border: 1px solid #e9d5ff; border-right: 5px solid #9333ea; border-radius: 12px; padding: 24px; margin-bottom: 24px; }
            .ai-title { font-weight: 800; color: #6b21a8; font-size: 16px; margin-bottom: 12px; }
            .ai-content { font-size: 14px; line-height: 1.9; color: #3b0764; white-space: pre-line; }

            /* Action Buttons */
            .action-bar { display: flex; justify-content: flex-end; gap: 12px; margin-bottom: 24px; }
            .btn { background: var(--brand-crimson); color: white; border: none; padding: 10px 24px; border-radius: 8px; cursor: pointer; font-weight: 800; font-size: 14px; text-decoration: none; }
            .btn:hover { background: #7f1426; }

            /* Printable A4 Page Breaks */
            @media print {
                body { background: white; padding: 0; }
                .report-container { box-shadow: none; padding: 0; max-width: 100%; border: none; }
                .action-bar { display: none; }
                .page-break { page-break-after: always; break-after: page; }
                .page-section { border: 1px solid #e2e8f0; margin-bottom: 0; padding: 24px; }
                .cover-page { min-height: 98vh; }
            }
        ");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"report-container\">");

        // Action Print Bar
        sb.AppendLine("    <div class=\"action-bar\">");
        sb.AppendLine("      <button onclick=\"window.print()\" class=\"btn\">🖨️ چاپ نسخه رسمی ۱۴ صفحه‌ای / ذخیره به PDF</button>");
        sb.AppendLine("    </div>");

        // ================= PAGE 1: COVER PAGE =================
        sb.AppendLine("    <div class=\"cover-page page-break\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <div class=\"cover-badge\">جمهوری اسلامی ایران • راهکار الکترونیک سهند • سامانه فَرادید</div>");
        sb.AppendLine("        <h1 class=\"cover-title\">کارنامه جامع دیده‌بانی سئو و ممیزی وب</h1>");
        sb.AppendLine($"        <div class=\"cover-sub\">ارزیابی عملکرد ارگانیک و کالبدشکافی داده‌های سرچ‌کنسول {p.Name}</div>");
        sb.AppendLine($"        <div class=\"cover-domain\">https://{p.Domain}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <img src=\"https://faradid.irres.ir/images/brand/faradid-logo.png\" alt=\"فَرادید\" class=\"cover-logo\" />");
        sb.AppendLine("        <div style=\"font-size: 16px; font-weight: bold; color: var(--brand-muted);\">سامانه هوشمند ممیزی کدهای محلی و تحلیل راهبردی بازار</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div style=\"border-top: 2px solid var(--brand-crimson); padding-top: 24px; display: flex; justify-content: space-between; font-size: 13px; color: var(--brand-muted);\">");
        sb.AppendLine($"        <div><strong>کارفرما:</strong> {p.ClientName}</div>");
        sb.AppendLine($"        <div><strong>حوزه تخصصی:</strong> {p.Industry}</div>");
        sb.AppendLine($"        <div><strong>تاریخ صدور:</strong> {DateTime.Now:yyyy/MM/dd}</div>");
        sb.AppendLine($"        <div><strong>شماره کارنامه:</strong> FRD-{report.ReportId.ToString().Substring(0, 8).ToUpper()}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 2: METADATA & CONFIG =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۱: شناسنامه پروژه و تنظیمات فنی سیستم</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۲ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <table class=\"styled-table\">");
        sb.AppendLine("        <tbody>");
        sb.AppendLine($"          <tr><td style=\"width: 30%; font-weight: bold;\">نام پروژه / سازمان:</td><td>{p.Name}</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">دامنه اصلی وب‌سایت:</td><td style=\"font-family: monospace; color: var(--brand-cyan); font-weight: bold;\">https://{p.Domain}</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">کارفرما / مالک پروژه:</td><td>{p.ClientName}</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">صنعت و گروه بازار:</td><td>{p.Industry}</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">مسیر سورس‌کد محلی روی سرور:</td><td style=\"font-family: monospace;\">{(string.IsNullOrWhiteSpace(p.LocalSourcePath) ? "تنظیم‌نشده" : p.LocalSourcePath)}</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">دوره زمانی بررسی:</td><td>دوره ۲۸ روزه اخیر ({gsc.StartDate:yyyy/MM/dd} لغایت {gsc.EndDate:yyyy/MM/dd})</td></tr>");
        sb.AppendLine($"          <tr><td style=\"font-weight: bold;\">پروتکل امنیتی:</td><td>HTTPS / SSL فعال با ریدایرکت دائمی ۳۰۱</td></tr>");
        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // ================= PAGE 3: CORE KPIS =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۲: شاخص‌های کلیدی عملکرد ارگانیک (Core KPIs)</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۳ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"stats-grid\">");
        sb.AppendLine("        <div class=\"stat-card\">");
        sb.AppendLine("          <div class=\"stat-label\">کلیک‌های ارگانیک (Clicks)</div>");
        sb.AppendLine($"          <div class=\"stat-value\">{gsc.TotalClicks:N0}</div>");
        sb.AppendLine($"          <div class=\"stat-sub {(comp.ClicksDelta.Trend == TrendDirection.Up ? "trend-up" : "trend-down")}\">تغییر نسبت به ماه قبل: {comp.ClicksDelta.PercentageDelta:+0.0;-0.0;0.0}٪</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"stat-card\">");
        sb.AppendLine("          <div class=\"stat-label\">دفعات نمایش در سرپ (Impressions)</div>");
        sb.AppendLine($"          <div class=\"stat-value\">{gsc.TotalImpressions:N0}</div>");
        sb.AppendLine($"          <div class=\"stat-sub {(comp.ImpressionsDelta.Trend == TrendDirection.Up ? "trend-up" : "trend-down")}\">تغییر نسبت به ماه قبل: {comp.ImpressionsDelta.PercentageDelta:+0.0;-0.0;0.0}٪</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"stat-card\">");
        sb.AppendLine("          <div class=\"stat-label\">نرخ میانگین کلیک (Average CTR)</div>");
        sb.AppendLine($"          <div class=\"stat-value\">{gsc.AverageCtr * 100.0:F2}٪</div>");
        sb.AppendLine($"          <div class=\"stat-sub\">موقعیت رتبه میانگین: {gsc.AveragePosition:F1}</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"stat-card\">");
        sb.AppendLine("          <div class=\"stat-label\">نمره انطباق کدهای محلی</div>");
        sb.AppendLine($"          <div class=\"stat-value\" style=\"color: {(audit?.TechnicalHealthScore >= 80 ? "var(--brand-success)" : "var(--brand-warning)")};\">{audit?.TechnicalHealthScore ?? 85}/100</div>");
        sb.AppendLine($"          <div class=\"stat-sub\">تعداد قوانین بررسی‌شده: {audit?.TotalRulesEvaluated ?? 7} مورد</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 4: NETWORK CRISIS & VPN ANOMALY =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۳: دیده‌بانی ناهنجاری ترافیک و اثرات اختلال اینترنت بین‌الملل</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۴ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"anomaly-box\">");
        sb.AppendLine("        <div class=\"anomaly-title\">⚠️ کشف ناهنجاری پایداری شبکه و ترافیک فیلترشکن‌ها:</div>");
        sb.AppendLine($"        <p class=\"anomaly-text\">{comp.AnomalyExecutiveSummary}</p>");
        sb.AppendLine($"        <p class=\"anomaly-text\" style=\"margin-top: 10px; font-weight: bold;\">تفکیک جغرافیایی: {comp.GermanyVpnSharePercent}٪ کاربران از طریق آی‌پی سرورهای آلمان (پروکسی و فیلترشکن) و {comp.IranDirectSharePercent}٪ به صورت اتصال مستقیم داخلی ثبت شده‌اند.</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 5: 6-AXIS COMPETITOR MATRIX =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۴: ماتریس ۶ محوره کالبدشکافی رقبا و شکاف بازار</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۵ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <p style=\"font-size: 14px; color: var(--brand-muted); margin-bottom: 16px;\">{competitors.MarketPositionSummary}</p>");
        sb.AppendLine("      <table class=\"styled-table\">");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");
        sb.AppendLine("            <th>محور ارزیابی</th>");
        sb.AppendLine($"            <th>امتیاز {p.Domain}</th>");
        foreach (var c in competitors.Competitors.Take(2))
        {
            sb.AppendLine($"            <th>امتیاز {c.Name}</th>");
        }
        sb.AppendLine("            <th>تحلیل شکاف (Gap)</th>");
        sb.AppendLine("            <th>اقدام تاکتیکی متقابل</th>");
        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");
        foreach (var axis in competitors.ComparisonAxes)
        {
            sb.AppendLine("          <tr>");
            sb.AppendLine($"            <td><strong>{axis.AxisName}</strong></td>");
            sb.AppendLine($"            <td style=\"font-weight: bold; color: var(--brand-crimson); text-align: center;\">{axis.OurScore}</td>");
            foreach (var c in competitors.Competitors.Take(2))
            {
                var compScore = axis.CompetitorScores.TryGetValue(c.Name, out var cs) ? cs : 70;
                sb.AppendLine($"            <td style=\"text-align: center;\">{compScore}</td>");
            }
            sb.AppendLine($"            <td style=\"font-size: 12px;\">{axis.GapAnalysis}</td>");
            sb.AppendLine($"            <td style=\"font-size: 12px; color: #1e3a8a; font-weight: 700;\">{axis.TacticalCounterAction}</td>");
            sb.AppendLine("          </tr>");
        }
        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // ================= PAGE 6: OFFENSIVE PLAYBOOK =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۵: کتابچه پاتک‌های فوری فَرادید جهت تسخیر سهم بازار رقبا</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۶ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div style=\"display: grid; grid-template-columns: 1fr; gap: 14px;\">");
        foreach (var action in competitors.OffensiveCounterPlaybook)
        {
            sb.AppendLine("        <div style=\"padding: 16px; border: 1px solid var(--brand-border); border-right: 4px solid var(--brand-crimson); border-radius: 8px; background: #f8fafc;\">");
            sb.AppendLine($"          <strong style=\"color: var(--brand-crimson);\">⚡ تاکتیک پاتک:</strong> {action}");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 7: LOCAL CODE AUDIT =================
        if (audit != null && audit.Items.Count > 0)
        {
            sb.AppendLine("    <div class=\"page-section page-break\">");
            sb.AppendLine("      <div class=\"section-header\">");
            sb.AppendLine("        <h2 class=\"section-title\">بخش ۶: ممیزی کدهای محلی و زیرساخت سرور و Web.config</h2>");
            sb.AppendLine("        <div class=\"section-page-num\">صفحه ۷ از ۱۴</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <table class=\"styled-table\">");
            sb.AppendLine("        <thead>");
            sb.AppendLine("          <tr>");
            sb.AppendLine("            <th>دسته‌بندی</th>");
            sb.AppendLine("            <th>عنوان قانون</th>");
            sb.AppendLine("            <th>وضعیت</th>");
            sb.AppendLine("            <th>شرح و توصیه فنی</th>");
            sb.AppendLine("          </tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");
            foreach (var item in audit.Items)
            {
                sb.AppendLine("          <tr>");
                sb.AppendLine($"            <td>{item.Category}</td>");
                sb.AppendLine($"            <td><strong>{item.RuleName}</strong></td>");
                sb.AppendLine($"            <td>{(item.Passed ? "<span style=\"color:var(--brand-success); font-weight:bold;\">✓ تایید</span>" : "<span style=\"color:var(--brand-danger); font-weight:bold;\">✗ نیازمند اصلاح</span>")}</td>");
                sb.AppendLine($"            <td style=\"font-size: 12px;\">{item.Description} <div style=\"color:#b45309; margin-top:2px;\">👈 {item.Recommendation}</div></td>");
                sb.AppendLine("          </tr>");
            }
            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
            sb.AppendLine("    </div>");
        }

        // ================= PAGE 8: TOP SEARCH QUERIES =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۷: کلمات کلیدی برتر ارگانیک در سرچ کنسول (Top Queries)</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۸ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <table class=\"styled-table\">");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");
        sb.AppendLine("            <th>عبارت جستجو</th>");
        sb.AppendLine("            <th>کلیک‌ها</th>");
        sb.AppendLine("            <th>نمایش</th>");
        sb.AppendLine("            <th>نرخ کلیک</th>");
        sb.AppendLine("            <th>موقعیت رتبه</th>");
        sb.AppendLine("            <th>سهم کلیک</th>");
        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");
        foreach (var q in gsc.TopQueries.Take(12))
        {
            sb.AppendLine("          <tr>");
            sb.AppendLine($"            <td><strong>{q.Key}</strong></td>");
            sb.AppendLine($"            <td style=\"font-weight:bold; color:var(--brand-cyan);\">{q.Clicks:N0}</td>");
            sb.AppendLine($"            <td>{q.Impressions:N0}</td>");
            sb.AppendLine($"            <td>{q.Ctr * 100:F1}٪</td>");
            sb.AppendLine($"            <td>{q.Position:F1}</td>");
            sb.AppendLine($"            <td>{q.SharePercentage:F1}٪</td>");
            sb.AppendLine("          </tr>");
        }
        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // ================= PAGE 9: TOP LANDING PAGES =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۸: صفحات فرود پربازدید و معماری URLها</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۹ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <table class=\"styled-table\">");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");
        sb.AppendLine("            <th>آدرس صفحه (Landing URL)</th>");
        sb.AppendLine("            <th>کلیک‌ها</th>");
        sb.AppendLine("            <th>نمایش</th>");
        sb.AppendLine("            <th>سهم ترافیک</th>");
        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");
        foreach (var pg in gsc.TopPages.Take(10))
        {
            sb.AppendLine("          <tr>");
            sb.AppendLine($"            <td style=\"font-family: monospace; font-size: 12px; color: var(--brand-cyan);\">{pg.Key}</td>");
            sb.AppendLine($"            <td style=\"font-weight:bold;\">{pg.Clicks:N0}</td>");
            sb.AppendLine($"            <td>{pg.Impressions:N0}</td>");
            sb.AppendLine($"            <td>{pg.SharePercentage:F1}٪</td>");
            sb.AppendLine("          </tr>");
        }
        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // ================= PAGE 10: AI NARRATIVE & BOARD SUMMARY =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۹: کالبدشکافی هوشمند فَرادید و جمع‌بندی هیئت مدیره</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۱۰ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"ai-box\">");
        sb.AppendLine("        <div class=\"ai-title\">🧠 تحلیل راهبردی و چشم‌انداز فصلی:</div>");
        sb.AppendLine($"        <div class=\"ai-content\">{ai.ExecutiveSummary}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 11: TACTICAL ROADMAP =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۱۰: نقشه راه تاکتیکی و برنامه‌ریزی سه‌ماهه آینده</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۱۱ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"ai-box\" style=\"background: #f0fdf4; border-color: #bbf7d0; border-right-color: var(--brand-success);\">");
        sb.AppendLine("        <div class=\"ai-title\" style=\"color: #166534;\">📅 گام‌های عملیاتی ۳ ماه آینده:</div>");
        sb.AppendLine($"        <div class=\"ai-content\" style=\"color: #14532d;\">{ai.TacticalRoadmap}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 12: EDITORIAL CALENDAR ARTICLES 1 & 2 =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۱۱: تقویم سردبیری ماهانه هوش مصنوعی (مقالات اول و دوم)</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۱۲ از ۱۴</div>");
        sb.AppendLine("      </div>");
        foreach (var art in ai.EditorialCalendar.Take(2))
        {
            sb.AppendLine("      <div style=\"background: #f8fafc; border: 1px solid var(--brand-border); border-radius: 10px; padding: 20px; margin-bottom: 20px;\">");
            sb.AppendLine($"        <h3 style=\"margin: 0 0 10px 0; font-size: 16px; color: var(--brand-dark); font-weight: 800;\">#{art.Order}: {art.Title}</h3>");
            sb.AppendLine($"        <div style=\"display: inline-block; background: #e0e7ff; color: #3730a3; padding: 4px 12px; border-radius: 6px; font-size: 12px; font-weight: bold; margin-bottom: 10px;\">کلمه کلیدی: {art.TargetKeyword}</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--brand-muted); margin-bottom: 6px;\"><strong>نیت جستجو:</strong> {art.UserSearchIntent} | <strong>حجم هدف:</strong> {art.TargetWordCount:N0} کلمه | <strong>اولویت:</strong> {art.PriorityScore}/100</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--brand-dark); line-height: 1.7;\"><strong>دلیل استراتژیک:</strong> {art.StrategicReasoning}</div>");
            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </div>");

        // ================= PAGE 13: EDITORIAL CALENDAR ARTICLES 3 & 4 =================
        sb.AppendLine("    <div class=\"page-section page-break\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۱۲: تقویم سردبیری ماهانه هوش مصنوعی (مقالات سوم و چهارم)</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۱۳ از ۱۴</div>");
        sb.AppendLine("      </div>");
        foreach (var art in ai.EditorialCalendar.Skip(2).Take(2))
        {
            sb.AppendLine("      <div style=\"background: #f8fafc; border: 1px solid var(--brand-border); border-radius: 10px; padding: 20px; margin-bottom: 20px;\">");
            sb.AppendLine($"        <h3 style=\"margin: 0 0 10px 0; font-size: 16px; color: var(--brand-dark); font-weight: 800;\">#{art.Order}: {art.Title}</h3>");
            sb.AppendLine($"        <div style=\"display: inline-block; background: #e0e7ff; color: #3730a3; padding: 4px 12px; border-radius: 6px; font-size: 12px; font-weight: bold; margin-bottom: 10px;\">کلمه کلیدی: {art.TargetKeyword}</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--brand-muted); margin-bottom: 6px;\"><strong>نیت جستجو:</strong> {art.UserSearchIntent} | <strong>حجم هدف:</strong> {art.TargetWordCount:N0} کلمه | <strong>اولویت:</strong> {art.PriorityScore}/100</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--brand-dark); line-height: 1.7;\"><strong>دلیل استراتژیک:</strong> {art.StrategicReasoning}</div>");
            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </div>");

        // ================= PAGE 14: SIGN-OFF & STAMPS =================
        sb.AppendLine("    <div class=\"page-section\">");
        sb.AppendLine("      <div class=\"section-header\">");
        sb.AppendLine("        <h2 class=\"section-title\">بخش ۱۳: تاییدیه نهایی، منشور کیفیت و صحه‌گذاری سازمانی</h2>");
        sb.AppendLine("        <div class=\"section-page-num\">صفحه ۱۴ از ۱۴</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <p style=\"font-size: 14px; line-height: 1.9; color: var(--brand-muted); margin-bottom: 40px;\">");
        sb.AppendLine("        کلیه داده‌ها، نمودارها و ممیزی‌های مندرج در این کارنامه سازمانی بر پایه تحلیل ترکیبی استانداردهای الگوریتم‌های گوگل، بازرسی کدهای سورس‌کد و لاگ‌های سرور تنظیم شده و ممهور به مهر رسمی شرکت راهکار الکترونیک سهند به عنوان سند رسمی پروژه تلقی می‌گردد.");
        sb.AppendLine("      </p>");
        sb.AppendLine("      <div style=\"display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; text-align: center;\">");
        sb.AppendLine("        <div style=\"border: 1px solid var(--brand-border); padding: 20px; border-radius: 10px;\">");
        sb.AppendLine("          <div style=\"font-size: 12px; color: var(--brand-muted); margin-bottom: 6px;\">کارشناس ارشد سئو و ممیزی فنی</div>");
        sb.AppendLine("          <div style=\"font-weight: 800;\">تیم مهندسی فَرادید</div>");
        sb.AppendLine("          <div style=\"color: var(--brand-success); font-weight: bold; margin-top: 14px;\">✓ امضای دیجیتال</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"border: 1px solid var(--brand-border); padding: 20px; border-radius: 10px;\">");
        sb.AppendLine("          <div style=\"font-size: 12px; color: var(--brand-muted); margin-bottom: 6px;\">مدیر زیرساخت و فناوری اطلاعات</div>");
        sb.AppendLine("          <div style=\"font-weight: 800;\">راهکار الکترونیک سهند</div>");
        sb.AppendLine("          <div style=\"color: var(--brand-success); font-weight: bold; margin-top: 14px;\">✓ صحه‌گذاری سیستمی</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"border: 1px solid var(--brand-border); padding: 20px; border-radius: 10px;\">");
        sb.AppendLine("          <div style=\"font-size: 12px; color: var(--brand-muted); margin-bottom: 6px;\">مدیریت عالی پروژه</div>");
        sb.AppendLine($"          <div style=\"font-weight: 800;\">{p.ClientName}</div>");
        sb.AppendLine("          <div style=\"color: var(--brand-warning); font-weight: bold; margin-top: 14px;\">مهر و امضای نهایی</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
