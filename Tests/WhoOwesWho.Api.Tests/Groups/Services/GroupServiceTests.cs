using FluentAssertions;
using Moq;
using WhoOwesWho.Api.Groups.Domain;
using WhoOwesWho.Api.Groups.Repositories;
using WhoOwesWho.Api.Groups.Services;
using WhoOwesWho.Common.Groups.Dto;

namespace WhoOwesWho.Api.Tests.Groups.Services;

public sealed class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _groupRepository = new(MockBehavior.Strict);
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _sut = new GroupService(_groupRepository.Object);
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldThrow_WhenTitleIsEmpty()
    {
        var userId = Guid.NewGuid();
        var request = new CreateGroupRequestDto("   ");

        var act = () => _sut.CreateGroupAsync(userId, request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Group title cannot be empty.");
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldCreateGroup_WithAuthenticatedMember_AndPersist()
    {
        var userId = Guid.NewGuid();
        var request = new CreateGroupRequestDto("  Trip  ");

        Group? addedGroup = null;
        _groupRepository
            .Setup(r => r.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
            .Callback<Group, CancellationToken>((g, _) => addedGroup = g)
            .ReturnsAsync((Group g, CancellationToken _) => g);

        var result = await _sut.CreateGroupAsync(userId, request, CancellationToken.None);

        result.Title.Should().Be("Trip");
        result.CreatorUserId.Should().Be(userId);
        result.GroupId.Should().NotBeEmpty();

        addedGroup.Should().NotBeNull();
        addedGroup!.Title.Should().Be("Trip");
        addedGroup.CreatorUserId.Should().Be(userId);
        addedGroup.Members.Should().HaveCount(1);
        addedGroup.Members[0].Name.Should().Be("You");
        addedGroup.Payments.Should().BeEmpty();

        _groupRepository.Verify(r => r.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllGroupsAsync_ShouldReturnSummaries()
    {
        var userId = Guid.NewGuid();
        var groups = new List<Group>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CreatorUserId = userId,
                Title = "A",
                Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
                Payments = []
            },
            new()
            {
                Id = Guid.NewGuid(),
                CreatorUserId = userId,
                Title = "B",
                Members =
                [
                    new Member { Id = Guid.NewGuid(), Name = "You" },
                    new Member { Id = Guid.NewGuid(), Name = "Alice" }
                ],
                Payments =
                [
                    new Payment
                    {
                        Id = Guid.NewGuid(),
                        MemberId = Guid.NewGuid(),
                        MemberName = "You",
                        Amount = 10,
                        PaymentDate = DateTime.UtcNow,
                        Description = "x"
                    }
                ]
            }
        };

        _groupRepository
            .Setup(r => r.GetAllByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        var result = await _sut.GetAllGroupsAsync(userId, CancellationToken.None);

        result.Should().BeEquivalentTo(
            new List<GroupSummaryDto>
            {
                new(groups[0].Id, "A", 1, 0),
                new(groups[1].Id, "B", 2, 1)
            },
            o => o.WithoutStrictOrdering());

        _groupRepository.Verify(r => r.GetAllByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDetailsAsync_ShouldThrow_WhenGroupNotFound()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Group?)null);

        var act = () => _sut.GetGroupDetailsAsync(userId, groupId, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Group not found.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDetailsAsync_ShouldThrow_WhenUserIsNotCreator()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = groupId,
                CreatorUserId = Guid.NewGuid(),
                Title = "t",
                Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
                Payments = []
            });

        var act = () => _sut.GetGroupDetailsAsync(userId, groupId, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to access this group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDetailsAsync_ShouldReturnDetails_WithMembersAndPayments()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };
        var payId = Guid.NewGuid();
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice],
            Payments =
            [
                new Payment
                {
                    Id = payId,
                    MemberId = alice.Id,
                    MemberName = alice.Name,
                    Amount = 12.34m,
                    PaymentDate = dt,
                    Description = "Lunch"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var result = await _sut.GetGroupDetailsAsync(userId, groupId, CancellationToken.None);

        result.Should().BeEquivalentTo(new GroupDetailsResponseDto(
            groupId,
            "t",
            userId,
            new List<MemberDto> { new(you.Id, "You"), new(alice.Id, "Alice") },
            new List<PaymentDto> { new(payId, alice.Id, "Alice", 12.34m, dt, "Lunch") }));

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldThrow_WhenGroupHasPayments()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = Guid.NewGuid(),
                    MemberName = "You",
                    Amount = 1,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.DeleteGroupAsync(userId, groupId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete a group that contains payments.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldThrow_WhenRepositoryDeleteFails()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _groupRepository
            .Setup(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.DeleteGroupAsync(userId, groupId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to delete the group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldDelete_WhenNoPayments()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _groupRepository
            .Setup(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.DeleteGroupAsync(userId, groupId, CancellationToken.None);

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = groupId,
                CreatorUserId = userId,
                Title = "t",
                Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
                Payments = []
            });

        var act = () => _sut.AddMemberAsync(userId, groupId, new AddMemberRequestDto("  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Member name cannot be empty.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenNameIsReserved()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = groupId,
                CreatorUserId = userId,
                Title = "t",
                Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
                Payments = []
            });

        var act = () => _sut.AddMemberAsync(userId, groupId, new AddMemberRequestDto("you"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Member name 'You' is reserved and cannot be used.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenDuplicateNameExists_IgnoringCase()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = groupId,
                CreatorUserId = userId,
                Title = "t",
                Members =
                [
                    new Member { Id = Guid.NewGuid(), Name = "You" },
                    new Member { Id = Guid.NewGuid(), Name = "Alice" }
                ],
                Payments = []
            });

        var act = () => _sut.AddMemberAsync(userId, groupId, new AddMemberRequestDto("  aLiCe  "), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A member with this name already exists in the group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddMember_AndPersist()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepository
            .Setup(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.AddMemberAsync(userId, groupId, new AddMemberRequestDto("  Alice  "), CancellationToken.None);

        result.Name.Should().Be("Alice");
        result.MemberId.Should().NotBeEmpty();

        group.Members.Should().ContainSingle(m => m.Id == result.MemberId && m.Name == "Alice");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrow_WhenMemberNotFound()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.RemoveMemberAsync(userId, groupId, memberId, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Member not found in the group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrow_WhenMemberIsAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.RemoveMemberAsync(userId, groupId, you.Id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The authenticated user cannot be removed from the group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrow_WhenMemberHasPayments()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = alice.Id,
                    MemberName = "Alice",
                    Amount = 1,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.RemoveMemberAsync(userId, groupId, alice.Id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove a member that has payments associated with them.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldRemoveMember_AndPersist()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _groupRepository
            .Setup(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RemoveMemberAsync(userId, groupId, alice.Id, CancellationToken.None);

        group.Members.Should().NotContain(m => m.Id == alice.Id);

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldThrow_WhenAmountIsNotPositive()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.AddPaymentAsync(userId, groupId, new AddPaymentRequestDto(you.Id, 0m, DateTime.UtcNow, "d"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Payment amount must be greater than zero.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldThrow_WhenDescriptionIsEmpty()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.AddPaymentAsync(userId, groupId, new AddPaymentRequestDto(you.Id, 1m, DateTime.UtcNow, "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Payment description cannot be empty.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldThrow_WhenMemberDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.AddPaymentAsync(userId, groupId, new AddPaymentRequestDto(Guid.NewGuid(), 1m, DateTime.UtcNow, "d"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Payment must reference an existing group member.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldAddPayment_AndPersist()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _groupRepository
            .Setup(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _sut.AddPaymentAsync(userId, groupId, new AddPaymentRequestDto(you.Id, 12.345m, dt, "  Lunch "), CancellationToken.None);

        result.PaymentId.Should().NotBeEmpty();
        result.MemberId.Should().Be(you.Id);
        result.MemberName.Should().Be("You");
        result.Amount.Should().Be(12.345m);
        result.PaymentDate.Should().Be(dt);
        result.Description.Should().Be("Lunch");

        group.Payments.Should().ContainSingle(p => p.Id == result.PaymentId);

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemovePaymentAsync_ShouldThrow_WhenPaymentNotFound()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var act = () => _sut.RemovePaymentAsync(userId, groupId, paymentId, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Payment not found in the group.");

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemovePaymentAsync_ShouldRemovePayment_AndPersist()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var paymentId = Guid.NewGuid();
        var you = new Member { Id = Guid.NewGuid(), Name = "You" };

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you],
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    MemberId = you.Id,
                    MemberName = "You",
                    Amount = 1,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _groupRepository
            .Setup(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RemovePaymentAsync(userId, groupId, paymentId, CancellationToken.None);

        group.Payments.Should().BeEmpty();

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.Verify(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDebtsAsync_ShouldReturnEmpty_WhenNoPayments()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [new Member { Id = Guid.NewGuid(), Name = "You" }],
            Payments = []
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var result = await _sut.GetGroupDebtsAsync(userId, groupId, CancellationToken.None);

        result.Should().BeEmpty();

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDebtsAsync_ShouldReturnDebts_Minimized_AndRounded_OrderIndependent()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };
        var bob = new Member { Id = Guid.NewGuid(), Name = "Bob" };

        // Total = 100. Equal share = 33.33 (rounded AwayFromZero)
        // Paid: You=100, Alice=0, Bob=0
        // Net: You=+66.67, Alice=-33.33, Bob=-33.33
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice, bob],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = you.Id,
                    MemberName = "You",
                    Amount = 100m,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var result = await _sut.GetGroupDebtsAsync(userId, groupId, CancellationToken.None);

        result.Should().BeEquivalentTo(
            new[]
            {
                new DebtResponseDto(alice.Id, "Alice", you.Id, "You", 33.33m),
                new DebtResponseDto(bob.Id, "Bob", you.Id, "You", 33.33m)
            },
            o => o.WithoutStrictOrdering());

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDebtsAsync_ShouldIgnorePayments_WhenMemberIdIsUnknown()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };

        // Payment references non-existing member; algorithm should ignore it (totalPaid only accumulates known ids)
        // But totalGroupPayments still includes it, so everyone ends up owing an equal share with no creditors.
        // That should result in no debts because there are no positive balances.
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = Guid.NewGuid(),
                    MemberName = "Ghost",
                    Amount = 10m,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var result = await _sut.GetGroupDebtsAsync(userId, groupId, CancellationToken.None);

        result.Should().BeEmpty();

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupDebtsAsync_ShouldApplyThreshold_AndSkipTinyDebts()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var you = new Member { Id = Guid.NewGuid(), Name = "You" };
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };

        // Total=0.02 -> equal share=0.01
        // Paid: You=0.02 => net=+0.01, Alice=0 => net=-0.01
        // debtAmount=0.01 -> not added because condition is > 0.01
        var group = new Group
        {
            Id = groupId,
            CreatorUserId = userId,
            Title = "t",
            Members = [you, alice],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = you.Id,
                    MemberName = "You",
                    Amount = 0.02m,
                    PaymentDate = DateTime.UtcNow,
                    Description = "d"
                }
            ]
        };

        _groupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var result = await _sut.GetGroupDebtsAsync(userId, groupId, CancellationToken.None);

        result.Should().BeEmpty();

        _groupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepository.VerifyNoOtherCalls();
    }
}
