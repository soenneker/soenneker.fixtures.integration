[![](https://img.shields.io/nuget/v/soenneker.fixtures.integration.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.fixtures.integration/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.fixtures.integration/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.fixtures.integration/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.fixtures.integration.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.fixtures.integration/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.fixtures.integration/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.fixtures.integration/actions/workflows/codeql.yml)

# Soenneker.Fixtures.Integration

A reusable xUnit fixture for lazily creating multiple authenticated ASP.NET Core `WebApplicationFactory<T>` instances with test logging and Bogus data generation.

## Installation

```bash
dotnet add package Soenneker.Fixtures.Integration
```

## Define a fixture

```csharp
public sealed class ApiFixture : IntegrationFixture
{
    public ApiFixture()
    {
        RegisterFactory<Program>("net10.0");
    }
}

[CollectionDefinition("API")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
```

`projectName` identifies the directory directly beneath the parent of `AppContext.BaseDirectory` that contains `appsettings.json`. For a typical test output at `bin/Debug/net10.0`, passing `net10.0` loads the copied `bin/Debug/net10.0/appsettings.json`. The resolved path must stay beneath that parent directory.

Register each startup type once. Repeated concurrent registrations for the same `TStartup` are safe; the first project name wins.

## Use a factory

```csharp
[Collection("API")]
public sealed class AccountTests(ApiFixture fixture)
{
    [Fact]
    public async Task Gets_account()
    {
        WebApplicationFactory<Program> factory = fixture.GetFactory<Program>().Value;
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/account");
        response.EnsureSuccessStatusCode();
    }
}
```

Factories are lazy: registration does not start an application, and fixture disposal skips factories that were never created. Asking for an unregistered startup type throws `InvalidOperationException`.

## Test host behavior

Each created factory:

- Adds the selected `appsettings.json` without file watching.
- Registers the JWT utility and integration-test startup filter.
- Sets the default authentication scheme to `Test` and installs `TestAuthHandler`.
- Sends verbose Serilog events to the injectable xUnit output sink.

The test authentication handler is for in-memory integration hosts only. Do not copy it into production service registration. Verbose test logs can contain application data, so avoid real credentials and production personal data in test configuration and requests.

## Generated data

After xUnit calls `InitializeAsync()`, `Faker` and `AutoFaker` are available. A derived fixture can assign `AutoFakerConfig` before initialization to customize generation. Factory instances and generated-data helpers are shared according to the xUnit fixture lifetime, so tests should not mutate shared configuration after use begins.
