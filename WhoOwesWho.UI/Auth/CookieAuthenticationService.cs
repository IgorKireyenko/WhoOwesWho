using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WhoOwesWho.Common.Auth.Dto;

namespace WhoOwesWho.UI.Auth;

public interface ICookieAuthenticationService
{
    Task SignInAsync(LoginResponseDto loginResponse);
    Task SignOutAsync();
}

public sealed class CookieAuthenticationService(
    IHttpContextAccessor httpContextAccessor,
    ITokenStore tokenStore) : ICookieAuthenticationService
{
    public async Task SignInAsync(LoginResponseDto loginResponse)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
            new(ClaimTypes.Email, loginResponse.Email),
            new(ClaimTypes.Name, loginResponse.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = loginResponse.ExpiresAt
            });

        httpContext.User = claimsPrincipal;

        await tokenStore.SetTokenAsync(loginResponse.AccessToken);
    }

    public async Task SignOutAsync()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        await tokenStore.ClearTokenAsync();
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
