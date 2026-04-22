using Soenneker.Tests.HostedUnit;

namespace Soenneker.Fixtures.Integration.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class IntegrationFixtureTests : HostedUnitTest
{

    public IntegrationFixtureTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
