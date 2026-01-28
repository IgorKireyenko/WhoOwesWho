namespace WhoOwesWho.Common.Auth.Dto;

public sealed record LoginRequestDto(
    string Email,
    string Password);
