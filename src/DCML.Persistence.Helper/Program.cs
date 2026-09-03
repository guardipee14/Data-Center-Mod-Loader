using System;
using System.Collections.Generic;
using System.Formats.Nrbf;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DCML.Persistence.Helper;

internal static class Program
{
    private const long MaximumSaveBytes =
        256L * 1024L * 1024L;

    private const int MaximumSerializedArrayElements =
        2_000_000;

    public static int Main(
        string[] args)
    {
        try
        {
            if (
                args.Length != 1 ||
                string.IsNullOrWhiteSpace(args[0])
            )
            {
                Console.Error.WriteLine(
                    "A single explicit save path is required.");
                return 2;
            }

            HelperSnapshot snapshot =
                Read(
                    args[0]);

            Console.Out.Write(
                JsonSerializer.Serialize(
                    snapshot));

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                exception.GetType().FullName +
                ": " +
                exception.Message);
            return 1;
        }
    }

    private static HelperSnapshot Read(
        string path)
    {
        FileInfo file =
            new(
                Path.GetFullPath(path));

        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "The configured save was not found.",
                file.FullName);
        }

        if (
            file.Length <= 0 ||
            file.Length > MaximumSaveBytes
        )
        {
            throw new InvalidDataException(
                "The save length is outside the read-only safety bounds.");
        }

        using FileStream stream =
            new(
                file.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

        if (!NrbfDecoder.StartsWithPayloadHeader(stream))
        {
            throw new InvalidDataException(
                "The configured save does not contain an NRBF payload header.");
        }

        stream.Position = 0;

        SerializationRecord root =
            NrbfDecoder.Decode(stream);

        var visitor =
            new Visitor();

        visitor.Walk(root);

        if (visitor.NetworkSaveDataCount != 1)
        {
            throw new InvalidDataException(
                "Expected exactly one NetworkSaveData record but observed " +
                visitor.NetworkSaveDataCount +
                ".");
        }

        IGrouping<int, HelperCable>? duplicate =
            visitor.Cables
                .GroupBy(value => value.CableID)
                .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidDataException(
                "Duplicate cable ID observed: " +
                duplicate.Key +
                ".");
        }

        return new HelperSnapshot
        {
            SourcePath = file.FullName,
            SourceLength = file.Length,
            SourceLastWriteTimeUtc = file.LastWriteTimeUtc,
            NetworkSaveDataCount = visitor.NetworkSaveDataCount,
            Cables = visitor.Cables.ToArray(),
            ServerIDs = visitor.ServerIDs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            SwitchIDs = visitor.SwitchIDs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            RouterIDs = visitor.RouterIDs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            FirewallIDs = visitor.FirewallIDs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            PatchPanelIDs = visitor.PatchPanelIDs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            CustomerIDs = visitor.CustomerIDs.OrderBy(value => value).ToArray()
        };
    }

    private sealed class Visitor
    {
        private readonly HashSet<SerializationRecordId> _visited = new();
        private readonly List<HelperCable> _cables = new();
        private readonly HashSet<string> _serverIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _switchIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _routerIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _firewallIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _patchPanelIds = new(StringComparer.Ordinal);
        private readonly HashSet<int> _customerIds = new();

        public IReadOnlyList<HelperCable> Cables => _cables;
        public IReadOnlyCollection<string> ServerIDs => _serverIds;
        public IReadOnlyCollection<string> SwitchIDs => _switchIds;
        public IReadOnlyCollection<string> RouterIDs => _routerIds;
        public IReadOnlyCollection<string> FirewallIDs => _firewallIds;
        public IReadOnlyCollection<string> PatchPanelIDs => _patchPanelIds;
        public IReadOnlyCollection<int> CustomerIDs => _customerIds;
        public int NetworkSaveDataCount { get; private set; }

        public void Walk(
            SerializationRecord root)
        {
            var pending =
                new Stack<SerializationRecord>();

            pending.Push(root);

            while (pending.Count > 0)
            {
                SerializationRecord current =
                    pending.Pop();

                if (!_visited.Add(current.Id))
                {
                    continue;
                }

                string typeName =
                    TypeNameOf(current);

                if (current is ClassRecord cls)
                {
                    ObserveEntity(
                        typeName,
                        cls);

                    if (IsType(typeName, "NetworkSaveData"))
                    {
                        NetworkSaveDataCount++;
                    }

                    if (IsType(typeName, "CableSaveData"))
                    {
                        _cables.Add(
                            ReadCable(cls));
                    }

                    foreach (
                        string member in
                        cls.MemberNames)
                    {
                        object? value;

                        try
                        {
                            value =
                                cls.GetRawValue(member);
                        }
                        catch
                        {
                            continue;
                        }

                        if (
                            value is SerializationRecord
                                record
                        )
                        {
                            pending.Push(record);
                        }
                    }

                    continue;
                }

                if (
                    current is
                        SZArrayRecord<SerializationRecord>
                            records
                )
                {
                    if (
                        records.Length >
                            MaximumSerializedArrayElements
                    )
                    {
                        throw new InvalidDataException(
                            "A serialized object array exceeded the safety limit.");
                    }

                    foreach (
                        SerializationRecord? item in
                        records.GetArray(true)
                    )
                    {
                        if (item is not null)
                        {
                            pending.Push(item);
                        }
                    }

                    continue;
                }

                if (current is ArrayRecord array)
                {
                    long count = 1;

                    foreach (
                        int length in
                        array.Lengths)
                    {
                        count *= length;

                        if (
                            count >
                                MaximumSerializedArrayElements
                        )
                        {
                            throw new InvalidDataException(
                                "A serialized array exceeded the safety limit.");
                        }
                    }
                }
            }
        }

        private void ObserveEntity(
            string typeName,
            ClassRecord record)
        {
            if (IsType(typeName, "ServerSaveData"))
            {
                Add(
                    _serverIds,
                    ReadString(record, "serverID"));
            }

            if (IsType(typeName, "RouterSaveData"))
            {
                string id =
                    ReadString(record, "switchID");

                Add(_routerIds, id);
                Add(_switchIds, id);
            }
            else if (IsType(typeName, "FirewallSaveData"))
            {
                string id =
                    ReadString(record, "switchID");

                Add(_firewallIds, id);
                Add(_switchIds, id);
            }
            else if (IsType(typeName, "SwitchSaveData"))
            {
                Add(
                    _switchIds,
                    ReadString(record, "switchID"));
            }

            if (IsType(typeName, "PatchPanelSaveData"))
            {
                Add(
                    _patchPanelIds,
                    ReadString(record, "patchPanelID"));
            }

            if (IsType(typeName, "CustomerBaseSaveData"))
            {
                int? id =
                    ReadInt(record, "customerID");

                if (id.HasValue)
                {
                    _customerIds.Add(id.Value);
                }
            }
        }

        private static HelperCable ReadCable(
            ClassRecord cable)
        {
            int? id =
                ReadInt(cable, "cableID");

            if (
                !id.HasValue ||
                id.Value < 0
            )
            {
                throw new InvalidDataException(
                    "A CableSaveData record did not contain a valid cableID.");
            }

            ClassRecord? start =
                ReadClass(cable, "startPoint");

            ClassRecord? end =
                ReadClass(cable, "endPoint");

            if (
                start is null ||
                end is null
            )
            {
                throw new InvalidDataException(
                    "A CableSaveData record did not contain both endpoints.");
            }

            return new HelperCable
            {
                CableID = id.Value,
                Start = ReadEndpoint(start),
                End = ReadEndpoint(end)
            };
        }

        private static HelperEndpoint ReadEndpoint(
            ClassRecord endpoint)
        {
            return new HelperEndpoint
            {
                LinkType = ReadLinkType(endpoint),
                ServerID = ReadString(endpoint, "serverID"),
                SwitchID = ReadString(endpoint, "switchID"),
                CustomerID = ReadInt(endpoint, "customerID"),
                Position = ReadVector(endpoint, "position")
            };
        }

        private static int ReadLinkType(
            ClassRecord record)
        {
            string? member =
                FindMember(record, "type");

            if (member is null)
            {
                return 0;
            }

            object? raw =
                record.GetRawValue(member);

            if (raw is ClassRecord enumRecord)
            {
                return ReadInt(enumRecord, "value__") ?? 0;
            }

            if (raw is null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(
                    raw,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static string ReadVector(
            ClassRecord record,
            string name)
        {
            ClassRecord? vector =
                ReadClass(record, name);

            if (vector is null)
            {
                return string.Empty;
            }

            double? x = ReadDouble(vector, "x");
            double? y = ReadDouble(vector, "y");
            double? z = ReadDouble(vector, "z");

            if (
                !x.HasValue ||
                !y.HasValue ||
                !z.HasValue
            )
            {
                return string.Empty;
            }

            return
                x.Value.ToString("R", CultureInfo.InvariantCulture) +
                "," +
                y.Value.ToString("R", CultureInfo.InvariantCulture) +
                "," +
                z.Value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static ClassRecord? ReadClass(
            ClassRecord record,
            string name)
        {
            string? member =
                FindMember(record, name);

            return
                member is null
                    ? null
                    : record.GetRawValue(member) as ClassRecord;
        }

        private static int? ReadInt(
            ClassRecord record,
            string name)
        {
            string? member =
                FindMember(record, name);

            if (member is null)
            {
                return null;
            }

            object? raw =
                record.GetRawValue(member);

            if (raw is null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt32(
                    raw,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static double? ReadDouble(
            ClassRecord record,
            string name)
        {
            string? member =
                FindMember(record, name);

            if (member is null)
            {
                return null;
            }

            object? raw =
                record.GetRawValue(member);

            if (raw is null)
            {
                return null;
            }

            try
            {
                return Convert.ToDouble(
                    raw,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static string ReadString(
            ClassRecord record,
            string name)
        {
            string? member =
                FindMember(record, name);

            if (member is null)
            {
                return string.Empty;
            }

            return
                record.GetRawValue(member)?.ToString()
                ?? string.Empty;
        }

        private static string? FindMember(
            ClassRecord record,
            string logicalName)
        {
            foreach (
                string member in
                record.MemberNames)
            {
                if (
                    string.Equals(
                        Normalize(member),
                        logicalName,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    return member;
                }
            }

            return null;
        }

        private static string Normalize(
            string name)
        {
            if (
                name.Length >= 3 &&
                name[0] == '<'
            )
            {
                int close =
                    name.IndexOf('>');

                if (close > 1)
                {
                    return name.Substring(
                        1,
                        close - 1);
                }
            }

            return name.TrimStart('_');
        }

        private static bool IsType(
            string qualified,
            string simple)
        {
            string type =
                qualified
                    .Split(',', 2)[0]
                    .Trim();

            return
                type.Equals(simple, StringComparison.Ordinal) ||
                type.EndsWith("." + simple, StringComparison.Ordinal) ||
                type.EndsWith("+" + simple, StringComparison.Ordinal);
        }

        private static string TypeNameOf(
            SerializationRecord record)
        {
            return
                record.TypeName.AssemblyQualifiedName ??
                record.TypeName.ToString() ??
                string.Empty;
        }

        private static void Add(
            ISet<string> set,
            string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                set.Add(value);
            }
        }
    }

    public sealed class HelperSnapshot
    {
        public string SourcePath { get; set; } = string.Empty;
        public long SourceLength { get; set; }
        public DateTime SourceLastWriteTimeUtc { get; set; }
        public int NetworkSaveDataCount { get; set; }
        public HelperCable[] Cables { get; set; } = Array.Empty<HelperCable>();
        public string[] ServerIDs { get; set; } = Array.Empty<string>();
        public string[] SwitchIDs { get; set; } = Array.Empty<string>();
        public string[] RouterIDs { get; set; } = Array.Empty<string>();
        public string[] FirewallIDs { get; set; } = Array.Empty<string>();
        public string[] PatchPanelIDs { get; set; } = Array.Empty<string>();
        public int[] CustomerIDs { get; set; } = Array.Empty<int>();
    }

    public sealed class HelperCable
    {
        public int CableID { get; set; }
        public HelperEndpoint Start { get; set; } = new();
        public HelperEndpoint End { get; set; } = new();
    }

    public sealed class HelperEndpoint
    {
        public int LinkType { get; set; }
        public string ServerID { get; set; } = string.Empty;
        public string SwitchID { get; set; } = string.Empty;
        public int? CustomerID { get; set; }
        public string Position { get; set; } = string.Empty;
    }
}
