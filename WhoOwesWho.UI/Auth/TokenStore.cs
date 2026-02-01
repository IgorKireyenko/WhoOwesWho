using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Authorization;

namespace WhoOwesWho.UI.Auth;

public sealed class TokenStore(AuthenticationStateProvider authenticationStateProvider) : ITokenStore
{
    private static readonly ConcurrentDictionary<string, string> Tokens = new();

    public async Task<string?> GetTokenAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId is null)
        {
            return null;
        }

        Tokens.TryGetValue(userId, out var token);
        return token;
    }

    public async Task SetTokenAsync(string token)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId is not null)
        {
            Tokens[userId] = token;
        }
    }

    public async Task ClearTokenAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId is not null)
        {
            Tokens.TryRemove(userId, out _);
        }
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}
