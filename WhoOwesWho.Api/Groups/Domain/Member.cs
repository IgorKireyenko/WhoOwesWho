namespace WhoOwesWho.Api.Groups.Domain;

public sealed class Member
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
