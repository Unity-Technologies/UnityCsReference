// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.DataModel;
using UnityEngine.Bindings;

namespace Unity.DataModel;

[NativeHeader("Runtime/Export/Scripting/DataModel/UDMRefResolverExplicit.bindings.h")]
[StructLayout(LayoutKind.Sequential)]
internal struct ReferenceEntry
{
    internal Reference reference;
    internal IntPtr objPtr;
}

internal sealed class UDMRefResolverExplicit : UDMRefResolver, IDisposable
{
    internal UDMRefResolverExplicit() : base(IntPtr.Zero)
    {

    }
    public void Dispose()
    {

    }
};
