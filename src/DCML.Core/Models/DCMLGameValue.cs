using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DCML.Core.Models;

public sealed class DCMLGameValue
{
    private readonly IReadOnlyList<DCMLGameReference> _referenceValues;

    public DCMLGameValue(
        DCMLGameValueKind kind,
        string? typeName = null,
        string? stringValue = null,
        bool? booleanValue = null,
        long? integerValue = null,
        double? numberValue = null,
        string? diagnostic = null,
        DCMLGameReference? referenceValue = null,
        IEnumerable<DCMLGameReference>? referenceValues = null,
        int? collectionCount = null)
    {
        if (!Enum.IsDefined(typeof(DCMLGameValueKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        TypeName = string.IsNullOrWhiteSpace(typeName) ? string.Empty : typeName.Trim();
        StringValue = stringValue;
        BooleanValue = booleanValue;
        IntegerValue = integerValue;
        NumberValue = numberValue;
        Diagnostic = diagnostic ?? string.Empty;
        ReferenceValue = referenceValue;

        var references =
            new List<DCMLGameReference>();

        if (referenceValues is not null)
        {
            foreach (
                DCMLGameReference reference in
                referenceValues)
            {
                if (reference is not null)
                {
                    references.Add(
                        reference);
                }
            }
        }

        _referenceValues =
            new ReadOnlyCollection<DCMLGameReference>(
                references);

        CollectionCount =
            collectionCount;
    }

    public DCMLGameValueKind Kind { get; }

    public string TypeName { get; }

    public string? StringValue { get; }

    public bool? BooleanValue { get; }

    public long? IntegerValue { get; }

    public double? NumberValue { get; }

    public string Diagnostic { get; }

    public DCMLGameReference? ReferenceValue { get; }

    public IReadOnlyList<DCMLGameReference> ReferenceValues =>
        _referenceValues;

    public int? CollectionCount { get; }
}
