using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Threading.Tasks;
using Soenneker.Fixtures.Integration.Abstract;

namespace Soenneker.Fixtures.Integration;

internal sealed class FactoryHolder<TStartup> : IFactoryHolder where TStartup : class
{
    public Lazy<WebApplicationFactory<TStartup>> Factory { get; }

    public FactoryHolder(string projectName)
    {
        // Lazy is already thread-safe with isThreadSafe:true
        Factory = new Lazy<WebApplicationFactory<TStartup>>(
            () => IntegrationFixture.BuildFactory(new WebApplicationFactory<TStartup>(), projectName),
            isThreadSafe: true);
    }

    public ValueTask DisposeIfCreatedAsync()
    {
        if (!Factory.IsValueCreated)
            return ValueTask.CompletedTask;

        // WebApplicationFactory implements IDisposable (not IAsyncDisposable).
        // If you ever swap in a factory that *is* async-disposable, this still handles it.
        object value = Factory.Value;

        if (value is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        if (value is IDisposable disposable)
            disposable.Dispose();

        return ValueTask.CompletedTask;
    }
}