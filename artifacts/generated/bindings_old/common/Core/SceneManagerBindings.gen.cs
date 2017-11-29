// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine.Events;

namespace UnityEngine.SceneManagement
{



    public enum LoadSceneMode { Single, Additive };


[RequiredByNativeCode]
public partial class SceneManager
{
    public extern static int sceneCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static int sceneCountInBuildSettings
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Scene GetActiveScene () {
        Scene result;
        INTERNAL_CALL_GetActiveScene ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetActiveScene (out Scene value);
    public static bool SetActiveScene (Scene scene) {
        return INTERNAL_CALL_SetActiveScene ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SetActiveScene (ref Scene scene);
    public static Scene GetSceneByPath (string scenePath) {
        Scene result;
        INTERNAL_CALL_GetSceneByPath ( scenePath, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSceneByPath (string scenePath, out Scene value);
    public static Scene GetSceneByName (string name) {
        Scene result;
        INTERNAL_CALL_GetSceneByName ( name, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSceneByName (string name, out Scene value);
    public static Scene GetSceneByBuildIndex (int buildIndex) {
        Scene result;
        INTERNAL_CALL_GetSceneByBuildIndex ( buildIndex, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSceneByBuildIndex (int buildIndex, out Scene value);
    public static Scene GetSceneAt (int index) {
        Scene result;
        INTERNAL_CALL_GetSceneAt ( index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSceneAt (int index, out Scene value);
    [System.Obsolete ("Use SceneManager.sceneCount and SceneManager.GetSceneAt(int index) to loop the all scenes instead.")]
static public Scene[] GetAllScenes()
        {
            var scenes = new Scene[sceneCount];
            for (int index = 0; index < sceneCount; ++index)
            {
                scenes[index] = GetSceneAt(index);
            }
            return scenes;
        }
    
    
    
    [uei.ExcludeFromDocs]
public static void LoadScene (string sceneName) {
    LoadSceneMode mode = LoadSceneMode.Single;
    LoadScene ( sceneName, mode );
}

public static void LoadScene(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")]  LoadSceneMode mode )
        {
            LoadSceneAsyncNameIndexInternal(sceneName, -1, mode == LoadSceneMode.Additive ? true : false, true);
        }

    
    [uei.ExcludeFromDocs]
public static void LoadScene (int sceneBuildIndex) {
    LoadSceneMode mode = LoadSceneMode.Single;
    LoadScene ( sceneBuildIndex, mode );
}

public static void LoadScene(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")]  LoadSceneMode mode )
        {
            LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, mode == LoadSceneMode.Additive ? true : false, true);
        }

    
    [uei.ExcludeFromDocs]
public static AsyncOperation LoadSceneAsync (string sceneName) {
    LoadSceneMode mode = LoadSceneMode.Single;
    return LoadSceneAsync ( sceneName, mode );
}

public static AsyncOperation LoadSceneAsync(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")]  LoadSceneMode mode )
        {
            return LoadSceneAsyncNameIndexInternal(sceneName, -1, mode == LoadSceneMode.Additive ? true : false, false);
        }

    
    [uei.ExcludeFromDocs]
public static AsyncOperation LoadSceneAsync (int sceneBuildIndex) {
    LoadSceneMode mode = LoadSceneMode.Single;
    return LoadSceneAsync ( sceneBuildIndex, mode );
}

public static AsyncOperation LoadSceneAsync(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")]  LoadSceneMode mode )
        {
            return LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, mode == LoadSceneMode.Additive ? true : false, false);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AsyncOperation LoadSceneAsyncNameIndexInternal (string sceneName, int sceneBuildIndex, bool isAdditive, bool mustCompleteNextFrame) ;

    public static Scene CreateScene (string sceneName) {
        Scene result;
        INTERNAL_CALL_CreateScene ( sceneName, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreateScene (string sceneName, out Scene value);
    [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
    static public bool UnloadScene(Scene scene)
        {
            return UnloadSceneInternal(scene);
        }
    
    
    private static bool UnloadSceneInternal (Scene scene) {
        return INTERNAL_CALL_UnloadSceneInternal ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_UnloadSceneInternal (ref Scene scene);
    [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
    static public bool UnloadScene(int sceneBuildIndex)
        {
            bool success;
            UnloadSceneNameIndexInternal("", sceneBuildIndex, true, out success);
            return success;
        }
    
    
    [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
    static public bool UnloadScene(string sceneName)
        {
            bool success;
            UnloadSceneNameIndexInternal(sceneName, -1, true, out success);
            return success;
        }
    
    static public AsyncOperation UnloadSceneAsync(int sceneBuildIndex)
        {
            bool success;
            return UnloadSceneNameIndexInternal("", sceneBuildIndex, false, out success);
        }
    
    static public AsyncOperation UnloadSceneAsync(string sceneName)
        {
            bool success;
            return UnloadSceneNameIndexInternal(sceneName, -1, false, out success);
        }
    
    static public AsyncOperation UnloadSceneAsync(Scene scene)
        {
            return UnloadSceneAsyncInternal(scene);
        }
    
    
    private static AsyncOperation UnloadSceneAsyncInternal (Scene scene) {
        return INTERNAL_CALL_UnloadSceneAsyncInternal ( ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AsyncOperation INTERNAL_CALL_UnloadSceneAsyncInternal (ref Scene scene);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AsyncOperation UnloadSceneNameIndexInternal (string sceneName, int sceneBuildIndex, bool immediately, out bool outSuccess) ;

    public static void MergeScenes (Scene sourceScene, Scene destinationScene) {
        INTERNAL_CALL_MergeScenes ( ref sourceScene, ref destinationScene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MergeScenes (ref Scene sourceScene, ref Scene destinationScene);
    public static void MoveGameObjectToScene (GameObject go, Scene scene) {
        INTERNAL_CALL_MoveGameObjectToScene ( go, ref scene );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveGameObjectToScene (GameObject go, ref Scene scene);
    
            public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;
    
    
    [RequiredByNativeCode]
    private static void Internal_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (sceneLoaded != null)
            {
                sceneLoaded(scene, mode);
            }
        }
    
            public static event UnityAction<Scene> sceneUnloaded;
    
    
    [RequiredByNativeCode]
    private static void Internal_SceneUnloaded(Scene scene)
        {
            if (sceneUnloaded != null)
            {
                sceneUnloaded(scene);
            }
        }
    
            public static event UnityAction<Scene, Scene> activeSceneChanged;
    
    
    [RequiredByNativeCode]
    private static void Internal_ActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            if (activeSceneChanged != null)
            {
                activeSceneChanged(previousActiveScene, newActiveScene);
            }
        }
    
    
}


}  
