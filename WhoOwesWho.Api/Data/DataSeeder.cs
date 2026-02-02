using WhoOwesWho.Api.Auth.Domain;
using WhoOwesWho.Api.Auth.Repositories;
using WhoOwesWho.Api.Auth.Services;
using WhoOwesWho.Api.Groups.Domain;
using WhoOwesWho.Api.Groups.Repositories;

namespace WhoOwesWho.Api.Data;

public sealed class DataSeeder(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IGroupRepository groupRepository,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        const string testEmail = "user@example.com";
        const string testPassword = "password123";

        var exists = await userRepository.EmailExistsAsync(testEmail, cancellationToken);
        if (exists)
        {
            logger.LogInformation("Test user '{Email}' already exists. Skipping seed.", testEmail);
            return;
        }

        var testUser = new AppUser
        {
            Email = testEmail,
            PasswordHash = passwordHasher.Hash(testPassword)
        };

        await userRepository.AddAsync(testUser, cancellationToken);

        logger.LogInformation("Test user '{Email}' created successfully with password '{Password}'.", testEmail, testPassword);

        await SeedGroupsForUserAsync(testUser.Id, cancellationToken);
    }

    private async Task SeedGroupsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var youMemberId = Guid.NewGuid();
        var alice = new Member { Id = Guid.NewGuid(), Name = "Alice" };
        var bob = new Member { Id = Guid.NewGuid(), Name = "Bob" };
        var charlie = new Member { Id = Guid.NewGuid(), Name = "Charlie" };

        var group1 = new Group
        {
            Id = Guid.NewGuid(),
            CreatorUserId = userId,
            Title = "Weekend Trip",
            Members =
            [
                new Member { Id = youMemberId, Name = "You" },
                alice,
                bob
            ],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = youMemberId,
                    MemberName = "You",
                    Amount = 150.00m,
                    PaymentDate = DateTime.UtcNow.AddDays(-2),
                    Description = "Hotel booking"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = alice.Id,
                    MemberName = alice.Name,
                    Amount = 75.50m,
                    PaymentDate = DateTime.UtcNow.AddDays(-1),
                    Description = "Groceries"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = bob.Id,
                    MemberName = bob.Name,
                    Amount = 45.00m,
                    PaymentDate = DateTime.UtcNow,
                    Description = "Gas"
                }
            ]
        };

        var group2 = new Group
        {
            Id = Guid.NewGuid(),
            CreatorUserId = userId,
            Title = "Office Lunch",
            Members =
            [
                new Member { Id = youMemberId, Name = "You" },
                charlie,
                alice
            ],
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = charlie.Id,
                    MemberName = charlie.Name,
                    Amount = 32.50m,
                    PaymentDate = DateTime.UtcNow.AddDays(-3),
                    Description = "Pizza delivery"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = youMemberId,
                    MemberName = "You",
                    Amount = 28.75m,
                    PaymentDate = DateTime.UtcNow.AddDays(-1),
                    Description = "Coffee and snacks"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = alice.Id,
                    MemberName = alice.Name,
                    Amount = 41.00m,
                    PaymentDate = DateTime.UtcNow,
                    Description = "Sushi takeout"
                }
            ]
        };

        await groupRepository.AddAsync(group1, cancellationToken);
        await groupRepository.AddAsync(group2, cancellationToken);

        logger.LogInformation("Created {Count} test groups for user {UserId}.", 2, userId);
    }
}
