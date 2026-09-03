using System.Linq;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameObjectParentFilterTests
{
    [Fact]
    public void Query_DefaultsToNoParentFilters()
    {
        var query =
            new DCMLGameObjectQuery();

        Assert.Empty(
            query.ParentInstanceIds);
    }

    [Fact]
    public void Query_DeduplicatesParentFilters()
    {
        var query =
            new DCMLGameObjectQuery(
                parentInstanceIds:
                    new[] { 10, 20, 10 });

        Assert.Equal(
            new[] { 10, 20 },
            query.ParentInstanceIds);
    }

    [Fact]
    public void Query_AllowsExactObjectAndParentFiltersTogether()
    {
        var query =
            new DCMLGameObjectQuery(
                instanceIds:
                    new[] { 100, 200 },
                parentInstanceIds:
                    new[] { 10, 20 });

        Assert.Equal(
            new[] { 100, 200 },
            query.InstanceIds);

        Assert.Equal(
            new[] { 10, 20 },
            query.ParentInstanceIds);
    }

    [Fact]
    public void Query_CopiesParentFilterInput()
    {
        int[] source =
        {
            10,
            20
        };

        var query =
            new DCMLGameObjectQuery(
                parentInstanceIds:
                    source);

        source[0] =
            999;

        Assert.Equal(
            new[] { 10, 20 },
            query.ParentInstanceIds);
    }

    [Fact]
    public void Query_PreservesExistingFiltersWithParentIds()
    {
        var query =
            new DCMLGameObjectQuery(
                nameContains:
                    "Switch",
                sceneName:
                    "BaseScene",
                componentTypeNamePrefix:
                    "Il2Cpp.",
                includeInactive:
                    false,
                maxResults:
                    32,
                skipResults:
                    4,
                parentInstanceIds:
                    new[] { 77 });

        Assert.Equal(
            "Switch",
            query.NameContains);

        Assert.Equal(
            "BaseScene",
            query.SceneName);

        Assert.Equal(
            "Il2Cpp.",
            query.ComponentTypeNamePrefix);

        Assert.False(
            query.IncludeInactive);

        Assert.Equal(
            32,
            query.MaxResults);

        Assert.Equal(
            4,
            query.SkipResults);

        Assert.Single(
            query.ParentInstanceIds);

        Assert.Equal(
            77,
            query.ParentInstanceIds.Single());
    }
}
