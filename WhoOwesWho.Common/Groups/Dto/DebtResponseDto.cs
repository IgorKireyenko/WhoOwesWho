namespace WhoOwesWho.Common.Groups.Dto;

public sealed record DebtResponseDto(
    Guid FromMemberId,
    string FromMemberName,
    Guid ToMemberId,
    string ToMemberName,
    decimal Amount);
