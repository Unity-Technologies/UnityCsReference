// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;
using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextRenderingIndices.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct TextRenderingIndices
    {
        public int meshIndex;
        public int textElementInfoIndex;
    }
}
