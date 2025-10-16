// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.DataModel;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.DataModel;

[UsedByNativeCode]
[StructLayout(LayoutKind.Sequential)]
[NativeClass("UDMWriteData")]
[NativeHeader("Runtime/Export/Scripting/DataModel/UDMWriteData.h")]
internal struct UdmWriteData
{
    internal UdmObjectId objectId;
    internal EntityId instanceId;
    internal byte isStrippedObject;
};
