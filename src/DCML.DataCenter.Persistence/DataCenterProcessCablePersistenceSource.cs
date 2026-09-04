using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Persistence;

/// <summary>
/// Reads an explicitly selected Data Center save through an out-of-process
/// decoder and exposes the result through the decoder-agnostic
/// <see cref="IDataCenterCablePersistenceSource"/> contract.
/// </summary>
/// <remarks>
/// This adapter never searches save directories and never selects a save on
/// behalf of the caller. The save file, process host, and helper assembly are
/// all supplied explicitly.
/// </remarks>
public sealed class DataCenterProcessCablePersistenceSource :
    IDataCenterCablePersistenceSource
{
    private readonly string _hostPath;

    private readonly string _helperDllPath;

    public DataCenterProcessCablePersistenceSource(
        string hostPath,
        string helperDllPath,
        string savePath)
    {
        _hostPath =
            NormalizeRequiredPath(
                hostPath,
                nameof(hostPath));

        _helperDllPath =
            NormalizeRequiredPath(
                helperDllPath,
                nameof(helperDllPath));

        SourcePath =
            NormalizeRequiredPath(
                savePath,
                nameof(savePath));
    }

    public string SourcePath { get; }

    private static string NormalizeRequiredPath(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Persistence paths must be supplied explicitly.",
                parameterName);
        }

        return
            Path.GetFullPath(
                value);
    }

    public async Task<DataCenterCablePersistenceSnapshot> ReadAsync()
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    _hostPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true,

                WorkingDirectory =
                    Path.GetDirectoryName(
                        _helperDllPath) ??
                    string.Empty
            };

        startInfo.ArgumentList.Add(
            _helperDllPath);

        startInfo.ArgumentList.Add(
            SourcePath);

        using Process process =
            new();

        process.StartInfo =
            startInfo;

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "The persistence helper process did not start.");
        }

        string stdout =
            await process.StandardOutput
                .ReadToEndAsync()
                .ConfigureAwait(
                    false);

        string stderr =
            await process.StandardError
                .ReadToEndAsync()
                .ConfigureAwait(
                    false);

        await process.WaitForExitAsync()
            .ConfigureAwait(
                false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Persistence helper failed with exit code " +
                process.ExitCode +
                ": " +
                stderr);
        }

        HelperSnapshot? raw =
            JsonSerializer.Deserialize<HelperSnapshot>(
                stdout);

        if (raw is null)
        {
            throw new InvalidDataException(
                "Persistence helper returned no JSON snapshot.");
        }

        DataCenterCablePersistenceRecord[] cables =
            raw.Cables
                .Select(
                    cable =>
                        new DataCenterCablePersistenceRecord(
                            cable.CableID,
                            ToEndpoint(
                                DataCenterPhysicalCableEndpointSide.Start,
                                cable.Start),
                            ToEndpoint(
                                DataCenterPhysicalCableEndpointSide.End,
                                cable.End)))
                .ToArray();

        return
            new DataCenterCablePersistenceSnapshot(
                raw.SourcePath,
                raw.SourceLength,
                raw.SourceLastWriteTimeUtc,
                cables,
                raw.ServerIDs,
                raw.SwitchIDs,
                raw.RouterIDs,
                raw.FirewallIDs,
                raw.PatchPanelIDs,
                raw.CustomerIDs);
    }

    private static DataCenterCablePersistenceEndpoint ToEndpoint(
        DataCenterPhysicalCableEndpointSide side,
        HelperEndpoint endpoint)
    {
        return
            new DataCenterCablePersistenceEndpoint(
                side,
                endpoint.LinkType,
                endpoint.ServerID,
                endpoint.SwitchID,
                endpoint.CustomerID,
                endpoint.Position);
    }

    private sealed class HelperSnapshot
    {
        public string SourcePath { get; set; } =
            string.Empty;

        public long SourceLength { get; set; }

        public DateTime SourceLastWriteTimeUtc { get; set; }

        public int NetworkSaveDataCount { get; set; }

        public HelperCable[] Cables { get; set; } =
            Array.Empty<HelperCable>();

        public string[] ServerIDs { get; set; } =
            Array.Empty<string>();

        public string[] SwitchIDs { get; set; } =
            Array.Empty<string>();

        public string[] RouterIDs { get; set; } =
            Array.Empty<string>();

        public string[] FirewallIDs { get; set; } =
            Array.Empty<string>();

        public string[] PatchPanelIDs { get; set; } =
            Array.Empty<string>();

        public int[] CustomerIDs { get; set; } =
            Array.Empty<int>();
    }

    private sealed class HelperCable
    {
        public int CableID { get; set; }

        public HelperEndpoint Start { get; set; } =
            new();

        public HelperEndpoint End { get; set; } =
            new();
    }

    private sealed class HelperEndpoint
    {
        public int LinkType { get; set; }

        public string ServerID { get; set; } =
            string.Empty;

        public string SwitchID { get; set; } =
            string.Empty;

        public int? CustomerID { get; set; }

        public string Position { get; set; } =
            string.Empty;
    }
}
