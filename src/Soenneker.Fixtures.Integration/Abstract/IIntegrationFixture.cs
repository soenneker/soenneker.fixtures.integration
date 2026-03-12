using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;
using Soenneker.Utils.AutoBogus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Soenneker.Fixtures.Integration.Abstract;

/// <summary>
/// Provides a reusable and generic integration test xunit fixture that dynamically registers and configures WebApplicationFactory instances for multiple ASP.NET Core projects with support for custom app settings, authentication, logging, and test utilities.
/// </summary>
public interface IIntegrationFixture : IAsyncLifetime
{
    /// <summary>
    /// A configured instance of <see cref="Faker"/> for generating random data in tests.
    /// </summary>
    Faker Faker { get; }

    /// <summary>
    /// A configured instance of <see cref="AutoFaker"/> using optional custom configuration.
    /// </summary>
    AutoFaker AutoFaker { get; }

    /// <summary>
    /// Initializes the integration fixture with any needed setup logic, such as Faker configuration.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes once the fixture is initialized.</returns>
    new ValueTask InitializeAsync();

    /// <summary>
    /// Registers a lazy <see cref="WebApplicationFactory{TStartup}"/> for the specified startup type and project name.
    /// </summary>
    /// <typeparam name="TStartup">The startup class of the application under test.</typeparam>
    /// <param name="projectName">The name of the test project containing the appsettings.json file.</param>
    void RegisterFactory<TStartup>(string projectName) where TStartup : class;

    /// <summary>
    /// Gets a registered lazy <see cref="WebApplicationFactory{TStartup}"/> for the specified startup type.
    /// </summary>
    /// <typeparam name="TStartup">The startup class of the application under test.</typeparam>
    /// <returns>The registered <see cref="Lazy{T}"/> factory.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the factory was not registered first.</exception>
    Lazy<WebApplicationFactory<TStartup>> GetFactory<TStartup>() where TStartup : class;
}