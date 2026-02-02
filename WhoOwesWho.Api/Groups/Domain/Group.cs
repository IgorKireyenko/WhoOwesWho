namespace WhoOwesWho.Api.Groups.Domain;

public sealed class Group
{
    public required Guid Id { get; init; }
    public required Guid CreatorUserId { get; init; }
    public required string Title { get; init; }
    public required List<Member> Members { get; init; }
    public required List<Payment> Payments { get; init; }
}
