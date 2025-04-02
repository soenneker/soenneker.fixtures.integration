using Microsoft.AspNetCore.Mvc.Testing;
using System;
using Xunit;

namespace Soenneker.Fixtures.Integration.Abstract;

/// <summary>
/// Provides a reusable and generic integration test xunit fixture that dynamically registers and configures WebApplicationFactory instances for multiple ASP.NET Core projects with support for custom app settings, authentication, logging, and test utilities.
/// </summary>
public interface IIntegrationFixture : IAsyncLifetime
{
    void RegisterFactory<TStartup>(string projectName) where TStartup : class;

    Lazy<WebApplicationFactory<TStartup>> GetFactory<TStartup>() where TStartup : class;
}
