using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;

namespace Faradid.Engine;

public sealed class GscDataParser : IGscDataParser
{
    public async Task<GscPeriodSummary> ParseCsvAsync(Stream csvStream, string periodName, CancellationToken ct = default)
    {
        var records = new List<GscMetricRecord>();
        using var reader = new StreamReader(csvStream);
        
        string? headerLine = await reader.ReadLineAsync(ct);
        if (headerLine == null)
        {
            return new GscPeriodSummary { PeriodName = periodName };
        }

        var headers = headerLine.Split(',').Select(h => h.Trim('"', ' ', '\t')).ToArray();
        int queryIdx = Array.FindIndex(headers, h => h.Equals("Query", StringComparison.OrdinalIgnoreCase) || h.Equals("Top queries", StringComparison.OrdinalIgnoreCase));
        int pageIdx = Array.FindIndex(headers, h => h.Equals("Page", StringComparison.OrdinalIgnoreCase) || h.Equals("Top pages", StringComparison.OrdinalIgnoreCase));
        int countryIdx = Array.FindIndex(headers, h => h.Equals("Country", StringComparison.OrdinalIgnoreCase));
        int deviceIdx = Array.FindIndex(headers, h => h.Equals("Device", StringComparison.OrdinalIgnoreCase));
        int dateIdx = Array.FindIndex(headers, h => h.Equals("Date", StringComparison.OrdinalIgnoreCase));
        int clicksIdx = Array.FindIndex(headers, h => h.Equals("Clicks", StringComparison.OrdinalIgnoreCase));
        int impIdx = Array.FindIndex(headers, h => h.Equals("Impressions", StringComparison.OrdinalIgnoreCase));
        int ctrIdx = Array.FindIndex(headers, h => h.Equals("CTR", StringComparison.OrdinalIgnoreCase));
        int posIdx = Array.FindIndex(headers, h => h.Equals("Position", StringComparison.OrdinalIgnoreCase));

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseCsvLine(line);
            if (parts.Count == 0) continue;

            var record = new GscMetricRecord();

            if (queryIdx >= 0 && queryIdx < parts.Count) record.Query = parts[queryIdx];
            if (pageIdx >= 0 && pageIdx < parts.Count) record.Page = parts[pageIdx];
            if (countryIdx >= 0 && countryIdx < parts.Count) record.Country = parts[countryIdx];
            if (deviceIdx >= 0 && deviceIdx < parts.Count) record.Device = parts[deviceIdx];

            if (dateIdx >= 0 && dateIdx < parts.Count && DateTime.TryParse(parts[dateIdx], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                record.Date = dt;
            }
            else
            {
                record.Date = DateTime.UtcNow.Date;
            }

            if (clicksIdx >= 0 && clicksIdx < parts.Count && long.TryParse(parts[clicksIdx], out var clk))
                record.Clicks = clk;

            if (impIdx >= 0 && impIdx < parts.Count && long.TryParse(parts[impIdx], out var imp))
                record.Impressions = imp;

            if (ctrIdx >= 0 && ctrIdx < parts.Count)
            {
                var ctrStr = parts[ctrIdx].Replace("%", "").Trim();
                if (double.TryParse(ctrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var ctrVal))
                    record.Ctr = ctrVal > 1.0 ? ctrVal / 100.0 : ctrVal;
            }
            else if (record.Impressions > 0)
            {
                record.Ctr = (double)record.Clicks / record.Impressions;
            }

            if (posIdx >= 0 && posIdx < parts.Count && double.TryParse(parts[posIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var posVal))
                record.Position = posVal;

            records.Add(record);
        }

        return AggregateRecords(records, periodName);
    }

    public Task<GscPeriodSummary> ParseJsonAsync(string jsonContent, string periodName, CancellationToken ct = default)
    {
        var records = JsonSerializer.Deserialize<List<GscMetricRecord>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<GscMetricRecord>();

        return Task.FromResult(AggregateRecords(records, periodName));
    }

    public GscPeriodSummary CreatePresetData(string domain, string periodName, int days = 28)
    {
        var cleanDomain = domain.ToLowerInvariant().Replace("https://", "").Replace("http://", "").TrimEnd('/');
        var records = new List<GscMetricRecord>();
        var now = DateTime.UtcNow.Date;

        long baseClicksDaily;
        long baseImpDaily;
        double avgPos;
        List<(string Query, double Weight, double Pos)> queryPresets;
        List<(string Page, double Weight)> pagePresets;

        if (cleanDomain.Contains("irres"))
        {
            baseClicksDaily = 22;
            baseImpDaily = 480;
            avgPos = 14.8;
            queryPresets = new()
            {
                ("راهکار الکترونیک سهند", 0.35, 1.2),
                ("طراحی نرم افزار تبریز", 0.15, 6.4),
                ("سیستم کنترل تردد سازمانی", 0.12, 8.1),
                ("نرم افزار مدیریت پسماند", 0.10, 4.3),
                ("سامانه اتوماسیون صنعتی سهند", 0.08, 9.2),
                ("هوشمندسازی اداری", 0.07, 12.5),
                ("irres", 0.08, 2.0),
                ("اتوماسیون تردد پرسنلی", 0.05, 11.0)
            };
            pagePresets = new()
            {
                ("https://irres.ir/", 0.45),
                ("https://irres.ir/solutions/access-control", 0.20),
                ("https://irres.ir/solutions/waste-management", 0.15),
                ("https://irres.ir/about-us", 0.10),
                ("https://irres.ir/contact", 0.10)
            };
        }
        else if (cleanDomain == "yonarak.com" || cleanDomain.EndsWith(".yonarak.com"))
        {
            baseClicksDaily = 14;
            baseImpDaily = 320;
            avgPos = 7.4;
            queryPresets = new()
            {
                ("یوناراک", 0.25, 1.3),
                ("قیمت دستگاه دیسک تراش یوناراک", 0.20, 1.9),
                ("قیمت دستگاه دیسک تراش تبریز", 0.18, 1.0),
                ("معایب تراش دیسک درجا", 0.15, 3.5),
                ("دیسک تراش یوناراک", 0.12, 1.3),
                ("دستگاه دیسک تراش رومیزی", 0.10, 4.2)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.45),
                ($"https://{cleanDomain}/product/دستگاه-دیسک-تراش/", 0.30),
                ($"https://{cleanDomain}/articles/", 0.15),
                ($"https://{cleanDomain}/contact-us/", 0.10)
            };
        }
        else if (cleanDomain.Contains("yonarak"))
        {
            baseClicksDaily = 5;
            baseImpDaily = 95;
            avgPos = 9.5;
            queryPresets = new()
            {
                ("یوناراک", 0.40, 1.5),
                ("خدمات اسپارک و وایرکات CNC", 0.25, 4.8),
                ("قطعه سازی و تراشکاری صنعتی تبریز", 0.20, 6.2),
                ("تولید قطعات خودرویی و ابزار دقیق", 0.15, 8.1)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.55),
                ($"https://{cleanDomain}/services", 0.30),
                ($"https://{cleanDomain}/contact", 0.15)
            };
        }
        else if (cleanDomain.Contains("asazist"))
        {
            baseClicksDaily = 28;
            baseImpDaily = 950;
            avgPos = 8.4;
            queryPresets = new()
            {
                ("آسا زیست", 0.28, 1.0),
                ("سمپاشی و دفع آفات خانگی تبریز", 0.24, 5.2),
                ("بهترین سم برای ریشه کنی ساس و سوسک", 0.20, 6.3),
                ("شرکت سمپاشی مجاز بهداشت", 0.16, 4.4),
                ("مقابله با جوندگان و حشرات موذی", 0.12, 7.5)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.40),
                ($"https://{cleanDomain}/pest-control", 0.30),
                ($"https://{cleanDomain}/blog", 0.20),
                ($"https://{cleanDomain}/contact", 0.10)
            };
        }
        else if (cleanDomain.Contains("spinora"))
        {
            baseClicksDaily = 10;
            baseImpDaily = 220;
            avgPos = 14.5;
            queryPresets = new()
            {
                ("اسپینورا", 0.35, 1.8),
                ("گردونه شانس آنلاین فروشگاهی", 0.25, 7.4),
                ("افزایش فروش سایت با گیمیفیکیشن", 0.22, 9.1),
                ("افزونه گردونه شانس وردپرس و سایت", 0.18, 11.2)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.50),
                ($"https://{cleanDomain}/features", 0.30),
                ($"https://{cleanDomain}/pricing", 0.20)
            };
        }
        else if (cleanDomain.Contains("tbztvrepair"))
        {
            baseClicksDaily = 65;
            baseImpDaily = 1350;
            avgPos = 9.8;
            queryPresets = new()
            {
                ("تعمیر تلویزیون تبریز", 0.35, 2.8),
                ("تعمیر بک لایت تلویزیون ال جی تبریز", 0.25, 3.2),
                ("نمایندگی تعمیر سامسونگ تبریز", 0.18, 4.5),
                ("تعمیر پنل آبخورده تلویزیون", 0.12, 6.1),
                ("هزینه تعمیر تلویزیون سونی", 0.10, 7.8)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.50),
                ($"https://{cleanDomain}/lg-repair-tabriz", 0.25),
                ($"https://{cleanDomain}/samsung-repair-tabriz", 0.15),
                ($"https://{cleanDomain}/backlight-replacement", 0.10)
            };
        }
        else
        {
            var brand = cleanDomain.Split('.')[0];
            baseClicksDaily = 20;
            baseImpDaily = 420;
            avgPos = 11.4;
            queryPresets = new()
            {
                (brand, 0.35, 1.5),
                ($"{brand} خدمات و محصولات", 0.25, 5.2),
                ($"قیمت و خرید {brand}", 0.20, 7.4),
                ($"نمایندگی رسمی {brand}", 0.20, 9.1)
            };
            pagePresets = new()
            {
                ($"https://{cleanDomain}/", 0.55),
                ($"https://{cleanDomain}/products", 0.25),
                ($"https://{cleanDomain}/contact", 0.20)
            };
        }

        var rnd = new Random(domain.GetHashCode());

        for (int i = days - 1; i >= 0; i--)
        {
            var dt = now.AddDays(-i);
            
            // Introduce network crisis dip on days 12 to 8 ago (simulating recent Iranian disruption)
            double crisisMultiplier = 1.0;
            if (i >= 8 && i <= 13)
            {
                crisisMultiplier = 0.42; // 58% traffic drop
            }

            var dayClicks = (long)(baseClicksDaily * crisisMultiplier * (0.8 + rnd.NextDouble() * 0.4));
            var dayImp = (long)(baseImpDaily * crisisMultiplier * (0.85 + rnd.NextDouble() * 0.3));

            // Split into countries: Iran vs Germany/VPN vs others
            long deClicks = (long)(dayClicks * 0.48); // Foreign VPN traffic
            long irClicks = (long)(dayClicks * 0.44); // Direct domestic Iran
            long otherClicks = dayClicks - deClicks - irClicks;

            records.Add(new GscMetricRecord
            {
                Date = dt,
                Country = "deu", // Germany (VPN)
                Device = "MOBILE",
                Clicks = deClicks,
                Impressions = (long)(dayImp * 0.50),
                Ctr = 0.052,
                Position = avgPos
            });

            records.Add(new GscMetricRecord
            {
                Date = dt,
                Country = "irn", // Iran
                Device = "MOBILE",
                Clicks = (long)(irClicks * 0.7),
                Impressions = (long)(dayImp * 0.35 * 0.7),
                Ctr = 0.048,
                Position = avgPos
            });

            records.Add(new GscMetricRecord
            {
                Date = dt,
                Country = "irn",
                Device = "DESKTOP",
                Clicks = (long)(irClicks * 0.3),
                Impressions = (long)(dayImp * 0.35 * 0.3),
                Ctr = 0.045,
                Position = avgPos - 0.5
            });

            if (otherClicks > 0)
            {
                records.Add(new GscMetricRecord
                {
                    Date = dt,
                    Country = "nld",
                    Device = "MOBILE",
                    Clicks = otherClicks,
                    Impressions = (long)(dayImp * 0.15),
                    Ctr = 0.049,
                    Position = avgPos
                });
            }
        }

        // Add Query and Page distributions
        var summary = AggregateRecords(records, periodName);

        // Populate top queries
        long totalQueryClicks = summary.TotalClicks;
        foreach (var q in queryPresets)
        {
            var qClicks = (long)(totalQueryClicks * q.Weight);
            var qImp = (long)(qClicks / (0.045 + (15.0 - Math.Min(14.0, q.Pos)) * 0.005));
            summary.TopQueries.Add(new GscDimensionAggregate
            {
                Key = q.Query,
                Label = q.Query,
                Clicks = qClicks,
                Impressions = qImp,
                Ctr = qImp > 0 ? (double)qClicks / qImp : 0,
                Position = q.Pos,
                SharePercentage = Math.Round(q.Weight * 100.0, 1)
            });
        }

        // Populate top pages
        foreach (var p in pagePresets)
        {
            var pClicks = (long)(totalQueryClicks * p.Weight);
            var pImp = (long)(pClicks / 0.048);
            summary.TopPages.Add(new GscDimensionAggregate
            {
                Key = p.Page,
                Label = p.Page,
                Clicks = pClicks,
                Impressions = pImp,
                Ctr = pImp > 0 ? (double)pClicks / pImp : 0,
                Position = avgPos,
                SharePercentage = Math.Round(p.Weight * 100.0, 1)
            });
        }

        return summary;
    }

    private static GscPeriodSummary AggregateRecords(List<GscMetricRecord> records, string periodName)
    {
        var summary = new GscPeriodSummary
        {
            PeriodName = periodName,
            DailyMetrics = records
        };

        if (records.Count == 0) return summary;

        summary.StartDate = records.Min(r => r.Date);
        summary.EndDate = records.Max(r => r.Date);
        summary.TotalClicks = records.Sum(r => r.Clicks);
        summary.TotalImpressions = records.Sum(r => r.Impressions);
        summary.AverageCtr = summary.TotalImpressions > 0 ? (double)summary.TotalClicks / summary.TotalImpressions : 0;
        summary.AveragePosition = records.Where(r => r.Position > 0).DefaultIfEmpty().Average(r => r?.Position ?? 0);

        // Aggregate by Country
        var byCountry = records.GroupBy(r => string.IsNullOrWhiteSpace(r.Country) ? "UNKNOWN" : r.Country.ToUpperInvariant())
            .Select(g =>
            {
                var clk = g.Sum(x => x.Clicks);
                var imp = g.Sum(x => x.Impressions);
                var name = TranslateCountry(g.Key);
                return new GscDimensionAggregate
                {
                    Key = g.Key,
                    Label = name,
                    Clicks = clk,
                    Impressions = imp,
                    Ctr = imp > 0 ? (double)clk / imp : 0,
                    Position = g.Where(x => x.Position > 0).DefaultIfEmpty().Average(x => x?.Position ?? 0),
                    SharePercentage = summary.TotalClicks > 0 ? Math.Round((double)clk / summary.TotalClicks * 100.0, 1) : 0
                };
            })
            .OrderByDescending(c => c.Clicks)
            .ToList();
        summary.CountryBreakdown = byCountry;

        // Aggregate by Device
        var byDevice = records.GroupBy(r => string.IsNullOrWhiteSpace(r.Device) ? "UNKNOWN" : r.Device.ToUpperInvariant())
            .Select(g =>
            {
                var clk = g.Sum(x => x.Clicks);
                var imp = g.Sum(x => x.Impressions);
                var label = g.Key switch
                {
                    "MOBILE" => "موبایل (Mobile)",
                    "DESKTOP" => "رایانه (Desktop)",
                    "TABLET" => "تبلت (Tablet)",
                    _ => g.Key
                };
                return new GscDimensionAggregate
                {
                    Key = g.Key,
                    Label = label,
                    Clicks = clk,
                    Impressions = imp,
                    Ctr = imp > 0 ? (double)clk / imp : 0,
                    Position = g.Where(x => x.Position > 0).DefaultIfEmpty().Average(x => x?.Position ?? 0),
                    SharePercentage = summary.TotalClicks > 0 ? Math.Round((double)clk / summary.TotalClicks * 100.0, 1) : 0
                };
            })
            .OrderByDescending(d => d.Clicks)
            .ToList();
        summary.DeviceBreakdown = byDevice;

        return summary;
    }

    public static string TranslateCountry(string code) => code switch
    {
        "IRN" => "ایران (IRN)",
        "DEU" => "آلمان / فیلترشکن (DEU)",
        "USA" => "آمریکا (USA)",
        "NLD" => "هلند (NLD)",
        "GBR" => "انگلستان (GBR)",
        "TUR" => "ترکیه (TUR)",
        "FRA" => "فرانسه (FRA)",
        "ARE" => "امارات متحده (ARE)",
        _ => code
    };

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var currentStr = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentStr.ToString().Trim('\"', ' '));
                currentStr.Clear();
            }
            else
            {
                currentStr.Append(c);
            }
        }
        result.Add(currentStr.ToString().Trim('\"', ' '));
        return result;
    }
}
