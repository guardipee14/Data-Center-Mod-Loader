using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.MultiModuleProbe.Consumer;

public sealed class ConsumerModule :
    IDCMLModule
{
    public const string ModuleId =
        "dcml.probe.consumer";

    public const string PublisherModuleId =
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

    private bool _responseReceived;

    public string Id =>
        ModuleId;

    public string Name =>
        "DCML Multi-Module Consumer Probe";

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
                "Consumer probe did not receive IDCMLEventBus.");
        }

        _subscription =
            _eventBus.Subscribe<string>(
                OnMessage);
    }

    public void Start()
    {
        Append(
            "Start");

        _responseReceived =
            false;

        Append(
            "RequestPublishing");

        _eventBus!.Publish(
            RequestMessage);

        if (!_responseReceived)
        {
            Append(
                "Error:ResponseNotReceived");

            throw new InvalidOperationException(
                "Publisher response was not received during the synchronous event handshake.");
        }

        Append(
            "HandshakeComplete");
    }

    public void Stop()
    {
        Append(
            "Stop");

        _eventBus!.Publish(
            ConsumerStoppingMessage);

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
                ResponseMessage,
                StringComparison.Ordinal))
        {
            return;
        }

        _responseReceived =
            true;

        Append(
            "ResponseReceived");
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
