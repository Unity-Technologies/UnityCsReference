// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Copy of MeshInfo, where we pass only the parameters necessary for rendering on the Native side
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/MeshInfo.h")]
    [UsedByNativeCode("MeshInfo")]
    internal struct MeshInfoBindings
    {
        public int vertexCount;
        public TextCoreVertex[] vertexData;
        public Material material;
    }
}
