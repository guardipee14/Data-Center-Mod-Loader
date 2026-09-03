using System.Collections.Generic;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLReferenceCollectionValueTests
{
    [Fact]
    public void ReferenceCollectionKind_IsAdditiveStableValue()
    {
        Assert.Equal(
            9,
            (int) DCMLGameValueKind.ReferenceCollection);
    }

    [Fact]
    public void GameValue_PreservesReferenceCollection()
    {
        var first =
            new DCMLGameReference(
                11,
                "Cable A",
                "Il2Cpp.CableLink");

        var second =
            new DCMLGameReference(
                12,
                "Cable B",
                "Il2Cpp.CableLink");

        var value =
            new DCMLGameValue(
                DCMLGameValueKind.ReferenceCollection,
                "CableArray",
                referenceValues:
                    new[]
                    {
                        first,
                        second
                    },
                collectionCount:
                    2);

        Assert.Equal(
            2,
            value.ReferenceValues.Count);

        Assert.Same(
            first,
            value.ReferenceValues[0]);

        Assert.Same(
            second,
            value.ReferenceValues[1]);
    }

    [Fact]
    public void GameValue_PreservesDeclaredCollectionCount()
    {
        var value =
            new DCMLGameValue(
                DCMLGameValueKind.ReferenceCollection,
                "CableArray",
                referenceValues:
                    new[]
                    {
                        new DCMLGameReference(
                            11,
                            "Cable A",
                            "Il2Cpp.CableLink")
                    },
                collectionCount:
                    4);

        Assert.Equal(
            4,
            value.CollectionCount);
    }

    [Fact]
    public void GameValue_CopiesReferenceCollectionInput()
    {
        var source =
            new List<DCMLGameReference>
            {
                new DCMLGameReference(
                    11,
                    "Cable A",
                    "Il2Cpp.CableLink")
            };

        var value =
            new DCMLGameValue(
                DCMLGameValueKind.ReferenceCollection,
                "CableArray",
                referenceValues:
                    source,
                collectionCount:
                    1);

        source.Add(
            new DCMLGameReference(
                12,
                "Cable B",
                "Il2Cpp.CableLink"));

        Assert.Single(
            value.ReferenceValues);
    }

    [Fact]
    public void ExistingSingleReference_RemainsIndependent()
    {
        var reference =
            new DCMLGameReference(
                42,
                "SFP",
                "Il2Cpp.SFPModule");

        var value =
            new DCMLGameValue(
                DCMLGameValueKind.Reference,
                "Il2Cpp.SFPModule",
                referenceValue:
                    reference);

        Assert.Same(
            reference,
            value.ReferenceValue);

        Assert.Empty(
            value.ReferenceValues);

        Assert.Null(
            value.CollectionCount);
    }
}
