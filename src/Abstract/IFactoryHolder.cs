using System.Threading.Tasks;

namespace Soenneker.Fixtures.Integration.Abstract;

internal interface IFactoryHolder
{
    ValueTask DisposeIfCreatedAsync();
}