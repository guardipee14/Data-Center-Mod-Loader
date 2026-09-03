using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.OptionalProbe.Consumer;

public sealed class ConsumerModule :
    IDCMLModule
{
    public const string ModuleId =
        "dcml.probe.optional-consumer";

    public const string OptionalProviderModuleId =
        "dcml.probe.optional-provider";

    public const string QueryMessage =
        "dcml.probe.optional.query";

    public const string PresenceMessage =
        "dcml.probe.optional.present";

    private IDCMLEventBus? _eventBus;

    private IDisposable? _subscription;

    private string _tracePath =
        string.Empty;

    private bool _providerObserved;

    public string Id =>
        ModuleId;

    public string Name =>
        "DCML Optional Dependency Consumer Probe";

    public string Version =>
        "1.0.0";

    public void Initialize(
        IDCMLModuleContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        Directory.CreateDirectory(
            context.DataDirectory);

        _tracePath =
            Path.Combine(
                context.DataDirectory,
                "optional-dependency-probe.log");

        File.WriteAllText(
            _tracePath,
            "Initialize" +
            Environment.NewLine);

        _eventBus =
            context.Services.GetService(
                typeof(IDCMLEventBus))
            as IDCMLEventBus;

        if (_eventBus is null)
        {
            throw new InvalidOperationException(
                "Optional consumer probe did not receive IDCMLEventBus.");
        }

        _subscription =
            _eventBus.Subscribe<string>(
                OnMessage);
    }

    public void Start()
    {
        Append(
            "Start");

        _providerObserved =
            false;

        Append(
            "OptionalProviderQueryPublishing");

        _eventBus!.Publish(
            QueryMessage);

        Append(
            _providerObserved
                ? "OptionalProviderObservedDuringQuery"
                : "OptionalProviderNotObservedDuringQuery");

        Append(
            "ConsumerRunning");
    }

    public void Stop()
    {
        Append(
            _providerObserved
                ? "Stop:ProviderObserved"
                : "Stop:ProviderNotObserved");

        _subscription?.Dispose();
        _subscription =
            null;
    }

    private void OnMessage(
        string message)
    {
        if (
            !string.Equals(
                message,
                PresenceMessage,
                StringComparison.Ordinal))
        {
            return;
        }

        if (!_providerObserved)
        {
            _providerObserved =
                true;

            Append(
                "OptionalProviderObserved");
        }
    }

    private void Append(
        string value)
    {
        File.AppendAllText(
            _tracePath,
            value +
            Environment.NewLine);
    }
}
