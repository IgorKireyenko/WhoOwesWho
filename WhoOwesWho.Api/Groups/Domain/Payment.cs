namespace WhoOwesWho.Api.Groups.Domain;

public sealed class Payment
{
    public required Guid Id { get; init; }
    public required Guid MemberId { get; init; }
    public required string MemberName { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime PaymentDate { get; init; }
    public required string Description { get; init; }
}
