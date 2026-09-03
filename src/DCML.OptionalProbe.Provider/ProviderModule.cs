using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.OptionalProbe.Provider;

public sealed class ProviderModule :
    IDCMLModule
{
    public const string ModuleId =
        "dcml.probe.optional-provider";

    public const string QueryMessage =
        "dcml.probe.optional.query";

    public const string PresenceMessage =
        "dcml.probe.optional.present";

    private IDCMLEventBus? _eventBus;

    private IDisposable? _subscription;

    private string _tracePath =
        string.Empty;

    private bool _started;

    public string Id =>
        ModuleId;

    public string Name =>
        "DCML Optional Dependency Provider Probe";

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
                "Optional provider probe did not receive IDCMLEventBus.");
        }

        _subscription =
            _eventBus.Subscribe<string>(
                OnMessage);
    }

    public void Start()
    {
        _started =
            true;

        Append(
            "Start");

        Append(
            "PresencePublishing");

        _eventBus!.Publish(
            PresenceMessage);

        Append(
            "PresencePublished");
    }

    public void Stop()
    {
        Append(
            "Stop");

        _started =
            false;

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
                QueryMessage,
                StringComparison.Ordinal))
        {
            return;
        }

        Append(
            "QueryReceived");

        if (!_started)
        {
            Append(
                "QueryIgnoredBeforeStart");

            return;
        }

        Append(
            "PresencePublishingForQuery");

        _eventBus!.Publish(
            PresenceMessage);

        Append(
            "PresencePublishedForQuery");
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
