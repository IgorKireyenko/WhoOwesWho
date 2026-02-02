using System.Collections.Concurrent;
using WhoOwesWho.Api.Groups.Domain;

namespace WhoOwesWho.Api.Groups.Repositories;

public sealed class InMemoryGroupRepository : IGroupRepository
{
    private readonly ConcurrentDictionary<Guid, Group> _groups = new();

    public Task<Group> AddAsync(Group group, CancellationToken cancellationToken)
    {
        if (!_groups.TryAdd(group.Id, group))
        {
            throw new InvalidOperationException("Group with this ID already exists.");
        }

        return Task.FromResult(group);
    }

    public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        _groups.TryGetValue(groupId, out var group);
        return Task.FromResult(group);
    }

    public Task<List<Group>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userGroups = _groups.Values
            .Where(g => g.CreatorUserId == userId)
            .ToList();

        return Task.FromResult(userGroups);
    }

    public Task<bool> DeleteAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_groups.TryRemove(groupId, out _));
    }

    public Task<bool> UpdateAsync(Group group, CancellationToken cancellationToken)
    {
        if (!_groups.ContainsKey(group.Id))
        {
            return Task.FromResult(false);
        }

        _groups[group.Id] = group;
        return Task.FromResult(true);
    }
}
