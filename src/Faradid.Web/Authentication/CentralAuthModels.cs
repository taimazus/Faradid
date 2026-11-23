using System;

namespace Faradid.Web.Authentication;

public sealed class CentralAuthConfig
{
    public bool Enabled { get; set; } = true;
    public string Authority { get; set; } = "https://auth.irres.ir";
    public string ClientId { get; set; } = "کنسول گزارش سئو فرادید";
    public string ClientSecret { get; set; } = "sec_QtplhEUPwAv4b7INNUapn5UozXKazNt0fkSvbCaC944";
    public string RedirectUri { get; set; } = "https://faradid.irres.ir/";
    public string CallbackPath { get; set; } = "/sso/callback";
    public string DisplayName { get; set; } = "سامانه هویت متمرکز سهند (Irres Gate)";
    public bool AllowLocalBypass { get; set; } = false;
    public string AuthorizePath { get; set; } = "/sso/login";
    public string TokenPath { get; set; } = "/connect/token";
    public string UserInfoPath { get; set; } = "/connect/userinfo";
}

public sealed record CentralAuthUserProfile(
    string Id,
    string Username,
    string Email,
    string FullName,
    string PhoneNumber,
    string[] Roles);

public sealed record CentralAuthTokenBundle(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresInSeconds,
    CentralAuthUserProfile User);

public sealed class CentralAuthResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private CentralAuthResult(bool isSuccess, T? value, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static CentralAuthResult<T> Success(T value) => new(true, value, null, null);
    public static CentralAuthResult<T> Failure(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}
