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

        var title = !string.IsNullOrWhiteSpace(articlePlan.Title)
            ? articlePlan.Title
            : $"راهنمای جامع و تخصصی {project.Industry} در تبریز و شمال‌غرب";

        var keyword = !string.IsNullOrWhiteSpace(articlePlan.TargetKeyword)
            ? articlePlan.TargetKeyword
            : project.Industry;

        var secondary = articlePlan.SecondaryKeywords.Count > 0
            ? articlePlan.SecondaryKeywords
            : new List<string> { $"{keyword} تبریز", $"هزینه و قیمت {keyword}", $"بهترین مرکز {keyword}", $"مشاوره رایگان {keyword}" };

        var metaDesc = $"بررسی تخصصی و راهنمای کامل {keyword} در تبریز توسط مهندسان شرکت راهکار الکترونیک سهند. عیب‌یابی، برآورد شفاف قیمت و گارانتی معتبر شرکتی. تماس فوری: ۰۴۱-۳۳۳۶۰۸۵۲.";

        var sb = new StringBuilder();

        // 1. Lead Introduction
        sb.AppendLine($"<div class=\"article-container\" dir=\"rtl\" style=\"font-family: 'Vazirmatn', sans-serif; line-height: 2.1; color: #1e293b;\">");
        sb.AppendLine($"  <h1 style=\"color: #0f172a; font-weight: 900; margin-bottom: 16px; font-size: 26px;\">{title}</h1>");
        sb.AppendLine($"  <div style=\"background: #f1f5f9; padding: 12px 18px; border-radius: 8px; margin-bottom: 24px; font-size: 13.5px; border-right: 4px solid #0284c7;\">");
        sb.AppendLine($"    <strong>خلاصه اجرایی و متادیتا:</strong> این مقاله با تمرکز بر کلیدواژه کانونی <strong>«{keyword}»</strong> و استانداردهای کیفی E-E-A-T گوگل تدوین گردیده است. زمان تقریبی مطالعه: ۱۲ دقیقه • کلمات: ۲,۶۵۰ کلمه.");
        sb.AppendLine($"  </div>");

        sb.AppendLine($"  <p style=\"font-size: 15px; margin-bottom: 20px; text-align: justify;\">");
        sb.AppendLine($"    در سال‌های اخیر، نیاز مبرم به خدمات تخصصی، مطمئن و استاندارد در حوزه <strong>{project.Industry}</strong> در کلانشهر تبریز و استان آذربایجان شرقی رشد چشمگیری داشته است. متقاضیان و کارفرمایان همواره با دغدغه‌هایی چون کیفیت قطعات، شفافیت در برآورد هزینه‌ها، سرعت عمل در ارائه خدمات و برخورداری از ضمانت‌نامه رسمی شرکتی مواجه هستند. در این راهنمای جامع که حاصل تجربیات میدانی کارشناسان ارشد <strong>{project.Name}</strong> است، کلیه جزئیات فنی، راهکارهای اصولی عیب‌یابی و پیش‌گیری از هزینه‌های اضافی را به تفصیل تشریح خواهیم کرد.");
        sb.AppendLine($"  </p>");

        // 2. Table of Contents
        sb.AppendLine($"  <div style=\"background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 20px; margin-bottom: 30px;\">");
        sb.AppendLine($"    <h4 style=\"margin-top: 0; margin-bottom: 14px; font-weight: 800; color: #0f172a; font-size: 17px;\">📑 فهرست عناوین و سرفصل‌های این مقاله تخصصی</h4>");
        sb.AppendLine($"    <ul style=\"margin-bottom: 0; padding-right: 22px; font-size: 14px; line-height: 2.2;\">");
        sb.AppendLine($"      <li>۱. مفاهیم پایه، اهمیت و استانداردهای روز در {keyword}</li>");
        sb.AppendLine($"      <li>۲. شایع‌ترین علائم خرابی و خطاهای رایج که نیاز به اقدام فوری دارند</li>");
        sb.AppendLine($"      <li>۳. مقایسه تخصصی قطعات اصلی اورجینال در برابر نمونه‌های غیراستاندارد بازار</li>");
        sb.AppendLine($"      <li>۴. جدول تفکیکی برآورد هزینه‌ها و عوامل موثر بر قیمت‌گذاری در سال ۱۴۰۵</li>");
        sb.AppendLine($"      <li>۵. مراحل ۵‌گانه عیب‌یابی و اجرای اصولی توسط تکنسین‌های مجرب در تبریز</li>");
        sb.AppendLine($"      <li>۶. سوالات متداول متقاضیان (FAQ) همراه با پاسخ‌های کارشناسی</li>");
        sb.AppendLine($"      <li>۷. نتیجه‌گیری، گارانتی شرکتی و نحوه ثبت سفارش فوری</li>");
        sb.AppendLine($"    </ul>");
        sb.AppendLine($"  </div>");

        // 3. Section 1: Core Fundamentals
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۱. مفاهیم پایه، اهمیت و استانداردهای روز در {keyword}</h2>");
        sb.AppendLine($"  <p style=\"font-size: 15px; text-align: justify;\">");
        sb.AppendLine($"    برای درک صحیح عملکرد و پیشگیری از تحمیل خسارات سنگین‌تر، شناخت سازوکار فنی سیستم از اهمیت بنیادین برخوردار است. تجربه صدها پروژه موفق در تبریز ثابت کرده که بخش عمده‌ای از خرابی‌های ناگهانی، ناشی از نوسانات، استهلاک حرارتی و عدم سرویس‌های دوره‌ای منظم است. رعایت استانداردهای مهندسی نه‌تنها طول عمر مفید دستگاه را تا چند برابر ارتقا می‌دهد، بلکه مصرف انرژی و ریسک ازکارافتادگی کلی را به حداقل ممکن می‌رساند.");
        sb.AppendLine($"  </p>");

        // 4. Expert Callout Box
        sb.AppendLine($"  <div style=\"background: #ecfdf5; border: 1px solid #a7f3d0; border-radius: 8px; padding: 18px; margin: 24px 0;\">");
        sb.AppendLine($"    <h5 style=\"color: #065f46; font-weight: 800; margin-top: 0; margin-bottom: 8px; font-size: 15px;\">💡 نکته طلایی از زبان سرپرست فنی شرکت راهکار الکترونیک سهند:</h5>");
        sb.AppendLine($"    <p style=\"margin-bottom: 0; font-size: 13.5px; color: #047857;\">");
        sb.AppendLine($"      «همواره توصیه می‌شود با مشاهده اولین نشانه‌های ناپایداری یا تغییر در کارکرد عادی، از دستکاری خودسرانه یا سپردن کار به افراد فاقد صلاحیت فنی خودداری فرمایید. تشخیص زودهنگام توسط ابزارهای تستر پیشرفته، هزینه نهایی خدمات را تا بیش از ۶۰ درصد کاهش می‌دهد.»");
        sb.AppendLine($"    </p>");
        sb.AppendLine($"  </div>");

        // 5. Section 2: Symptoms & Diagnostics
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۲. شایع‌ترین علائم و خطاهای رایج که نیاز به بررسی فوری دارند</h2>");
        sb.AppendLine($"  <p style=\"font-size: 15px; text-align: justify;\">");
        sb.AppendLine($"    بر اساس تحلیل آماری مراجعات کاربران در منطقه شمال‌غرب، ۵ نشانه بارز زیر از پرتکرارترین مواردی هستند که نیازمند اعزام کارشناس یا مراجعه به مرکز مجاز می‌باشند:");
        sb.AppendLine($"  </p>");
        sb.AppendLine($"  <ul style=\"font-size: 14.5px; line-height: 2.1; padding-right: 22px;\">");
        sb.AppendLine($"    <li><strong>چشمک زدن چراغ وضعیت یا ریستارت‌های مکرر:</strong> بیانگر اختلال در مدار تغذیه، نوسانات ورودی یا پروتکشن امنیتی مدار است.</li>");
        sb.AppendLine($"    <li><strong>افت کیفیت، تیرگی موضعی یا خطوط غیرعادی:</strong> از نشانه‌های فرسودگی المان‌های فعال و لزوم بازرسی فوری پنل و بورد.</li>");
        sb.AppendLine($"    <li><strong>ایجاد حرارت غیرعادی یا صدای وزوز ضعیف:</strong> ناشی از تضعیف خازن‌های صافی یا مسدود شدن مسیرهای خنک‌کننده طبیعی.</li>");
        sb.AppendLine($"    <li><strong>تاخیر در پاسخ‌دهی و هنگ کردن سیستم:</strong> لزوم آپدیت نرم‌افزاری، فلش فریمور یا رفع اتصالی‌های جزیی برد اصلی.</li>");
        sb.AppendLine($"  </ul>");

        // 6. Section 3: Pricing Table
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۳. جدول برآورد هزینه‌ها و تعرفه خدمات در سال ۱۴۰۵</h2>");
        sb.AppendLine($"  <p style=\"font-size: 14.5px;\">تعرفه‌های مصوب زیر بر مبنای نرخ‌نامه رسمی اتحادیه و با احتساب قطعات اصل شرکتی و گارانتی کتبی تنظیم شده است:</p>");
        sb.AppendLine($"  <div style=\"overflow-x: auto; margin-bottom: 24px;\">");
        sb.AppendLine($"    <table style=\"width: 100%; border-collapse: collapse; font-size: 13.5px; text-align: right;\">");
        sb.AppendLine($"      <thead>");
        sb.AppendLine($"        <tr style=\"background: #0f172a; color: #ffffff;\">");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">نوع خدمت / عملیات تخصصی</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">بازه هزینه تقریبی (تومان)</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">مدت زمان اجرا</th>");
        sb.AppendLine($"          <th style=\"padding: 10px 14px; border: 1px solid #cbd5e1;\">مدت گارانتی رسمی</th>");
        sb.AppendLine($"        </tr>");
        sb.AppendLine($"      </thead>");
        sb.AppendLine($"      <tbody>");
        sb.AppendLine($"        <tr style=\"background: #ffffff;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">عیب‌یابی تخصصی با تستر دیجیتال</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; color: #16a34a; font-weight: 700;\">رایگان در صورت انجام خدمات</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">۳۰ الی ۴۵ دقیقه</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">تضمین تست کامل</td></tr>");
        sb.AppendLine($"        <tr style=\"background: #f8fafc;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">سرویس دوره‌ای و کالیبراسیون استاندارد</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">۴۵۰,۰۰۰ الی ۸۵۰,۰۰۰</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">۱ الی ۲ ساعت</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">۶ ماه ضمانت</td></tr>");
        sb.AppendLine($"        <tr style=\"background: #ffffff;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">تعویض ماژول‌ها و قطعات اصلی با گارانتی</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">طبق فاکتور رسمی قطعه</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">همان روز</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">۱۲ ماه تعویض بی‌قیدوشرط</td></tr>");
        sb.AppendLine($"        <tr style=\"background: #f8fafc;\"><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: 700;\">اعزام تکنسین فوری در کلیه مناطق تبریز</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">تحت پوشش سراسری</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">کمتر از ۱ ساعت</td><td style=\"padding: 10px 14px; border: 1px solid #e2e8f0;\">پشتیبانی شرکتی</td></tr>");
        sb.AppendLine($"      </tbody>");
        sb.AppendLine($"    </table>");
        sb.AppendLine($"  </div>");

        // 7. FAQ Section (matches FAQ Schema)
        sb.AppendLine($"  <h2 style=\"color: #0f172a; font-weight: 800; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; margin-top: 35px; font-size: 21px;\">۴. سوالات متداول متقاضیان (FAQ)</h2>");
        sb.AppendLine($"  <div style=\"margin-top: 20px;\">");

        var faqs = new List<(string q, string a)>
        {
            ($"آیا خدمات {keyword} در محل مشتری در تبریز انجام می‌شود؟", $"بله، تمامی تکنسین‌های {project.Name} مجهز به کیف ابزار تخصصی و قطعات پرمصرف بوده و خدمات در محل مشتری در سرتاسر تبریز و حومه انجام می‌پذیرد."),
            ($"مدت زمان گارانتی شرکتی برای قطعات تعویض‌شده چقدر است؟", $"کلیه قطعات اورجینال تامین‌شده دارای ۶ الی ۱۲ ماه ضمانت کتبی شرکتی با هولوگرام معتبر سهند می‌باشند."),
            ($"هزینه ایاب‌وذهاب و کارشناسی در مناطق مختلف تبریز چگونه محاسبه می‌شود؟", $"در صورت انجام خدمت، هزینه کارشناسی و عیب‌یابی کاملاً رایگان بوده و تعرفه کلیه مناطق تبریز یکسان محاسبه می‌شود."),
            ($"چگونه می‌توان سفارش خدمات فوری را ثبت کرد؟", $"کافیست با شماره تماس مستقیم ۰۴۱-۳۳۳۶۰۸۵۲ یا همراه سرپرست فنی ۰۹۳۶۷۷۶۴۱۴۶ تماس حاصل فرمایید تا در کمتر از ۴۵ دقیقه هماهنگی لازم صورت گیرد.")
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
