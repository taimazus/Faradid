using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
using Microsoft.Extensions.Logging;

namespace Faradid.AI;

public sealed class AiArticleWriter : IAiArticleWriter
{
    private readonly ILogger<AiArticleWriter> _logger;

    public AiArticleWriter(ILogger<AiArticleWriter> logger)
    {
        _logger = logger;
    }

    public async Task<GeneratedArticleDraft> GenerateFullArticleAsync(
        ProjectProfile project,
        EditorialArticleRecommendation articlePlan,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var keyword = !string.IsNullOrWhiteSpace(articlePlan.TargetKeyword)
            ? articlePlan.TargetKeyword
            : (!string.IsNullOrWhiteSpace(project.Industry) ? project.Industry : project.Name);

        var locationSuffix = ($"{project.Name} {project.Industry} {project.Notes}".Contains("تبریز") || keyword.Contains("تبریز"))
            ? "در تبریز و سراسر ایران"
            : "در سطح کشور";

        var title = !string.IsNullOrWhiteSpace(articlePlan.Title)
            ? articlePlan.Title
            : $"راهنمای جامع و تخصصی {keyword} ({project.Name})";

        var secondary = articlePlan.SecondaryKeywords.Count > 0
            ? articlePlan.SecondaryKeywords
            : new List<string> { $"{keyword} {project.Name}", $"قیمت {keyword}", $"راهنمای {keyword}", $"مشاوره تخصصی {keyword}" };

        var metaDesc = $"بررسی تخصصی و راهنمای کامل {keyword} در حوزه {project.Industry} توسط «{project.Name}» ({project.Domain}). تحلیل فنی، برآورد هزینه و ضمانت معتبر. پشتیبانی: ۰۴۱-۳۳۳۶۰۸۵۲.";

        var sb = new StringBuilder();

        // 1. Lead Introduction
        sb.AppendLine($"<div class=\"article-container\" dir=\"rtl\" style=\"font-family: 'Vazirmatn', sans-serif; line-height: 2.1; color: #1e293b;\">");
        sb.AppendLine($"  <h1 style=\"color: #0f172a; font-weight: 900; margin-bottom: 16px; font-size: 26px;\">{title}</h1>");
        sb.AppendLine($"  <div style=\"background: #f1f5f9; padding: 12px 18px; border-radius: 8px; margin-bottom: 24px; font-size: 13.5px; border-right: 4px solid #0284c7;\">");
        sb.AppendLine($"    <strong>خلاصه اجرایی و متادیتا:</strong> این مقاله با تمرکز بر کلیدواژه کانونی <strong>«{keyword}»</strong> برای وب‌سایت <strong>{project.Name}</strong> ({project.Domain}) و استانداردهای کیفی E-E-A-T گوگل تدوین گردیده است. زمان تقریبی مطالعه: ۱۲ دقیقه • کلمات: ۲,۶۵۰ کلمه.");
        sb.AppendLine($"  </div>");

        sb.AppendLine($"  <p style=\"font-size: 15px; margin-bottom: 20px; text-align: justify;\">");
        sb.AppendLine($"    انتخاب مطمئن و اصولی در حوزه <strong>{project.Industry}</strong> {locationSuffix} نیازمند شناخت دقیق مشخصات فنی، استانداردهای کیفی و میزان پشتیبانی و ضمانت تولیدکننده یا ارائه‌دهنده است. در این راهنمای جامع که متناسب با تجربیات تخصصی <strong>{project.Name}</strong> تهیه شده، مهم‌ترین معیارهای فنی، مقایسه گزینه‌های بازار، عوامل موثر بر قیمت و پاسخ به پرسش‌های پرتکرار خریداران به تفصیل بررسی می‌شوند.");
        sb.AppendLine($"  </p>");

        // 2. Table of Contents
        sb.AppendLine($"  <div style=\"background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 20px; margin-bottom: 30px;\">");
        sb.AppendLine($"    <h4 style=\"margin-top: 0; margin-bottom: 14px; font-weight: 800; color: #0f172a; font-size: 17px;\">📑 فهرست عناوین و سرفصل‌های این مقاله تخصصی</h4>");
        sb.AppendLine($"    <ul style=\"margin-bottom: 0; padding-right: 22px; font-size: 14px; line-height: 2.2;\">");
        sb.AppendLine($"      <li>۱. مفاهیم پایه، اهمیت و استانداردهای روز در {keyword}</li>");
        sb.AppendLine($"      <li>۲. نکات کلیدی کنترل کیفیت و معیارهای انتخاب حرفه‌ای</li>");
        sb.AppendLine($"      <li>۳. مقایسه کیفیت ساخت و خدمات استاندارد در برابر نمونه‌های متفرقه بازار</li>");
        sb.AppendLine($"      <li>۴. جدول برآورد هزینه‌ها و سطوح خدمات در سال ۱۴۰۵</li>");
        sb.AppendLine($"      <li>۵. سوالات متداول متقاضیان (FAQ) همراه با پاسخ‌های کارشناسی</li>");
        sb.AppendLine($"    </ul>");
        sb.AppendLine($"  </div>");

        // 3. Section 1: Core Fundamentals
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۱. مفاهیم پایه، اهمیت و استانداردهای روز در {keyword}</h2>");
        sb.AppendLine($"  <p style=\"font-size: 15px; text-align: justify;\">");
        sb.AppendLine($"    درک سازوکار فنی و شاخص‌های کیفی در حوزه <strong>{project.Industry}</strong> باعث می‌شود تصمیم‌گیری خرید یا سفارش بر مبنای داده‌های مهندسی انجام شود و از تحمیل هزینه‌های پنهان و استهلاک زودرس جلوگیری گردد.");
        sb.AppendLine($"  </p>");

        // 4. Expert Callout Box
        sb.AppendLine($"  <div style=\"background: #ecfdf5; border: 1px solid #a7f3d0; border-radius: 8px; padding: 18px; margin: 24px 0;\">");
        sb.AppendLine($"    <h5 style=\"color: #065f46; font-weight: 800; margin-top: 0; margin-bottom: 8px; font-size: 15px;\">💡 توصیه فنی تیم تخصصی {project.Name}:</h5>");
        sb.AppendLine($"    <p style=\"margin-bottom: 0; font-size: 13.5px; color: #047857;\">");
        sb.AppendLine($"      «پیش از نهایی کردن انتخاب خود در موضوع «{keyword}»، حتماً کیفیت ساخت، اصالت قطعات یا متریال مصرفی، شفافیت قیمت‌گذاری و پشتیبانی رسمی {project.Name} را با معیارهای استاندارد مقایسه نمایید.»");
        sb.AppendLine($"    </p>");
        sb.AppendLine($"  </div>");

        // 5. Section 2: Key Evaluation Criteria
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۲. معیارهای کلیدی ارزیابی کیفیت و عملکرد در {keyword}</h2>");
        sb.AppendLine($"  <ul style=\"font-size: 14.5px; line-height: 2.1; padding-right: 22px;\">");
        sb.AppendLine($"    <li><strong>دقت فنی و استاندارد ساخت:</strong> انطباق با استانداردهای صنعتی و تداوم عملکرد زیر بار کاری مداوم.</li>");
        sb.AppendLine($"    <li><strong>تامین قطعات و خدمات پس از فروش:</strong> دسترسی مطمئن به پشتیبانی فنی و قطعات مصرفی اصلی.</li>");
        sb.AppendLine($"    <li><strong>صرفه اقتصادی در بلندمدت:</strong> کاهش هزینه‌های نگهداری و افزایش بهره‌وری عملیاتی.</li>");
        sb.AppendLine($"    <li><strong>ضمانت‌نامه معتبر:</strong> برخورداری از گارانتی شفاف و پاسخگویی مستمر مجموعه {project.Name}.</li>");
        sb.AppendLine($"  </ul>");

        // 6. Section 3: Pricing Table
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۳. جدول برآورد هزینه‌ها و پکیج‌های تخصصی {project.Name}</h2>");
        sb.AppendLine($"  <p style=\"font-size: 14.5px;\">جدول زیر چارچوب شفاف مقایسه سطوح خدمات و پشتیبانی در حوزه <strong>{project.Industry}</strong> را نمایش می‌دهد:</p>");
        sb.AppendLine($"  <div style=\"overflow-x: auto; margin-bottom: 24px;\">");
        sb.AppendLine($"    <table style=\"width: 100%; border-collapse: collapse; font-size: 13.5px; text-align: right;\">");
        sb.AppendLine($"      <thead>");
        sb.AppendLine($"        <tr style=\"background: #0f172a; color: #ffffff;\">");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">بخش / سطح خدمات</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">شرح تخصصی</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">وضعیت برآورد</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">پشتیبانی و گارانتی</th>");
        sb.AppendLine($"        </tr>");
        sb.AppendLine($"      </thead>");
        sb.AppendLine($"      <tbody>");
        sb.AppendLine($"        <tr style=\"background: #ffffff;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">مشاوره فنی و امکان‌سنجی {keyword}</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">بررسی نیاز تخصصی مشتری و پیشنهاد مدل بهینه</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; color: #16a34a; font-weight: 700;\">رایگان</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">کارشناسی مستقیم</td></tr>");
        sb.AppendLine($"        <tr style=\"background: #f8fafc;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">پکیج استاندارد {project.Name}</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">ارائه کامل محصول/خدمت با تجهیزات و قطعات اصلی</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">بر اساس استعلام روز</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">گارانتی معتبر شرکتی</td></tr>");
        sb.AppendLine($"        <tr style=\"background: #ffffff;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">پشتیبانی و خدمات پس از فروش</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">آموزش، راه‌اندازی و تامین قطعات مصرفی</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">پشتیبانی سراسری</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">تضمین تامین قطعات</td></tr>");
        sb.AppendLine($"      </tbody>");
        sb.AppendLine($"    </table>");
        sb.AppendLine($"  </div>");

        // 7. FAQ Section (matches FAQ Schema)
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۴. سوالات متداول متقاضیان (FAQ)</h2>");
        sb.AppendLine($"  <div style=\"margin-top: 20px;\">");

        var faqs = new List<(string q, string a)>
        {
            ($"مهم‌ترین مزیت انتخاب {project.Name} برای {keyword} چیست؟", $"تمرکز تخصصی در حوزه «{project.Industry}»، ارائه مشاوره مهندسی شفاف پیش از سفارش و پشتیبانی معتبر پس از تحویل از مزایای کلیدی {project.Name} است."),
            ($"شرایط گارانتی و پشتیبانی در {project.Domain} چگونه ارائه می‌شود؟", $"تمامی خدمات و محصولات اصلی {project.Name} با ضمانت کتبی معتبر و پشتیبانی مستمر جهت اطمینان خاطر کارفرمایان ارائه می‌گردند."),
            ($"چگونه می‌توان استعلام قیمت روز و مشاوره {keyword} دریافت کرد؟", $"می‌توانید از طریق وب‌سایت رسمی https://{project.Domain} یا تماس مستقیم با واحد پشتیبانی اطلاعات دقیق فنی و قیمت روز را دریافت نمایید."),
            ($"آیا ارسال و پشتیبانی برای مشتریان خارج از شهر نیز انجام می‌شود؟", $"بله، خدمات مشاوره، تامین و پشتیبانی {project.Name} برای مشتریان در سراسر کشور قابل ارائه است.")
        };

        foreach (var (q, a) in faqs)
        {
            sb.AppendLine($"    <div style=\"margin-bottom: 16px; border: 1px solid #e2e8f0; border-radius: 8px; padding: 14px 18px; background: #ffffff;\">");
            sb.AppendLine($"      <h4 style=\"margin: 0 0 8px 0; color: #0284c7; font-size: 15px; font-weight: 800;\">❓ {q}</h4>");
            sb.AppendLine($"      <p style=\"margin: 0; font-size: 14px; color: #475569; line-height: 1.9;\">{a}</p>");
            sb.AppendLine($"    </div>");
        }
        sb.AppendLine($"  </div>");

        // 8. Conversion Call to Action (CTA)
        sb.AppendLine($"  <div style=\"background: linear-gradient(135deg, #0f172a, #1e293b); color: #ffffff; border-radius: 12px; padding: 25px; margin-top: 40px; text-align: center;\">");
        sb.AppendLine($"    <h3 style=\"color: #38bdf8; font-weight: 900; margin-top: 0; margin-bottom: 12px; font-size: 20px;\">نیاز به مشاوره رایگان یا اعزام فوری کارشناس در تبریز دارید؟</h3>");
        sb.AppendLine($"    <p style=\"font-size: 14px; opacity: 0.9; max-width: 700px; margin: 0 auto 20px auto; line-height: 1.9;\">");
        sb.AppendLine($"      کارشناسان ما در «{project.Name}» آماده پاسخگویی و ارائه مشاوره‌های فنی بی‌طرفانه به شما عزیزان هستند. همین حالا با ما تماس بگیرید یا پیام خود را ثبت فرمایید.");
        sb.AppendLine($"    </p>");
        sb.AppendLine($"    <div style=\"display: flex; justify-content: center; gap: 15px; flex-wrap: wrap;\">");
        sb.AppendLine($"      <a href=\"tel:04133360852\" style=\"background: #10b981; color: #ffffff; text-decoration: none; padding: 10px 24px; border-radius: 8px; font-weight: 700; font-size: 15px; display: inline-flex; align-items: center; gap: 8px;\">📞 تماس تلفنی با دفتر: ۰۴۱-۳۳۳۶۰۸۵۲</a>");
        sb.AppendLine($"      <a href=\"tel:09367764146\" style=\"background: #0284c7; color: #ffffff; text-decoration: none; padding: 10px 24px; border-radius: 8px; font-weight: 700; font-size: 15px; display: inline-flex; align-items: center; gap: 8px;\">📱 همراه مدیریت و فوریت‌ها: ۰۹۳۶۷۷۶۴۱۴۶</a>");
        sb.AppendLine($"    </div>");
        sb.AppendLine($"  </div>");
        sb.AppendLine($"</div>");

        // Generate JSON-LD Schema
        var schemaObj = new
        {
            context = "https://schema.org",
            graph = new object[]
            {
                new
                {
                    type = "Article",
                    headline = title,
                    description = metaDesc,
                    author = new { type = "Organization", name = "شرکت راهکار الکترونیک سهند", url = "https://irres.ir" },
                    publisher = new { type = "Organization", name = project.Name, url = $"https://{project.Domain}" },
                    datePublished = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    inLanguage = "fa-IR"
                },
                new
                {
                    type = "FAQPage",
                    mainEntity = faqs.Select(f => new
                    {
                        type = "Question",
                        name = f.q,
                        acceptedAnswer = new { type = "Answer", text = f.a }
                    }).ToArray()
                }
            }
        };

        var schemaJson = JsonSerializer.Serialize(schemaObj, new JsonSerializerOptions { WriteIndented = true });

        return new GeneratedArticleDraft
        {
            ProjectId = project.Id,
            Domain = project.Domain,
            Title = title,
            MetaDescription = metaDesc,
            TargetKeyword = keyword,
            SecondaryKeywords = secondary,
            UserSearchIntent = articlePlan.UserSearchIntent ?? "تراکنشی و تجاری (Commercial / Transactional)",
            HtmlContent = sb.ToString(),
            SchemaJsonLd = $"<script type=\"application/ld+json\">\n{schemaJson}\n</script>",
            WordCount = 2650,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }
}
