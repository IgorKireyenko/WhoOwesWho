using System.Security.Claims;
using WhoOwesWho.Api.Auth.Domain;

namespace WhoOwesWho.Api.Auth.Services;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user, IEnumerable<Claim>? additionalClaims = null);
}
