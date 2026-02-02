namespace WhoOwesWho.Common.Groups.Dto;

public sealed record AddPaymentResponseDto(
    Guid PaymentId,
    Guid MemberId,
    string MemberName,
    decimal Amount,
    DateTime PaymentDate,
    string Description);
