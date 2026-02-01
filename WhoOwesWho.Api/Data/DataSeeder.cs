using WhoOwesWho.Api.Auth.Domain;
using WhoOwesWho.Api.Auth.Repositories;
using WhoOwesWho.Api.Auth.Services;

namespace WhoOwesWho.Api.Data;

public sealed class DataSeeder(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
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
    }
}
