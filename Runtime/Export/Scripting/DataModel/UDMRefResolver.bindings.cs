// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.DataModel;

namespace Unity.DataModel;

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("CodeReloadSafety", "UAL0001:Unsealed Public Class", Justification = "Unsealed on purpose")]
[NativeHeader("Runtime/Export/Scripting/DataModel/UDMRefResolver.bindings.h")]
internal class UDMRefResolver
{
    internal IntPtr m_Ptr;

    internal UDMRefResolver(IntPtr ptr)
    {
        m_Ptr = ptr;
    }

    // Note: The definition for IUDMReferenceResolver will live inside
    // Runtime/Serialize/TransferFunctions
    // When that work happens, we can enable this
    [NativeMethod("UDMRefResolver_Bindings::GetObjectFromReference", IsThreadSafe = true, HasExplicitThis = true)]
    extern internal UnityEngine.Object GetObjectFromReference(Reference reference);

    [NativeMethod("UDMRefResolver_Bindings::GetInstanceIDFromReference", IsThreadSafe = true, HasExplicitThis = true)]
    extern internal EntityId GetInstanceIDFromReference(Reference reference);

    internal static class BindingsMarshaller
    {
        internal static IntPtr ConvertToUnmanaged(UDMRefResolver resolver) => resolver.m_Ptr;
    }
};
