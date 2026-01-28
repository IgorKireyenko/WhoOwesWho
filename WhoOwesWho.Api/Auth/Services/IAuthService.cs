using WhoOwesWho.Common.Auth.Dto;

namespace WhoOwesWho.Api.Auth.Services;

public interface IAuthService
{
    Task<SignUpResponseDto> SignUpAsync(SignUpRequestDto request, CancellationToken cancellationToken);

    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
