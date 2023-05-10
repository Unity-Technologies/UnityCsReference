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
    /// Structure which contains the vertex attributes (geometry) of the text object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/TextCoreVertex.h")]
    [UsedByNativeCode("TextCoreVertex")]
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct TextCoreVertex
    {
        public Vector3 position;
        public Color32 color;
        public Vector2 uv0;
        public Vector2 uv2;
    }
}
