// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineInternal;

namespace UnityEngine
{


public partial class GUILayoutUtility
{
    private static Rect Internal_GetWindowRect (int windowID) {
        Rect result;
        INTERNAL_CALL_Internal_GetWindowRect ( windowID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetWindowRect (int windowID, out Rect value);
    private static void Internal_MoveWindow (int windowID, Rect r) {
        INTERNAL_CALL_Internal_MoveWindow ( windowID, ref r );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_MoveWindow (int windowID, ref Rect r);
    internal static Rect GetWindowsBounds () {
        Rect result;
        INTERNAL_CALL_GetWindowsBounds ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetWindowsBounds (out Rect value);
}



}
