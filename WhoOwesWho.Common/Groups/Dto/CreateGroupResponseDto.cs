namespace WhoOwesWho.Common.Groups.Dto;

public sealed record CreateGroupResponseDto(
    Guid GroupId,
    string Title,
    Guid CreatorUserId);
