using Bogus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.XUnit.Injectable;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Serilog.Sinks.XUnit.Injectable.Extensions;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.ValueTask;
using Soenneker.Fixtures.Integration.Abstract;
using Soenneker.StartupFilters.IntegrationTests.Registrars;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;
using Soenneker.Utils.Jwt.Registrars;
using Soenneker.Utils.Test.AuthHandler;
using System;
using System.Collections.Concurrent;
using Soenneker.Extensions.String;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Soenneker.Fixtures.Integration;

// Cannot be sealed
/// <inheritdoc cref="IIntegrationFixture" />
public class IntegrationFixture : IIntegrationFixture
{
    // Defensive cache; avoids duplicate FS checks if multiple factories resolve the same project.
    private static readonly ConcurrentDictionary<(string BaseDirectory, string ProjectName), string> _appSettingsPathCache = new();

    private readonly ConcurrentDictionary<Type, IFactoryHolder> _factories = new();

    public Faker Faker { get; private set; } = null!;

    public AutoFaker AutoFaker { get; private set; } = null!;

    /// <summary>
    /// Gets or sets auto faker config.
    /// </summary>
    public AutoFakerConfig? AutoFakerConfig { get; set; }

    public ValueTask InitializeAsync()
    {
        AutoFakerConfig config = AutoFakerConfig ?? new AutoFakerConfig();
        AutoFaker = new AutoFaker(config);
        Faker = AutoFaker.Faker;
        return ValueTask.CompletedTask;
    }

    public void RegisterFactory<TStartup>(string projectName) where TStartup : class
    {
        _factories.GetOrAdd(typeof(TStartup), static (_, state) => new FactoryHolder<TStartup>(state), projectName);
    }

    public Lazy<WebApplicationFactory<TStartup>> GetFactory<TStartup>() where TStartup : class
    {
        if (_factories.TryGetValue(typeof(TStartup), out IFactoryHolder? holder))
            return ((FactoryHolder<TStartup>)holder).Factory;

        throw new InvalidOperationException($"Factory for type {typeof(TStartup).Name} has not been registered.");
    }

    internal static WebApplicationFactory<T> BuildFactory<T>(WebApplicationFactory<T> factory, string projectName) where T : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                string appSettingsPath = GetAppSettingsPath(projectName);
                configBuilder.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
            });

            builder.ConfigureTestServices(static services =>
            {
                services.AddJwtUtilAsScoped();
                services.AddIntegrationTestsStartupFilterAsSingleton();

                services.AddAuthentication(DeployEnvironment.Test.Name)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(DeployEnvironment.Test.Name, static _ =>
                        {
                        });
            });

            builder.ConfigureServices(static services =>
            {
                services.AddSingleton<IInjectableTestOutputSink, InjectableTestOutputSink>();

                services.AddSerilog(static (sp, loggerConfiguration) =>
                {
                    var sink = sp.GetRequiredService<IInjectableTestOutputSink>();

                    loggerConfiguration.MinimumLevel.Verbose()
                                       .WriteTo.Async(a => a.InjectableTestOutput(sink))
                                       .Enrich.FromLogContext();
                });
            });
        });
    }

    /// <summary>
    /// Gets app settings path.
    /// </summary>
    /// <param name="projectName">Name of the project to target.</param>
    /// <returns>The requested text.</returns>
    public static string GetAppSettingsPath(string projectName)
    {
        string baseDir = AppContext.BaseDirectory;

        return _appSettingsPathCache.GetOrAdd((baseDir, projectName), static key =>
        {
            string? parent = Directory.GetParent(key.BaseDirectory)
                                      ?.FullName;

            if (parent.IsNullOrWhiteSpace())
                throw new InvalidOperationException($"Cannot resolve the parent of test base directory '{key.BaseDirectory}'.");

            string root = Path.GetFullPath(parent);
            string path = Path.GetFullPath(Path.Combine(root, key.ProjectName, "appsettings.json"));
            string rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
            StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (!path.StartsWith(rootPrefix, comparison))
                throw new InvalidOperationException("The project name resolves outside the test output directory.");

            if (!File.Exists(path))
                throw new FileNotFoundException($"The integration-test appsettings file was not found at '{path}'.", path);

            return path;
        });
    }

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        // Dispose all *created* factories
        foreach (IFactoryHolder holder in _factories.Values)
        {
            await holder.DisposeIfCreated()
                        .NoSync();
        }
    }
}
