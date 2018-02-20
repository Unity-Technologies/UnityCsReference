// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Collections
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public sealed class ReadOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public sealed class WriteOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DeallocateOnJobCompletionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeFixedLengthAttribute : Attribute
    {
        public NativeFixedLengthAttribute(int fixedLength) { FixedLength = fixedLength; }
        public int FixedLength;
    }

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeMatchesParallelForLengthAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeDisableParallelForRestrictionAttribute : Attribute
    {}
}

namespace Unity.Collections.LowLevel.Unsafe
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeContainerAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeContainerIsAtomicWriteOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeContainerSupportsMinMaxWriteRestrictionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeContainerSupportsDeallocateOnJobCompletionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeContainerNeedsThreadIndexAttribute : Attribute
    {}

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class WriteAccessRequiredAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeDisableUnsafePtrRestrictionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeSetClassTypeToNullOnScheduleAttribute : Attribute
    {}
}
