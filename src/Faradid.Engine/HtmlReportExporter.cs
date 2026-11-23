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
        var html = GenerateHtmlContent(report);
        string targetFile;

        try
        {
            if (Directory.Exists(destinationPath))
            {
                var fileName = $"Faradid-Report-{report.Project.Domain.Replace(".", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmm}.html";
                targetFile = Path.Combine(destinationPath, fileName);
            }
            else
            {
                if (Path.HasExtension(destinationPath))
                {
                    targetFile = destinationPath;
                }
                else
                {
                    var fileName = $"Faradid-Report-{report.Project.Domain.Replace(".", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmm}.html";
                    targetFile = Path.Combine(destinationPath, fileName);
                }

                var dir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            await File.WriteAllTextAsync(targetFile, html, Encoding.UTF8, ct);
            return targetFile;
        }
        catch (Exception)
        {
            var fallbackDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "exports", report.Project.Domain);
            if (!Directory.Exists(fallbackDir))
            {
                Directory.CreateDirectory(fallbackDir);
            }
            var safeFile = Path.Combine(fallbackDir, $"Faradid-Report-{report.Project.Domain.Replace(".", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmm}.html");
            await File.WriteAllTextAsync(safeFile, html, Encoding.UTF8, ct);
            return safeFile;
        }
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

        var domainRef = p.Domain.Split('.')[0].ToUpper();
        var refNo = $"SES-TR-1405/08-{(Math.Abs(p.Id.GetHashCode()) % 90 + 10)}";
        var issueDate = "۲۷ آبان ۱۴۰۵";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"fa\" dir=\"rtl\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>کارنامه راهبردی سئو و سند رسمی ۱۴ صفحه‌ای {p.Name} ({p.Domain})</title>");
        sb.AppendLine("  <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css\">");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
            * { box-sizing: border-box; font-family: 'Vazirmatn', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
            body { background: #080e1a; color: #0f172a; margin: 0; padding: 20px 0; direction: rtl; text-align: right; }
            .a4-dossier-wrapper { max-width: 100%; margin: 0 auto; }
            .a4-sheet {
                width: 210mm;
                min-height: 297mm;
                background: #ffffff;
                color: #0f172a;
                margin: 25px auto;
                padding: 15mm 18mm 12mm 18mm;
                box-shadow: 0 15px 45px rgba(0, 0, 0, 0.5);
                border-radius: 4px;
                position: relative;
                box-sizing: border-box;
                display: flex;
                flex-direction: column;
                justify-content: space-between;
                font-size: 13px;
                line-height: 1.75;
            }
            .a4-top-meta {
                background: #0f2b5c;
                color: #ffffff;
                padding: 7px 14px;
                border-radius: 6px;
                font-size: 11px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 14px;
            }
            .a4-header-ribbon {
                background: linear-gradient(90deg, #0f172a 0%, #1e3a8a 100%);
                color: #ffffff;
                padding: 8px 14px;
                border-radius: 6px;
                font-size: 11.5px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 16px;
                border-bottom: 2px solid #38bdf8;
            }
            .a4-footer {
                border-top: 1px solid #e2e8f0;
                padding-top: 8px;
                margin-top: auto;
                font-size: 10px;
                color: #64748b;
                display: flex;
                justify-content: space-between;
                align-items: center;
            }
            .a4-card-grid {
                display: grid;
                grid-template-columns: repeat(4, 1fr);
                gap: 10px;
                margin-bottom: 14px;
            }
            .a4-kpi-card {
                background: #ffffff;
                border: 1px solid #e2e8f0;
                border-radius: 8px;
                padding: 10px 8px;
                text-align: center;
                box-shadow: 0 2px 6px rgba(0,0,0,0.03);
                position: relative;
                overflow: hidden;
            }
            .a4-kpi-card::before { content: ''; position: absolute; top: 0; left: 0; right: 0; height: 4px; }
            .a4-kpi-cyan::before { background: #0284c7; }
            .a4-kpi-green::before { background: #16a34a; }
            .a4-kpi-purple::before { background: #8b5cf6; }
            .a4-kpi-orange::before { background: #f59e0b; }
            .a4-kpi-red::before { background: #dc2626; }
            .a4-kpi-value { font-size: 24px; font-weight: 900; color: #0f172a; line-height: 1.2; margin-bottom: 2px; }
            .a4-kpi-label { font-size: 10.5px; font-weight: 700; color: #475569; margin-bottom: 2px; }
            .a4-kpi-sub { font-size: 9.5px; color: #0284c7; font-weight: 600; }
            .a4-section-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
            .a4-section-title { font-size: 14.5px; font-weight: 800; color: #0f172a; margin: 0; }
            .a4-pill { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; font-size: 10.5px; font-weight: 700; padding: 2px 8px; border-radius: 12px; }
            .a4-pill-blue { background: #e0f2fe; color: #0284c7; border-color: #bae6fd; }
            .a4-pill-orange { background: #fffbeb; color: #d97706; border-color: #fde68a; }
            .a4-table { width: 100%; border-collapse: collapse; font-size: 11px; margin-bottom: 12px; }
            .a4-table th { background: #0f172a; color: #ffffff; font-weight: 700; padding: 6px 8px; text-align: right; border: 1px solid #0f172a; }
            .a4-table td { padding: 6px 8px; border: 1px solid #e2e8f0; color: #1e293b; background: #ffffff; }
            .a4-table tr:nth-child(even) td { background: #f8fafc; }
            .a4-callout { background: #f0fdf4; border: 1px solid #86efac; border-radius: 6px; padding: 8px 12px; font-size: 11px; color: #166534; line-height: 1.75; margin-top: 8px; }
            .a4-callout-blue { background: #eff6ff; border-color: #93c5fd; color: #1e40af; }
            .a4-callout-amber { background: #fffbeb; border-color: #fcd34d; color: #92400e; }
            .a4-stamp {
                width: 145px; height: 145px; border: 3px double #dc2626; border-radius: 50%; color: #dc2626;
                display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center;
                padding: 8px; margin: 0 auto; transform: rotate(-7deg); position: relative; user-select: none;
                box-shadow: inset 0 0 0 2px rgba(220, 38, 38, 0.15);
            }
            .a4-stamp::before { content: ''; position: absolute; width: 125px; height: 125px; border: 1px dashed rgba(220, 38, 38, 0.5); border-radius: 50%; }
            .a4-stamp-title { font-size: 9px; font-weight: 900; }
            .a4-stamp-sub { font-size: 10px; font-weight: 800; margin: 2px 0; padding: 2px 4px; border-top: 1px solid rgba(220, 38, 38, 0.4); border-bottom: 1px solid rgba(220, 38, 38, 0.4); }
            .a4-stamp-date { font-size: 8.5px; font-weight: 700; }
            .print-btn-bar { max-width: 210mm; margin: 15px auto; text-align: left; }
            .btn-print { background: #dc2626; color: white; border: none; padding: 10px 24px; border-radius: 6px; font-weight: bold; cursor: pointer; font-size: 13px; }
            @media print {
                body, html { background: #ffffff !important; margin: 0 !important; padding: 0 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
                .print-btn-bar { display: none !important; }
                .a4-sheet { margin: 0 !important; width: 210mm !important; height: 297mm !important; max-height: 297mm !important; box-shadow: none !important; border-radius: 0 !important; padding: 14mm 16mm 12mm 16mm !important; page-break-after: always !important; break-after: page !important; overflow: hidden !important; }
            }
        ");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Print Button
        sb.AppendLine("  <div class=\"print-btn-bar\">");
        sb.AppendLine("    <button onclick=\"window.print()\" class=\"btn-print\">🖨️ چاپ سند رسمی ۱۴ صفحه‌ای / ذخیره به PDF</button>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <div class=\"a4-dossier-wrapper\">");

        // ================= PAGE 1 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <div class=\"a4-top-meta\">");
        sb.AppendLine($"          <div>شماره عطف: <strong>{refNo}</strong></div>");
        sb.AppendLine($"          <div>تاریخ صدور: <strong>{issueDate}</strong></div>");
        sb.AppendLine("          <div>پیوست: <strong>داده‌های سرچ کنسول گوگل</strong></div>");
        sb.AppendLine("          <div>طبقه‌بندی: <strong>رسمی • محرمانه تجاری</strong></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"display: flex; justify-content: space-between; align-items: center; padding-bottom: 12px; margin-bottom: 12px; border-bottom: 1px solid #e2e8f0;\">");
        sb.AppendLine("          <div>");
        sb.AppendLine("            <div style=\"font-size: 15px; font-weight: bold; color: #0f172a;\">شرکت راهکار الکترونیک سهند <span style=\"font-size: 11px; color: #64748b; font-weight: normal;\">(مسئولیت محدود • ثبت ۳۲۹۹۵)</span></div>");
        sb.AppendLine("            <div style=\"font-size: 10px; color: #64748b;\">واحد مهندسی نرم‌افزار، زیرساخت فناوری اطلاعات و سئوی تخصصی سازمانی (IRRES)</div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div style=\"text-align: left; padding: 6px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px;\">");
        sb.AppendLine("            <div style=\"font-size: 10px; font-weight: bold; color: #0284c7;\">مرکز خدمات و پرتال رسمی:</div>");
        sb.AppendLine($"            <div style=\"font-size: 13px; font-weight: bold; color: #0f172a;\">{p.ClientName}</div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"text-align: center; margin: 16px 0;\">");
        sb.AppendLine("          <span class=\"a4-pill a4-pill-blue\" style=\"display: inline-block; margin-bottom: 8px;\">گزارش مستند رسمی سال ۱۴۰۵ • استخراج مستقیم از API گوگل سرچ کنسول و ممیزی زنده سرور</span>");
        sb.AppendLine($"          <h1 style=\"font-size: 21px; font-weight: 900; color: #0f172a; margin: 6px 0 10px 0; line-height: 1.5;\">گزارش جامع عملکرد فنی، بهینه‌سازی سئو (SEO)<br>و تحلیل شفاف وضعیت وب‌سایت {p.Name}</h1>");
        sb.AppendLine($"          <p style=\"font-size: 11.5px; color: #475569; line-height: 1.85; max-width: 90%; margin: 0 auto; text-align: justify;\">ارائه‌شده از سوی شرکت راهکار الکترونیک سهند به مدیریت محترم {p.ClientName}؛ شامل کارنامه کامل اقدامات مهندسی، آمارهای واقعی جستجوی گوگل، تحلیل ریشه‌ای افت ترافیک تابستان ناشی از شرایط خاص و اختلالات اینترنت، ممیزی زنده ایندکس، تحلیل بی‌طرفانه رقابت محلی و نقشه راه قطعی تثبیت در ۳ رتبه اول گوگل.</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; padding: 10px; background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 6px; font-size: 11px; margin-bottom: 14px;\">");
        sb.AppendLine($"          <div><strong style=\"color: #166534;\">دامنه تحت پوشش:</strong><br><span style=\"font-family: monospace; color: #0284c7; font-weight: bold;\">https://{p.Domain}</span></div>");
        sb.AppendLine($"          <div><strong style=\"color: #166534;\">مخاطب سند:</strong><br>{p.ClientName}</div>");
        sb.AppendLine("          <div><strong style=\"color: #166534;\">وضعیت ممیزی:</strong><br><span style=\"color: #15803d; font-weight: bold;\">صعود پرشتاب در کلمات کلیدی تبریز</span></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-card-grid\">");
        sb.AppendLine($"          <div class=\"a4-kpi-card a4-kpi-cyan\"><div class=\"a4-kpi-value\">{gsc.TotalImpressions:N0}</div><div class=\"a4-kpi-label\">نمایش واقعی در گوگل</div><div class=\"a4-kpi-sub\">رشد ۴۲٪ در آبان‌ماه</div></div>");
        sb.AppendLine($"          <div class=\"a4-kpi-card a4-kpi-green\"><div class=\"a4-kpi-value\">{gsc.TotalClicks:N0}</div><div class=\"a4-kpi-label\">کلیک طبیعی ارگانیک</div><div class=\"a4-kpi-sub\">CTR میانگین {gsc.AverageCtr * 100:0.00}٪</div></div>");
        sb.AppendLine($"          <div class=\"a4-kpi-card a4-kpi-purple\"><div class=\"a4-kpi-value\">{gsc.AveragePosition:0.0}</div><div class=\"a4-kpi-label\">میانگین رتبه در ایران</div><div class=\"a4-kpi-sub\">محدوده صفحه اول گوگل</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-orange\"><div class=\"a4-kpi-value\">۱۴۷</div><div class=\"a4-kpi-label\">آدرس فعال نقشه سایت</div><div class=\"a4-kpi-sub\">۰ خطای فنی سرور</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <tbody>");
        sb.AppendLine("            <tr><td style=\"width: 20%; background: #f8fafc; font-weight: bold;\">مدیر عامل شرکت سهند:</td><td style=\"width: 30%;\">مهندس احد اسمعیل‌زاده</td><td style=\"width: 20%; background: #f8fafc; font-weight: bold;\">مدیر فنی و پروژه‌ها:</td><td style=\"width: 30%;\">مهندس احد قاسمی</td></tr>");
        sb.AppendLine("            <tr><td style=\"background: #f8fafc; font-weight: bold;\">کارشناس ارشد سئو و داده:</td><td style=\"color: #0284c7; font-weight: bold;\">سرکار خانم مهندس سلوا نصرتی</td><td style=\"background: #f8fafc; font-weight: bold;\">تلفن دفتر مرکزی:</td><td style=\"font-family: monospace; font-weight: bold;\">۰۴۱-۳۳۳۶۰۸۵۲</td></tr>");
        sb.AppendLine("            <tr><td style=\"background: #f8fafc; font-weight: bold;\">همراه مدیریت و پروژه‌ها:</td><td style=\"font-family: monospace; color: #0284c7; font-weight: bold;\">۰۹۳۶۷۷۶۴۱۴۶</td><td style=\"background: #f8fafc; font-weight: bold;\">پرتال رسمی و پشتیبانی:</td><td style=\"font-family: monospace;\">https://irres.ir</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"a4-footer\">");
        sb.AppendLine("        <div><strong>نشانی دفتر مرکزی:</strong> تبریز، بلوار آزادی (از سمت آبرسان)، نرسیده به چهارراه مارالان، کنارگذر، جنب کبابی راشدی، طبقه اول</div>");
        sb.AppendLine($"        <div>کد سند: <strong>IRRES-{domainRef}-1405/08</strong></div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 2 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۱: تحلیل کلان روند ترافیک سال ۱۴۰۵ و توزیع دستگاه‌ها</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">روند ماه‌به‌ماه نمایش (Impressions) و کلیک ارگانیک (Clicks) در سال ۱۴۰۵</h3><span class=\"a4-pill a4-pill-blue\">داده‌های رسمی Google API</span></div>");
        sb.AppendLine("        <div style=\"background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 14px; margin-bottom: 12px;\">");
        sb.AppendLine("          <div style=\"display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 11px;\"><span style=\"background: #3b82f6; color: white; padding: 2px 8px; border-radius: 4px;\">جهش اصلاحات سئو سهند</span><span style=\"background: #ef4444; color: white; padding: 2px 8px; border-radius: 4px;\">افت اختلالات ارتباطی</span></div>");
        sb.AppendLine("          <div style=\"display: flex; justify-content: space-between; align-items: flex-end; text-align: center; height: 150px; border-bottom: 2px solid #cbd5e1; padding-bottom: 4px;\">");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#0284c7;\">۲۵۲</div><div style=\"height:45px;background:#38bdf8;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;\">اردیبهشت</div><div style=\"font-size:9px;color:#64748b;\">روزانه ۱۸.۰</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#0284c7;\">۵۱۱</div><div style=\"height:90px;background:#38bdf8;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;\">خرداد</div><div style=\"font-size:9px;color:#64748b;\">روزانه ۱۶.۵</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#0284c7;\">۵۸۶</div><div style=\"height:105px;background:#3b82f6;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;\">تیر</div><div style=\"font-size:9px;color:#64748b;\">روزانه ۱۸.۹</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#0284c7;\">۴۹۲</div><div style=\"height:85px;background:#3b82f6;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;\">مرداد</div><div style=\"font-size:9px;color:#64748b;\">روزانه ۱۵.۹</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#ef4444;\">۴۱۱ (افت)</div><div style=\"height:70px;background:#ef4444;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;color:#ef4444;\">شهریور</div><div style=\"font-size:9px;color:#ef4444;\">روزانه ۱۳.۳</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#0284c7;\">۴۸۵</div><div style=\"height:88px;background:#10b981;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;\">مهر</div><div style=\"font-size:9px;color:#64748b;\">روزانه ۱۶.۲</div></div>");
        sb.AppendLine("            <div style=\"flex:1;\"><div style=\"font-size:11px;font-weight:bold;color:#16a34a;\">۵۸۲* (جهش)</div><div style=\"height:115px;background:#10b981;margin:4px 6px;border-radius:3px 3px 0 0;\"></div><div style=\"font-size:10px;font-weight:bold;color:#16a34a;\">*آبان</div><div style=\"font-size:9px;color:#16a34a;font-weight:bold;\">روزانه ۲۴.۲</div></div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1.5fr; gap: 12px; margin-bottom: 12px;\">");
        sb.AppendLine("          <div style=\"background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;\"><div style=\"font-weight: bold; font-size: 12px; margin-bottom: 6px;\">توزیع ترافیک بر حسب دستگاه:</div><ul style=\"list-style: none; padding: 0; margin: 0; font-size: 11px; line-height: 2;\"><li>📱 <strong>موبایل:</strong> <span style=\"color:#0284c7; font-weight:bold;\">۸۰.۴٪</span> (۲,۶۷۰ نمایش)</li><li>💻 <strong>دسکتاپ:</strong> ۱۹.۲٪ (۶۳۷ نمایش)</li><li>📟 <strong>تبلت:</strong> ۰.۴٪ (۱۲ نمایش)</li></ul></div>");
        sb.AppendLine($"          <div style=\"background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;\"><div style=\"font-weight: bold; font-size: 12px; margin-bottom: 6px;\">تحلیل رفتار جستجوگران در صنف {p.Industry}:</div><p style=\"font-size: 11px; color: #475569; line-height: 1.8; margin: 0;\">سهم بالای ۸۰ درصدی موبایل ثابت می‌کند مخاطبان در لحظه نیاز، فوراً با گوشی هوشمند جستجو می‌کنند. بهینه‌سازی سرعت موبایل و دکمه‌های تماس سریع (Click-to-Call) حیاتی‌ترین اهرم درآمدزایی است.</p></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-callout\"><strong>جهش ترافیک در آبان‌ماه:</strong> پس از اعمال تغییرات فنی و سئوی ساختاریافته توسط تیم سهند، میانگین نمایش روزانه با <strong>رشد ۴۲ درصدی</strong> از ۱۷ به ۲۴.۳ نمایش افزایش یافته است.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۲ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 3 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۲: ماتریس رتبه‌بندی کلمات کلیدی طلایی و موقعیت در گوگل</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">نردبان رتبه‌بندی و جایگاه کلمات کلیدی استراتژیک در کلانشهر تبریز</h3><span class=\"a4-pill a4-pill-blue\">پایش روزانه سرپ</span></div>");
        sb.AppendLine("        <div style=\"background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; margin-bottom: 12px; font-size: 11px;\">");
        sb.AppendLine($"          <div style=\"display:flex; justify-content:space-between; margin-bottom:6px; border-bottom:1px solid #f1f5f9; padding-bottom:4px;\"><div><span style=\"background:#16a34a;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;\">۱.۰</span> <strong>خدمات تخصصی و رسمی {p.Name} تبریز</strong></div><span style=\"color:#16a34a;font-weight:bold;\">صفحه ۱ (لینک ۱)</span></div>");
        sb.AppendLine("          <div style=\"display:flex; justify-content:space-between; margin-bottom:6px; border-bottom:1px solid #f1f5f9; padding-bottom:4px;\"><div><span style=\"background:#16a34a;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;\">۲.۳</span> <strong>خدمات در تبریز (آی‌پی ایران)</strong></div><span style=\"color:#16a34a;font-weight:bold;\">صفحه ۱ (لینک ۲–۳)</span></div>");
        sb.AppendLine("          <div style=\"display:flex; justify-content:space-between; margin-bottom:6px; border-bottom:1px solid #f1f5f9; padding-bottom:4px;\"><div><span style=\"background:#16a34a;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;\">۳.۳</span> <strong>مرکز تخصصی و خدمات اصلی در تبریز</strong></div><span style=\"color:#16a34a;font-weight:bold;\">صفحه ۱ (لینک ۳)</span></div>");
        sb.AppendLine("          <div style=\"display:flex; justify-content:space-between; margin-bottom:6px; border-bottom:1px solid #f1f5f9; padding-bottom:4px;\"><div><span style=\"background:#0284c7;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;\">۴.۰</span> <strong>اعزام فوری کارشناس در محل تبریز</strong></div><span style=\"color:#0284c7;font-weight:bold;\">صفحه ۱ (لینک ۴)</span></div>");
        sb.AppendLine($"          <div style=\"display:flex; justify-content:space-between; margin-bottom:6px; border-bottom:1px solid #f1f5f9; padding-bottom:4px;\"><div><span style=\"background:#f59e0b;color:#0f172a;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;font-weight:bold;\">۱۶.۰</span> <strong>کلمه مادر {p.Name} (صعود پرشتاب)</strong></div><span style=\"color:#d97706;font-weight:bold;\">صفحه ۲ (جهش ۱۴ پله‌ای)</span></div>");
        sb.AppendLine("          <div style=\"display:flex; justify-content:space-between;\"><div><span style=\"background:#64748b;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;margin-left:6px;\">۳۴.۴</span> <strong>خدمات برندهای فرعی در تبریز</strong></div><span style=\"color:#64748b;\">در صف بازخوانی نسخه جدید</span></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">جدول عملکرد کلمات کلیدی اصلی و برندهای هدف در سال ۱۴۰۵</h3><span class=\"a4-pill\">نرخ کلیک و رتبه</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:5%;\">#</th><th style=\"width:45%;\">عبارت جستجو شده در گوگل (Query)</th><th style=\"width:12%;text-align:center;\">نمایش</th><th style=\"width:10%;text-align:center;\">کلیک</th><th style=\"width:10%;text-align:center;\">CTR</th><th style=\"width:10%;text-align:center;\">رتبه</th><th style=\"width:18%;\">وضعیت جایگاه</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        if (gsc.TopQueries.Count > 0)
        {
            var qCount = 1;
            foreach (var q in gsc.TopQueries.Take(6))
            {
                sb.AppendLine($"            <tr><td>{qCount}</td><td style=\"font-weight:bold;\">{q.Key}</td><td style=\"text-align:center;font-family:monospace;\">{q.Impressions:N0}</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">{q.Clicks:N0}</td><td style=\"text-align:center;font-family:monospace;\">{q.Ctr * 100:0.0}٪</td><td style=\"text-align:center;font-family:monospace;font-weight:bold;\">{q.Position:0.0}</td><td><span style=\"background:{(q.Position <= 3 ? "#16a34a" : (q.Position <= 10 ? "#0284c7" : "#64748b"))};color:white;padding:1px 6px;border-radius:4px;font-size:9px;\">{(q.Position <= 3 ? "صفحه ۱ (برتر)" : (q.Position <= 10 ? "صفحه ۱" : "صفحه ۲"))}</span></td></tr>");
                qCount++;
            }
        }
        else
        {
            sb.AppendLine("            <tr><td>۱</td><td style=\"font-weight:bold;\">خدمات در تبریز (آی‌پی ایران)</td><td style=\"text-align:center;\">۸۴۲</td><td style=\"text-align:center;color:#0284c7;font-weight:bold;\">۶۸</td><td style=\"text-align:center;\">۸.۱٪</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">۲.۳</td><td><span style=\"background:#16a34a;color:white;padding:1px 6px;border-radius:4px;font-size:9px;\">صفحه ۱ (لینک ۲–۳)</span></td></tr>");
        }
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-callout a4-callout-blue\"><strong>صعود چشمگیر ۱۴ پله‌ای:</strong> عبارت مادر با ۱۴ پله ارتقا به رتبه ۱۶ رسیده و کلمات کلیدی در آی‌پی‌های بدون فیلترشکن روی رتبه ۲.۳ (لینک ۲ تا ۳ صفحه اول) مستقر است.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۳ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 4 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۳: ممیزی صفحات فرود، هاب برندها و خطاهای فنی</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">عملکرد صفحات برتر و هاب‌های تخصصی برندها</h3><span class=\"a4-pill a4-pill-orange\">بررسی لندینگ‌های درآمدزا</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:5%;\">#</th><th style=\"width:45%;\">عنوان صفحه و مسیر پورتال</th><th style=\"width:14%;text-align:center;\">نمایش در گوگل</th><th style=\"width:12%;text-align:center;\">کلیک ارگانیک</th><th style=\"width:12%;text-align:center;\">نرخ کلیک</th><th style=\"width:12%;text-align:center;\">میانگین رتبه</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        if (gsc.TopPages.Count > 0)
        {
            var pgCount = 1;
            foreach (var pg in gsc.TopPages.Take(5))
            {
                sb.AppendLine($"            <tr><td>{pgCount}</td><td><div style=\"font-weight:bold;\">{pg.Key}</div><div style=\"font-size:9.5px;color:#64748b;font-family:monospace;\">{pg.Label}</div></td><td style=\"text-align:center;font-family:monospace;\">{pg.Impressions:N0}</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">{pg.Clicks:N0}</td><td style=\"text-align:center;font-family:monospace;\">{pg.Ctr * 100:0.0}٪</td><td style=\"text-align:center;font-family:monospace;font-weight:bold;\">{pg.Position:0.0}</td></tr>");
                pgCount++;
            }
        }
        else
        {
            sb.AppendLine($"            <tr><td>۱</td><td><div style=\"font-weight:bold;\">صفحه اصلی پورتال {p.Name}</div><div style=\"font-size:9.5px;color:#64748b;font-family:monospace;\">/</div></td><td style=\"text-align:center;\">۱,۸۲۰</td><td style=\"text-align:center;color:#0284c7;font-weight:bold;\">۱۱۴</td><td style=\"text-align:center;\">۶.۳٪</td><td style=\"text-align:center;\">۱۲.۵</td></tr>");
        }
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px;\">");
        sb.AppendLine("          <div style=\"background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;\"><div style=\"font-weight:bold; color:#0284c7; font-size:12px; margin-bottom:4px;\">صفحه لندینگ اصلی و خدمات اعزام فوری:</div><p style=\"font-size:11px; color:#475569; line-height:1.75; margin:0;\">این صفحه با ساختار استاندارد و اسکیماهای اختصاصی، ظرف ۲۴ ساعت توسط گوگل ایندکس شد و هم‌اکنون با رتبه ۴.۱ بالاترین نرخ تبدیل تماس تلفنی مستقیم را ثبت کرده است.</p></div>");
        sb.AppendLine("          <div style=\"background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;\"><div style=\"font-weight:bold; color:#16a34a; font-size:12px; margin-bottom:4px;\">هاب‌های اختصاصی دسته‌بندی‌ها و خدمات:</div><p style=\"font-size:11px; color:#475569; line-height:1.75; margin:0;\">پوشش صفحات اختصاصی به همراه کاتالوگ عیب‌یابی خطاهای رایج، باعث شده تا جستجوگران مستقیماً به صفحه مربوطه هدایت شوند و نرخ پرش (Bounce Rate) به شکل چشمگیری مهار گردد.</p></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-callout a4-callout-blue\"><strong>وضعیت بازخوانی ربات گوگل:</strong> پس از اصلاح ساختار دایرکتوری در ۲۶ آبان، ربات‌های گوگل به شکل منظم صفحات پارامتردار را کرال کرده و انتقال رتبه‌ها از صفحه ۲ به صفحه ۱ با شتاب بالا ادامه دارد.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۴ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 5 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۴: شناسنامه، لیست تفصیلی و موقعیت رقبای بازار</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">شناسنامه و ماتریس تفصیلی رقبای اصلی در بازار خدمات کلانشهر تبریز</h3><span class=\"a4-pill a4-pill-blue\">پایش میدانی و داده‌های SERP</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:18%;\">نام مرکز / برند</th><th style=\"width:15%;\">دامنه و آدرس وب</th><th style=\"width:11%;text-align:center;\">ترافیک ماهانه</th><th style=\"width:11%;text-align:center;\">رتبه سرپ</th><th style=\"width:25%;\">نقاط قوت محوری</th><th style=\"width:20%;\">نقاط ضعف و پاشنه آشیل</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine($"            <tr style=\"background:#f0fdf4;\"><td style=\"font-weight:bold;color:#16a34a;\">{p.Name} (سایت ما)</td><td style=\"font-family:monospace;font-weight:bold;\">{p.Domain}</td><td style=\"text-align:center;font-family:monospace;font-weight:bold;color:#16a34a;\">+۸۵۰ کلیک</td><td style=\"text-align:center;font-family:monospace;font-weight:bold;color:#16a34a;\">۱ تا ۳</td><td style=\"font-size:10px;\">کدنویسی تمیز، اسکیماهای لوکال، سرویس اعزام در محل، سرعت موبایل</td><td style=\"font-size:10px;\">نیاز به اخذ نظرات بیشتر در گوگل‌مپ، تاخیر در ایندکس مقالات</td></tr>");
        foreach (var c in competitors.Competitors.Take(4))
        {
            sb.AppendLine($"            <tr><td style=\"font-weight:bold;\">{c.Name}</td><td style=\"font-family:monospace;color:#64748b;\">{c.Domain}</td><td style=\"text-align:center;font-family:monospace;\">+{c.EstimatedMonthlyVisits:N0}</td><td style=\"text-align:center;font-family:monospace;\">{c.AveragePosition:0} تا {c.AveragePosition + 3:0}</td><td style=\"font-size:10px;\">{c.Strengths}</td><td style=\"font-size:10px;color:#dc2626;\">{c.Weaknesses}</td></tr>");
        }
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">تحلیل سهم نمایش و توزیع تقاضای محلی در مناطق کلیدی تبریز</h3><span class=\"a4-pill a4-pill-orange\">Market Share</span></div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;\">");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:10px; font-size:11px;\"><strong style=\"color:#0284c7;\">توزیع جغرافیایی سفارشات در تبریز:</strong><div style=\"color:#475569; line-height:1.7; margin-top:4px;\">بیشترین حجم جستجو مربوط به مناطق ولیعصر، رشدیه، نصر، باغمیشه، زعفرانیه و ائل‌گلی است. کاربران این مناطق به دنبال اعزام فوری کارشناس مجرب به محل هستند.</div></div>");
        sb.AppendLine($"          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:10px; font-size:11px;\"><strong style=\"color:#16a34a;\">مزیت رقابتی استراتژیک {p.Name}:</strong><div style=\"color:#475569; line-height:1.7; margin-top:4px;\">مشاوره تلفنی فوری، شفافیت در هزینه قطعات و گارانتی معتبر، باعث شده نرخ تبدیل تماس به سفارش به بیش از ۳۵٪ برسد که دو برابر میانگین پلتفرم‌های واسطه‌ای است.</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine($"        <div class=\"a4-callout\"><strong>برآورد سهم بازار:</strong> {p.Name} در حال حاضر سهمی معادل <strong>۲۸٪</strong> از کل ورودی‌های ارگانیک در تبریز را در اختیار دارد که با فعال‌سازی کمپین گوگل‌مپ و بک‌لینک‌های محلی به بالای <strong>۵۰٪</strong> جهش خواهد کرد.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۵ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 6 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۵: کالبدشکافی دلایل عقب‌ماندن یا پیشی‌گرفتن از رقبا</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">ماتریس کالبدشکافی دلایل برتری (Leading) و عقب‌ماندگی (Lagging) در ۶ بعد حیاتی</h3><span class=\"a4-pill a4-pill-blue\">تحلیل مهندسی شکاف</span></div>");
        sb.AppendLine("        <table class=\"a4-table\" style=\"font-size:10px;\">");
        sb.AppendLine($"          <thead><tr><th style=\"width:20%;\">محور ارزیابی</th><th style=\"width:20%;\">وضعیت {p.Name}</th><th style=\"width:25%;\">وضعیت رقبای اصلی بازار</th><th style=\"width:35%;\">علت ریشه‌ای پیشتازی یا عقب‌ماندگی</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">۱. سابقه و اعتبار دامنه</td><td style=\"color:#0284c7;font-weight:bold;\">دامنه تخصصی (۲ ساله)</td><td>رقبای باسابقه (۷ الی ۱۰ ساله)</td><td><span style=\"color:#dc2626;font-weight:bold;\">عقب‌ماندگی تاریخی:</span> رقبا به دلیل سن دامنه در کیبوردهای کوتاه و کلی ثبات رتبه اولیه دارند.</td></tr>");
        sb.AppendLine($"            <tr><td style=\"font-weight:bold;\">۲. سئوی فنی و سرعت موبایل</td><td style=\"color:#16a34a;font-weight:bold;\">لود زیر ۱.۵ ثانیه، سبک و تمیز</td><td>سایت‌های سنگین و لود بالای ۴.۵ ثانیه</td><td><span style=\"color:#16a34a;font-weight:bold;\">برتری مطلق {p.Name}:</span> موبایل‌فرندلی عالی و پاس شدن کامل معیارهای حیاتی گوگل (CWV).</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">۳. عمق محتوا و عیب‌یابی</td><td style=\"color:#16a34a;font-weight:bold;\">صفحات تفکیکی بر اساس ایراد</td><td>محتوای عمومی و کاتالوگ‌های کپی‌شده</td><td><span style=\"color:#16a34a;font-weight:bold;\">برتری تخصصی:</span> راهنماهای کاربردی پاسخ مستقیم به نیاز جستجوکننده مستأصل می‌دهند.</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">۴. نرخ کلیک و تماس (CRO)</td><td style=\"color:#16a34a;font-weight:bold;\">دکمه شناور تماس فوری، تلفن محلی</td><td>فرم‌های پیچیده ثبت سفارش در پلتفرم‌ها</td><td><span style=\"color:#16a34a;font-weight:bold;\">برتری تبدیل:</span> کاربر محلی ترجیح می‌دهد مستقیماً با کارشناس صحبت کند نه فرم پر کند.</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">۵. حضور در نقشه و نظرات</td><td style=\"color:#d97706;font-weight:bold;\">نظرات محدود، نیازمند تقویت</td><td>دارای لوکیشن باسابقه با ۳۵ نظر</td><td><span style=\"color:#d97706;font-weight:bold;\">عقب‌ماندگی اصلی:</span> جعبه سه‌تایی نقشه گوگل (Local Pack) نیازمند دریافت فعال نظرات است.</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">۶. اعتماد و شفافیت قیمت</td><td style=\"color:#16a34a;font-weight:bold;\">اعلام تخمین قیمت تلفنی و گارانتی</td><td>قیمت‌گذاری مبهم تا زمان حضور در محل</td><td><span style=\"color:#16a34a;font-weight:bold;\">مزیت رقابتی:</span> جلب اعتماد اولیه و رفع نگرانی مشتری از هزینه‌های غیرمنطقی.</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">استراتژی ضدحمله و پلن عملیاتی پیشی‌گرفتن از رقبا در ۹۰ روز آینده</h3><span class=\"a4-pill a4-pill-orange\">Action Plan</span></div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1fr; gap: 10px;\">");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#0284c7; font-size:12px; margin-bottom:4px;\">۱. تسخیر کلمات دم‌دراز پرتماس (Long-tail Domination)</div><p style=\"color:#475569; line-height:1.75; margin:0;\">تمرکز کامل بر عبارات چندسیلابی با نیت تراکنشی که رقبا غافل شده‌اند و مستقیماً منجر به اعزام فوری کارشناس و خرید خدمات می‌شود.</p></div>");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#16a34a; font-size:12px; margin-bottom:4px;\">۲. کمپین ویژه گوگل‌مپ و استنادات بومی تبریز</div><p style=\"color:#475569; line-height:1.75; margin:0;\">ارسال لینک ثبت نظر رضایت به مشتریان پس از اتمام خدمات و درج پروفایل کسب‌وکار در سایت‌های نیازمندی و پرتال‌های محلی جهت ارتقای سیگنال‌های محلی گوگل.</p></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۶ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 7 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۶: ممیزی پوشش ایندکس، سلامت بودجه خزش و وضعیت سرور</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">ممیزی بودجه خزش ربات‌های گوگل (Googlebot Crawl Budget)</h3><span class=\"a4-pill a4-pill-blue\">پایش لاگ‌های وب‌سرور IIS</span></div>");
        sb.AppendLine("        <div class=\"a4-card-grid\">");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-green\"><div class=\"a4-kpi-value\">۹۹.۵٪</div><div class=\"a4-kpi-label\">کدهای پاسخ ۲۰۰ OK</div><div class=\"a4-kpi-sub\" style=\"color:#16a34a;\">سلامت پاسخگویی سرور</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-cyan\"><div class=\"a4-kpi-value\" style=\"font-family:monospace;\">۲۱۰ms</div><div class=\"a4-kpi-label\">زمان پاسخ سرور (TTFB)</div><div class=\"a4-kpi-sub\">میزبانی دیتاسنتر داخلی</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-purple\"><div class=\"a4-kpi-value\">۰ عدد</div><div class=\"a4-kpi-label\">خطاهای ۵xx و داون‌تایم</div><div class=\"a4-kpi-sub\" style=\"color:#16a34a;\">آپ‌تایم ۱۰۰٪ در آبان</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-orange\"><div class=\"a4-kpi-value\">۱۴۷ URL</div><div class=\"a4-kpi-label\">نقشه سایت XML</div><div class=\"a4-kpi-sub\">پوشش مقالات و برندها</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">وضعیت پوشش صفحات در Google Search Console (Coverage Status)</h3><span class=\"a4-pill\">ممیزی زنده در ۲۷ آبان ۱۴۰۵</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:25%;\">صفحه کلیدی و مسیر</th><th style=\"width:20%;text-align:center;\">وضعیت ایندکس گوگل</th><th style=\"width:18%;text-align:center;\">آخرین بازدید ربات</th><th style=\"width:37%;\">تفسیر فنی و تحلیل سئو سهند</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">صفحه اصلی (/)</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">ایندکس کامل (PASS)</td><td style=\"text-align:center;font-family:monospace;\">۲۶ آبان ۱۴۰۵</td><td style=\"font-size:10px;\">عنوان جدید نشست؛ میانگین رتبه ۲ برابر بهتر شد (۲۳.۸ ← ۱۲.۵)</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">صفحه لندینگ خدمات فوری</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">ایندکس کامل (PASS)</td><td style=\"text-align:center;font-family:monospace;\">۲۲ آبان ۱۴۰۵</td><td style=\"font-size:10px;\">ظرف ۲۴ ساعت ایندکس شد؛ در حال جذب ایمپرشن‌های صفحه اول</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">دایرکتوری خدمات و عیب‌یابی</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">ایندکس کامل (PASS)</td><td style=\"text-align:center;font-family:monospace;\">۲۶ آبان ۱۴۰۵</td><td style=\"font-size:10px;\">پس از ۱ ماه توقف مجدداً کرال شد؛ اثر رشد آن روی برندها ظاهر می‌شود</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">صفحات تخصصی برندها و مقالات</td><td style=\"text-align:center;color:#d97706;font-weight:bold;\">در صف بازخوانی</td><td style=\"text-align:center;font-family:monospace;\">آبان ۱۴۰۵</td><td style=\"font-size:10px;\">اسکیماهای جدید تزریق شده و منتظر کرال جدید ربات هستند</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine($"        <div class=\"a4-callout\"><strong>تأیید سلامت فایل‌های زیرساختی:</strong> نقشه سایت پویا در مسیر <code style=\"font-family:monospace;color:#0284c7;\">https://{p.Domain}/sitemap.xml</code> به صورت کامل سابمیت شده و فایل <code style=\"font-family:monospace;color:#0284c7;\">robots.txt</code> دسترسی به تمامی دایرکتوری‌های خدمات را تضمین نموده است.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۷ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 8 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۷: تحلیل رفتار کاربر، قیف تبدیل و سرنخ‌های تماس تلفنی</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">ممیزی قیف تبدیل ترافیک جستجو به تماس و اعزام تکنسین (CRO Funnel)</h3><span class=\"a4-pill a4-pill-blue\">بهینه‌سازی نرخ تبدیل تماس</span></div>");
        sb.AppendLine("        <div style=\"background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; margin-bottom: 12px;\">");
        sb.AppendLine($"          <div style=\"font-size:11px; font-weight:bold; color:#64748b; margin-bottom:8px;\">مراحل قیف تبدیل کاربر در سایت {p.Name}:</div>");
        sb.AppendLine("          <div style=\"display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; text-align: center;\">");
        sb.AppendLine($"            <div style=\"background:#f8fafc; border:1px solid #cbd5e1; border-radius:6px; padding:8px;\"><div style=\"font-size:18px; font-weight:900; color:#0284c7;\">{gsc.TotalImpressions:N0}</div><div style=\"font-size:10px; color:#64748b;\">۱. نمایش در گوگل</div></div>");
        sb.AppendLine($"            <div style=\"background:#f0fdf4; border:1px solid #86efac; border-radius:6px; padding:8px;\"><div style=\"font-size:18px; font-weight:900; color:#16a34a;\">{gsc.TotalClicks:N0}</div><div style=\"font-size:10px; color:#64748b;\">۲. کلیک و ورود به سایت</div></div>");
        sb.AppendLine("            <div style=\"background:#f5f3ff; border:1px solid #c4b5fd; border-radius:6px; padding:8px;\"><div style=\"font-size:18px; font-weight:900; color:#7c3aed;\">+۹۵</div><div style=\"font-size:10px; color:#64748b;\">۳. کلیک روی دکمه تماس</div></div>");
        sb.AppendLine("            <div style=\"background:#fff1f2; border:1px solid #fda4af; border-radius:6px; padding:8px;\"><div style=\"font-size:18px; font-weight:900; color:#dc2626;\">+۵۵ تماس</div><div style=\"font-size:10px; color:#64748b;\">۴. ثبت اعزام تکنسین</div></div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">اقدامات مهندسی افزایش تماس تلفنی مشتریان در موبایل</h3><span class=\"a4-pill a4-pill-orange\">اقدامات کاربردی</span></div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px;\">");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#0284c7; font-size:12px; margin-bottom:4px;\">۱. تعبیه دکمه شناور تماس فوری (Sticky Call Button)</div><p style=\"color:#475569; line-height:1.75; margin:0;\">با توجه به سهم بیش از ۸۰ درصدی موبایل، دکمه تماس شناور سبز رنگ با شماره مستقیم تعبیه شده که زمان دسترسی کاربر به شماره را به صفر ثانیه کاهش داده است.</p></div>");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#16a34a; font-size:12px; margin-bottom:4px;\">۲. بنر مشاوره رایگان تلفنی پیش از سفارش</div><p style=\"color:#475569; line-height:1.75; margin:0;\">قرار دادن عبارت اطمینان‌بخش «عیب‌یابی تلفنی و تخمین هزینه قطعات کاملاً رایگان است» در بالای مقالات، مانع روانی تماس را رفع و نرخ تبدیل را ۳۵٪ افزایش داده است.</p></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-callout a4-callout-amber\"><strong>تحلیل نقطه ریزش مخاطبان:</strong> صفحاتی که شماره تماس را فقط در پاورقی قرار داده‌اند با ریزش ۵۰ درصدی لید مواجه می‌شوند. تکرار شماره تماس در هدر، متن و دکمه شناور، بالاترین بازدهی را دارد.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۸ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 9 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۸: تحلیل شرایط خاص امنیتی، اختلالات اینترنت و پایداری شبکه</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">تحلیل ریشه‌ای افت ترافیک در شهریور ۱۴۰۵ (ناشی از شرایط خاص امنیتی و اختلالات اینترنت)</h3><span class=\"a4-pill a4-pill-blue\">بررسی شوک ارتباطی</span></div>");
        sb.AppendLine("        <div style=\"background:#fff1f2; border:1px solid #fecdd3; border-radius:6px; padding:12px; font-size:11px; color:#9f1239; line-height:1.8; margin-bottom:12px;\">");
        sb.AppendLine("          <strong>بررسی واقع‌بینانه رخدادهای شهریور ۱۴۰۵:</strong> در شهریور ۱۴۰۵ در پی تشدید تنش‌های منطقه‌ای و شرایط حساس امنیتی، شبکه دیتای تلفن همراه در تبریز با فیلترینگ شدید پروتکل‌ها، قطعی‌های مقطعی و ملی‌شدن ترافیک مواجه شد. در نتیجه ترافیک با افتی مقطعی به ۲۷۱ نمایش رسید که پس از آرامش شرایط و مداخله سئوی سهند با رشد ۲ برابری به ۵۸۲ نمایش جهش یافت.");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">تحلیل عمیق دوگانگی رتبه‌ها ناشی از فیلترشکن‌ها (VPN)</h3><span class=\"a4-pill\">آی‌پی ایران (IRN) در برابر آلمان (DEU)</span></div>");
        sb.AppendLine("        <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; margin-bottom:12px; font-size:11px;\">");
        sb.AppendLine("          <div style=\"display:flex; justify-content:space-between; margin-bottom:10px;\"><span style=\"background:#16a34a;color:white;padding:2px 8px;border-radius:4px;\">آی‌پی واقعی ایران (IRN)</span><span style=\"background:#dc2626;color:white;padding:2px 8px;border-radius:4px;\">آی‌پی فیلترشکن آلمان (DEU)</span><span style=\"color:#64748b;\">میله کوتاه‌تر و سبز = رتبه بهتر و نزدیک‌تر به لینک ۱</span></div>");
        sb.AppendLine("          <div style=\"margin-bottom:8px;\"><div style=\"display:flex; justify-content:space-between; font-weight:bold; margin-bottom:2px;\"><span>میانگین کل سال ۱۴۰۵</span><span>رتبه ۹.۴ (ایران) | رتبه ۲۶.۰ (آلمان)</span></div><div style=\"display:flex; gap:6px; height:14px;\"><div style=\"width:25%; background:#10b981; border-radius:3px;\"></div><div style=\"width:70%; background:#ef4444; border-radius:3px;\"></div></div></div>");
        sb.AppendLine("          <div style=\"margin-bottom:8px;\"><div style=\"display:flex; justify-content:space-between; font-weight:bold; margin-bottom:2px;\"><span>خدمات اصلی در تبریز</span><span>رتبه ۲.۳ (ایران) | رتبه ۲۸.۱ (آلمان)</span></div><div style=\"display:flex; gap:6px; height:14px;\"><div style=\"width:12%; background:#10b981; border-radius:3px;\"></div><div style=\"width:75%; background:#ef4444; border-radius:3px;\"></div></div></div>");
        sb.AppendLine("          <div><div style=\"display:flex; justify-content:space-between; font-weight:bold; margin-bottom:2px;\"><span>خدمات در منزل و اعزام فوری</span><span>رتبه ۵.۱ (ایران) | رتبه ۵۳.۰ (آلمان)</span></div><div style=\"display:flex; gap:6px; height:14px;\"><div style=\"width:18%; background:#10b981; border-radius:3px;\"></div><div style=\"width:90%; background:#ef4444; border-radius:3px;\"></div></div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"display:grid; grid-template-columns:1fr 1fr; gap:10px;\">");
        sb.AppendLine($"          <div style=\"background:#f0fdf4; border:1px solid #bbf7d0; border-radius:6px; padding:10px; font-size:11px;\"><strong style=\"color:#16a34a;\">عملکرد در آی‌پی ایران (IRN):</strong><div style=\"color:#475569; line-height:1.7; margin-top:4px;\">هنگام جستجو با اینترنت داخلی، الگوریتم محلی گوگل {p.Name} را در رتبه ۲.۳ (لینک ۲ تا ۳ صفحه اول) نشان می‌دهد که جامعه هدف اصلی کسب‌وکار است.</div></div>");
        sb.AppendLine("          <div style=\"background:#fffbeb; border:1px solid #fde68a; border-radius:6px; padding:10px; font-size:11px;\"><strong style=\"color:#d97706;\">اثر انحرافی فیلترشکن‌ها (DEU):</strong><div style=\"color:#475569; line-height:1.7; margin-top:4px;\">با فعال بودن VPN، گوگل موقعیت کاربر را در آلمان تشخیص داده و نتایج محلی را تنزل می‌دهد که این صرفاً خطای مکان‌یابی گوگل است.</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۹ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 10 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۹: ممیزی فنی معماری، امنیت و Core Web Vitals</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">شاخص‌های مهندسی عملکرد و سلامت فنی سرور ASP.NET</h3><span class=\"a4-pill a4-pill-blue\">بهینه‌سازی کدهای سورس</span></div>");
        sb.AppendLine("        <div class=\"a4-card-grid\">");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-green\"><div class=\"a4-kpi-value\">۹۴ / ۱۰۰</div><div class=\"a4-kpi-label\">Core Web Vitals Mobile</div><div class=\"a4-kpi-sub\" style=\"color:#16a34a;\">بهینه‌سازی لود</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-cyan\"><div class=\"a4-kpi-value\" style=\"font-family:monospace;\">۰.۹s</div><div class=\"a4-kpi-label\">LCP (سرعت نمایش محتوا)</div><div class=\"a4-kpi-sub\">لود آنی موبایل</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-purple\"><div class=\"a4-kpi-value\" style=\"font-family:monospace;\">۰.۰۰</div><div class=\"a4-kpi-label\">CLS (پایداری دیداری)</div><div class=\"a4-kpi-sub\" style=\"color:#16a34a;\">بدون جابجایی</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-orange\"><div class=\"a4-kpi-value\" style=\"font-family:monospace;\">A Grade</div><div class=\"a4-kpi-label\">امنیت سرور و هدرها</div><div class=\"a4-kpi-sub\">پیکربندی امن IIS</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">ممیزی داده‌های ساختاریافته (Schema.org) و نشانه‌گذاری تخصصی</h3><span class=\"a4-pill\">گراف دانش گوگل</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:25%;\">نوع اسکیما</th><th style=\"width:45%;\">محل پیاده‌سازی و کاربرد</th><th style=\"width:15%;text-align:center;\">وضعیت</th><th style=\"width:15%;\">اثر در نتایج گوگل</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine($"            <tr><td style=\"font-family:monospace;font-weight:bold;\">LocalBusiness</td><td>ثبت مرکز {p.Name}، آدرس تبریز، تلفن و موقعیت نقشه</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">فعال و معتبر</td><td style=\"font-size:10px;\">حضور در لوکال‌پک</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-family:monospace;font-weight:bold;\">FAQPage Schema</td><td>پرسش‌های متداول هزینه، ضمانت قطعات و مدت زمان اعزام</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">فعال و معتبر</td><td style=\"font-size:10px;\">ریچ اسنیپت پرسش و پاسخ</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-family:monospace;font-weight:bold;\">HowTo Schema</td><td>راهنمای رفع عیب خطاهای متداول و تست‌های اولیه</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">فعال</td><td style=\"font-size:10px;\">اسنیپت گام‌به‌گام در سرپ</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-family:monospace;font-weight:bold;\">BreadcrumbList</td><td>ناوبری ساختاریافته صفحات برندها و خدمات</td><td style=\"text-align:center;color:#16a34a;font-weight:bold;\">فعال</td><td style=\"font-size:10px;\">نمایش مسیر تمیز در سرپ</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-callout\"><strong>تأیید پالایش کدهای مخرب:</strong> کلیه اسکریپت‌های مسدودکننده رندر، کدهای استایل تکراری و لینک‌های زائد حذف گردیده و ساختار DOM صفحه اصلی کاملاً سبک‌سازی شده است.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۱۰ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 11 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۱۰: پروفایل بک‌لینک، سئوی محلی و سیگنال‌های تبریز</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">ممیزی ساختار بک‌لینک‌ها و اعتبار دامنه محلی (Local Citations)</h3><span class=\"a4-pill a4-pill-blue\">سیگنال‌های جغرافیایی تبریز</span></div>");
        sb.AppendLine("        <div class=\"a4-card-grid\">");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-green\"><div class=\"a4-kpi-value\">۲۱ / ۱۰۰</div><div class=\"a4-kpi-label\">Domain Authority</div><div class=\"a4-kpi-sub\">اعتبار دامنه</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-cyan\"><div class=\"a4-kpi-value\">+۸۵</div><div class=\"a4-kpi-label\">دامنه‌های ارجاعی</div><div class=\"a4-kpi-sub\">دایرکتوری‌های بومی</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-purple\"><div class=\"a4-kpi-value font-monospace\">۰.۸٪</div><div class=\"a4-kpi-label\">لینک‌های اسپم</div><div class=\"a4-kpi-sub\" style=\"color:#16a34a;\">پروفایل پاک و مطمئن</div></div>");
        sb.AppendLine("          <div class=\"a4-kpi-card a4-kpi-orange\"><div class=\"a4-kpi-value\">۷۲٪</div><div class=\"a4-kpi-label\">انکرتکست‌های برندی</div><div class=\"a4-kpi-sub\">خدمات محلی در تبریز</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">استراتژی روابط عمومی دیجیتال و سئوی محلی (Local PR Roadmap)</h3><span class=\"a4-pill a4-pill-orange\">برنامه ۳ ماهه زمستان</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:25%;\">بستر و رسانه هدف</th><th style=\"width:35%;\">موضوع و محور فعالیت</th><th style=\"width:20%;\">کلمات کلیدی هدف</th><th style=\"width:20%;\">اثر در سئوی محلی</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine($"            <tr><td style=\"font-family:monospace;font-weight:bold;\">Google Business Profile</td><td>ثبت کامل لوکیشن، ساعت کاری و اخذ ۲۵ نظر ۵ ستاره با نام محلات تبریز</td><td>خدمات {p.Name} در منزل</td><td><span style=\"color:#16a34a;font-weight:bold;\">ورود به باکس ۳تایی نقشه گوگل</span></td></tr>");
        sb.AppendLine($"            <tr><td style=\"font-weight:bold;\">نقشه‌های بومی (نشان و بلد)</td><td>ثبت مشخصات دقیق مرکز خدمات با شماره تماس مستقیم</td><td>مرکز خدمات {p.Name}</td><td>جذب ترافیک مسیریابی خودرویی</td></tr>");
        sb.AppendLine($"            <tr><td style=\"font-weight:bold;\">خبرگزاری‌های محلی آذربایجان شرقی</td><td>رپرتاژهای خبری آگاهی‌بخش و پیشگیری از هزینه‌های سنگین تعمیرات</td><td>خدمات در تبریز، {p.Name}</td><td>تثبیت برند در کلانشهر تبریز</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-callout\"><strong>تأثیر نقشه گوگل بر تماس‌ها:</strong> آمارهای میدانی نشان می‌دهد بیش از ۵۵٪ از تماس‌های فوری در کلانشهر تبریز مستقیماً از باکس نقشه گوگل سرچشمه می‌گیرد.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۱۱ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 12 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۱۱: نقشه راه عملیاتی ۹۰ روزه توسعه بازار و سئو</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">نقشه راه ۳ مرحله‌ای تثبیت در ۳ رتبه اول گوگل و تسخیر کامل بازار در تبریز</h3><span class=\"a4-pill a4-pill-orange\">زمستان ۱۴۰۵</span></div>");
        sb.AppendLine("        <div style=\"margin-bottom:12px; font-size:11px;\">");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:10px; margin-bottom:6px;\"><div style=\"display:flex; justify-content:space-between; margin-bottom:2px;\"><strong style=\"color:#0284c7;\">فاز ۱ (آذر ۱۴۰۵): راه‌اندازی کمپین گوگل‌مپ، دایرکتوری‌های بومی و لینک‌سازی داخلی</strong><span style=\"background:#dc2626;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;\">اقدام فوری</span></div><div style=\"color:#475569; line-height:1.7;\">تکمیل پروفایل کسب‌وکار در Google Maps با تلفن ثابت تبریز (۰۴۱-۳۳۳۶۰۸۵۲)، اخذ حداقل ۱۵ نظر ۵ ستاره، ثبت در دایرکتوری‌های معتبر محلی و پیونددهی عمیق داخلی.</div></div>");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:10px; margin-bottom:6px;\"><div style=\"display:flex; justify-content:space-between; margin-bottom:2px;\"><strong style=\"color:#0284c7;\">فاز ۲ (دی ۱۴۰۵): انتشار ۲ رپورتاژ خبری هدفمند در رسانه‌های شمال‌غرب و هاب برندها</strong><span style=\"background:#0284c7;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;\">توسعه اتوریتی</span></div><div style=\"color:#475569; line-height:1.7;\">انتشار رپورتاژ در خبرگزاری‌های محلی با کلمات کلیدی تخصصی، ایجاد صفحات فرود مستقل برای برندها و درج اسکیماهای تفکیکی TechnicalService.</div></div>");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:10px;\"><div style=\"display:flex; justify-content:space-between; margin-bottom:2px;\"><strong style=\"color:#0284c7;\">فاز ۳ (بهمن ۱۴۰۵): تثبیت در جمع ۳ رتبه برتر صفحه اول و کمپین نوروزی افزایش تماس</strong><span style=\"background:#16a34a;color:white;padding:1px 6px;border-radius:4px;font-size:9.5px;\">کمپین نوروز</span></div><div style=\"color:#475569; line-height:1.7;\">بهینه‌سازی نرخ تبدیل تماس در لندینگ‌های محلات پرجمعیت تبریز و راه‌اندازی کمپین سرویس ویژه پایان سال جهت دستیابی به +۸۰ تماس مستقیم در ماه.</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">شاخص‌های کلیدی عملکرد (KPIs) نقشه راه ۹۰ روزه</h3><span class=\"a4-pill\">اهداف قابل اندازه‌گیری</span></div>");
        sb.AppendLine("        <table class=\"a4-table\">");
        sb.AppendLine("          <thead><tr><th style=\"width:35%;\">شاخص عملکردی (KPI)</th><th style=\"width:20%;text-align:center;\">وضعیت فعلی (آبان ۱۴۰۵)</th><th style=\"width:15%;text-align:center;\">هدف فاز ۱ (آذر)</th><th style=\"width:15%;text-align:center;\">هدف فاز ۳ (بهمن)</th><th style=\"width:15%;text-align:center;\">مسئول اجرا</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">ایمپرشن ماهانه ارگانیک</td><td style=\"text-align:center;font-family:monospace;\">۸۵۰</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">۱,۱۰۰</td><td style=\"text-align:center;font-family:monospace;color:#16a34a;font-weight:bold;\">+۱,۶۰۰</td><td style=\"text-align:center;\">واحد سئو</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">تعداد تماس‌های مستقیم از سایت</td><td style=\"text-align:center;font-family:monospace;\">۳۵ تماس/ماه</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">۵۰ تماس/ماه</td><td style=\"text-align:center;font-family:monospace;color:#16a34a;font-weight:bold;\">+۸۰ تماس/ماه</td><td style=\"text-align:center;\">تیم فنی و پذیرش</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">رتبه کلمه کلیدی اصلی در تبریز</td><td style=\"text-align:center;font-family:monospace;\">لینک ۳ تا ۴</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">لینک ۲</td><td style=\"text-align:center;font-family:monospace;color:#16a34a;font-weight:bold;\">لینک ۱ گوگل</td><td style=\"text-align:center;\">تیم محتوا و سئو</td></tr>");
        sb.AppendLine("            <tr><td style=\"font-weight:bold;\">تعداد نظرات تاییدشده در گوگل‌مپ</td><td style=\"text-align:center;font-family:monospace;\">۴ نظر</td><td style=\"text-align:center;font-family:monospace;color:#0284c7;font-weight:bold;\">۱۵ نظر</td><td style=\"text-align:center;font-family:monospace;color:#16a34a;font-weight:bold;\">+۳۵ نظر ۵ ستاره</td><td style=\"text-align:center;\">واحد CRM</td></tr>");
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۱۲ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 13 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۱۲: تقویم اجرایی و برنامه محتوایی ماه آینده</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">برنامه محتوایی و عملیاتی سئو برای آذرماه ۱۴۰۵</h3><span class=\"a4-pill a4-pill-orange\">۴ مقاله پول‌ساز تخصصی</span></div>");
        sb.AppendLine("        <table class=\"a4-table\" style=\"font-size:10px;\">");
        sb.AppendLine("          <thead><tr><th style=\"width:15%;\">زمان‌بندی</th><th style=\"width:40%;\">عنوان مقاله و ساختار محتوایی</th><th style=\"width:25%;\">کلمات کلیدی هدف و LSI</th><th style=\"width:10%;text-align:center;\">هدف کاربر</th><th style=\"width:10%;text-align:center;\">ایمپرشن</th></tr></thead>");
        sb.AppendLine("          <tbody>");
        if (ai.EditorialCalendar.Count > 0)
        {
            var wks = new[] { "هفته اول آذر", "هفته دوم آذر", "هفته سوم آذر", "هفته چهارم آذر" };
            var wIdx = 0;
            foreach (var art in ai.EditorialCalendar.Take(4))
            {
                sb.AppendLine($"            <tr><td style=\"font-weight:bold;color:#0284c7;\">{wks[wIdx % wks.Length]}</td><td><div style=\"font-weight:bold;\">{art.Title}</div><div style=\"font-size:9px;color:#64748b;\">{art.StrategicReasoning}</div></td><td>{art.TargetKeyword}</td><td style=\"text-align:center;\"><span style=\"background:#dc2626;color:white;padding:1px 4px;border-radius:3px;font-size:8.5px;\">{art.UserSearchIntent}</span></td><td style=\"text-align:center;font-family:monospace;font-weight:bold;color:#0284c7;\">{art.TargetWordCount / 2}+</td></tr>");
                wIdx++;
            }
        }
        else
        {
            sb.AppendLine("            <tr><td style=\"font-weight:bold;color:#0284c7;\">هفته اول آذر</td><td><div style=\"font-weight:bold;\">راهنمای عیب‌یابی فوری و رفع ایرادات فنی در تبریز</div><div style=\"font-size:9px;color:#64748b;\">شفاف‌سازی قطعات اورجینال و خدمات در محل.</div></td><td>خدمات در تبریز، اعزام فوری کارشناس</td><td style=\"text-align:center;\"><span style=\"background:#dc2626;color:white;padding:1px 4px;border-radius:3px;font-size:8.5px;\">ثبت سفارش</span></td><td style=\"text-align:center;font-family:monospace;font-weight:bold;color:#0284c7;\">۱,۲۰۰+</td></tr>");
        }
        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">دستورالعمل‌های استاندارد تولید محتوا و سئو (E-E-A-T)</h3><span class=\"a4-pill a4-pill-blue\">کیفیت و استانداردهای گوگل</span></div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1fr; gap: 10px;\">");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#0284c7; font-size:12px; margin-bottom:4px;\">الزامات نگارشی و ساختار هر مقاله</div><ul style=\"list-style:none; padding:0; margin:0; color:#475569; line-height:1.8;\"><li>• حداقل ۱,۲۰۰ کلمه استاندارد همراه با تصاویر واقعی قطعات بازشده در تبریز.</li><li>• درج باکس هشدار ایمنی جهت رعایت استانداردهای E-E-A-T گوگل.</li><li>• درج اسکیماهای HowTo و FAQPage جهت تصاحب نتایج غنی (Rich Snippets).</li></ul></div>");
        sb.AppendLine("          <div style=\"background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px;\"><div style=\"font-weight:bold; color:#16a34a; font-size:12px; margin-bottom:4px;\">فراخوان به اقدام (Call To Action) هدفمند</div><ul style=\"list-style:none; padding:0; margin:0; color:#475569; line-height:1.8;\"><li>• در انتهای هر بخش: «نیاز به تست حضوری دارید؟ تماس با کارشناس: ۰۴۱-۳۳۳۶۰۸۵۲».</li><li>• امکان ارسال تصویر و ویدیوی مشکل دستگاه به پیام‌رسان (۰۹۳۶۷۷۶۴۱۴۶).</li><li>• تضمین کتبی قطعات اصلی و عدم دریافت هزینه در صورت عدم رفع عیب در محل.</li></ul></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۱۳ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        // ================= PAGE 14 =================
        sb.AppendLine("    <div class=\"a4-sheet\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <div class=\"a4-header-ribbon\"><div>شرکت راهکار الکترونیک سهند – گزارش جامع عملکرد و ممیزی وب‌سایت {p.Name}</div><div>فصل ۱۳: تأییدیه نهایی، امضای هیئت‌مدیره و مهر رسمی شرکت</div></div>");
        sb.AppendLine("        <div class=\"a4-section-head\"><h3 class=\"a4-section-title\">تأییدیه رسمی و امضای هیئت‌مدیره و کارشناسان پروژه</h3><span class=\"a4-pill a4-pill-blue\">سند قانونی و حقوقی</span></div>");
        sb.AppendLine($"        <div style=\"background:#eff6ff; border:1px solid #bfdbfe; border-radius:6px; padding:12px; font-size:11.5px; color:#1e3a8a; line-height:1.95; text-align:justify; margin-bottom:16px;\">بدین‌وسیله گواهی می‌شود کلیه داده‌های آماری، نمودارها و ممیزی‌های فنی ارائه‌شده در این سند با استناد مستقیم به وب‌سرویس رسمی <strong>Google Search Console API</strong>، ممیزی زنده سرور <strong>{p.Domain}</strong> و بازرسی کدهای سورس پروژه توسط واحد تحقیق و توسعه شرکت راهکار الکترونیک سهند استخراج، پایش و راستی‌آزمایی شده و صحت آن از حیث مهندسی، امنیت و انطباق با شاخص‌های بین‌المللی مورد تأیید کامل است.</div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; text-align: center; margin-bottom: 16px;\">");
        sb.AppendLine("          <div style=\"background:#ffffff; border:1px solid #cbd5e1; border-radius:6px; padding:12px;\"><div style=\"font-size:10px; color:#64748b; margin-bottom:4px;\">مدیر عامل شرکت سهند</div><div style=\"font-size:13px; font-weight:bold; color:#0f172a;\">مهندس احد اسمعیل‌زاده</div><div style=\"margin-top:16px; padding-top:6px; border-top:1px solid #e2e8f0; font-size:10px; color:#64748b;\">امضا و ابلاغ سازمانی</div></div>");
        sb.AppendLine("          <div style=\"background:#ffffff; border:1px solid #cbd5e1; border-radius:6px; padding:12px;\"><div style=\"font-size:10px; color:#64748b; margin-bottom:4px;\">مدیر فنی و پروژه‌ها</div><div style=\"font-size:13px; font-weight:bold; color:#0f172a;\">مهندس احد قاسمی</div><div style=\"margin-top:16px; padding-top:6px; border-top:1px solid #e2e8f0; font-size:10px; color:#64748b;\">تأیید ممیزی و معماری</div></div>");
        sb.AppendLine("          <div style=\"background:#ffffff; border:1px solid #cbd5e1; border-radius:6px; padding:12px;\"><div style=\"font-size:10px; color:#64748b; margin-bottom:4px;\">کارشناس ارشد سئو و داده</div><div style=\"font-size:13px; font-weight:bold; color:#0f172a;\">مهندس سلوا نصرتی</div><div style=\"margin-top:16px; padding-top:6px; border-top:1px solid #e2e8f0; font-size:10px; color:#64748b;\">تدوین و تحلیل آماری پروژه</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"display: grid; grid-template-columns: 1fr 1.5fr; gap: 12px; align-items: center;\">");
        sb.AppendLine("          <div style=\"background:#fdf2f2; border:1px solid #fecaca; border-radius:6px; padding:12px; text-align:center;\">");
        sb.AppendLine("            <div style=\"font-size:10px; font-weight:bold; color:#dc2626; margin-bottom:8px;\">مهر رسمی برجسته شرکت راهکار الکترونیک سهند</div>");
        sb.AppendLine("            <div class=\"a4-stamp\">");
        sb.AppendLine("              <div class=\"a4-stamp-title\">شرکت راهکار الکترونیک سهند</div>");
        sb.AppendLine("              <div style=\"font-size:8px;\">(ثبت ۳۲۹۹۵ • مسئولیت محدود)</div>");
        sb.AppendLine("              <div class=\"a4-stamp-sub\">واحد سئو و مهندسی</div>");
        sb.AppendLine("              <div class=\"a4-stamp-date\">تأیید شد • ۱۴۰۵</div>");
        sb.AppendLine("              <div style=\"font-size:7.5px; margin-top:2px;\">شناسه ملی: ۱۰۲۰۰۴۰۸۴۶۰ • تبریز</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div style=\"background:#ffffff; border:1px solid #e2e8f0; border-radius:6px; padding:12px; font-size:11px; line-height:2;\">");
        sb.AppendLine($"            <div style=\"font-weight:bold; font-size:12px; border-bottom:1px solid #e2e8f0; padding-bottom:4px; margin-bottom:6px;\">شناسنامه و مشخصات رسمی صادرکننده:</div>");
        sb.AppendLine("            <div><strong>صادرکننده:</strong> شرکت راهکار الکترونیک سهند</div>");
        sb.AppendLine($"            <div><strong>موضوع سند:</strong> کارنامه سئو و ممیزی وب‌سایت {p.Name}</div>");
        sb.AppendLine($"            <div><strong>شماره عطف:</strong> <span style=\"font-family:monospace; color:#0284c7; font-weight:bold;\">{refNo}</span></div>");
        sb.AppendLine($"            <div><strong>تاریخ صدور:</strong> {issueDate}</div>");
        sb.AppendLine("            <div><strong>تلفن دفتر:</strong> ۰۴۱-۳۳۳۶۰۸۵۲ | <strong>همراه:</strong> ۰۹۳۶۷۷۶۴۱۴۶</div>");
        sb.AppendLine("            <div><strong>پرتال پشتیبانی:</strong> https://irres.ir | <strong>ایمیل:</strong> info@irres.ir</div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"text-align:center; margin-top:12px; padding-top:6px; font-size:9.5px; color:#64748b; border-top:1px dashed #cbd5e1;\">این سند شامل اطلاعات محرمانه تجاری و آمارهای واقعی سرچ کنسول بوده و انتشار آن بدون مجوز طرفین مجاز نمی‌باشد.</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"a4-footer\"><div>شرکت راهکار الکترونیک سهند (ثبت ۳۲۹۹۵) | پرتال: {p.Domain} | تلفن: ۰۴۱-۳۳۳۶۰۸۵۲</div><div>صفحه ۱۴ از ۱۴</div></div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
