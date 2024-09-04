// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextElementInfo.h")]
    /// <summary>
    /// Structure containing information about individual text elements (character or sprites).
    /// </summary>
    internal struct NativeTextElementInfo
    {
        public int glyphID;
        public TextCoreVertex bottomLeft;
        public TextCoreVertex topLeft;
        public TextCoreVertex topRight;
        public TextCoreVertex bottomRight;
    }
}
