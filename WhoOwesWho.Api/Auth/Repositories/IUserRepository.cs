using WhoOwesWho.Api.Auth.Domain;

namespace WhoOwesWho.Api.Auth.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(AppUser user, CancellationToken cancellationToken);
}
