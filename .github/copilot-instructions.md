# Copilot instructions — WhoOwesWho

## Big picture

- Solution: `WhoOwesWho.Api.slnx` contains two .NET 10 web projects:
  - `WhoOwesWho.Api/` — ASP.NET Core Web API (controllers + OpenAPI in Development).
  - `WhoOwesWho.UI/` — Blazor Server (.NET 10) using Razor Components with interactive server render mode.
- Domain intent (from `README.md`): help split financial expenses between several people. The repo is currently scaffolded (sample WeatherForecast API + default Blazor page).

## How to run locally (Windows / PowerShell)

- API (ports from `WhoOwesWho.Api/Properties/launchSettings.json`):
  - HTTP: `http://localhost:5275`
  - HTTPS: `https://localhost:7255`
- UI (ports from `WhoOwesWho.UI/Properties/launchSettings.json`):
  - HTTP: `http://localhost:5168`
  - HTTPS: `https://localhost:7171`

```powershell
# API
cd C:\Projects\Godel\WhoOwesWho
 dotnet run --project .\WhoOwesWho.Api\WhoOwesWho.Api.csproj

# UI (run in a second terminal)
 dotnet run --project .\WhoOwesWho.UI\WhoOwesWho.UI.csproj
```

## API conventions (current)

- Minimal hosting in `WhoOwesWho.Api/Program.cs`:
  - `builder.Services.AddControllers()` and `app.MapControllers()`.
  - OpenAPI is enabled via `builder.Services.AddOpenApi()` and mapped only in Development (`app.MapOpenApi()`).
- Controller routing uses attribute routing like `WeatherForecastController` in `WhoOwesWho.Api/Controllers/`:
  - `[ApiController]` + `[Route("[controller]")]`.
- There’s a sample request file at `WhoOwesWho.Api/WhoOwesWho.Api.http` (uses `http://localhost:5275/weatherforecast`).

## UI conventions (current)

- Blazor Server setup in `WhoOwesWho.UI/Program.cs`:
  - `AddRazorComponents().AddInteractiveServerComponents()`.
  - `app.MapStaticAssets()` and `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
  - Status code handling: `UseStatusCodePagesWithReExecute("/not-found")`.
- Routing is centralized in `WhoOwesWho.UI/Components/Routes.razor` and uses `Layout.MainLayout` by default.
- Global usings/imports are in `WhoOwesWho.UI/Components/_Imports.razor`.

## Project settings

- Projects target `net10.0`, with `Nullable` and `ImplicitUsings` enabled (see both `.csproj` files).

## Design & Engineering Principles

- Put new API endpoints under `WhoOwesWho.Api/Controllers/` following the existing controller pattern.
- Put new UI pages under `WhoOwesWho.UI/Components/Pages/` and route them via `@page` (see `Home.razor`).
- Place request and response DTOs in the existing shared class library: `WhoOwesWho.Common` (shared with other applications).
- If you introduce UI → API calls, make the base URL/config explicit (no existing `HttpClient` wiring yet in this repo), and keep it discoverable via configuration (`appsettings*.json`).
- Follow Clean Architecture principles
- Follow .Net coding standards, SOLID principles and established patterns
- Use meaningful names for variables, methods, classes, and namespaces
- Keep methods and classes focused on a single responsibility
- Favor maintainable, readable solutions over clever ones
- Optimize for long-term maintainability
- Use dependency injection everywhere
- Make code testable by default
- Handle errors explicitly

## Assumptions & Defaults

- Do not invent missing requirements
- Follow existing patterns in the repository
