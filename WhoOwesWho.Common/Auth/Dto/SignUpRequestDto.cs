namespace WhoOwesWho.Common.Auth.Dto;

public sealed record SignUpRequestDto(
    string Email,
    string Password);
