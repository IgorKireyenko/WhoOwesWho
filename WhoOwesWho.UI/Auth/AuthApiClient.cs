using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WhoOwesWho.Common.Auth.Dto;

namespace WhoOwesWho.UI.Auth;

public interface IAuthApiClient
{
    Task SignUpAsync(SignUpRequestDto request, CancellationToken cancellationToken);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}

public sealed class AuthApiClient(HttpClient httpClient, IOptions<AuthApiOptions> options) : IAuthApiClient
{
    public Task SignUpAsync(SignUpRequestDto request, CancellationToken cancellationToken)
        => PostOrThrowAsync<SignUpRequestDto, SignUpResponseDto>("api/auth/signup", request, cancellationToken);

    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
        => PostOrThrowAsync<LoginRequestDto, LoginResponseDto>("api/auth/login", request, cancellationToken);

    private async Task<TResponse> PostOrThrowAsync<TRequest, TResponse>(string relativeUrl, TRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var response = await httpClient.PostAsJsonAsync(relativeUrl, request, cancellationToken);

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
            throw new InvalidOperationException("Auth API base URL is not configured. Set AuthApi:BaseUrl in appsettings.");
        }

        httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }
}
