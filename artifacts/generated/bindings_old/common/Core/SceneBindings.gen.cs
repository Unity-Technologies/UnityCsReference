// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System.Collections.Generic;

namespace UnityEngine.SceneManagement
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Scene
{
    
            internal enum LoadingState
        {
            NotLoaded = 0,

            Loading,

            Loaded
        }
    
    
    
            private int m_Handle;
    
            internal int handle { get { return m_Handle; } }
            internal Scene.LoadingState loadingState { get { return GetLoadingStateInternal(handle); } }
    
    public bool IsValid() { return IsValidInternal(handle); }
            public string path { get { return GetPathInternal(handle); } }
            public string name { get { return GetNameInternal(handle); } internal set {SetNameInternal(handle, value); }}
            internal string guid { get { return GetGUIDInternal(handle); }}
            public bool isLoaded { get { return GetIsLoadedInternal(handle); }}
            public int buildIndex { get { return GetBuildIndexInternal(handle); }}
            public bool isDirty { get { return GetIsDirtyInternal(handle); }}
            public int rootCount { get {return GetRootCountInternal(handle); }}
    
    public GameObject[] GetRootGameObjects()
        {
            var rootGameObjects = new List<GameObject>(rootCount);
            GetRootGameObjects(rootGameObjects);

            return rootGameObjects.ToArray();
        }
    
    public void GetRootGameObjects(List<GameObject> rootGameObjects)
        {
            if (rootGameObjects.Capacity < rootCount)
                rootGameObjects.Capacity = rootCount;

            rootGameObjects.Clear();

            if (!IsValid())
                throw new System.ArgumentException("The scene is invalid.");

            if (!Application.isPlaying && !isLoaded)
                throw new System.ArgumentException("The scene is not loaded.");

            if (rootCount == 0)
                return;

            GetRootGameObjectsInternal(handle, rootGameObjects);
        }
    
            public static bool operator==(Scene lhs, Scene rhs) { return lhs.handle == rhs.handle; }
            public static bool operator!=(Scene lhs, Scene rhs) { return lhs.handle != rhs.handle; }
    public override int GetHashCode() { return m_Handle; }
    public override bool Equals(object other)
        {
            if (!(other is Scene))
                return false;

            Scene rhs = (Scene)other;
            return handle == rhs.handle;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool IsValidInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetPathInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetNameInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetNameInternal (int sceneHandle, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetGUIDInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetIsLoadedInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Scene.LoadingState GetLoadingStateInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetIsDirtyInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetBuildIndexInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetRootCountInternal (int sceneHandle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetRootGameObjectsInternal (int sceneHandle, object resultRootList) ;

}


}  
