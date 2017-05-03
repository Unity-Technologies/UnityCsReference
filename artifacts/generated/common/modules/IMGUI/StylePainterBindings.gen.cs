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

    public void DrawRect_Internal (Rect screenRect, Color color, float borderWidth, float borderRadius) {
        INTERNAL_CALL_DrawRect_Internal ( this, ref screenRect, ref color, borderWidth, borderRadius );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawRect_Internal (StylePainter self, ref Rect screenRect, ref Color color, float borderWidth, float borderRadius);
    public void DrawTexture_Internal (Rect screenRect, Texture texture, Rect sourceRect, Color color, float borderWidth, float borderRadius, int leftBorder, int topBorder, int rightBorder, int bottomBorder) {
        INTERNAL_CALL_DrawTexture_Internal ( this, ref screenRect, texture, ref sourceRect, ref color, borderWidth, borderRadius, leftBorder, topBorder, rightBorder, bottomBorder );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawTexture_Internal (StylePainter self, ref Rect screenRect, Texture texture, ref Rect sourceRect, ref Color color, float borderWidth, float borderRadius, int leftBorder, int topBorder, int rightBorder, int bottomBorder);
    public void DrawText_Internal (Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping textClipping) {
        INTERNAL_CALL_DrawText_Internal ( this, ref screenRect, text, font, fontSize, fontStyle, ref fontColor, anchor, wordWrap, wordWrapWidth, richText, textClipping );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawText_Internal (StylePainter self, ref Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, ref Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping textClipping);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float ComputeTextWidth (string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float ComputeTextHeight (string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText) ;

}


}
