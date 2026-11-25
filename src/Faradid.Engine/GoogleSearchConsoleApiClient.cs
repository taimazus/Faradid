using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Faradid.Core.Interfaces;
using Faradid.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Faradid.Engine;

public sealed class GoogleSearchConsoleApiClient : IGscApiClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleSearchConsoleApiClient> _logger;
    private readonly IGscDataParser _presetParser;
    private readonly string _snapshotDir;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedAccessToken;
    private DateTime _tokenExpiresUtc = DateTime.MinValue;
    private List<(string SiteUrl, string PermissionLevel)>? _cachedSites;
    private DateTime _sitesCacheExpiresUtc = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public GoogleSearchConsoleApiClient(
        HttpClient http,
        IConfiguration config,
        ILogger<GoogleSearchConsoleApiClient> logger,
        IGscDataParser presetParser)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _presetParser = presetParser;
        _snapshotDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "gsc_snapshots");
        if (!Directory.Exists(_snapshotDir))
        {
            Directory.CreateDirectory(_snapshotDir);
        }
    }

    public string? ResolveCredentialsPath()
    {
        var configured = _config["GoogleSearchConsole:ServiceAccountKeyJsonPath"];
        var envPath = Environment.GetEnvironmentVariable("FARADID_GSC_SA_PATH")
                      ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(configured);
            candidates.Add(Path.Combine(AppContext.BaseDirectory, configured));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), configured));
        }
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            candidates.Add(envPath);
        }

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "App_Data", "service_account.json"));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "service_account.json"));

        return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
    {
        var keyPath = ResolveCredentialsPath();
        return Task.FromResult(!string.IsNullOrWhiteSpace(keyPath));
    }

    public async Task<bool> VerifySiteAccessAsync(string siteUrl, CancellationToken ct = default)
    {
        try
        {
            var resolved = await ResolvePropertyUrlAsync(siteUrl, ct);
            return !string.IsNullOrWhiteSpace(resolved);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying GSC site access for {SiteUrl}", siteUrl);
            return false;
        }
    }

    public async Task<string?> ResolvePropertyUrlAsync(string siteUrl, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var sites = await GetAccessibleSitesAsync(token, ct);
        if (sites.Count == 0) return null;

        var cleanDomain = NormalizeDomain(siteUrl);
        if (string.IsNullOrWhiteSpace(cleanDomain)) return null;

        var scDomainTarget = $"sc-domain:{cleanDomain}";
        var exactSc = sites.FirstOrDefault(s => s.SiteUrl.Equals(scDomainTarget, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(exactSc.SiteUrl)) return exactSc.SiteUrl;

        var httpsTarget = $"https://{cleanDomain}/";
        var exactHttps = sites.FirstOrDefault(s => s.SiteUrl.Equals(httpsTarget, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(exactHttps.SiteUrl)) return exactHttps.SiteUrl;

        var wwwTarget = $"https://www.{cleanDomain}/";
        var exactWww = sites.FirstOrDefault(s => s.SiteUrl.Equals(wwwTarget, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(exactWww.SiteUrl)) return exactWww.SiteUrl;

        var httpTarget = $"http://{cleanDomain}/";
        var exactHttp = sites.FirstOrDefault(s => s.SiteUrl.Equals(httpTarget, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(exactHttp.SiteUrl)) return exactHttp.SiteUrl;

        // Check subdomain sc-domain only if parent domain matches cleanly (never confuse yonarak.ir with yonarak.com)
        var subMatch = sites.FirstOrDefault(s =>
            s.SiteUrl.EndsWith($".{cleanDomain}", StringComparison.OrdinalIgnoreCase) ||
            s.SiteUrl.Contains($"://{cleanDomain}/", StringComparison.OrdinalIgnoreCase));
        return subMatch.SiteUrl;
    }

    public async Task<GscPeriodSummary> FetchSearchAnalyticsAsync(string siteUrl, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var cleanDomain = NormalizeDomain(siteUrl);
        var periodName = $"همگام‌سازی زنده API سرچ کنسول گوگل ({startDate:yyyy/MM/dd} الی {endDate:yyyy/MM/dd})";

        // 1. Check short-term snapshot cache first (3h TTL)
        var cacheFile = Path.Combine(_snapshotDir, $"{SanitizeFileName(cleanDomain)}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.json");
        try
        {
            if (File.Exists(cacheFile) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile)).TotalHours < 3)
            {
                var cachedJson = await File.ReadAllTextAsync(cacheFile, ct);
                var cached = JsonSerializer.Deserialize<GscPeriodSummary>(cachedJson, JsonOptions);
                if (cached != null && cached.IsLiveData)
                {
                    cached.PeriodName = periodName;
                    return cached;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading cached GSC snapshot for {Domain}", cleanDomain);
        }

        // 2. Authenticate via Service Account OAuth2 JWT and fetch real analytics
        try
        {
            var token = await GetAccessTokenAsync(ct);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var propertyUrl = await ResolvePropertyUrlAsync(cleanDomain, ct);
                if (!string.IsNullOrWhiteSpace(propertyUrl))
                {
                    var liveSummary = await FetchLiveDimensionsAsync(token, propertyUrl, periodName, startDate, endDate, ct);
                    liveSummary.IsLiveData = true;
                    liveSummary.DataSource = "Google Search Console API (Live)";
                    liveSummary.ResolvedPropertyUrl = propertyUrl;

                    try
                    {
                        await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(liveSummary, JsonOptions), ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed saving GSC snapshot cache for {Domain}", cleanDomain);
                    }

                    return liveSummary;
                }
                else
                {
                    _logger.LogWarning("Domain {Domain} is not verified/accessible in Google Search Console Service Account.", cleanDomain);
                }
            }
            else
            {
                _logger.LogWarning("Google Search Console Service Account credentials missing or failed OAuth token exchange.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live GSC API fetch failed for {Domain}.", cleanDomain);
        }

        // 3. Honest Fallback: never label preset data as live direct sync
        var days = Math.Max(7, (int)Math.Ceiling((endDate - startDate).TotalDays));
        var fallback = _presetParser.CreatePresetData(cleanDomain, $"{periodName} [داده برآوردی محلی — عدم دسترسی API]", days);
        fallback.StartDate = startDate.Date;
        fallback.EndDate = endDate.Date;
        fallback.IsLiveData = false;
        fallback.DataSource = "OfflineCalibratedFallback";
        return fallback;
    }

    private async Task<GscPeriodSummary> FetchLiveDimensionsAsync(
        string token,
        string propertyUrl,
        string periodName,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var startStr = startDate.ToString("yyyy-MM-dd");
        var endStr = endDate.ToString("yyyy-MM-dd");

        var dateTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "date" }, 500, ct);
        var queryTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "query" }, 50, ct);
        var pageTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "page" }, 30, ct);
        var countryTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "country" }, 25, ct);
        var deviceTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "device" }, 10, ct);
        var queryPageTask = QueryGscRowsAsync(token, propertyUrl, startStr, endStr, new[] { "query", "page" }, 100, ct);

        await Task.WhenAll(dateTask, queryTask, pageTask, countryTask, deviceTask, queryPageTask);

        var dateRows = await dateTask;
        var queryRows = await queryTask;
        var pageRows = await pageTask;
        var countryRows = await countryTask;
        var deviceRows = await deviceTask;
        var queryPageRows = await queryPageTask;

        var dailyMetrics = dateRows
            .Where(r => r.Keys.Count > 0 && DateTime.TryParse(r.Keys[0], out _))
            .Select(r => new GscMetricRecord
            {
                Date = DateTime.Parse(r.Keys[0]),
                Clicks = r.Clicks,
                Impressions = r.Impressions,
                Ctr = r.Ctr,
                Position = r.Position
            })
            .OrderBy(m => m.Date)
            .ToList();

        long totalClicks = dailyMetrics.Sum(m => m.Clicks);
        long totalImpressions = dailyMetrics.Sum(m => m.Impressions);

        if (totalClicks == 0 && totalImpressions == 0 && queryRows.Count > 0)
        {
            totalClicks = queryRows.Sum(q => q.Clicks);
            totalImpressions = queryRows.Sum(q => q.Impressions);
        }

        double avgCtr = totalImpressions > 0 ? (double)totalClicks / totalImpressions : 0.0;
        double avgPosition = dailyMetrics.Count > 0
            ? (dailyMetrics.Sum(d => d.Impressions) > 0
                ? dailyMetrics.Sum(d => d.Position * d.Impressions) / dailyMetrics.Sum(d => d.Impressions)
                : dailyMetrics.Average(d => d.Position))
            : (queryRows.Count > 0 ? queryRows.Average(q => q.Position) : 0.0);

        // Map query -> best performing page URL from query+page pairs
        var bestUrlByQuery = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var qp in queryPageRows.OrderByDescending(x => x.Clicks).ThenByDescending(x => x.Impressions))
        {
            if (qp.Keys.Count >= 2 && !string.IsNullOrWhiteSpace(qp.Keys[0]) && !string.IsNullOrWhiteSpace(qp.Keys[1]))
            {
                bestUrlByQuery.TryAdd(qp.Keys[0], qp.Keys[1]);
            }
        }

        var topQueries = queryRows
            .Where(r => r.Keys.Count > 0 && !string.IsNullOrWhiteSpace(r.Keys[0]))
            .OrderByDescending(r => r.Clicks)
            .ThenByDescending(r => r.Impressions)
            .Take(50)
            .Select(r => new GscDimensionAggregate
            {
                Key = r.Keys[0],
                Label = bestUrlByQuery.TryGetValue(r.Keys[0], out var bestUrl) ? bestUrl : r.Keys[0],
                Clicks = r.Clicks,
                Impressions = r.Impressions,
                Ctr = r.Ctr,
                Position = Math.Round(r.Position, 1),
                SharePercentage = totalClicks > 0 ? Math.Round((double)r.Clicks / totalClicks * 100.0, 1) : 0.0
            })
            .ToList();

        var topPages = pageRows
            .Where(r => r.Keys.Count > 0 && !string.IsNullOrWhiteSpace(r.Keys[0]))
            .OrderByDescending(r => r.Clicks)
            .ThenByDescending(r => r.Impressions)
            .Take(30)
            .Select(r => new GscDimensionAggregate
            {
                Key = r.Keys[0],
                Label = r.Keys[0],
                Clicks = r.Clicks,
                Impressions = r.Impressions,
                Ctr = r.Ctr,
                Position = Math.Round(r.Position, 1),
                SharePercentage = totalClicks > 0 ? Math.Round((double)r.Clicks / totalClicks * 100.0, 1) : 0.0
            })
            .ToList();

        long countryClickSum = Math.Max(1, countryRows.Sum(c => c.Clicks));
        var countryBreakdown = countryRows
            .Where(r => r.Keys.Count > 0)
            .OrderByDescending(r => r.Clicks)
            .ThenByDescending(r => r.Impressions)
            .Select(r =>
            {
                var code = r.Keys[0].ToUpperInvariant();
                return new GscDimensionAggregate
                {
                    Key = code,
                    Label = GscDataParser.TranslateCountry(code),
                    Clicks = r.Clicks,
                    Impressions = r.Impressions,
                    Ctr = r.Ctr,
                    Position = Math.Round(r.Position, 1),
                    SharePercentage = Math.Round((double)r.Clicks / countryClickSum * 100.0, 1)
                };
            })
            .ToList();

        long deviceClickSum = deviceRows.Sum(d => d.Clicks);
        long deviceImpSum = Math.Max(1, deviceRows.Sum(d => d.Impressions));
        var deviceBreakdown = deviceRows
            .Where(r => r.Keys.Count > 0)
            .OrderByDescending(r => r.Impressions)
            .Select(r =>
            {
                var key = r.Keys[0].ToUpperInvariant();
                var label = key switch
                {
                    "MOBILE" => "موبایل (Mobile)",
                    "DESKTOP" => "رایانه (Desktop)",
                    "TABLET" => "تبلت (Tablet)",
                    _ => key
                };
                double share = deviceClickSum > 0
                    ? Math.Round((double)r.Clicks / deviceClickSum * 100.0, 1)
                    : Math.Round((double)r.Impressions / deviceImpSum * 100.0, 1);

                return new GscDimensionAggregate
                {
                    Key = key,
                    Label = label,
                    Clicks = r.Clicks,
                    Impressions = r.Impressions,
                    Ctr = r.Ctr,
                    Position = Math.Round(r.Position, 1),
                    SharePercentage = share
                };
            })
            .ToList();

        return new GscPeriodSummary
        {
            PeriodName = periodName,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalClicks = totalClicks,
            TotalImpressions = totalImpressions,
            AverageCtr = avgCtr,
            AveragePosition = Math.Round(avgPosition, 1),
            TopQueries = topQueries,
            TopPages = topPages,
            CountryBreakdown = countryBreakdown,
            DeviceBreakdown = deviceBreakdown,
            DailyMetrics = dailyMetrics
        };
    }

    private async Task<List<GscApiRow>> QueryGscRowsAsync(
        string token,
        string propertyUrl,
        string startDate,
        string endDate,
        string[] dimensions,
        int rowLimit,
        CancellationToken ct)
    {
        var apiUrl = $"https://www.googleapis.com/webmasters/v3/sites/{Uri.EscapeDataString(propertyUrl)}/searchAnalytics/query";
        var payload = new
        {
            startDate,
            endDate,
            dimensions,
            rowLimit
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("GSC searchAnalytics query ({Dims}) failed with HTTP {Status}: {Body}", string.Join(",", dimensions), resp.StatusCode, errBody);
            return new List<GscApiRow>();
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var list = new List<GscApiRow>();

        if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rowsEl.EnumerateArray())
            {
                var item = new GscApiRow
                {
                    Clicks = r.TryGetProperty("clicks", out var c) ? (long)Math.Round(c.GetDouble()) : 0,
                    Impressions = r.TryGetProperty("impressions", out var im) ? (long)Math.Round(im.GetDouble()) : 0,
                    Ctr = r.TryGetProperty("ctr", out var ctr) ? ctr.GetDouble() : 0.0,
                    Position = r.TryGetProperty("position", out var pos) ? pos.GetDouble() : 0.0
                };
                if (r.TryGetProperty("keys", out var keysEl) && keysEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var k in keysEl.EnumerateArray())
                    {
                        item.Keys.Add(k.GetString() ?? string.Empty);
                    }
                }
                list.Add(item);
            }
        }

        return list;
    }

    private async Task<List<(string SiteUrl, string PermissionLevel)>> GetAccessibleSitesAsync(string token, CancellationToken ct)
    {
        if (_cachedSites != null && DateTime.UtcNow < _sitesCacheExpiresUtc)
        {
            return _cachedSites;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/webmasters/v3/sites");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed listing GSC sites: HTTP {Status}", resp.StatusCode);
            return new List<(string, string)>();
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var list = new List<(string SiteUrl, string PermissionLevel)>();

        if (doc.RootElement.TryGetProperty("siteEntry", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in entries.EnumerateArray())
            {
                var siteUrl = e.TryGetProperty("siteUrl", out var su) ? su.GetString() : null;
                var perm = e.TryGetProperty("permissionLevel", out var pl) ? pl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(siteUrl))
                {
                    list.Add((siteUrl, perm ?? string.Empty));
                }
            }
        }

        _cachedSites = list;
        _sitesCacheExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        return list;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresUtc)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresUtc)
            {
                return _cachedAccessToken;
            }

            var keyPath = ResolveCredentialsPath();
            if (string.IsNullOrWhiteSpace(keyPath) || !File.Exists(keyPath))
            {
                return null;
            }

            var saJson = await File.ReadAllTextAsync(keyPath, ct);
            using var doc = JsonDocument.Parse(saJson);
            var root = doc.RootElement;

            var clientEmail = root.TryGetProperty("client_email", out var ce) ? ce.GetString() : null;
            var privateKey = root.TryGetProperty("private_key", out var pk) ? pk.GetString() : null;
            var tokenUri = root.TryGetProperty("token_uri", out var tu) ? tu.GetString() : "https://oauth2.googleapis.com/token";

            if (string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(privateKey))
            {
                _logger.LogWarning("Invalid Google service_account.json structure at {Path}", keyPath);
                return null;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var headerJson = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
            var payloadJson = JsonSerializer.Serialize(new
            {
                iss = clientEmail,
                scope = "https://www.googleapis.com/auth/webmasters.readonly",
                aud = tokenUri,
                iat = now,
                exp = now + 3600
            });

            var unsignedToken = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson))}";
            byte[] signature;
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKey.AsSpan());
                signature = rsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            var jwt = $"{unsignedToken}.{Base64UrlEncode(signature)}";
            using var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwt)
            });

            using var resp = await _http.PostAsync(tokenUri, formContent, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Google OAuth token exchange failed HTTP {Status}: {Body}", resp.StatusCode, err);
                return null;
            }

            var respJson = await resp.Content.ReadAsStringAsync(ct);
            using var tokenDoc = JsonDocument.Parse(respJson);
            if (tokenDoc.RootElement.TryGetProperty("access_token", out var at))
            {
                var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600;
                _cachedAccessToken = at.GetString();
                _tokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 120));
                return _cachedAccessToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed obtaining Google OAuth2 access token from Service Account.");
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string NormalizeDomain(string url)
    {
        var clean = (url ?? string.Empty).Trim().ToLowerInvariant();
        if (clean.StartsWith("sc-domain:")) clean = clean.Substring("sc-domain:".Length);
        clean = clean.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        if (clean.StartsWith("www.")) clean = clean.Substring(4);
        var slashIdx = clean.IndexOf('/');
        return slashIdx >= 0 ? clean.Substring(0, slashIdx) : clean;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(ch, '_');
        }
        return name;
    }

    private sealed class GscApiRow
    {
        public List<string> Keys { get; } = new();
        public long Clicks { get; set; }
        public long Impressions { get; set; }
        public double Ctr { get; set; }
        public double Position { get; set; }
    }
}
