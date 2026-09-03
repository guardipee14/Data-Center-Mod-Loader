using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.MultiModuleProbe.Publisher;

public sealed class PublisherModule :
    IDCMLModule
{
    public const string ModuleId =
        "dcml.probe.publisher";

    public const string RequestMessage =
        "dcml.probe.request";

    public const string ResponseMessage =
        "dcml.probe.response";

    public const string ConsumerStoppingMessage =
        "dcml.probe.consumer-stopping";

    private IDCMLEventBus? _eventBus;

    private IDisposable? _subscription;

    private string _tracePath =
        string.Empty;

    private bool _started;

    public string Id =>
        ModuleId;

    public string Name =>
        "DCML Multi-Module Publisher Probe";

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
                "multimodule-probe.log");

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
                "Publisher probe did not receive IDCMLEventBus.");
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
            string.Equals(
                message,
                RequestMessage,
                StringComparison.Ordinal))
        {
            Append(
                "RequestReceived");

            if (!_started)
            {
                Append(
                    "Error:RequestBeforePublisherStart");

                throw new InvalidOperationException(
                    "Consumer request arrived before the publisher reached Start.");
            }

            Append(
                "ResponsePublishing");

            _eventBus!.Publish(
                ResponseMessage);

            Append(
                "ResponsePublished");

            return;
        }

        if (
            string.Equals(
                message,
                ConsumerStoppingMessage,
                StringComparison.Ordinal))
        {
            if (!_started)
            {
                throw new InvalidOperationException(
                    "Consumer stopped after the publisher was already stopped.");
            }

            Append(
                "ConsumerStopObserved");
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
