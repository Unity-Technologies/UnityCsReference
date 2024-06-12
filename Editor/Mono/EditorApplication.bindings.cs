// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Main Application class.
    [NativeHeader("Editor/Mono/EditorApplication.bindings.h")]
    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [NativeHeader("Runtime/Input/TimeManager.h")]
    [NativeHeader("Editor/Src/ProjectVersion.h")]
    [NativeHeader("Runtime/Misc/BuildSettings.h")]
    [StaticAccessor("EditorApplicationBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class EditorApplication
    {
        internal static extern string kLastOpenedScene
        {
            [NativeMethod("GetLastOpenedScenePref")]
            get;
        }

        // Load the level at /path/ in play mode.
        [Obsolete("Use EditorSceneManager.LoadSceneInPlayMode instead.")]
        public static void LoadLevelInPlayMode(string path)
        {
            LoadSceneParameters parameters = new LoadSceneParameters {loadSceneMode = LoadSceneMode.Single};
            EditorSceneManager.LoadSceneInPlayMode(path, parameters);
        }

        // Load the level at /path/ additively in play mode.
        [Obsolete("Use EditorSceneManager.LoadSceneInPlayMode instead.")]
        public static void LoadLevelAdditiveInPlayMode(string path)
        {
            LoadSceneParameters parameters = new LoadSceneParameters {loadSceneMode = LoadSceneMode.Additive};
            EditorSceneManager.LoadSceneInPlayMode(path, parameters);
        }

        // Load the level at /path/ in play mode asynchronously.
        [Obsolete("Use EditorSceneManager.LoadSceneAsyncInPlayMode instead.")]
        public static AsyncOperation LoadLevelAsyncInPlayMode(string path)
        {
            LoadSceneParameters parameters = new LoadSceneParameters {loadSceneMode = LoadSceneMode.Single};
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
        }

        // Load the level at /path/ additively in play mode asynchronously.
        [Obsolete("Use EditorSceneManager.LoadSceneAsyncInPlayMode instead.")]
        public static AsyncOperation LoadLevelAdditiveAsyncInPlayMode(string path)
        {
            LoadSceneParameters parameters = new LoadSceneParameters {loadSceneMode = LoadSceneMode.Additive};
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
        }

        // Open another project.
        public static void OpenProject(string projectPath, params string[] args)
        {
            OpenProjectInternal(projectPath, args);
        }

        private static extern void OpenProjectInternal(string projectPath, string[] args);

        internal static extern void OpenFileGeneric(string path);

        // Saves all serializable assets that have not yet been written to disk (eg. Materials)
        [System.Obsolete("Use AssetDatabase.SaveAssets instead (UnityUpgradable) -> AssetDatabase.SaveAssets()", true)]
        public static void SaveAssets() {}

        // Is editor currently in play mode?
        public static extern bool isPlaying
        {
            get;
            set;
        }

        public static void EnterPlaymode()
        {
            isPlaying = true;
        }

        public static void ExitPlaymode()
        {
            isPlaying = false;
        }

        // Is editor either currently in play mode, or about to switch to it? (RO)
        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        public static extern bool isPlayingOrWillChangePlaymode
        {
            [NativeMethod("IsPlayingOrWillEnterExitPlaymode")]
            get;
        }

        // Perform a single frame step.
        [StaticAccessor("GetApplication().GetPlayerLoopController()", StaticAccessorType.Dot)]
        public static extern void Step();

        // Is editor currently paused?
        [StaticAccessor("GetApplication().GetPlayerLoopController()", StaticAccessorType.Dot)]
        public static extern bool isPaused
        {
            [NativeMethod("IsPaused")]
            get;
            [NativeMethod("SetPaused")]
            set;
        }

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern bool IsInitialized();

        // Is editor currently compiling scripts? (RO)
        public static bool isCompiling => EditorCompilationInterface.IsCompiling();

        // Is editor currently updating? (RO)
        public static extern bool isUpdating
        {
            get;
        }

        // Is remote connected to a client app?
        public static extern bool isRemoteConnected
        {
            get;
        }

        [Obsolete("ScriptingRuntimeVersion has been deprecated in 2019.3 due to the removal of legacy mono")]
        public static ScriptingRuntimeVersion scriptingRuntimeVersion
        {
            get { return ScriptingRuntimeVersion.Latest; }
        }

        [StaticAccessor("ScriptingManager", StaticAccessorType.DoubleColon)]
        internal static extern bool useLibmonoBackendForIl2cpp
        {
            [NativeName("UseLibmonoBackendForIl2cpp")]
            get;
        }

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern string GetLicenseType();

        // Prevents loading of assemblies when it is inconvenient.
        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        public static extern void LockReloadAssemblies();

        //  Must be called after LockReloadAssemblies, to reenable loading of assemblies.
        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        public static extern void UnlockReloadAssemblies();

        //  Check if assemblies are unlocked.
        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern bool CanReloadAssemblies();

        private static extern bool ExecuteMenuItemInternal(string menuItemPath, bool logErrorOnUnfoundItem);

        // Invokes the menu item in the specified path.
        public static bool ExecuteMenuItem(string menuItemPath)
        {
            var sanitizedPath = MenuService.SanitizeMenuItemName(menuItemPath);
            var isDefaultMode = ModeService.currentId == ModeService.k_DefaultModeId;
            var result = ExecuteMenuItemInternal(sanitizedPath, isDefaultMode);
            if (result)
                return true;

            if (!isDefaultMode)
            {
                // Check if the menu was found first. If so, it means that the menu just
                // didn't pass validation. No need to check further.
                if (Menu.MenuItemExists(sanitizedPath))
                    return false;

                var menuItems = TypeCache.GetMethodsWithAttribute<MenuItem>();
                MethodInfo validateItem = null;
                MethodInfo executeItem = null;
                foreach (var item in menuItems)
                {
                    MenuItem itemData = (MenuItem)item.GetCustomAttributes(typeof(MenuItem), false)[0];
                    var hotKeyIndex = Menu.FindHotkeyStartIndex(itemData.menuItem);
                    var hasHotkey = hotKeyIndex != -1 && hotKeyIndex != itemData.menuItem.Length;
                    bool matches;
                    if (!hasHotkey)
                    {
                        matches = itemData.menuItem == sanitizedPath;
                    }
                    else
                    {
                        var stringView = itemData.menuItem.AsSpan(0, hotKeyIndex);
                        stringView = stringView.TrimEnd();
                        matches = stringView == sanitizedPath;
                    }
                    if (!matches)
                        continue;

                    if (itemData.validate)
                        validateItem = item;
                    else
                        executeItem = item;

                    if (validateItem != null && executeItem != null)
                        break;
                }

                if (validateItem != null)
                {
                    if (!ExecuteMenuItem(validateItem, true))
                        return false;
                }

                if (executeItem != null)
                {
                    return ExecuteMenuItem(executeItem, false);
                }

                Debug.LogError($"ExecuteMenuItem failed because there is no menu named '{menuItemPath}'");
            }

            return false;
        }

        static bool ExecuteMenuItem(MethodInfo menuMethodInfo, bool validate)
        {
            if (menuMethodInfo.GetParameters().Length == 0)
            {
                var result = menuMethodInfo.Invoke(null, new object[0]);
                return !validate || (bool)result;
            }

            if (menuMethodInfo.GetParameters()[0].ParameterType == typeof(MenuCommand))
            {
                var result = menuMethodInfo.Invoke(null, new[] { new MenuCommand(null) });
                return !validate || (bool)result;
            }
            return false;
        }

        // Validates the menu item in the specific path
        internal static extern bool ValidateMenuItem(string menuItemPath);

        // Like ExecuteMenuItem, but applies action to specified GameObjects if the menu action supports it.
        internal static extern  bool ExecuteMenuItemOnGameObjects(string menuItemPath, GameObject[] objects);

        // Like ExecuteMenuItem, but applies action to specified GameObjects if the menu action supports it.
        internal static extern  bool ExecuteMenuItemWithTemporaryContext(string menuItemPath, Object[] objects);

        // Path to the Unity editor contents folder (RO)
        public static extern string applicationContentsPath
        {
            [FreeFunction("GetApplicationContentsPath", IsThreadSafe = true)]
            get;
        }

        // Returns the path to the Unity editor application (RO)
        public static extern string applicationPath
        {
            [FreeFunction("GetApplicationPath", IsThreadSafe = true)]
            get;
        }

        // Returns true if resources are being built
        internal static extern bool isBuildingAnyResources
        {
            [FreeFunction("IsBuildingAnyResources")]
            get;
        }

        // Returns true if the Package Manager is disabled
        internal static extern bool isPackageManagerDisabled
        {
            [FreeFunction("IsPackageManagerDisabled")]
            get;
        }

        internal static extern string userJavascriptPackagesPath
        {
            get;
        }

        public static extern bool isTemporaryProject
        {
            [FreeFunction("IsTemporaryProject")]
            get;
        }

        [NativeThrows]
        public static extern void SetTemporaryProjectKeepPath(string path);

        // Exit the Unity editor application.
        public static extern void Exit(int returnValue);

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern void SetSceneRepaintDirty();

        public static void QueuePlayerLoopUpdate() { SetSceneRepaintDirty(); }

        internal static extern void UpdateSceneIfNeeded();

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern void UpdateMainWindowTitle();

        // Plays system beep sound.
        [FreeFunction("UnityBeep")]
        public static extern void Beep();

        internal static extern Object tagManager
        {
            [FreeFunction]
            get;
        }

        internal static extern Object renderSettings
        {
            [FreeFunction]
            get;
        }

        // The time since the editor was started (RO)
        public static extern double timeSinceStartup
        {
            [FreeFunction]
            get;
        }

        internal static extern string windowTitle
        {
            [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
            get;
        }

        internal static extern void CloseAndRelaunch(string[] arguments);

        internal static extern void RequestCloseAndRelaunchWithCurrentArguments();

        // Triggers the editor to restart, after which all scripts will be recompiled.
        internal static void RestartEditorAndRecompileScripts()
        {
            // Clear the script assemblies so we compile after the restart.
            EditorCompilationInterface.Instance.DeleteScriptAssemblies();

            RequestCloseAndRelaunchWithCurrentArguments();
        }

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern void FileMenuNewScene();

        [ThreadSafe]
        internal static extern void SignalTick();

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern void UpdateInteractionModeSettings();

        internal static extern void UpdateTooltipsInPlayModeSettings();

        [FreeFunction("GetProjectVersion().Write")]
        internal static extern void WriteVersion();

        internal static extern GUID buildSessionGUID
        {
            [FreeFunction("GetBuildSettings().GetBuildSessionGUID")]
            get;
        }
    }
}
