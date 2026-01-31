namespace WhoOwesWho.UI.Auth;

public sealed class AuthApiOptions
{
    public const string SectionName = "AuthApi";

    public string BaseUrl { get; init; } = string.Empty;
}
