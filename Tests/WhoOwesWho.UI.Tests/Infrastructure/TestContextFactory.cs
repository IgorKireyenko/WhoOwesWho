using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhoOwesWho.UI.Auth;

namespace WhoOwesWho.UI.Tests.Infrastructure;

internal static class TestContextFactory
{
    public static (BunitContext Ctx, Mock<IAuthApiClient> AuthApiMock, Mock<ICookieAuthenticationService> CookieAuthMock, NavigationManager Nav) Create()
    {
        var ctx = new BunitContext();

        var authApiMock = new Mock<IAuthApiClient>(MockBehavior.Strict);
        var cookieAuthMock = new Mock<ICookieAuthenticationService>(MockBehavior.Strict);

        ctx.Services.AddSingleton(authApiMock.Object);
        ctx.Services.AddSingleton(cookieAuthMock.Object);

        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        return (ctx, authApiMock, cookieAuthMock, nav);
    }
}
