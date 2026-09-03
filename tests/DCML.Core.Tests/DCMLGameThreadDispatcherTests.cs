using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameThreadDispatcherTests
{
    [Fact]
    public void Capability_HasStableIdentifier()
    {
        Assert.Equal(
            "dcml.game.main-thread",
            DCMLRuntimeCapabilities.GameMainThread);
    }

    [Fact]
    public void Constructor_CapturesCurrentThreadAsMainThread()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        Assert.True(
            dispatcher.IsMainThread);
    }

    [Fact]
    public void Post_DrainsInFifoOrder()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        var values =
            new List<int>();

        dispatcher.Post(
            () => values.Add(1));

        dispatcher.Post(
            () => values.Add(2));

        int executed =
            dispatcher.Drain();

        Assert.Equal(
            2,
            executed);

        Assert.Equal(
            new[]
            {
                1,
                2
            },
            values);
    }

    [Fact]
    public void Drain_RespectsMaximumActions()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        int count =
            0;

        dispatcher.Post(
            () => count++);

        dispatcher.Post(
            () => count++);

        Assert.Equal(
            1,
            dispatcher.Drain(1));

        Assert.Equal(
            1,
            count);

        Assert.Equal(
            1,
            dispatcher.PendingCount);

        Assert.Equal(
            1,
            dispatcher.Drain());

        Assert.Equal(
            2,
            count);
    }

    [Fact]
    public void Drain_DefersWorkPostedByCurrentDrainUntilNextDrain()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        var values =
            new List<int>();

        dispatcher.Post(
            () =>
            {
                values.Add(
                    1);

                dispatcher.Post(
                    () =>
                        values.Add(
                            2));
            });

        Assert.Equal(
            1,
            dispatcher.Drain());

        Assert.Equal(
            new[]
            {
                1
            },
            values);

        Assert.Equal(
            1,
            dispatcher.PendingCount);

        Assert.Equal(
            1,
            dispatcher.Drain());

        Assert.Equal(
            new[]
            {
                1,
                2
            },
            values);
    }

    [Fact]
    public async Task InvokeAsync_OnMainThreadExecutesImmediately()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        int value =
            await dispatcher.InvokeAsync(
                () => 42);

        Assert.Equal(
            42,
            value);

        Assert.Equal(
            0,
            dispatcher.PendingCount);
    }

    [Fact]
    public async Task InvokeAsync_FromWorkerWaitsForMainThreadDrain()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        using var queued =
            new ManualResetEventSlim(
                false);

        Task<bool> worker =
            Task.Run(
                async () =>
                {
                    bool workerWasMain =
                        dispatcher.IsMainThread;

                    Task<bool> invocation =
                        dispatcher.InvokeAsync(
                            () =>
                                dispatcher.IsMainThread);

                    queued.Set();

                    bool callbackWasMain =
                        await invocation.ConfigureAwait(
                            false);

                    return
                        !workerWasMain &&
                        callbackWasMain;
                });

        Assert.True(
            queued.Wait(
                TimeSpan.FromSeconds(
                    5)));

        Assert.False(
            worker.IsCompleted);

        // Drain before the test's first await so this call remains
        // on the exact thread captured by the dispatcher constructor.
        dispatcher.Drain();

        Assert.True(
            await worker.ConfigureAwait(
                false));
    }

    [Fact]
    public async Task InvokeAsync_PropagatesCallbackFailure()
    {
        var dispatcher =
            new DCMLGameThreadDispatcher();

        Task invocation =
            dispatcher.InvokeAsync(
                () =>
                    throw new InvalidOperationException(
                        "boom"));

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await invocation.ConfigureAwait(
                        false));

        Assert.Equal(
            "boom",
            exception.Message);
    }

    [Fact]
    public void PostedFailure_IsolatedAndLaterWorkStillRuns()
    {
        int failures =
            0;

        var dispatcher =
            new DCMLGameThreadDispatcher(
                _ =>
                    failures++);

        int completed =
            0;

        dispatcher.Post(
            () =>
                throw new InvalidOperationException(
                    "boom"));

        dispatcher.Post(
            () =>
                completed++);

        Assert.Equal(
            2,
            dispatcher.Drain());

        Assert.Equal(
            1,
            failures);

        Assert.Equal(
            1,
            completed);
    }
}
