// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using Boo.Lang;

namespace UnityEditor
{


public enum ReplacePrefabOptions
{
    
    Default = 0,
    
    ConnectToPrefab = 1,
    
    ReplaceNameBased = 2,
}

public enum PrefabType
{
    
    None = 0,
    
    Prefab = 1,
    
    ModelPrefab = 2,
    
    PrefabInstance = 3,
    
    ModelPrefabInstance = 4,
    
    MissingPrefabInstance = 5,
    
    DisconnectedPrefabInstance = 6,
    
    DisconnectedModelPrefabInstance = 7
}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class PropertyModification
{
    public Object target;
    public string propertyPath;
    public string value;
    public Object objectReference;
    
    
    
    
}

public sealed partial class PrefabUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetPrefabParent (Object source) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetPrefabObject (Object targetObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  PropertyModification[] GetPropertyModifications (Object targetPrefab) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetPropertyModifications (Object targetPrefab, PropertyModification[] modifications) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object InstantiateAttachedAsset (Object targetObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RecordPrefabInstancePropertyModifications (Object targetObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void MergeAllPrefabInstances (Object targetObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DisconnectPrefabInstance (Object targetObject) ;

    
    public static Object InstantiatePrefab(Object target)
        {
            return InternalInstantiatePrefab(target, EditorSceneManager.GetTargetSceneForNewGameObjects());
        }
    
    public static Object InstantiatePrefab(Object target, Scene destinationScene)
        {
            return InternalInstantiatePrefab(target, destinationScene);
        }
    
    
    private static Object InternalInstantiatePrefab (Object target, Scene destinationScene) {
        return INTERNAL_CALL_InternalInstantiatePrefab ( target, ref destinationScene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_InternalInstantiatePrefab (Object target, ref Scene destinationScene);
    public static Object CreateEmptyPrefab(string path)
        {
            if (!Utils.Paths.IsValidAssetPathWithErrorLogging(path, ".prefab"))
                return null;

            return Internal_CreateEmptyPrefab(path);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Object Internal_CreateEmptyPrefab (string path) ;

    [uei.ExcludeFromDocs]
public static GameObject CreatePrefab (string path, GameObject go) {
    ReplacePrefabOptions options = ReplacePrefabOptions.Default;
    return CreatePrefab ( path, go, options );
}

public static GameObject CreatePrefab(string path, GameObject go, [uei.DefaultValue("ReplacePrefabOptions.Default")]  ReplacePrefabOptions options )
        {
            if (!Utils.Paths.IsValidAssetPathWithErrorLogging(path, ".prefab"))
                return null;

            return Internal_CreatePrefab(path, go, options);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  GameObject Internal_CreatePrefab (string path, GameObject go, [uei.DefaultValue("ReplacePrefabOptions.Default")]  ReplacePrefabOptions options ) ;

    [uei.ExcludeFromDocs]
    private static GameObject Internal_CreatePrefab (string path, GameObject go) {
        ReplacePrefabOptions options = ReplacePrefabOptions.Default;
        return Internal_CreatePrefab ( path, go, options );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject ReplacePrefab (GameObject go, Object targetPrefab, [uei.DefaultValue("ReplacePrefabOptions.Default")]  ReplacePrefabOptions options ) ;

    [uei.ExcludeFromDocs]
    public static GameObject ReplacePrefab (GameObject go, Object targetPrefab) {
        ReplacePrefabOptions options = ReplacePrefabOptions.Default;
        return ReplacePrefab ( go, targetPrefab, options );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject ConnectGameObjectToPrefab (GameObject go, GameObject sourcePrefab) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject FindRootGameObjectWithSameParentPrefab (GameObject target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject FindValidUploadPrefabInstanceRoot (GameObject target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ReconnectToLastPrefab (GameObject go) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ResetToPrefabState (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsComponentAddedToPrefabInstance (Object source) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RevertPrefabInstance (GameObject go) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  PrefabType GetPrefabType (Object target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject FindPrefabRoot (GameObject source) ;

    public delegate void PrefabInstanceUpdated(GameObject instance);
    public static PrefabInstanceUpdated prefabInstanceUpdated;
    private static void Internal_CallPrefabInstanceUpdated(GameObject instance)
        {
            if (prefabInstanceUpdated != null)
                prefabInstanceUpdated(instance);
        }
    
    
}

}
