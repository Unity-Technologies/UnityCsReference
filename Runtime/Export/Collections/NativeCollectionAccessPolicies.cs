// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Collections
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class WriteOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class DeallocateOnJobCompletionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class NativeFixedLengthAttribute : System.Attribute
    {
        public NativeFixedLengthAttribute(int fixedLength) { FixedLength = fixedLength; }
        public int FixedLength;
    }

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class NativeMatchesParallelForLengthAttribute : System.Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    internal class NativeDisableParallelForRestrictionAttribute : System.Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public class NativeContainerAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public class NativeContainerIsAtomicWriteOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public class NativeContainerSupportsMinMaxWriteRestrictionAttribute : Attribute
    {}

    [UnityEngine.Scripting.RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    public class NativeContainerSupportsDeallocateOnJobCompletionAttribute : System.Attribute
    {}
}
