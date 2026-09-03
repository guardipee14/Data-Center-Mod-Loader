using System.Linq;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLComponentStateGameObjectFilterTests
{
    [Fact]
    public void Query_DefaultsToNoGameObjectFilters()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CustomerBase");

        Assert.Empty(
            query.GameObjectInstanceIds);
    }

    [Fact]
    public void Query_DeduplicatesGameObjectFilters()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CustomerBase",
                gameObjectInstanceIds:
                    new[] { 10, 20, 10 });

        Assert.Equal(
            new[] { 10, 20 },
            query.GameObjectInstanceIds);
    }

    [Fact]
    public void Query_AllowsComponentAndGameObjectFiltersTogether()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CustomerBase",
                componentInstanceIds:
                    new[] { 100, 200 },
                gameObjectInstanceIds:
                    new[] { 10, 20 });

        Assert.Equal(
            new[] { 100, 200 },
            query.ComponentInstanceIds);

        Assert.Equal(
            new[] { 10, 20 },
            query.GameObjectInstanceIds);
    }

    [Fact]
    public void Query_CopiesGameObjectFilterInput()
    {
        int[] source =
        {
            10,
            20
        };

        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CustomerBase",
                gameObjectInstanceIds:
                    source);

        source[0] =
            999;

        Assert.Equal(
            new[] { 10, 20 },
            query.GameObjectInstanceIds);
    }

    [Fact]
    public void Query_PreservesExistingOptionsWithGameObjectFilters()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CustomerBase",
                memberNames:
                    new[] { "customerID", "device" },
                sceneName:
                    "BaseScene",
                scope:
                    DCMLGameComponentScope.Scene,
                includeInactive:
                    false,
                maxResults:
                    9,
                skipResults:
                    1,
                gameObjectInstanceIds:
                    new[] { 77 });

        Assert.Equal(
            "BaseScene",
            query.SceneName);

        Assert.False(
            query.IncludeInactive);

        Assert.Equal(
            9,
            query.MaxResults);

        Assert.Equal(
            1,
            query.SkipResults);

        Assert.Equal(
            2,
            query.MemberNames.Count);

        Assert.Single(
            query.GameObjectInstanceIds);

        Assert.Equal(
            77,
            query.GameObjectInstanceIds.Single());
    }
}
