using Bogus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.XUnit.Injectable;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.String;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;
using Soenneker.Utils.Jwt.Registrars;
using Soenneker.Utils.Test.AuthHandler;
using System;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog.Sinks.XUnit.Injectable.Extensions;
using Soenneker.Fixtures.Integration.Abstract;
using Soenneker.StartupFilters.IntegrationTests.Registrars;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Fixtures.Integration;

// Cannot be sealed
///<inheritdoc cref="IIntegrationFixture"/>
public class IntegrationFixture : IIntegrationFixture
{
    public Dictionary<Type, object> Factories { get; } = new();

    public Faker Faker { get; private set; } = null!;
    public AutoFaker AutoFaker { get; private set; } = null!;
    public AutoFakerConfig? AutoFakerConfig { get; set; }

    private InjectableTestOutputSink? _injectableTestOutputSink;

    public ValueTask InitializeAsync()
    {
        AutoFakerConfig config = AutoFakerConfig ?? new AutoFakerConfig();
        AutoFaker = new AutoFaker(config);
        Faker = AutoFaker.Faker;
        return ValueTask.CompletedTask;
    }

    public void RegisterFactory<TStartup>(string projectName) where TStartup : class
    {
        var factory = new Lazy<WebApplicationFactory<TStartup>>(() =>
        {
            var baseFactory = new WebApplicationFactory<TStartup>();
            return BuildFactory(baseFactory, projectName);
        }, isThreadSafe: true);

        Factories[typeof(TStartup)] = factory;
    }

    public Lazy<WebApplicationFactory<TStartup>> GetFactory<TStartup>() where TStartup : class
    {
        if (Factories.TryGetValue(typeof(TStartup), out object? factory))
            return (Lazy<WebApplicationFactory<TStartup>>)factory;

        throw new InvalidOperationException($"Factory for type {typeof(TStartup).Name} has not been registered.");
    }

    private WebApplicationFactory<T> BuildFactory<T>(WebApplicationFactory<T> factory, string projectName) where T : class
    {
        // Create the sink once (per fixture) if it's not there yet
        _injectableTestOutputSink ??= new InjectableTestOutputSink();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                string appSettingsPath = GetAppSettingsPath(projectName);
                configBuilder.AddJsonFile(appSettingsPath);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddJwtUtilAsScoped();
                services.AddIntegrationTestsStartupFilterAsSingleton();

                services.AddAuthentication(DeployEnvironment.Test.Name)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(DeployEnvironment.Test.Name, _ => { });
            });

            builder.ConfigureServices(services =>
            {
                // Register the single fixture-scoped sink
                services.AddSingleton<IInjectableTestOutputSink>(_injectableTestOutputSink);

                services.AddSerilog((_, loggerConfiguration) =>
                {
                    loggerConfiguration.MinimumLevel.Verbose()
                        .WriteTo.Async(a => a.InjectableTestOutput(_injectableTestOutputSink)) // async wrapper OK
                        .Enrich.FromLogContext();
                });
            });
        });
    }

    public static string GetAppSettingsPath(string projectName)
    {
        string dllDir = Directory.GetCurrentDirectory();
        var info = new DirectoryInfo(dllDir);
        var appSettingsDir = info.Parent?.ToString();

        if (appSettingsDir.IsNullOrEmpty())
            throw new Exception($"AppSettings path does not exist! {appSettingsDir}");

        string baseAppSettings = Path.Combine(appSettingsDir, projectName, "appsettings.json");

        if (!File.Exists(baseAppSettings))
            throw new Exception($"appsettings.json file does not exist at {baseAppSettings}! dllDir: {dllDir}");

        return baseAppSettings;
    }

    public async ValueTask DisposeAsync()
    {
        // Dispose all created factories (handle both async & sync)
        foreach (object factoryObj in Factories.Values)
        {
            if (factoryObj is Lazy<object> { IsValueCreated: true } lazy)
            {
                switch (lazy.Value)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync()
                            .NoSync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }

        if (_injectableTestOutputSink is not null)
        {
            await _injectableTestOutputSink.DisposeAsync()
                .NoSync();
            _injectableTestOutputSink = null;
        }

        await Log.CloseAndFlushAsync()
            .NoSync();

        Log.Logger = Serilog.Core.Logger.None;
    }
}