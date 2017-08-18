// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;

namespace UnityEngine
{



[UsedByNativeCode]
internal sealed partial class StylePainter : IStylePainter
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init () ;

    internal void DrawRect_Internal (Rect screenRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses) {
        INTERNAL_CALL_DrawRect_Internal ( this, ref screenRect, ref color, ref borderWidths, ref borderRadiuses );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawRect_Internal (StylePainter self, ref Rect screenRect, ref Color color, ref Vector4 borderWidths, ref Vector4 borderRadiuses);
    internal void DrawTexture_Internal (Rect screenRect, Texture texture, Rect sourceRect, Color color, Vector4 borderWidths, Vector4 borderRadiuses, int leftBorder, int topBorder, int rightBorder, int bottomBorder) {
        INTERNAL_CALL_DrawTexture_Internal ( this, ref screenRect, texture, ref sourceRect, ref color, ref borderWidths, ref borderRadiuses, leftBorder, topBorder, rightBorder, bottomBorder );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawTexture_Internal (StylePainter self, ref Rect screenRect, Texture texture, ref Rect sourceRect, ref Color color, ref Vector4 borderWidths, ref Vector4 borderRadiuses, int leftBorder, int topBorder, int rightBorder, int bottomBorder);
    internal void DrawText_Internal (Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping textClipping) {
        INTERNAL_CALL_DrawText_Internal ( this, ref screenRect, text, font, fontSize, fontStyle, ref fontColor, anchor, wordWrap, wordWrapWidth, richText, textClipping );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawText_Internal (StylePainter self, ref Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, ref Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping textClipping);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float ComputeTextWidth_Internal (string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float ComputeTextHeight_Internal (string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText) ;

    public Vector2 GetCursorPosition_Internal (string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, float wordWrapWidth, bool richText, Rect screenRect, int cursorPosition) {
        Vector2 result;
        INTERNAL_CALL_GetCursorPosition_Internal ( this, text, font, fontSize, fontStyle, anchor, wordWrapWidth, richText, ref screenRect, cursorPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetCursorPosition_Internal (StylePainter self, string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, float wordWrapWidth, bool richText, ref Rect screenRect, int cursorPosition, out Vector2 value);
}


}
