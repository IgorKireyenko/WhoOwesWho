namespace WhoOwesWho.Common.Auth.Dto;

public sealed record SignUpResponseDto(
    Guid UserId,
    string Email);
