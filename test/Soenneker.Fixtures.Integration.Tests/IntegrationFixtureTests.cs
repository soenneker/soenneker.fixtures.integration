using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Fixtures.Integration.Tests;

[Collection("Collection")]
public class IntegrationFixtureTests : FixturedUnitTest
{

    public IntegrationFixtureTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
