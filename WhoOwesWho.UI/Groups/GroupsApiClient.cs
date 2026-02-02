using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WhoOwesWho.Common.Groups.Dto;
using WhoOwesWho.UI.Auth;

namespace WhoOwesWho.UI.Groups;

public interface IGroupsApiClient
{
    Task<List<GroupSummaryDto>> GetAllGroupsAsync(CancellationToken cancellationToken);
    Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid groupId, CancellationToken cancellationToken);
}

public sealed class GroupsApiClient(HttpClient httpClient, IOptions<GroupsApiOptions> options, ITokenStore tokenStore) : IGroupsApiClient
{
    public Task<List<GroupSummaryDto>> GetAllGroupsAsync(CancellationToken cancellationToken)
        => GetOrThrowAsync<List<GroupSummaryDto>>("api/groups", cancellationToken);

    public Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid groupId, CancellationToken cancellationToken)
        => GetOrThrowAsync<GroupDetailsResponseDto>($"api/groups/{groupId}", cancellationToken);

    private async Task<TResponse> GetOrThrowAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        await EnsureAuthenticatedAsync(cancellationToken);

        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            return payload ?? throw new InvalidOperationException("The server returned an empty response.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(error))
        {
            error = $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
        }

        throw new InvalidOperationException(error);
    }

    private void EnsureConfigured()
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Groups API base URL is not configured. Set GroupsApi:BaseUrl in appsettings.");
        }

        httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("User is not authenticated. Please log in.");
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
