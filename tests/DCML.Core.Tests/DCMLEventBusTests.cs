using System;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLEventBusTests
{
    [Fact]
    public void Publish_DeliversTypedEvent()
    {
        var eventBus = new DCMLEventBus();
        TestEvent? received = null;

        using var subscription =
            eventBus.Subscribe<TestEvent>(
                value => received = value);

        var expected = new TestEvent("hello");

        eventBus.Publish(expected);

        Assert.Same(expected, received);
    }

    [Fact]
    public void Publish_DeliversToMultipleSubscribers()
    {
        var eventBus = new DCMLEventBus();
        int deliveries = 0;

        using var first =
            eventBus.Subscribe<TestEvent>(
                _ => deliveries++);

        using var second =
            eventBus.Subscribe<TestEvent>(
                _ => deliveries++);

        eventBus.Publish(
            new TestEvent("hello"));

        Assert.Equal(2, deliveries);
    }

    [Fact]
    public void DisposedSubscription_DoesNotReceiveEvents()
    {
        var eventBus = new DCMLEventBus();
        int deliveries = 0;

        var subscription =
            eventBus.Subscribe<TestEvent>(
                _ => deliveries++);

        subscription.Dispose();

        eventBus.Publish(
            new TestEvent("hello"));

        Assert.Equal(0, deliveries);
    }

    [Fact]
    public void Publish_ContinuesAfterSubscriberFailure()
    {
        var eventBus = new DCMLEventBus();
        bool secondCalled = false;

        using var first =
            eventBus.Subscribe<TestEvent>(
                _ => throw new InvalidOperationException("boom"));

        using var second =
            eventBus.Subscribe<TestEvent>(
                _ => secondCalled = true);

        var exception =
            Assert.Throws<AggregateException>(
                () =>
                    eventBus.Publish(
                        new TestEvent("hello")));

        Assert.True(secondCalled);
        Assert.Single(exception.InnerExceptions);
    }

    [Fact]
    public void Subscribe_RejectsNullHandler()
    {
        var eventBus = new DCMLEventBus();

        Assert.Throws<ArgumentNullException>(
            () =>
                eventBus.Subscribe<TestEvent>(
                    null!));
    }

    [Fact]
    public void Publish_RejectsNullReferenceEvent()
    {
        var eventBus = new DCMLEventBus();

        Assert.Throws<ArgumentNullException>(
            () =>
                eventBus.Publish<TestEvent>(
                    null!));
    }

    [Fact]
    public void Publish_WithNoSubscribers_IsNoOp()
    {
        var eventBus = new DCMLEventBus();

        eventBus.Publish(
            new TestEvent("hello"));
    }

    private sealed class TestEvent
    {
        public TestEvent(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
