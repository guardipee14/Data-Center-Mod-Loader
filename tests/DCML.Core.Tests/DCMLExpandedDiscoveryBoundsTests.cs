using DCML.Core.Models;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLExpandedDiscoveryBoundsTests
{
    [Fact]
    public void GameObjectQuery_AllowsExpandedInventoryCeiling()
    {
        Assert.Equal(
            16384,
            DCMLGameObjectQuery.MaximumMaxResults);

        var query =
            new DCMLGameObjectQuery(
                maxResults:
                    DCMLGameObjectQuery.MaximumMaxResults);

        Assert.Equal(
            16384,
            query.MaxResults);
    }

    [Fact]
    public void ComponentCatalogQuery_DefaultUsesExpandedCeiling()
    {
        var query =
            new DataCenterComponentCatalogQuery();

        Assert.Equal(
            16384,
            query.MaxObjects);
    }
}
