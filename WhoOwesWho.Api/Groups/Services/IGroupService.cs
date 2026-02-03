using WhoOwesWho.Common.Groups.Dto;

namespace WhoOwesWho.Api.Groups.Services;

public interface IGroupService
{
    Task<CreateGroupResponseDto> CreateGroupAsync(Guid userId, CreateGroupRequestDto request, CancellationToken cancellationToken);
    Task<List<GroupSummaryDto>> GetAllGroupsAsync(Guid userId, CancellationToken cancellationToken);
    Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid userId, Guid groupId, CancellationToken cancellationToken);
    Task DeleteGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken);
    Task<AddMemberResponseDto> AddMemberAsync(Guid userId, Guid groupId, AddMemberRequestDto request, CancellationToken cancellationToken);
    Task RemoveMemberAsync(Guid userId, Guid groupId, Guid memberId, CancellationToken cancellationToken);
    Task<AddPaymentResponseDto> AddPaymentAsync(Guid userId, Guid groupId, AddPaymentRequestDto request, CancellationToken cancellationToken);
    Task RemovePaymentAsync(Guid userId, Guid groupId, Guid paymentId, CancellationToken cancellationToken);
    Task<List<DebtResponseDto>> GetGroupDebtsAsync(Guid userId, Guid groupId, CancellationToken cancellationToken);
}
