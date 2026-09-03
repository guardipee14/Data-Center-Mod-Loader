using System.Threading.Tasks;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Abstractions;

public interface IDataCenterHardwareTopology
{
    Task<DataCenterHardwareTopologyGraph> CaptureAsync(
        DataCenterHardwareSnapshotQuery query);
}
