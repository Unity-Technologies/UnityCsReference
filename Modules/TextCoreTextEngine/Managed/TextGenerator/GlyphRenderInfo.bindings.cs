// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore
{
    // Ordinals must match the C++ NativeGlyphKind and the public
    // TextElement.GlyphKind. New values append; do not renumber.
    [NativeHeader("Modules/TextCoreTextEngine/Native/GlyphRenderInfo.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum NativeGlyphKind : int
    {
        Character = 0,
        Sprite = 1,
        // Reserved for future ATG decoration quads (<u>, <s>, <mark>).
        // Commented out in lockstep with TextElement.GlyphKind until the
        // generator emits them. When uncommenting, preserve these ordinals.
        // Underline = 2,
        // Strikethrough = 3,
        // Mark = 4,
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/GlyphRenderInfo.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct NativeGlyphRenderInfo
    {
        public int meshIndex;
        public int textElementInfoIndex;
        public int lineIndex;
        public int linkID;
        public NativeGlyphKind kind;
    }
}
