using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using WhoOwesWho.Common.Auth.Dto;
using WhoOwesWho.UI.Components.Pages;
using WhoOwesWho.UI.Tests.Infrastructure;

namespace WhoOwesWho.UI.Tests.Pages;

public sealed class LoginTests
{
    [Fact]
    public void Render_shows_heading_and_submit_button()
    {
        var (ctx, _, _, _) = TestContextFactory.Create();
        try
        {
            var cut = ctx.Render<Login>();

            cut.Find("h1").TextContent.Should().Be("Login");
            cut.Find("button[type='submit']").TextContent.Should().Be("Log in");
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_with_invalid_model_shows_validation_and_does_not_call_api()
    {
        var (ctx, authApiMock, cookieAuthMock, nav) = TestContextFactory.Create();
        try
        {
            var cut = ctx.Render<Login>();

            await cut.Find("form").SubmitAsync();

            authApiMock.VerifyNoOtherCalls();
            cookieAuthMock.VerifyNoOtherCalls();
            nav.Uri.Should().Be(nav.BaseUri);

            cut.Markup.Should().Contain("validation-message");
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_with_valid_credentials_calls_login_signin_and_navigates_home()
    {
        var (ctx, authApiMock, cookieAuthMock, nav) = TestContextFactory.Create();
        try
        {
            LoginRequestDto? capturedRequest = null;
            var loginResponse = new LoginResponseDto(
                UserId: Guid.NewGuid(),
                Email: "john@example.com",
                AccessToken: "token",
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(30));

            authApiMock
                .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
                .Callback<LoginRequestDto, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(loginResponse);

            cookieAuthMock
                .Setup(x => x.SignInAsync(loginResponse))
                .Returns(Task.CompletedTask);

            var cut = ctx.Render<Login>();

            cut.Find("#email").Change(loginResponse.Email);
            cut.Find("#password").Change("password123");

            await cut.Find("form").SubmitAsync();

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Email.Should().Be(loginResponse.Email);
            capturedRequest.Password.Should().Be("password123");

            authApiMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
            cookieAuthMock.Verify(x => x.SignInAsync(loginResponse), Times.Once);

            nav.Uri.Should().Be(nav.BaseUri);
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_when_api_throws_shows_error_and_does_not_navigate_or_sign_in()
    {
        var (ctx, authApiMock, cookieAuthMock, nav) = TestContextFactory.Create();
        try
        {
            authApiMock
                .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid credentials"));

            var cut = ctx.Render<Login>();

            cut.Find("#email").Change("john@example.com");
            cut.Find("#password").Change("password123");

            await cut.Find("form").SubmitAsync();

            cut.Find(".alert.alert-danger").TextContent.Should().Contain("Invalid credentials");

            cookieAuthMock.VerifyNoOtherCalls();
            nav.Uri.Should().Be(nav.BaseUri);
        }
        finally
        {
            ctx.Dispose();
        }
    }
}
