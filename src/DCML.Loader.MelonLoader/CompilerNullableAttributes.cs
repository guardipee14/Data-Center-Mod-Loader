using System;

#pragma warning disable 0436

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Loader-local compatibility definitions used when compiling
    /// against MelonLoader's IL2CPP-generated Unity assemblies.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.GenericParameter |
        AttributeTargets.Module |
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.ReturnValue,
        Inherited = false
    )]
    internal sealed class NullableAttribute :
        Attribute
    {
        public NullableAttribute(
            byte value
        )
        {
            NullableFlags =
                new[]
                {
                    value
                };
        }

        public NullableAttribute(
            byte[] value
        )
        {
            NullableFlags =
                value;
        }

        public byte[] NullableFlags { get; }
    }

    /// <summary>
    /// Loader-local compatibility definition for nullable context
    /// metadata emitted by the C# compiler.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Struct,
        Inherited = false
    )]
    internal sealed class NullableContextAttribute :
        Attribute
    {
        public NullableContextAttribute(
            byte value
        )
        {
            Flag =
                value;
        }

        public byte Flag { get; }
    }
}

#pragma warning restore 0436
