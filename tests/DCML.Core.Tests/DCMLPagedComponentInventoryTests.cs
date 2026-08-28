using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPagedComponentInventoryTests
{
    [Fact]
    public void GameObjectQuery_DefaultSkipIsZero()
    {
        Assert.Equal(
            0,
            new DCMLGameObjectQuery().SkipResults);
    }

    [Fact]
    public void GameObjectQuery_PreservesSkip()
    {
        Assert.Equal(
            123,
            new DCMLGameObjectQuery(
                skipResults: 123).SkipResults);
    }

    [Fact]
    public void GameObjectQuery_RejectsNegativeSkip()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameObjectQuery(
                    skipResults: -1));
    }

    [Fact]
    public void ComponentCatalogQuery_DefaultsToSinglePage()
    {
        var query =
            new DataCenterComponentCatalogQuery();

        Assert.False(
            query.ScanAllPages);

        Assert.Equal(
            DataCenterComponentCatalogQuery.DefaultMaxPages,
            query.MaxPages);
    }

    [Fact]
    public void ComponentCatalogQuery_RejectsInvalidPageBudget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DataCenterComponentCatalogQuery(
                    maxPages: 0));
    }

    [Fact]
    public void ComponentCatalog_PagesUntilPartialFinalPage()
    {
        var discovery =
            new PagedDiscovery(5);

        var snapshot =
            new DataCenterComponentCatalog(
                discovery)
            .Scan(
                new DataCenterComponentCatalogQuery(
                    typeNamePrefix: "Il2Cpp.",
                    maxObjects: 2,
                    scanAllPages: true,
                    maxPages: 10));

        Assert.Equal(5, snapshot.ScannedObjectCount);
        Assert.Equal(3, snapshot.PagesScanned);
        Assert.True(snapshot.IsComplete);
        Assert.Equal(new[] { 0, 2, 4 }, discovery.Skips);
    }

    [Fact]
    public void ComponentCatalog_MarksPageBudgetExhaustionIncomplete()
    {
        var snapshot =
            new DataCenterComponentCatalog(
                new PagedDiscovery(10))
            .Scan(
                new DataCenterComponentCatalogQuery(
                    typeNamePrefix: "Il2Cpp.",
                    maxObjects: 2,
                    scanAllPages: true,
                    maxPages: 2));

        Assert.Equal(4, snapshot.ScannedObjectCount);
        Assert.Equal(2, snapshot.PagesScanned);
        Assert.False(snapshot.IsComplete);
    }

    [Fact]
    public void Snapshot_PreservesPagingEvidence()
    {
        var snapshot =
            new DataCenterComponentCatalogSnapshot(
                "BaseScene",
                42,
                Array.Empty<DataCenterComponentTypeInfo>(),
                pagesScanned: 3,
                isComplete: false);

        Assert.Equal(3, snapshot.PagesScanned);
        Assert.False(snapshot.IsComplete);
    }

    private sealed class PagedDiscovery :
        IDCMLGameObjectDiscovery
    {
        private readonly int _total;

        public PagedDiscovery(
            int total)
        {
            _total = total;
        }

        public List<int> Skips { get; } =
            new List<int>();

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            Skips.Add(query.SkipResults);

            int count =
                Math.Min(
                    query.MaxResults,
                    Math.Max(
                        0,
                        _total -
                        query.SkipResults));

            return
                Enumerable.Range(
                    query.SkipResults,
                    count)
                .Select(
                    value =>
                        new DCMLGameObjectInfo(
                            value,
                            "Object" + value,
                            "BaseScene",
                            "World/Object" + value,
                            true,
                            new[] { "Il2Cpp.Device" }))
                .ToArray();
        }
    }
}
