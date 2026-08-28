using System.Collections.Generic;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Abstractions;

public interface IDataCenterEntityDiscovery
{
    IReadOnlyList<DataCenterEntityInfo> Find(
        DataCenterEntityQuery query);
}
