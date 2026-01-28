using WhoOwesWho.Api.Auth.Domain;
using WhoOwesWho.Api.Auth.Repositories;
using WhoOwesWho.Common.Auth.Dto;

namespace WhoOwesWho.Api.Auth.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<SignUpResponseDto> SignUpAsync(SignUpRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var exists = await userRepository.EmailExistsAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new AppUser
        {
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        await userRepository.AddAsync(user, cancellationToken);

        return new SignUpResponseDto(user.Id, user.Email);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var (token, expiresAt) = tokenService.CreateAccessToken(user);

        return new LoginResponseDto(
            AccessToken: token,
            ExpiresAt: expiresAt,
            UserId: user.Id,
            Email: user.Email);
    }
}
