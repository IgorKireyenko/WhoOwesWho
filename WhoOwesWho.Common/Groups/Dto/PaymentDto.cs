namespace WhoOwesWho.Common.Groups.Dto;

public sealed record PaymentDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    decimal Amount,
    DateTime PaymentDate,
    string Description);
