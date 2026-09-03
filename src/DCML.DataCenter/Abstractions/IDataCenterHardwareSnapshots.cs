using System.Threading.Tasks;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Abstractions;

public interface IDataCenterHardwareSnapshots
{
    Task<DataCenterHardwareSnapshotSet> CaptureAsync(
        DataCenterHardwareSnapshotQuery query);
}
