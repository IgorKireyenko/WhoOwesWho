namespace WhoOwesWho.Common.Groups.Dto;

public sealed record GroupSummaryDto(
    Guid Id,
    string Title,
    int MemberCount,
    int PaymentCount);
