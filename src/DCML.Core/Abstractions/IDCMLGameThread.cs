using System;
using System.Threading.Tasks;

namespace DCML.Core.Abstractions;

public interface IDCMLGameThread
{
    bool IsMainThread { get; }

    void Post(
        Action action);

    Task InvokeAsync(
        Action action);

    Task<T> InvokeAsync<T>(
        Func<T> function);
}
