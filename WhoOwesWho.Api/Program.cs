var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<WhoOwesWho.Api.Auth.Options.JwtOptions>()
    .Bind(builder.Configuration.GetSection(WhoOwesWho.Api.Auth.Options.JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<WhoOwesWho.Api.Auth.Repositories.IUserRepository, WhoOwesWho.Api.Auth.Repositories.InMemoryUserRepository>();
builder.Services.AddSingleton<WhoOwesWho.Api.Auth.Services.IPasswordHasher, WhoOwesWho.Api.Auth.Services.Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<WhoOwesWho.Api.Auth.Services.ITokenService, WhoOwesWho.Api.Auth.Services.JwtTokenService>();
builder.Services.AddScoped<WhoOwesWho.Api.Auth.Services.IAuthService, WhoOwesWho.Api.Auth.Services.AuthService>();
builder.Services.AddSingleton<WhoOwesWho.Api.Data.DataSeeder>();

builder.Services.AddSingleton<WhoOwesWho.Api.Groups.Repositories.IGroupRepository, WhoOwesWho.Api.Groups.Repositories.InMemoryGroupRepository>();
builder.Services.AddScoped<WhoOwesWho.Api.Groups.Services.IGroupService, WhoOwesWho.Api.Groups.Services.GroupService>();

builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(WhoOwesWho.Api.Auth.Options.JwtOptions.SectionName).Get<WhoOwesWho.Api.Auth.Options.JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration.");

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed test data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<WhoOwesWho.Api.Data.DataSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
