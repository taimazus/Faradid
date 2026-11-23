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
        else if (cleanDomain.Contains("yonarak"))
        {
            baseClicksDaily = 42;
            baseImpDaily = 850;
            avgPos = 12.3;
            queryPresets = new()
            {
                ("یونارک", 0.30, 1.1),
                ("کفسابی تبریز", 0.25, 3.8),
                ("نماشویی تبریز با طناب", 0.18, 4.2),
                ("پیچ رولپلاک سنگ نما", 0.12, 5.5),
                ("کفسابی سنگ مرمر", 0.08, 7.1),
                ("شستشوی نما بدون داربست", 0.07, 6.0)
            };
            pagePresets = new()
            {
                ("https://yonarak.ir/", 0.40),
                ("https://yonarak.ir/services/floor-polishing", 0.30),
                ("https://yonarak.ir/services/facade-cleaning", 0.20),
                ("https://yonarak.ir/contact", 0.10)
            };
        }
        else if (cleanDomain.Contains("asazist"))
        {
            baseClicksDaily = 58;
            baseImpDaily = 1100;
            avgPos = 11.2;
            queryPresets = new()
            {
                ("آسا زیست", 0.25, 1.0),
                ("دفع بهداشتی پسماند پزشکی", 0.22, 3.5),
                ("بی‌خطرسازی زباله بیمارستانی", 0.20, 4.1),
                ("امحای پسماند عفونی تبریز", 0.15, 2.9),
                ("قوانین مدیریت پسماند سلامت", 0.10, 8.4),
                ("دستگاه اتوکلاو بیمارستانی", 0.08, 9.8)
            };
            pagePresets = new()
            {
                ("https://asazist.com/", 0.35),
                ("https://asazist.com/medical-waste-disposal", 0.30),
                ("https://asazist.com/environmental-compliance", 0.20),
                ("https://asazist.com/services", 0.15)
            };
        }
        else if (cleanDomain.Contains("spinora"))
        {
            baseClicksDaily = 35;
            baseImpDaily = 720;
            avgPos = 15.5;
            queryPresets = new()
            {
                ("اسپینورا", 0.30, 1.3),
                ("خرید سیسمونی نوزاد", 0.20, 8.5),
                ("کالسکه سبک هواپیمایی", 0.18, 6.2),
                ("صندلی خودرو کودک ایزوفیکس", 0.15, 9.1),
                ("سرویس خواب نوزاد", 0.10, 14.2),
                ("spinora", 0.07, 2.1)
            };
            pagePresets = new()
            {
                ("https://spinora.ir/", 0.45),
                ("https://spinora.ir/strollers", 0.25),
                ("https://spinora.ir/car-seats", 0.18),
                ("https://spinora.ir/nursery", 0.12)
            };
        }
        else
        {
            // tbztvrepair or generic
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
                ("https://tbztvrepair.ir/", 0.50),
                ("https://tbztvrepair.ir/lg-repair-tabriz", 0.25),
                ("https://tbztvrepair.ir/samsung-repair-tabriz", 0.15),
                ("https://tbztvrepair.ir/backlight-replacement", 0.10)
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

    private static string TranslateCountry(string code) => code switch
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
