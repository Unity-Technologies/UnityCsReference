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
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using TierGraphicsSettingsEditorScript = UnityEditor.Rendering.TierSettings;




namespace UnityEditor.Rendering
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AlbedoSwatchInfo
{
    public string name;
    public Color color;
    public float minLuminance;
    public float maxLuminance;
}

public sealed partial class EditorGraphicsSettings
{
    internal static void SetTierSettingsImpl (BuildTargetGroup target, GraphicsTier tier, TierGraphicsSettingsEditorScript settings) {
        INTERNAL_CALL_SetTierSettingsImpl ( target, tier, ref settings );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTierSettingsImpl (BuildTargetGroup target, GraphicsTier tier, ref TierGraphicsSettingsEditorScript settings);
    internal static TierGraphicsSettingsEditorScript GetTierSettingsImpl (BuildTargetGroup target, GraphicsTier tier) {
        TierGraphicsSettingsEditorScript result;
        INTERNAL_CALL_GetTierSettingsImpl ( target, tier, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTierSettingsImpl (BuildTargetGroup target, GraphicsTier tier, out TierGraphicsSettingsEditorScript value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void OnUpdateTierSettingsImpl (BuildTargetGroup target, bool shouldReloadShaders) ;

    internal static TierGraphicsSettingsEditorScript GetCurrentTierSettingsImpl () {
        TierGraphicsSettingsEditorScript result;
        INTERNAL_CALL_GetCurrentTierSettingsImpl ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetCurrentTierSettingsImpl (out TierGraphicsSettingsEditorScript value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool AreTierSettingsAutomatic (BuildTargetGroup target, GraphicsTier tier) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void MakeTierSettingsAutomatic (BuildTargetGroup target, GraphicsTier tier, bool automatic) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RegisterUndoForGraphicsSettings () ;

    public extern static AlbedoSwatchInfo[] albedoSwatches
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}


} 
