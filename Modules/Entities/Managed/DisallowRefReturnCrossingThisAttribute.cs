// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    ///     What is this : Attribute signaling that ref returned values, of a type that has this attribute, cannot intersect
    ///     with
    ///     calls to methods that also have this attribute.
    ///     Motivation(s): ref returns of values that are backed by native memory (unsafe), like IComponentData in ecs chunks,
    ///     can have the referenced
    ///     memory invalidated by certain methods. A way is needed to detect these situations a compilation time to prevent
    ///     accessing invalidated references.
    ///     Notes:
    ///     - This attribute is used/feeds a Static Analyzer at compilation time.
    ///     - Attribute transfers with aggragations: struct A has this attribute, struct B has a field of type A; both A and B
    ///     are concidered to have the attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct
        | AttributeTargets.Method
        | AttributeTargets.Property
        | AttributeTargets.Interface)]
    public class DisallowRefReturnCrossingThisAttribute : Attribute
    {
    }
}
