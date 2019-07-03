// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Remoting.Editor")]

namespace UnityEditor.Remoting
{
    [NativeHeader("Editor/Mono/RemoteInput/Remoting.bindings.h")]
    internal partial class RemotingInternal
    {
        extern static private void ReceiveData(IntPtr buffer, int bufferSize);

        extern static public void SetConnectedExternally(bool connected);
        static public unsafe void ReceiveData(NativeArray<byte> buffer, int bufferSize) { ReceiveData((IntPtr)buffer.GetUnsafeReadOnlyPtr(), bufferSize); }
    }
}
