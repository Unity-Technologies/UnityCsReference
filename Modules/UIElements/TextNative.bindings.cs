// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Experimental.UIElements
{
    [NativeHeader("Modules/UIElements/TextNative.bindings.h")]
    internal static class TextNative
    {
        public static Vector2 GetCursorPosition(CursorPositionStylePainterParameters painterParams)
        {
            Font font = painterParams.font;
            if (font == null)
            {
                Debug.LogError("StylePainter: Can't process a null font.");
                return Vector2.zero;
            }

            string text = painterParams.text;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool richText = painterParams.richText;
            Rect rect = painterParams.rect;
            int cursorIndex = painterParams.cursorIndex;

            return GetCursorPosition(text, font, fontSize, fontStyle, anchor, wordWrapWidth, richText, rect, cursorIndex);
        }

        public static float ComputeTextWidth(TextStylePainterParameters painterParams)
        {
            string text = painterParams.text;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool wordWrap = painterParams.wordWrap;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            bool richText = painterParams.richText;

            return ComputeTextWidth(text, wordWrapWidth, wordWrap, font, fontSize, fontStyle, anchor, richText);
        }

        public static float ComputeTextHeight(TextStylePainterParameters painterParams)
        {
            string text = painterParams.text;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool wordWrap = painterParams.wordWrap;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            bool richText = painterParams.richText;

            return ComputeTextHeight(text, wordWrapWidth, wordWrap, font, fontSize, fontStyle, anchor, richText);
        }

        [FreeFunction(Name = "TextNative::ComputeTextWidth")]
        public static extern float ComputeTextWidth(string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);

        [FreeFunction(Name = "TextNative::ComputeTextHeight")]
        public static extern float ComputeTextHeight(string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);

        [FreeFunction(Name = "TextNative::GetCursorPosition")]
        public static extern Vector2 GetCursorPosition(string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, float wordWrapWidth, bool richText, Rect screenRect,
            int cursorPosition);
    }
}
