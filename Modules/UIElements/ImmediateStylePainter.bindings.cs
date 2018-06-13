// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.UIElements
{
    [NativeHeader("Modules/UIElements/ImmediateStylePainter.h")]
    internal partial class ImmediateStylePainter
    {
        internal static extern void DrawRect(Rect screenRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses);

        internal static extern void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses, int leftBorder, int topBorder,
            int rightBorder, int bottomBorder, bool usePremultiplyAlpha);

        internal static extern void DrawText(Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap,
            float wordWrapWidth, bool richText, TextClipping textClipping);
    }
}
