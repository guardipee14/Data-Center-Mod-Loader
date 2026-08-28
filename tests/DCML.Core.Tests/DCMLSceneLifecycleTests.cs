using System;
using DCML.Core.Models;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLSceneLifecycleTests
{
    [Fact]
    public void Event_PreservesSceneData()
    {
        var eventData =
            new DCMLSceneLifecycleEvent(
                DCMLSceneLifecycleStage.Initialized,
                7,
                "Gameplay",
                12);

        Assert.Equal(
            DCMLSceneLifecycleStage.Initialized,
            eventData.Stage);

        Assert.Equal(
            7,
            eventData.BuildIndex);

        Assert.Equal(
            "Gameplay",
            eventData.SceneName);

        Assert.Equal(
            12,
            eventData.Sequence);
    }

    [Fact]
    public void Event_NormalizesNullSceneName()
    {
        var eventData =
            new DCMLSceneLifecycleEvent(
                DCMLSceneLifecycleStage.Loaded,
                -1,
                null!,
                1);

        Assert.Equal(
            string.Empty,
            eventData.SceneName);
    }

    [Fact]
    public void Event_RejectsNoneStage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLSceneLifecycleEvent(
                    DCMLSceneLifecycleStage.None,
                    0,
                    "Scene",
                    1));
    }

    [Fact]
    public void Event_RejectsUnknownStage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLSceneLifecycleEvent(
                    (DCMLSceneLifecycleStage)999,
                    0,
                    "Scene",
                    1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Event_RejectsNonPositiveSequence(
        long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLSceneLifecycleEvent(
                    DCMLSceneLifecycleStage.Loaded,
                    0,
                    "Scene",
                    sequence));
    }

    [Fact]
    public void EventBus_DeliversSceneLifecycleEvent()
    {
        var eventBus =
            new DCMLEventBus();

        DCMLSceneLifecycleEvent? received =
            null;

        using var subscription =
            eventBus.Subscribe<DCMLSceneLifecycleEvent>(
                value =>
                    received = value);

        var expected =
            new DCMLSceneLifecycleEvent(
                DCMLSceneLifecycleStage.Unloaded,
                3,
                "Menu",
                5);

        eventBus.Publish(
            expected);

        Assert.Same(
            expected,
            received);
    }
}
