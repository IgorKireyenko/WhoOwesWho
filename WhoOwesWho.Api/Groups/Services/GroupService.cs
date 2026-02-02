using WhoOwesWho.Api.Groups.Domain;
using WhoOwesWho.Api.Groups.Repositories;
using WhoOwesWho.Common.Groups.Dto;

namespace WhoOwesWho.Api.Groups.Services;

public sealed class GroupService(IGroupRepository groupRepository) : IGroupService
{
    private const string ReservedMemberName = "You";

    public async Task<CreateGroupResponseDto> CreateGroupAsync(Guid userId, CreateGroupRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Group title cannot be empty.");
        }

        var authenticatedMember = new Member
        {
            Id = Guid.NewGuid(),
            Name = ReservedMemberName
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            CreatorUserId = userId,
            Title = request.Title.Trim(),
            Members = [authenticatedMember],
            Payments = []
        };

        await groupRepository.AddAsync(group, cancellationToken);

        return new CreateGroupResponseDto(
            group.Id,
            group.Title,
            group.CreatorUserId);
    }

    public async Task<List<GroupSummaryDto>> GetAllGroupsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var groups = await groupRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return groups.Select(g => new GroupSummaryDto(
            g.Id,
            g.Title,
            g.Members.Count,
            g.Payments.Count)).ToList();
    }

    public async Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        var memberDtos = group.Members.Select(m => new MemberDto(m.Id, m.Name)).ToList();
        var paymentDtos = group.Payments.Select(p => new PaymentDto(
            p.Id,
            p.MemberId,
            p.MemberName,
            p.Amount,
            p.PaymentDate,
            p.Description)).ToList();

        return new GroupDetailsResponseDto(
            group.Id,
            group.Title,
            group.CreatorUserId,
            memberDtos,
            paymentDtos);
    }

    public async Task DeleteGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        if (group.Payments.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a group that contains payments.");
        }

        var deleted = await groupRepository.DeleteAsync(groupId, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Failed to delete the group.");
        }
    }

    public async Task<AddMemberResponseDto> AddMemberAsync(Guid userId, Guid groupId, AddMemberRequestDto request, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Member name cannot be empty.");
        }

        var memberName = request.Name.Trim();

        if (memberName.Equals(ReservedMemberName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Member name '{ReservedMemberName}' is reserved and cannot be used.");
        }

        if (group.Members.Any(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A member with this name already exists in the group.");
        }

        var newMember = new Member
        {
            Id = Guid.NewGuid(),
            Name = memberName
        };

        group.Members.Add(newMember);
        await groupRepository.UpdateAsync(group, cancellationToken);

        return new AddMemberResponseDto(newMember.Id, newMember.Name);
    }

    public async Task RemoveMemberAsync(Guid userId, Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        var member = group.Members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new KeyNotFoundException("Member not found in the group.");

        if (member.Name.Equals(ReservedMemberName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The authenticated user cannot be removed from the group.");
        }

        if (group.Payments.Any(p => p.MemberId == memberId))
        {
            throw new InvalidOperationException("Cannot remove a member that has payments associated with them.");
        }

        group.Members.Remove(member);
        await groupRepository.UpdateAsync(group, cancellationToken);
    }

    public async Task<AddPaymentResponseDto> AddPaymentAsync(Guid userId, Guid groupId, AddPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Payment description cannot be empty.");
        }

        var member = group.Members.FirstOrDefault(m => m.Id == request.MemberId)
            ?? throw new ArgumentException("Payment must reference an existing group member.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            MemberName = member.Name,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Description = request.Description.Trim()
        };

        group.Payments.Add(payment);
        await groupRepository.UpdateAsync(group, cancellationToken);

        return new AddPaymentResponseDto(
            payment.Id,
            payment.MemberId,
            payment.MemberName,
            payment.Amount,
            payment.PaymentDate,
            payment.Description);
    }

    public async Task RemovePaymentAsync(Guid userId, Guid groupId, Guid paymentId, CancellationToken cancellationToken)
    {
        var group = await GetGroupAndValidateOwnershipAsync(userId, groupId, cancellationToken);

        var payment = group.Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new KeyNotFoundException("Payment not found in the group.");

        group.Payments.Remove(payment);
        await groupRepository.UpdateAsync(group, cancellationToken);
    }

    private async Task<Group> GetGroupAndValidateOwnershipAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new KeyNotFoundException("Group not found.");

        if (group.CreatorUserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this group.");
        }

        return group;
    }
}
