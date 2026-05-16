# Contributing to Whidy

Thank you for your interest in contributing.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (MVP is Windows-only)
- An Azure DevOps organisation with a PAT for testing

## Building

```bash
dotnet build
```

## Running locally

```bash
dotnet run -- yesterday
```

## Publishing a single-file executable

```bash
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net10.0/win-x64/publish/whidy.exe`

## Code conventions

- Follow the `.editorconfig` settings in the repository root
- Use `var` only when the type is apparent from the right-hand side
- Prefer expression-bodied members for simple properties
- Keep methods short and focused
- No commented-out code in pull requests

## Pull request process

1. Fork the repository and create a branch from `main`
2. Make your changes with clear, atomic commits
3. Ensure `dotnet build` passes with no warnings
4. Open a pull request with a description of what changed and why
5. Link any related issue in the PR description

## Reporting bugs

Open a [GitHub issue](https://github.com/filipecarneiro/whidy/issues) with:
- What you ran
- What you expected
- What happened instead
- Your Windows version and .NET SDK version (`dotnet --version`)
