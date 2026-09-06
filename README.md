# TroyOS

A .NET/C# repository.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the target framework used by the solution
- Git

## Getting started

The repository contains the TroyOS Blazor WebAssembly client and the Minimal API used for server-side prompt refinement.

```powershell
dotnet new sln -n Repository
dotnet new classlib -n src/Repository
dotnet sln add src/Repository/Repository.csproj
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Local prompt refinement

The API runs on `https://localhost:7295` and the client uses that address in Development. Set the local Gemini key with user secrets; it is never stored in source control:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_LOCAL_KEY" --project TroyOS.Api/TroyOS.Api.csproj
```

Run the `Run TroyOS` task to start both the API and client. The endpoint smoke tests are in [TroyOS.Api/TroyOS.Api.http](TroyOS.Api/TroyOS.Api.http).

The client sends only `POST /api/prompt/improve`. Gemini-specific code and credentials exist only in the API project. For a local publish, set `ApiBaseUrl` in the client `wwwroot/appsettings.json` to the deployed API HTTPS URL before publishing the client. The production GitHub Actions workflow reads that value from the `TROYOS_API_BASE_URL` repository secret and fails rather than publishing a client that would call the Cloudflare origin.

## MonsterASP.NET deployment

The API targets `net10.0`, matching the repository's current SDK and CI configuration. MonsterASP.NET runtime support was not present in repository documentation, so confirm that .NET 10 is enabled before deployment; otherwise change the API target framework and publish with the supported SDK.

Publish the API folder with:

```powershell
dotnet publish TroyOS.Api/TroyOS.Api.csproj -c Release -o .\publish-api
```

Upload the contents of `publish-api` (including `web.config` when generated) to the ASP.NET application folder. Configure these server-side application settings in the MonsterASP.NET control panel:

- `Gemini__ApiKey`: production Gemini key
- `Gemini__Model`: production model name
- `Cors__AllowedOrigins__0`: the production TroyOS HTTPS origin
- `ForwardedHeaders__TrustedProxyIps__0`: the trusted MonsterASP.NET proxy IP, when provided by the host
- `PromptRefinement__MaximumPromptCharacters`, `PromptRefinement__MaximumRequestBytes`: optional limits
- `RateLimiting__PermitLimit`, `RateLimiting__WindowSeconds`, `RateLimiting__QueueLimit`: optional abuse controls

Do not put `Gemini__ApiKey` in `appsettings.json`, the client `wwwroot`, JavaScript, or a publish artifact. The workflow uploads a `troyos-api-publish` artifact for manual MonsterASP.NET deployment; upload its contents to the API application directory. Confirm the deployed endpoint is HTTPS, inspect hosting logs for the API process, and send repeated requests to confirm HTTP 429 after the configured limit.

## Repository conventions

- `.gitignore` excludes generated .NET, IDE, test, package, and coverage files.
- `.editorconfig` provides shared formatting defaults for C# and common configuration files.
- Pull requests are validated by GitHub Actions when a .NET solution or project is present.

## Development workflow

1. Create or update the solution and projects.
2. Restore dependencies with `dotnet restore`.
3. Build with `dotnet build --configuration Release`.
4. Run tests with `dotnet test --configuration Release`.
5. Keep generated output out of source control.
