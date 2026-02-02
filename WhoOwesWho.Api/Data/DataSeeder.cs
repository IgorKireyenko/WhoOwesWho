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
        var group1 = new Group
        {
            Id = Guid.NewGuid(),
            CreatorUserId = userId,
            Title = "Weekend Trip",
            Members = [],
            Payments = []
        };

        var group2 = new Group
        {
            Id = Guid.NewGuid(),
            CreatorUserId = userId,
            Title = "Office Lunch",
            Members = [],
            Payments = []
        };

        await groupRepository.AddAsync(group1, cancellationToken);
        await groupRepository.AddAsync(group2, cancellationToken);

        logger.LogInformation("Created {Count} test groups for user {UserId}.", 2, userId);
    }
}
