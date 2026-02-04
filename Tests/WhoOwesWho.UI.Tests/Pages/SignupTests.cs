using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using WhoOwesWho.Common.Auth.Dto;
using WhoOwesWho.UI.Components.Pages;
using WhoOwesWho.UI.Tests.Infrastructure;

namespace WhoOwesWho.UI.Tests.Pages;

public sealed class SignupTests
{
    [Fact]
    public void Render_shows_heading_and_submit_button()
    {
        var (ctx, _, _, _) = TestContextFactory.Create();
        try
        {
            var cut = ctx.Render<Signup>();

            cut.Find("h1").TextContent.Should().Be("Create account");
            cut.Find("button[type='submit']").TextContent.Should().Be("Create account");
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_with_invalid_model_shows_validation_and_does_not_call_api()
    {
        var (ctx, authApiMock, _, nav) = TestContextFactory.Create();
        try
        {
            var cut = ctx.Render<Signup>();

            await cut.Find("form").SubmitAsync();

            authApiMock.VerifyNoOtherCalls();
            nav.Uri.Should().Be(nav.BaseUri);

            cut.Markup.Should().Contain("validation-message");
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_when_confirm_password_mismatches_shows_validation_and_does_not_call_api()
    {
        var (ctx, authApiMock, _, nav) = TestContextFactory.Create();
        try
        {
            var cut = ctx.Render<Signup>();

            cut.Find("#fullName").Change("John Doe");
            cut.Find("#email").Change("john@example.com");
            cut.Find("#password").Change("password123");
            cut.Find("#confirmPassword").Change("different123");

            await cut.Find("form").SubmitAsync();

            cut.Markup.Should().Contain("Passwords do not match.");
            authApiMock.VerifyNoOtherCalls();
            nav.Uri.Should().Be(nav.BaseUri);
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_with_valid_model_calls_signup_and_navigates_to_login()
    {
        var (ctx, authApiMock, _, nav) = TestContextFactory.Create();
        try
        {
            SignUpRequestDto? capturedRequest = null;

            authApiMock
                .Setup(x => x.SignUpAsync(It.IsAny<SignUpRequestDto>(), It.IsAny<CancellationToken>()))
                .Callback<SignUpRequestDto, CancellationToken>((req, _) => capturedRequest = req)
                .Returns(Task.CompletedTask);

            var cut = ctx.Render<Signup>();

            cut.Find("#fullName").Change("John Doe");
            cut.Find("#email").Change("john@example.com");
            cut.Find("#password").Change("password123");
            cut.Find("#confirmPassword").Change("password123");

            await cut.Find("form").SubmitAsync();

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Email.Should().Be("john@example.com");
            capturedRequest.Password.Should().Be("password123");

            authApiMock.Verify(x => x.SignUpAsync(It.IsAny<SignUpRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);

            nav.Uri.Should().Be(nav.BaseUri + "login");
        }
        finally
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public async Task Submit_when_api_throws_shows_error_and_does_not_navigate()
    {
        var (ctx, authApiMock, _, nav) = TestContextFactory.Create();
        try
        {
            authApiMock
                .Setup(x => x.SignUpAsync(It.IsAny<SignUpRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Email already exists"));

            var cut = ctx.Render<Signup>();

            cut.Find("#fullName").Change("John Doe");
            cut.Find("#email").Change("john@example.com");
            cut.Find("#password").Change("password123");
            cut.Find("#confirmPassword").Change("password123");

            await cut.Find("form").SubmitAsync();

            cut.Find(".alert.alert-danger").TextContent.Should().Contain("Email already exists");
            nav.Uri.Should().Be(nav.BaseUri);
        }
        finally
        {
            ctx.Dispose();
        }
    }
}
