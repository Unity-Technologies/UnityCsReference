// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using IntPtr = System.IntPtr;
using System;


namespace UnityEngine
{
internal partial class GUIDebugger
{
    public static void LogLayoutEntry (Rect rect, RectOffset margins, GUIStyle style) {
        INTERNAL_CALL_LogLayoutEntry ( ref rect, margins, style );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LogLayoutEntry (ref Rect rect, RectOffset margins, GUIStyle style);
    public static void LogLayoutGroupEntry (Rect rect, RectOffset margins, GUIStyle style, bool isVertical) {
        INTERNAL_CALL_LogLayoutGroupEntry ( ref rect, margins, style, isVertical );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LogLayoutGroupEntry (ref Rect rect, RectOffset margins, GUIStyle style, bool isVertical);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LogLayoutEndGroup () ;

    public static void LogBeginProperty (string targetTypeAssemblyQualifiedName, string path, Rect position) {
        INTERNAL_CALL_LogBeginProperty ( targetTypeAssemblyQualifiedName, path, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LogBeginProperty (string targetTypeAssemblyQualifiedName, string path, ref Rect position);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LogEndProperty () ;

}


}
