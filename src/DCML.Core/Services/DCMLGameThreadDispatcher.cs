using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DCML.Core.Abstractions;

namespace DCML.Core.Services;

public sealed class DCMLGameThreadDispatcher :
    IDCMLGameThread
{
    public const int DefaultMaximumActionsPerDrain =
        4096;

    private readonly int _mainThreadId;

    private readonly ConcurrentQueue<Action>
        _queue =
            new ConcurrentQueue<Action>();

    private readonly Action<Exception>?
        _unhandledExceptionHandler;

    public DCMLGameThreadDispatcher(
        Action<Exception>? unhandledExceptionHandler = null)
    {
        _mainThreadId =
            Thread.CurrentThread.ManagedThreadId;

        _unhandledExceptionHandler =
            unhandledExceptionHandler;
    }

    public bool IsMainThread =>
        Thread.CurrentThread.ManagedThreadId ==
        _mainThreadId;

    public int PendingCount =>
        _queue.Count;

    public void Post(
        Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(
                nameof(action));
        }

        _queue.Enqueue(
            action);
    }

    public Task InvokeAsync(
        Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(
                nameof(action));
        }

        if (IsMainThread)
        {
            try
            {
                action();

                return
                    Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return
                    Task.FromException(
                        exception);
            }
        }

        var completion =
            new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        _queue.Enqueue(
            () =>
            {
                try
                {
                    action();

                    completion.TrySetResult(
                        null);
                }
                catch (Exception exception)
                {
                    completion.TrySetException(
                        exception);
                }
            });

        return
            completion.Task;
    }

    public Task<T> InvokeAsync<T>(
        Func<T> function)
    {
        if (function is null)
        {
            throw new ArgumentNullException(
                nameof(function));
        }

        if (IsMainThread)
        {
            try
            {
                return
                    Task.FromResult(
                        function());
            }
            catch (Exception exception)
            {
                return
                    Task.FromException<T>(
                        exception);
            }
        }

        var completion =
            new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        _queue.Enqueue(
            () =>
            {
                try
                {
                    completion.TrySetResult(
                        function());
                }
                catch (Exception exception)
                {
                    completion.TrySetException(
                        exception);
                }
            });

        return
            completion.Task;
    }

    public int Drain(
        int maximumActions =
            DefaultMaximumActionsPerDrain)
    {
        if (!IsMainThread)
        {
            throw new InvalidOperationException(
                "The DCML game-thread queue may only be drained from its captured main thread.");
        }

        if (maximumActions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumActions),
                maximumActions,
                "Maximum actions must be greater than zero.");
        }

        int executed =
            0;

        int queuedAtDrainStart =
            Math.Min(
                maximumActions,
                _queue.Count);

        while (
            executed < queuedAtDrainStart &&
            _queue.TryDequeue(
                out Action? action)
        )
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                try
                {
                    _unhandledExceptionHandler?.Invoke(
                        exception);
                }
                catch
                {
                    // The diagnostic handler is not allowed to break
                    // dispatch of later queued work.
                }
            }

            executed++;
        }

        return
            executed;
    }
}
