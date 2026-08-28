using System;

namespace DCML.Core.Abstractions;

public interface IDCMLEventBus
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    void Publish<TEvent>(TEvent eventData);
}
