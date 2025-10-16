// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.DataModel;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.DataModel;

[StructLayout(LayoutKind.Sequential)]
[NativeClass("UDMReadData")]
[NativeHeader("Runtime/Export/Scripting/DataModel/UDMReadData.h")]
internal struct UdmReadData
{
    internal UdmObjectId objectId;
    internal EntityId instanceId;
};
