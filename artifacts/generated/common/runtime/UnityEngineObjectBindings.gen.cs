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


[Flags]
public enum HideFlags
{
    
    None = 0,
    
    HideInHierarchy = 1,
    
    HideInInspector = 2,
    
    DontSaveInEditor = 4,
    
    NotEditable = 8,
    
    DontSaveInBuild = 16,
    
    DontUnloadUnusedAsset = 32,
    DontSave = 4 + 16 + 32,
    
    HideAndDontSave = 1 + 4 + 8 + 16 + 32
}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public partial class Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Object Internal_CloneSingle (Object data) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Object Internal_CloneSingleWithParent (Object data, Transform parent, bool worldPositionStays) ;

    private static Object Internal_InstantiateSingle (Object data, Vector3 pos, Quaternion rot) {
        return INTERNAL_CALL_Internal_InstantiateSingle ( data, ref pos, ref rot );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_Internal_InstantiateSingle (Object data, ref Vector3 pos, ref Quaternion rot);
    private static Object Internal_InstantiateSingleWithParent (Object data, Transform parent, Vector3 pos, Quaternion rot) {
        return INTERNAL_CALL_Internal_InstantiateSingleWithParent ( data, parent, ref pos, ref rot );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_Internal_InstantiateSingleWithParent (Object data, Transform parent, ref Vector3 pos, ref Quaternion rot);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetOffsetOfInstanceIDInCPlusPlusObject () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void EnsureRunningOnMainThread () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Destroy (Object obj, [uei.DefaultValue("0.0F")]  float t ) ;

    [uei.ExcludeFromDocs]
    public static void Destroy (Object obj) {
        float t = 0.0F;
        Destroy ( obj, t );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DestroyImmediate (Object obj, [uei.DefaultValue("false")]  bool allowDestroyingAssets ) ;

    [uei.ExcludeFromDocs]
    public static void DestroyImmediate (Object obj) {
        bool allowDestroyingAssets = false;
        DestroyImmediate ( obj, allowDestroyingAssets );
    }

    [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] FindObjectsOfType (Type type) ;

    public extern  string name
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DontDestroyOnLoad (Object target) ;

    public extern HideFlags hideFlags
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DestroyObject (Object obj, [uei.DefaultValue("0.0F")]  float t ) ;

    [uei.ExcludeFromDocs]
    public static void DestroyObject (Object obj) {
        float t = 0.0F;
        DestroyObject ( obj, t );
    }

    [System.Obsolete ("use Object.FindObjectsOfType instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] FindSceneObjectsOfType (Type type) ;

    [System.Obsolete ("use Resources.FindObjectsOfTypeAll instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] FindObjectsOfTypeIncludingAssets (Type type) ;

    [System.Obsolete ("Please use Resources.FindObjectsOfTypeAll instead")]
public static Object[] FindObjectsOfTypeAll(Type type) { return Resources.FindObjectsOfTypeAll(type); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public override string ToString () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool DoesObjectWithInstanceIDExist (int instanceID) ;

}

}
