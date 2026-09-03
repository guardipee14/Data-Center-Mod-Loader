using System;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameObjectHierarchyContextTests
{
    [Fact]
    public void Query_DefaultsToNoExactInstanceIds()
    {
        var query =
            new DCMLGameObjectQuery();

        Assert.Empty(
            query.InstanceIds);
    }

    [Fact]
    public void Query_DeduplicatesExactInstanceIds()
    {
        var query =
            new DCMLGameObjectQuery(
                instanceIds:
                    new[] { 10, 20, 10 });

        Assert.Equal(
            new[] { 10, 20 },
            query.InstanceIds);
    }

    [Fact]
    public void Query_PreservesPagingAndExactIdsTogether()
    {
        var query =
            new DCMLGameObjectQuery(
                sceneName:
                    "BaseScene",
                maxResults:
                    18,
                skipResults:
                    2,
                instanceIds:
                    new[] { 100, 200 });

        Assert.Equal(
            "BaseScene",
            query.SceneName);

        Assert.Equal(
            18,
            query.MaxResults);

        Assert.Equal(
            2,
            query.SkipResults);

        Assert.Equal(
            new[] { 100, 200 },
            query.InstanceIds);
    }

    [Fact]
    public void GameObjectInfo_DefaultsToNoParent()
    {
        var info =
            new DCMLGameObjectInfo(
                10,
                "Child",
                "BaseScene",
                "Root/Child",
                true,
                Array.Empty<string>());

        Assert.Null(
            info.ParentInstanceId);
    }

    [Fact]
    public void GameObjectInfo_PreservesParentIdentity()
    {
        var info =
            new DCMLGameObjectInfo(
                10,
                "Child",
                "BaseScene",
                "Root/Parent/Child",
                true,
                new[]
                {
                    "Il2Cpp.CableLink"
                },
                parentInstanceId:
                    5);

        Assert.Equal(
            5,
            info.ParentInstanceId);

        Assert.Contains(
            "Il2Cpp.CableLink",
            info.ComponentTypeNames);
    }
}
