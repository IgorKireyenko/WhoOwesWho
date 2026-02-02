namespace WhoOwesWho.Common.Groups.Dto;

public sealed record AddPaymentRequestDto(
    Guid MemberId,
    decimal Amount,
    DateTime PaymentDate,
    string Description);
