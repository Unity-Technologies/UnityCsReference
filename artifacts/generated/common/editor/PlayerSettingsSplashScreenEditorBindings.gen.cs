// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
internal sealed partial class PlayerSettingsSplashScreenEditor
{
    public extern static bool licenseAllowsDisabling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal static Color GetSplashScreenActualBackgroundColor () {
        Color result;
        INTERNAL_CALL_GetSplashScreenActualBackgroundColor ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSplashScreenActualBackgroundColor (out Color value);
    internal static Texture2D GetSplashScreenActualBackgroundImage (Rect windowRect) {
        return INTERNAL_CALL_GetSplashScreenActualBackgroundImage ( ref windowRect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetSplashScreenActualBackgroundImage (ref Rect windowRect);
    internal static Rect GetSplashScreenActualUVs (Rect windowRect) {
        Rect result;
        INTERNAL_CALL_GetSplashScreenActualUVs ( ref windowRect, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSplashScreenActualUVs (ref Rect windowRect, out Rect value);
}

}
