namespace WhoOwesWho.Api.Auth.Domain;

public sealed class AppUser
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Email { get; init; }

    public required string PasswordHash { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
