namespace WhoOwesWho.Common.Auth.Dto;

public sealed record LoginResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email);
