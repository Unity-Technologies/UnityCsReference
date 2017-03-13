// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Collections
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    internal class ReadOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    internal class ReadWriteAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    internal class WriteOnlyAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    internal class DeallocateOnJobCompletionAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    internal class NativeContainerAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    internal class NativeContainerSupportsAtomicWriteAttribute : Attribute
    {}

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Struct)]
    internal class NativeContainerSupportsMinMaxWriteRestrictionAttribute : Attribute
    {}
}
