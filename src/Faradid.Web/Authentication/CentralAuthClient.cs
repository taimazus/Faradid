using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
        var authPath = _config.AuthorizePath.StartsWith('/') ? _config.AuthorizePath : "/" + _config.AuthorizePath;
        var redirectUri = Uri.EscapeDataString(_config.RedirectUri);
        var clientId = Uri.EscapeDataString(_config.ClientId);
        var encodedState = Uri.EscapeDataString(state);
        var nonce = Guid.NewGuid().ToString("N");
        var scope = Uri.EscapeDataString("openid profile phone email");

        var url = $"{baseAuth}{authPath}?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&state={encodedState}&nonce={nonce}";
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
            var tokenPath = _config.TokenPath.StartsWith('/') ? _config.TokenPath : "/" + _config.TokenPath;
            var tokenUrl = $"{baseAuth}{tokenPath}";

            var actualRedirect = redirectUri ?? _config.RedirectUri;

            var formParams = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code.Trim(),
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["redirect_uri"] = actualRedirect
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(formParams)
            };

            var tokenResponse = await _http.SendAsync(tokenRequest, ct);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(ct);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Central auth token exchange failed: StatusCode={StatusCode}, Content={Content}", tokenResponse.StatusCode, tokenContent);
                return CentralAuthResult<CentralAuthTokenBundle>.Failure("SSO_EXCHANGE_FAILED", $"خطا در تبادل توکن SSO (کد وضعیت: {(int)tokenResponse.StatusCode})");
            }

            using var tokenDoc = JsonDocument.Parse(tokenContent);
            var root = tokenDoc.RootElement;
            var accessToken = root.TryGetProperty("access_token", out var atProp) ? atProp.GetString() : null;
            var refreshToken = root.TryGetProperty("refresh_token", out var rtProp) ? rtProp.GetString() : string.Empty;
            var tokenType = root.TryGetProperty("token_type", out var ttProp) ? ttProp.GetString() : "Bearer";
            var expiresIn = root.TryGetProperty("expires_in", out var expProp) && expProp.TryGetInt32(out var exp) ? exp : 3600;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return CentralAuthResult<CentralAuthTokenBundle>.Failure("NO_ACCESS_TOKEN", "توکن دسترسی معتبر از سرور هویت دریافت نشد.");
            }

            // Fetch UserInfo
            var userInfoPath = _config.UserInfoPath.StartsWith('/') ? _config.UserInfoPath : "/" + _config.UserInfoPath;
            var userInfoUrl = $"{baseAuth}{userInfoPath}";

            var userRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await _http.SendAsync(userRequest, ct);
            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Central auth userinfo failed: StatusCode={StatusCode}", userResponse.StatusCode);
                return CentralAuthResult<CentralAuthTokenBundle>.Failure("USERINFO_FAILED", "خطا در دریافت مشخصات کاربر از سرور هویت.");
            }

            var userContent = await userResponse.Content.ReadAsStringAsync(ct);
            using var userDoc = JsonDocument.Parse(userContent);
            var uRoot = userDoc.RootElement;

            string GetString(params string[] names)
            {
                foreach (var name in names)
                {
                    if (uRoot.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val;
                    }
                }
                return string.Empty;
            }

            var userId = GetString("sub", "id", "userId", "user_id");
            if (string.IsNullOrWhiteSpace(userId)) userId = Guid.NewGuid().ToString("N");

            var username = GetString("username", "preferred_username", "name", "phone_number", "mobile");
            var email = GetString("email", "mail");
            var firstName = GetString("given_name", "first_name", "firstName");
            var lastName = GetString("family_name", "last_name", "lastName");
            var fullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = GetString("name", "fullName", "display_name");
            if (string.IsNullOrWhiteSpace(fullName)) fullName = username;

            var phone = GetString("phone_number", "phone", "mobile", "cellPhoneNumber");

            // Extract roles
            var rolesList = new List<string>();
            if (uRoot.TryGetProperty("role", out var rProp))
            {
                if (rProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in rProp.EnumerateArray())
                    {
                        var r = elem.GetString();
                        if (!string.IsNullOrWhiteSpace(r)) rolesList.Add(r);
                    }
                }
                else if (rProp.ValueKind == JsonValueKind.String)
                {
                    var r = rProp.GetString();
                    if (!string.IsNullOrWhiteSpace(r)) rolesList.Add(r);
                }
            }
            if (uRoot.TryGetProperty("roles", out var rolesProp) && rolesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var elem in rolesProp.EnumerateArray())
                {
                    var r = elem.GetString();
                    if (!string.IsNullOrWhiteSpace(r) && !rolesList.Contains(r)) rolesList.Add(r);
                }
            }

            // Default roles if empty
            if (rolesList.Count == 0)
            {
                if (email.Equals("admin@irres.ir", StringComparison.OrdinalIgnoreCase) || username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    rolesList.Add("Admin");
                }
                else
                {
                    rolesList.Add("Client");
                }
            }

            var profile = new CentralAuthUserProfile(
                Id: userId,
                Username: string.IsNullOrWhiteSpace(username) ? userId : username,
                Email: email,
                FullName: fullName,
                PhoneNumber: phone,
                Roles: rolesList.ToArray()
            );

            var tokenBundle = new CentralAuthTokenBundle(
                AccessToken: accessToken,
                RefreshToken: refreshToken ?? string.Empty,
                TokenType: tokenType ?? "Bearer",
                ExpiresInSeconds: expiresIn,
                User: profile
            );

            return CentralAuthResult<CentralAuthTokenBundle>.Success(tokenBundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while exchanging SSO code with Central Auth.");
            return CentralAuthResult<CentralAuthTokenBundle>.Failure("NETWORK_ERROR", $"خطای ارتباط با سرور هویت مرکزی: {ex.Message}");
        }
    }
}
