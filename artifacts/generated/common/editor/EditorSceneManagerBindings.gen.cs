// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{



    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading };
    public enum NewSceneMode { Single, Additive };
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects };


public sealed partial class EditorSceneManager : SceneManager
{
    public extern static int loadedSceneCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Scene OpenScene (string scenePath, [uei.DefaultValue("OpenSceneMode.Single")]  OpenSceneMode mode ) {
        Scene result;
        INTERNAL_CALL_OpenScene ( scenePath, mode, out result );
        return result;
    }

    [uei.ExcludeFromDocs]
    public static Scene OpenScene (string scenePath) {
        OpenSceneMode mode = OpenSceneMode.Single;
        Scene result;
        INTERNAL_CALL_OpenScene ( scenePath, mode, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_OpenScene (string scenePath, OpenSceneMode mode, out Scene value);
    public static Scene NewScene (NewSceneSetup setup, [uei.DefaultValue("NewSceneMode.Single")]  NewSceneMode mode ) {
        Scene result;
        INTERNAL_CALL_NewScene ( setup, mode, out result );
        return result;
    }

    [uei.ExcludeFromDocs]
    public static Scene NewScene (NewSceneSetup setup) {
        NewSceneMode mode = NewSceneMode.Single;
        Scene result;
        INTERNAL_CALL_NewScene ( setup, mode, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewScene (NewSceneSetup setup, NewSceneMode mode, out Scene value);
    public static Scene NewPreviewScene () {
        Scene result;
        INTERNAL_CALL_NewPreviewScene ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewPreviewScene (out Scene value);
    static internal bool CreateSceneAsset(string scenePath, bool createDefaultGameObjects)
        {
            if (!Utils.Paths.IsValidAssetPathWithErrorLogging(scenePath, ".unity"))
                return false;

            return Internal_CreateSceneAsset(scenePath, createDefaultGameObjects);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool Internal_CreateSceneAsset (string scenePath, bool createDefaultGameObjects) ;

    public static bool CloseScene (Scene scene, bool removeScene) {
        return INTERNAL_CALL_CloseScene ( ref scene, removeScene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CloseScene (ref Scene scene, bool removeScene);
    public static bool ClosePreviewScene (Scene scene) {
        return INTERNAL_CALL_ClosePreviewScene ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ClosePreviewScene (ref Scene scene);
    internal static bool ReloadScene (Scene scene) {
        return INTERNAL_CALL_ReloadScene ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ReloadScene (ref Scene scene);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetTargetSceneForNewGameObjects (int sceneHandle) ;

    internal static Scene GetTargetSceneForNewGameObjects () {
        Scene result;
        INTERNAL_CALL_GetTargetSceneForNewGameObjects ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTargetSceneForNewGameObjects (out Scene value);
    internal static Scene GetSceneByHandle (int handle) {
        Scene result;
        INTERNAL_CALL_GetSceneByHandle ( handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSceneByHandle (int handle, out Scene value);
    public static void MoveSceneBefore (Scene src, Scene dst) {
        INTERNAL_CALL_MoveSceneBefore ( ref src, ref dst );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveSceneBefore (ref Scene src, ref Scene dst);
    public static void MoveSceneAfter (Scene src, Scene dst) {
        INTERNAL_CALL_MoveSceneAfter ( ref src, ref dst );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveSceneAfter (ref Scene src, ref Scene dst);
    internal static bool SaveSceneAs (Scene scene) {
        return INTERNAL_CALL_SaveSceneAs ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SaveSceneAs (ref Scene scene);
    [uei.ExcludeFromDocs]
public static bool SaveScene (Scene scene, string dstScenePath ) {
    bool saveAsCopy = false;
    return SaveScene ( scene, dstScenePath, saveAsCopy );
}

[uei.ExcludeFromDocs]
public static bool SaveScene (Scene scene) {
    bool saveAsCopy = false;
    string dstScenePath = "";
    return SaveScene ( scene, dstScenePath, saveAsCopy );
}

public static bool SaveScene(Scene scene, [uei.DefaultValue("\"\"")]  string dstScenePath , [uei.DefaultValue("false")]  bool saveAsCopy )
        {
            if (!string.IsNullOrEmpty(dstScenePath))
                if (!Utils.Paths.IsValidAssetPathWithErrorLogging(dstScenePath, ".unity"))
                    return false;

            return Internal_SaveScene(scene, dstScenePath, saveAsCopy);
        }

    
    
    private static bool Internal_SaveScene (Scene scene, string dstScenePath, bool saveAsCopy) {
        return INTERNAL_CALL_Internal_SaveScene ( ref scene, dstScenePath, saveAsCopy );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_SaveScene (ref Scene scene, string dstScenePath, bool saveAsCopy);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SaveOpenScenes () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SaveScenes (Scene[] scenes) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SaveCurrentModifiedScenesIfUserWantsTo () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SaveModifiedScenesIfUserWantsTo (Scene[] scenes) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool EnsureUntitledSceneHasBeenSaved (string dialogContent) ;

    public static bool MarkSceneDirty (Scene scene) {
        return INTERNAL_CALL_MarkSceneDirty ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_MarkSceneDirty (ref Scene scene);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void MarkAllScenesDirty () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  SceneSetup[] GetSceneManagerSetup () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RestoreSceneManagerSetup (SceneSetup[] value) ;

    public extern static bool preventCrossSceneReferences
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static bool DetectCrossSceneReferences (Scene scene) {
        return INTERNAL_CALL_DetectCrossSceneReferences ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_DetectCrossSceneReferences (ref Scene scene);
    public extern static SceneAsset playModeStartScene
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    
            internal static UnityAction<Scene, NewSceneMode> sceneWasCreated;
            internal static UnityAction<Scene, OpenSceneMode> sceneWasOpened;
    
    private static void Internal_NewSceneWasCreated(Scene scene, NewSceneMode mode)
        {
            if (sceneWasCreated != null)
                sceneWasCreated(scene, mode);
        }
    
    private static void Internal_SceneWasOpened(Scene scene, OpenSceneMode mode)
        {
            if (sceneWasOpened != null)
                sceneWasOpened(scene, mode);
        }
    
    
}


}
