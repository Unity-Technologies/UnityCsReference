// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode,
     NativeHeader("Runtime/Utilities/TextUtil.h"),
     NativeHeader("Modules/IMGUI/StylePainter.h"),
     NativeHeader("Modules/TextRendering/Public/Font.h")]
    internal partial class StylePainter
    {
        [NativeMethod(IsThreadSafe = true)] private static extern IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr self);

        internal extern void DrawRect(Rect screenRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses);

        internal extern void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses, int leftBorder, int topBorder,
            int rightBorder, int bottomBorder, bool usePremultiplyAlpha);

        internal extern void DrawText(Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap,
            float wordWrapWidth, bool richText, TextClipping textClipping);

        public extern float ComputeTextWidth(string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);

        public extern float ComputeTextHeight(string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);

        public extern Vector2 GetCursorPosition(string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, float wordWrapWidth, bool richText, Rect screenRect,
            int cursorPosition);
    }
}
