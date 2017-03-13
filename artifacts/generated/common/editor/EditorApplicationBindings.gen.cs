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

namespace UnityEditor
{


public sealed partial class EditorApplication
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LoadLevelInPlayMode (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LoadLevelAdditiveInPlayMode (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AsyncOperation LoadLevelAsyncInPlayMode (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AsyncOperation LoadLevelAdditiveAsyncInPlayMode (string path) ;

    public static void OpenProject(string projectPath, params string[] args)
        {
            OpenProjectInternal(projectPath, args);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void OpenProjectInternal (string projectPath, string[] args) ;

    [System.Obsolete ("Use AssetDatabase.SaveAssets instead (UnityUpgradable) -> AssetDatabase.SaveAssets()", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SaveAssets () ;

    public extern static bool isPlaying
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool isPlayingOrWillChangePlaymode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Step () ;

    public extern static bool isPaused
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool isCompiling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isUpdating
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isRemoteConnected
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LockReloadAssemblies () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ExecuteMenuItem (string menuItemPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool ExecuteMenuItemOnGameObjects (string menuItemPath, GameObject[] objects) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool ExecuteMenuItemWithTemporaryContext (string menuItemPath, Object[] objects) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnlockReloadAssemblies () ;

    [ThreadAndSerializationSafe ()]
    public extern static string applicationContentsPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string applicationPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern static string userJavascriptPackagesPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isTemporaryProject
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetTemporaryProjectKeepPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Exit (int returnValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetSceneRepaintDirty () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void UpdateSceneIfNeeded () ;

    public delegate void ProjectWindowItemCallback(string guid, Rect selectionRect);
    
    
    public static ProjectWindowItemCallback projectWindowItemOnGUI;
    
    
    public static void RepaintProjectWindow()
        {
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }
    
    
    public static void RepaintAnimationWindow()
        {
            foreach (AnimEditor animEditor in AnimEditor.GetAllAnimationWindows())
                animEditor.Repaint();
        }
    
    
    public delegate void HierarchyWindowItemCallback(int instanceID, Rect selectionRect);
    
    
    public static HierarchyWindowItemCallback hierarchyWindowItemOnGUI;
    
    
    public static void RepaintHierarchyWindow()
        {
            foreach (SceneHierarchyWindow pw in Resources.FindObjectsOfTypeAll(typeof(SceneHierarchyWindow)))
                pw.Repaint();
        }
    
    
    public static void DirtyHierarchyWindowSorting()
        {
            foreach (SceneHierarchyWindow pw in Resources.FindObjectsOfTypeAll(typeof(SceneHierarchyWindow)))
                pw.DirtySortingMethods();
        }
    
    
    public delegate void CallbackFunction();
    
    
    public delegate void SerializedPropertyCallbackFunction(GenericMenu menu, SerializedProperty property);
    
    
    public static CallbackFunction update;
    
    
    public static CallbackFunction delayCall;
    
    
    public static CallbackFunction hierarchyWindowChanged;
    
    
    public static CallbackFunction projectWindowChanged;
    
    
    public static CallbackFunction searchChanged;
    
    
    internal static CallbackFunction assetLabelsChanged;
    
    
    internal static CallbackFunction assetBundleNameChanged;
    
    
    public static CallbackFunction modifierKeysChanged;
    
    
    public static CallbackFunction playmodeStateChanged;
    
    
    internal static CallbackFunction globalEventHandler;
    
    
    internal static CallbackFunction windowsReordered;
    
    
    public static SerializedPropertyCallbackFunction contextualPropertyMenu;
    
    
    private static void Internal_CallUpdateFunctions()
        {
            if (update != null)
                update();
        }
    
    
    private static void Internal_CallDelayFunctions()
        {
            CallbackFunction delay = delayCall;
            delayCall = null;

            if (delay != null)
                delay();
        }
    
    
    private static void Internal_SwitchSkin()
        {
            EditorGUIUtility.Internal_SwitchSkin();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RequestRepaintAllViews () ;

    private static void Internal_CallHierarchyWindowHasChanged()
        {
            if (hierarchyWindowChanged != null)
                hierarchyWindowChanged();
        }
    
    
    private static void Internal_CallProjectWindowHasChanged()
        {
            if (projectWindowChanged != null)
                projectWindowChanged();
        }
    
    
    internal static void Internal_CallSearchHasChanged()
        {
            if (searchChanged != null)
                searchChanged();
        }
    
    
    internal static void Internal_CallAssetLabelsHaveChanged()
        {
            if (assetLabelsChanged != null)
                assetLabelsChanged();
        }
    
    
    internal static void Internal_CallAssetBundleNameChanged()
        {
            if (assetBundleNameChanged != null)
                assetBundleNameChanged();
        }
    
    
    internal static void CallDelayed(CallbackFunction function, float timeFromNow)
        {
            delayedCallback = function;
            s_DelayedCallbackTime = Time.realtimeSinceStartup + timeFromNow;
            update += CheckCallDelayed;
        }
    
    
    static CallbackFunction delayedCallback;
    static float s_DelayedCallbackTime = 0.0f;
    
    static void CheckCallDelayed()
        {
            if (Time.realtimeSinceStartup > s_DelayedCallbackTime)
            {
                update -= CheckCallDelayed;
                delayedCallback();
            }
        }
    
    
    private static void Internal_PlaymodeStateChanged()
        {
            if (playmodeStateChanged != null)
                playmodeStateChanged();
        }
    
    
    private static void Internal_CallKeyboardModifiersChanged()
        {
            if (modifierKeysChanged != null)
                modifierKeysChanged();
        }
    
    
    private static void Internal_CallWindowsReordered()
        {
            if (windowsReordered != null)
                windowsReordered();
        }
    
    
    [RequiredByNativeCode]
    private static void Internal_CallGlobalEventHandler()
        {
            if (globalEventHandler != null)
                globalEventHandler();

            WindowLayout.MaximizeKeyHandler();

            Event.current = null;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Beep () ;

    internal extern static Object tagManager
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern static Object renderSettings
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static double timeSinceStartup
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

}
