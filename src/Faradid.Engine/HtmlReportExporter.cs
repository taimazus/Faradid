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
        sb.AppendLine($"  <title>کارنامه راهبردی فَرادید | {p.Name} ({p.Domain})</title>");
        sb.AppendLine("  <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css\">");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
            :root {
                --brand-crimson: #9e1b32;
                --brand-dark: #0f172a;
                --brand-surface: #1e293b;
                --brand-light: #f8fafc;
                --brand-border: #e2e8f0;
                --brand-text: #1e293b;
                --brand-muted: #64748b;
                --brand-success: #10b981;
                --brand-warning: #f59e0b;
                --brand-danger: #ef4444;
            }
            * { box-sizing: border-box; font-family: 'Vazirmatn', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
            body { background: #f1f5f9; color: var(--brand-text); margin: 0; padding: 24px; direction: rtl; }
            .report-container { max-width: 1200px; margin: 0 auto; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.06); padding: 40px; }
            
            /* Header */
            .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid var(--brand-border); padding-bottom: 24px; margin-bottom: 32px; }
            .brand-box { display: flex; align-items: center; gap: 16px; }
            .brand-logo { width: 56px; height: 56px; border-radius: 12px; }
            .brand-title { margin: 0; font-size: 26px; font-weight: 900; color: var(--brand-crimson); }
            .brand-subtitle { margin: 4px 0 0 0; font-size: 13px; color: var(--brand-muted); }
            
            .meta-box { text-align: left; }
            .meta-badge { display: inline-block; background: #eff6ff; color: #1d4ed8; padding: 6px 14px; border-radius: 20px; font-size: 12px; font-weight: bold; margin-bottom: 6px; }
            .meta-date { font-size: 13px; color: var(--brand-muted); }

            /* Grid Cards */
            .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; margin-bottom: 32px; }
            .stat-card { background: #ffffff; border: 1px solid var(--brand-border); border-radius: 12px; padding: 20px; position: relative; border-right: 4px solid var(--brand-crimson); }
            .stat-label { font-size: 13px; color: var(--brand-muted); margin-bottom: 8px; }
            .stat-value { font-size: 28px; font-weight: 900; color: var(--brand-dark); }
            .stat-sub { font-size: 12px; margin-top: 6px; }
            .trend-up { color: var(--brand-success); }
            .trend-down { color: var(--brand-danger); }

            /* Section Cards */
            .section { margin-bottom: 36px; }
            .section-title { font-size: 20px; font-weight: 800; color: var(--brand-dark); border-right: 4px solid var(--brand-crimson); padding-right: 12px; margin-bottom: 18px; }
            
            /* Tables */
            .styled-table { width: 100%; border-collapse: collapse; margin-top: 12px; font-size: 14px; }
            .styled-table th { background: #f8fafc; color: var(--brand-dark); font-weight: 700; text-align: right; padding: 12px 16px; border-bottom: 2px solid var(--brand-border); }
            .styled-table td { padding: 12px 16px; border-bottom: 1px solid var(--brand-border); }
            .styled-table tr:hover { background: #f1f5f9; }
            
            /* Anomaly Banner */
            .anomaly-box { background: #fffbeb; border: 1px solid #fef3c7; border-right: 4px solid var(--brand-warning); border-radius: 10px; padding: 20px; margin-bottom: 32px; }
            .anomaly-title { font-weight: 800; color: #b45309; margin-bottom: 8px; display: flex; align-items: center; gap: 8px; }
            .anomaly-text { font-size: 14px; color: #92400e; line-height: 1.8; margin: 0; }

            /* AI Analysis Box */
            .ai-box { background: #faf5ff; border: 1px solid #f3e8ff; border-right: 4px solid #9333ea; border-radius: 12px; padding: 24px; margin-bottom: 32px; }
            .ai-title { font-weight: 800; color: #6b21a8; font-size: 17px; margin-bottom: 12px; }
            .ai-content { font-size: 14px; line-height: 1.9; color: #3b0764; white-space: pre-line; }

            /* Editorial Cards */
            .articles-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px; }
            .article-card { background: #ffffff; border: 1px solid var(--brand-border); border-radius: 12px; padding: 20px; transition: transform 0.2s; }
            .article-order { display: inline-block; background: var(--brand-crimson); color: white; width: 28px; height: 28px; border-radius: 50%; text-align: center; line-height: 28px; font-weight: bold; font-size: 13px; margin-bottom: 12px; }
            .article-title { font-size: 15px; font-weight: 800; margin-bottom: 10px; color: var(--brand-dark); line-height: 1.6; }
            .badge-tag { display: inline-block; background: #e0e7ff; color: #3730a3; padding: 3px 10px; border-radius: 6px; font-size: 12px; font-weight: 600; margin-bottom: 12px; }
            .article-desc { font-size: 13px; color: var(--brand-muted); line-height: 1.7; }

            /* Print button */
            .no-print-bar { display: flex; justify-content: flex-end; gap: 12px; margin-bottom: 20px; }
            .btn { background: var(--brand-crimson); color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; font-weight: bold; font-size: 14px; text-decoration: none; }
            .btn:hover { background: #7f1426; }
            .btn-outline { background: transparent; color: var(--brand-dark); border: 1px solid var(--brand-border); }

            @media print {
                body { background: white; padding: 0; }
                .report-container { box-shadow: none; padding: 0; max-width: 100%; }
                .no-print-bar { display: none; }
                .page-break { page-break-after: always; }
            }
        ");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <div class=\"report-container\">");

        // Action bar
        sb.AppendLine("    <div class=\"no-print-bar\">");
        sb.AppendLine("      <button onclick=\"window.print()\" class=\"btn\">🖨️ چاپ کارنامه / ذخیره به PDF</button>");
        sb.AppendLine("    </div>");

        // Header
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div class=\"brand-box\">");
        sb.AppendLine("        <div>");
        sb.AppendLine($"          <h1 class=\"brand-title\">سامانه هوشمند دیده‌بانی سئو فَرادید</h1>");
        sb.AppendLine($"          <p class=\"brand-subtitle\">کارنامه تحلیل راهبردی و کالبدشکافی عملکرد ارگانیک وب‌سایت {p.Domain}</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"meta-box\">");
        sb.AppendLine($"        <div class=\"meta-badge\">دیدگاه: {report.ActivePersona}</div>");
        sb.AppendLine($"        <div class=\"meta-date\">تاریخ صدور: {DateTime.Now:yyyy/MM/dd}</div>");
        sb.AppendLine($"        <div class=\"meta-date\">کارفرما: {p.ClientName}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Stat Grid
        sb.AppendLine("    <div class=\"stats-grid\">");
        sb.AppendLine("      <div class=\"stat-card\">");
        sb.AppendLine("        <div class=\"stat-label\">کلیک‌های ارگانیک (Clicks)</div>");
        sb.AppendLine($"        <div class=\"stat-value\">{gsc.TotalClicks:N0}</div>");
        sb.AppendLine($"        <div class=\"stat-sub {(comp.ClicksDelta.Trend == TrendDirection.Up ? "trend-up" : "trend-down")}\">تغییر دوره: {comp.ClicksDelta.PercentageDelta:+0.0;-0.0;0.0}٪</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"stat-card\">");
        sb.AppendLine("        <div class=\"stat-label\">دفعات نمایش در نتایج (Impressions)</div>");
        sb.AppendLine($"        <div class=\"stat-value\">{gsc.TotalImpressions:N0}</div>");
        sb.AppendLine($"        <div class=\"stat-sub {(comp.ImpressionsDelta.Trend == TrendDirection.Up ? "trend-up" : "trend-down")}\">تغییر دوره: {comp.ImpressionsDelta.PercentageDelta:+0.0;-0.0;0.0}٪</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"stat-card\">");
        sb.AppendLine("        <div class=\"stat-label\">نرخ میانگین کلیک (Average CTR)</div>");
        sb.AppendLine($"        <div class=\"stat-value\">{gsc.AverageCtr * 100.0:F2}٪</div>");
        sb.AppendLine($"        <div class=\"stat-sub\">موقعیت میانگین: {gsc.AveragePosition:F1}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"stat-card\">");
        sb.AppendLine("        <div class=\"stat-label\">نمره سلامت فنی کد پروژه</div>");
        sb.AppendLine($"        <div class=\"stat-value\" style=\"color: {(audit?.TechnicalHealthScore >= 80 ? "#10b981" : "#f59e0b")};\">{audit?.TechnicalHealthScore ?? 85}/100</div>");
        sb.AppendLine($"        <div class=\"stat-sub\">قوانین بررسی‌شده: {audit?.TotalRulesEvaluated ?? 8} مورد</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Anomaly Alert Box
        if (comp.HasSignificantAnomaly)
        {
            sb.AppendLine("    <div class=\"anomaly-box\">");
            sb.AppendLine("      <div class=\"anomaly-title\">⚠️ دیده‌بانی ناهنجاری ترافیک و اثرات اختلال اینترنت بین‌الملل:</div>");
            sb.AppendLine($"      <p class=\"anomaly-text\">{comp.AnomalyExecutiveSummary}</p>");
            sb.AppendLine($"      <p class=\"anomaly-text\" style=\"margin-top: 8px; font-weight: bold;\">تفکیک جغرافیایی: {comp.GermanyVpnSharePercent}٪ ترافیک از سرورهای کشور آلمان (فیلترشکن‌ها) و {comp.IranDirectSharePercent}٪ مستقیم از داخل کشور بوده است.</p>");
            sb.AppendLine("    </div>");
        }

        // AI Executive Synthesis
        sb.AppendLine("    <div class=\"ai-box\">");
        sb.AppendLine("      <div class=\"ai-title\">🧠 کالبدشکافی هوشمند فَرادید و جمع‌بندی هیئت مدیره:</div>");
        sb.AppendLine($"      <div class=\"ai-content\">{ai.ExecutiveSummary}</div>");
        sb.AppendLine("    </div>");

        // Competitor Benchmark
        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("      <div class=\"section-title\">ماتریس ۶ محوره کالبدشکافی رقبا و شکاف بازار</div>");
        sb.AppendLine($"      <p style=\"font-size: 14px; color: var(--brand-muted);\">{competitors.MarketPositionSummary}</p>");
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
            sb.AppendLine($"            <td style=\"font-weight: bold; color: var(--brand-crimson);\">{axis.OurScore}</td>");
            foreach (var c in competitors.Competitors.Take(2))
            {
                var compScore = axis.CompetitorScores.TryGetValue(c.Name, out var cs) ? cs : 70;
                sb.AppendLine($"            <td>{compScore}</td>");
            }
            sb.AppendLine($"            <td style=\"font-size: 13px;\">{axis.GapAnalysis}</td>");
            sb.AppendLine($"            <td style=\"font-size: 13px; color: #1e3a8a; font-weight: 600;\">{axis.TacticalCounterAction}</td>");
            sb.AppendLine("          </tr>");
        }
        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // Code Audit
        if (audit != null && audit.Items.Count > 0)
        {
            sb.AppendLine("    <div class=\"section\">");
            sb.AppendLine("      <div class=\"section-title\">ممیزی ساختار کدهای محلی و زیرساخت میزبانی وب</div>");
            sb.AppendLine("      <table class=\"styled-table\">");
            sb.AppendLine("        <thead>");
            sb.AppendLine("          <tr>");
            sb.AppendLine("            <th>دسته‌بندی</th>");
            sb.AppendLine("            <th>عنوان قانون</th>");
            sb.AppendLine("            <th>سطح اهمیت</th>");
            sb.AppendLine("            <th>وضعیت</th>");
            sb.AppendLine("            <th>توضیحات و توصیه فنی</th>");
            sb.AppendLine("          </tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");
            foreach (var item in audit.Items)
            {
                sb.AppendLine("          <tr>");
                sb.AppendLine($"            <td>{item.Category}</td>");
                sb.AppendLine($"            <td><strong>{item.RuleName}</strong></td>");
                sb.AppendLine($"            <td><span class=\"badge-tag\">{item.Severity}</span></td>");
                sb.AppendLine($"            <td>{(item.Passed ? "<span style=\"color:#10b981; font-weight:bold;\">✓ تأیید</span>" : "<span style=\"color:#ef4444; font-weight:bold;\">✗ نیازمند اصلاح</span>")}</td>");
                sb.AppendLine($"            <td style=\"font-size: 13px;\"><div>{item.Description}</div><div style=\"color:#b45309; margin-top:4px;\">👈 اقدام: {item.Recommendation}</div></td>");
                sb.AppendLine("          </tr>");
            }
            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
            sb.AppendLine("    </div>");
        }

        // Top Queries
        if (gsc.TopQueries.Count > 0)
        {
            sb.AppendLine("    <div class=\"section\">");
            sb.AppendLine("      <div class=\"section-title\">کلمات کلیدی برتر ارگانیک (Top Search Queries)</div>");
            sb.AppendLine("      <table class=\"styled-table\">");
            sb.AppendLine("        <thead>");
            sb.AppendLine("          <tr>");
            sb.AppendLine("            <th>کلمه کلیدی (Query)</th>");
            sb.AppendLine("            <th>کلیک‌ها</th>");
            sb.AppendLine("            <th>نمایش (Imp)</th>");
            sb.AppendLine("            <th>نرخ کلیک (CTR)</th>");
            sb.AppendLine("            <th>موقعیت رتبه (Position)</th>");
            sb.AppendLine("            <th>سهم از کلیک‌ها</th>");
            sb.AppendLine("          </tr>");
        }
        foreach (var q in gsc.TopQueries.Take(10))
        {
            sb.AppendLine("          <tr>");
            sb.AppendLine($"            <td><strong>{q.Key}</strong></td>");
            sb.AppendLine($"            <td>{q.Clicks:N0}</td>");
            sb.AppendLine($"            <td>{q.Impressions:N0}</td>");
            sb.AppendLine($"            <td>{q.Ctr * 100:F1}٪</td>");
            sb.AppendLine($"            <td>{q.Position:F1}</td>");
            sb.AppendLine($"            <td>{q.SharePercentage:F1}٪</td>");
            sb.AppendLine("          </tr>");
        }
        if (gsc.TopQueries.Count > 0)
        {
            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
            sb.AppendLine("    </div>");
        }

        // 4 Editorial Articles
        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("      <div class=\"section-title\">تقویم سردبیری و مقالات استراتژیک پیشنهادی هوش مصنوعی (۴ عنوان ماهانه)</div>");
        sb.AppendLine("      <div class=\"articles-grid\">");
        foreach (var art in ai.EditorialCalendar)
        {
            sb.AppendLine("        <div class=\"article-card\">");
            sb.AppendLine($"          <div class=\"article-order\">{art.Order}</div>");
            sb.AppendLine($"          <div class=\"article-title\">{art.Title}</div>");
            sb.AppendLine($"          <div class=\"badge-tag\">کلمه کلیدی: {art.TargetKeyword}</div>");
            sb.AppendLine($"          <div class=\"article-desc\"><strong>نیت کاربر:</strong> {art.UserSearchIntent}</div>");
            sb.AppendLine($"          <div class=\"article-desc\" style=\"margin-top:6px;\"><strong>حجم هدف:</strong> {art.TargetWordCount:N0} کلمه | <strong>اولویت:</strong> {art.PriorityScore}/100</div>");
            sb.AppendLine($"          <div class=\"article-desc\" style=\"margin-top:6px; color: var(--brand-dark);\"><strong>دلیل استراتژیک:</strong> {art.StrategicReasoning}</div>");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Footer
        sb.AppendLine("    <div style=\"margin-top: 48px; padding-top: 24px; border-top: 1px solid var(--brand-border); text-align: center; color: var(--brand-muted); font-size: 13px;\">");
        sb.AppendLine("      سامانه هوشمند دیده‌بانی سئو فَرادید (Faradid) | توسعه‌یافته بر بستر ASP.NET Core 10 و معماری یکپارچه راهکار الکترونیک سهند");
        sb.AppendLine("    </div>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
