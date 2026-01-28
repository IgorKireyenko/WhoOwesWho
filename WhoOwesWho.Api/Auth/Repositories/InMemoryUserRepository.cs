using System.Collections.Concurrent;
using WhoOwesWho.Api.Auth.Domain;

namespace WhoOwesWho.Api.Auth.Repositories;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, AppUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_usersByEmail.ContainsKey(email));
    }

    public Task AddAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_usersByEmail.TryAdd(user.Email, user))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        return Task.CompletedTask;
    }
}
