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
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextCoreVertex.h")]
    [UsedByNativeCode("TextCoreVertex")]
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct TextCoreVertex
    {
        public Vector3 position;
        public Color32 color;
        /// UV0 contains the following information
        /// X, Y are the UV coordinates of the glyph in the atlas texture.
        public Vector2 uv0;
        // UV2 contains the following information
        /// X is the texture index in the texture atlas array
        /// Y is the SDF Scale where a negative value represents bold text
        public Vector2 uv2;
    }
}
