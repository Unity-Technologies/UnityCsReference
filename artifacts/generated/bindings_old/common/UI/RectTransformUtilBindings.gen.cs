// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEngine
{


public sealed partial class RectTransformUtility
{
    public static bool RectangleContainsScreenPoint (RectTransform rect, Vector2 screenPoint, Camera cam) {
        return INTERNAL_CALL_RectangleContainsScreenPoint ( rect, ref screenPoint, cam );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_RectangleContainsScreenPoint (RectTransform rect, ref Vector2 screenPoint, Camera cam);
    public static Vector2 PixelAdjustPoint (Vector2 point, Transform elementTransform, Canvas canvas) {
        Vector2 result;
        INTERNAL_CALL_PixelAdjustPoint ( ref point, elementTransform, canvas, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PixelAdjustPoint (ref Vector2 point, Transform elementTransform, Canvas canvas, out Vector2 value);
    public static Rect PixelAdjustRect (RectTransform rectTransform, Canvas canvas) {
        Rect result;
        INTERNAL_CALL_PixelAdjustRect ( rectTransform, canvas, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PixelAdjustRect (RectTransform rectTransform, Canvas canvas, out Rect value);
}


}
