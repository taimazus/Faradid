using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Faradid.Web.Authentication;

public interface ICentralAuthClient
{
    CentralAuthConfig GetConfig();
    string BuildAuthorizeUrl(string state, string? returnUrl = null);
    Task<CentralAuthResult<CentralAuthTokenBundle>> ExchangeSsoCodeAsync(string code, string? redirectUri = null, CancellationToken ct = default);
}

public sealed class CentralAuthClient : ICentralAuthClient
{
    private readonly HttpClient _http;
    private readonly CentralAuthConfig _config;
    private readonly ILogger<CentralAuthClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CentralAuthClient(
        HttpClient? http = null,
        CentralAuthConfig? config = null,
        ILogger<CentralAuthClient>? logger = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _config = config ?? new CentralAuthConfig();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CentralAuthClient>.Instance;
    }

    public CentralAuthConfig GetConfig() => _config;

    public string BuildAuthorizeUrl(string state, string? returnUrl = null)
    {
        var baseAuth = _config.Authority.TrimEnd('/');
        var redirectUri = Uri.EscapeDataString(_config.RedirectUri);
        var clientId = Uri.EscapeDataString(_config.ClientId);
        var encodedState = Uri.EscapeDataString(state);

        var url = $"{baseAuth}/sso/login?client_id={clientId}&redirect_uri={redirectUri}&state={encodedState}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            url += $"&return_url={Uri.EscapeDataString(returnUrl)}";
        }
        return url;
    }

    public async Task<CentralAuthResult<CentralAuthTokenBundle>> ExchangeSsoCodeAsync(string code, string? redirectUri = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return CentralAuthResult<CentralAuthTokenBundle>.Failure("EMPTY_CODE", "کد احراز هویت متمرکز ارسال نشده است.");
        }

        try
        {
            var baseAuth = _config.Authority.TrimEnd('/');
            var requestBody = new
            {
                code = code.Trim(),
                clientId = _config.ClientId,
                clientSecret = _config.ClientSecret,
                redirectUri = redirectUri ?? _config.RedirectUri
            };

            var response = await _http.PostAsJsonAsync($"{baseAuth}/api/v1/auth/sso/exchange", requestBody, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Central auth exchange failed: StatusCode={StatusCode}", response.StatusCode);
                return CentralAuthResult<CentralAuthTokenBundle>.Failure("SSO_EXCHANGE_FAILED", $"خطا در تبادل کد SSO (کد وضعیت: {(int)response.StatusCode})");
            }

            var tokenBundle = JsonSerializer.Deserialize<CentralAuthTokenBundle>(content, JsonOptions);
            if (tokenBundle == null)
            {
                return CentralAuthResult<CentralAuthTokenBundle>.Failure("INVALID_PAYLOAD", "پاسخ سرویس احراز هویت متمرکز قابل پردازش نیست.");
            }

            return CentralAuthResult<CentralAuthTokenBundle>.Success(tokenBundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while exchanging SSO code with Central Auth.");
            return CentralAuthResult<CentralAuthTokenBundle>.Failure("NETWORK_ERROR", $"خطای ارتباط با سرور هویت مرکزی: {ex.Message}");
        }
    }
}
