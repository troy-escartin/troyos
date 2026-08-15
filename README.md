# Repository

A .NET/C# repository.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the target framework used by the solution
- Git

## Getting started

This repository currently contains the baseline repository configuration. Add a solution and one or more projects before running the build and test commands below.

```powershell
dotnet new sln -n Repository
dotnet new classlib -n src/Repository
dotnet sln add src/Repository/Repository.csproj
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Replace the sample project and solution names with the names chosen for the application.

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
