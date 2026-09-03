using System;
using DCML.Core.Abstractions;
using DCML.DataCenter.Abstractions;

namespace DCML.DataCenter;

public sealed class DataCenterApi
{
    private DataCenterApi(
        IDataCenterEntityDiscovery entities,
        IDataCenterComponentCatalog components,
        IDataCenterHardwareSnapshots? hardware,
        IDataCenterHardwareTopology? topology)
    {
        Entities =
            entities ??
            throw new ArgumentNullException(
                nameof(entities));

        Components =
            components ??
            throw new ArgumentNullException(
                nameof(components));

        Hardware = hardware;
        Topology = topology;
    }

    public IDataCenterEntityDiscovery Entities { get; }

    public IDataCenterComponentCatalog Components { get; }

    public IDataCenterHardwareSnapshots? Hardware { get; }

    public IDataCenterHardwareTopology? Topology { get; }

    public static DataCenterApi Create(
        IDCMLModuleContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        IDCMLGameObjectDiscovery? gameObjectDiscovery =
            context.Services.GetService(
                typeof(IDCMLGameObjectDiscovery))
            as IDCMLGameObjectDiscovery;

        if (gameObjectDiscovery is null)
        {
            throw new InvalidOperationException(
                "The optional DCML.DataCenter API requires IDCMLGameObjectDiscovery to be available.");
        }

        IDCMLGameTypeCatalog? gameTypeCatalog =
            context.Services.GetService(
                typeof(IDCMLGameTypeCatalog))
            as IDCMLGameTypeCatalog;

        IDCMLGameComponentStateReader? componentStateReader =
            context.Services.GetService(
                typeof(IDCMLGameComponentStateReader))
            as IDCMLGameComponentStateReader;

        IDataCenterHardwareSnapshots? hardware =
            componentStateReader is null
                ? null
                : new DataCenterHardwareSnapshots(
                    componentStateReader);

        return
            new DataCenterApi(
                new DataCenterEntityDiscovery(
                    gameObjectDiscovery,
                    gameTypeCatalog),
                new DataCenterComponentCatalog(
                    gameObjectDiscovery),
                hardware,
                hardware is null
                    ? null
                    : new DataCenterHardwareTopology(
                        hardware,
                        componentStateReader));
    }
}
