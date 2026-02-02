namespace WhoOwesWho.Common.Groups.Dto;

public sealed record AddMemberResponseDto(
    Guid MemberId,
    string Name);
