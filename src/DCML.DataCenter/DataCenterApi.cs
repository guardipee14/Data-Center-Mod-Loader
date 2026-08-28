using System;
using DCML.Core.Abstractions;
using DCML.DataCenter.Abstractions;

namespace DCML.DataCenter;

public sealed class DataCenterApi
{
    private DataCenterApi(
        IDataCenterEntityDiscovery entities)
    {
        Entities =
            entities ??
            throw new ArgumentNullException(
                nameof(entities));
    }

    public IDataCenterEntityDiscovery Entities { get; }

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

        return
            new DataCenterApi(
                new DataCenterEntityDiscovery(
                    gameObjectDiscovery));
    }
}
