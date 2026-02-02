using WhoOwesWho.Api.Groups.Domain;

namespace WhoOwesWho.Api.Groups.Repositories;

public interface IGroupRepository
{
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken);
    Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<List<Group>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid groupId, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Group group, CancellationToken cancellationToken);
}
