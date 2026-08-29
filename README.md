[![](https://img.shields.io/nuget/v/soenneker.fixtures.integration.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.fixtures.integration/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.fixtures.integration/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.fixtures.integration/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.fixtures.integration.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.fixtures.integration/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.fixtures.integration/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.fixtures.integration/actions/workflows/codeql.yml)

# Soenneker.Fixtures.Integration

Provides a reusable and generic integration test xunit fixture that dynamically registers and configures WebApplicationFactory instances for multiple ASP.NET Core projects with support for custom app settings, authentication, logging, and test utilities.

## Install

```bash
dotnet add package Soenneker.Fixtures.Integration
```

## Quick start

```csharp
using Soenneker.Fixtures.Integration.Abstract;

IIntegrationFixture integrationFixture = /* resolve from DI */;
await integrationFixture.InitializeAsync();
```

Initializes the integration fixture with any needed setup logic, such as Faker configuration.

## What you get

- `IIntegrationFixture` — Provides a reusable and generic integration test xunit fixture that dynamically registers and configures WebApplicationFactory instances for multiple ASP.NET Core projects with support for custom app settings, authentication, logging, and test utilities.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IIntegrationFixture.Faker` | A configured instance of `Faker` for generating random data in tests. | A configured instance of `Faker` for generating random data in tests. |
| `IIntegrationFixture.AutoFaker` | A configured instance of `AutoFaker` using optional custom configuration. | A configured instance of `AutoFaker` using optional custom configuration. |
| `IIntegrationFixture.InitializeAsync()` | Initializes the integration fixture with any needed setup logic, such as Faker configuration. | A `ValueTask` that completes once the fixture is initialized. |

## Important behavior

- `IIntegrationFixture.GetFactory()`: Thrown if the factory was not registered first.
