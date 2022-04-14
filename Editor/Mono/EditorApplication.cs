// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEditorInternal;
using UnityEditor.Scripting;
using UnityEngine.TestTools;
using Unity.Profiling;
using UnityEditor.Profiling;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine.Profiling;

namespace UnityEditor
{
    public enum PlayModeStateChange
    {
        EnteredEditMode,
        ExitingEditMode,
        EnteredPlayMode,
        ExitingPlayMode,
    }

    // Must be kept in sync with enum in ScriptCompilationPipeline.h
    internal enum ScriptChangesDuringPlayOptions
    {
        RecompileAndContinuePlaying = 0,
        RecompileAfterFinishedPlaying = 1,
        StopPlayingAndRecompile = 2
    }

    public enum PauseState
    {
        Paused,
        Unpaused,
    }

    internal class ApplicationTitleDescriptor
    {
        public ApplicationTitleDescriptor(string projectName, string unityVersion, string activeSceneName, string targetName, bool codeCoverageEnabled)
        {
            title = "";
            this.projectName = projectName;
            this.unityVersion = unityVersion;
            this.activeSceneName = activeSceneName;
            this.targetName = targetName;
            this.codeCoverageEnabled = codeCoverageEnabled;
        }

        public string title;
        public string projectName { get; private set; }
        public string unityVersion { get; private set; }
        public string activeSceneName { get; private set; }
        public string targetName { get; private set; }
        public bool codeCoverageEnabled { get; private set; }
    }

    public sealed partial class EditorApplication
    {
        internal static UnityAction projectWasLoaded;
        internal static UnityAction editorApplicationQuit;

        [RequiredByNativeCode]
        static void Internal_ProjectWasLoaded()
        {
            projectWasLoaded?.Invoke();
        }

        [RequiredByNativeCode]
        static bool Internal_EditorApplicationWantsToQuit()
        {
            if (!m_WantsToQuitEvent.hasSubscribers)
                return true;

            foreach (Func<bool> continueQuit in m_WantsToQuitEvent)
            {
                try
                {
                    if (!continueQuit())
                        return false;
                }
                catch (Exception exception)
                {
                    Debug.LogWarningFormat("EditorApplication.wantsToQuit: Exception raised during quit event."
                        + Environment.NewLine +
                        "Check the exception error's callstack to find out which event handler threw the exception.");
                    Debug.LogException(exception);

                    if (InternalEditorUtility.isHumanControllingUs)
                    {
                        string st = exception.StackTrace;
                        StringBuilder dialogText = new StringBuilder("An exception was thrown here:");
                        dialogText.AppendLine(Environment.NewLine);
                        dialogText.AppendLine(st.Substring(0, st.IndexOf(Environment.NewLine)));

                        bool abortQuit = !EditorUtility.DisplayDialog("Error while quitting",
                            dialogText.ToString(),
                            "Ignore", "Cancel Quit");

                        if (abortQuit)
                            return false;
                    }
                }
            }

            return true;
        }

        static void Internal_EditorApplicationQuit()
        {
            // VersionControlObject might depend on packages that cleanup themselves using quitting event.
            // Therefore it's important to deactivate it beforehand.
            VersionControlManager.Deactivate();

            foreach (var evt in m_QuittingEvent)
                evt();
            editorApplicationQuit?.Invoke();
            ScriptCompilers.Cleanup();
        }

        // Delegate to be called for every visible list item in the ProjectWindow on every OnGUI event.
        public delegate void ProjectWindowItemCallback(string guid, Rect selectionRect);
        public delegate void ProjectWindowItemInstanceCallback(int instanceID, Rect selectionRect);

        // Delegate for OnGUI events for every visible list item in the ProjectWindow.
        public static ProjectWindowItemCallback projectWindowItemOnGUI;
        public static ProjectWindowItemInstanceCallback projectWindowItemInstanceOnGUI;

        // Can be used to ensure repaint of the ProjectWindow.
        public static void RepaintProjectWindow()
        {
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }

        // Can be used to ensure repaint of AnimationWindow
        public static void RepaintAnimationWindow()
        {
            foreach (AnimEditor animEditor in AnimEditor.GetAllAnimationWindows())
                animEditor.Repaint();
        }

        // Delegate to be called for every visible list item in the HierarchyWindow on every OnGUI event.
        public delegate void HierarchyWindowItemCallback(int instanceID, Rect selectionRect);

        // Delegate for OnGUI events for every visible list item in the HierarchyWindow.
        public static HierarchyWindowItemCallback hierarchyWindowItemOnGUI;

        // Delegate for refreshing hierarchies.
        internal static CallbackFunction refreshHierarchy;

        // Can be used to ensure repaint of the HierarchyWindow.
        public static void RepaintHierarchyWindow()
        {
            refreshHierarchy?.Invoke();
        }

        // Delegate for dirtying hierarchy sorting.
        internal static CallbackFunction dirtyHierarchySorting;

        public static void DirtyHierarchyWindowSorting()
        {
            dirtyHierarchySorting?.Invoke();
        }

        // Delegate to be called from [[EditorApplication]] callbacks.
        public delegate void CallbackFunction();

        // Delegate to be called from [[EditorApplication]] contextual inspector callbacks.
        public delegate void SerializedPropertyCallbackFunction(GenericMenu menu, SerializedProperty property);

        // Delegate for generic updates.
        public static CallbackFunction update;

        private static DelegateWithPerformanceTracker<CallbackFunction> m_UpdateEvent = new DelegateWithPerformanceTracker<CallbackFunction>($"{nameof(EditorApplication)}.{nameof(update)}");
        internal static event CallbackFunction tick;

        public static event Func<bool> wantsToQuit
        {
            add => m_WantsToQuitEvent.Add(value);
            remove => m_WantsToQuitEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Func<bool>> m_WantsToQuitEvent = new EventWithPerformanceTracker<Func<bool>>($"{nameof(EditorApplication)}.{nameof(wantsToQuit)}");

        public static event Action quitting
        {
            add => m_QuittingEvent.Add(value);
            remove => m_QuittingEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Action> m_QuittingEvent = new EventWithPerformanceTracker<Action>($"{nameof(EditorApplication)}.{nameof(quitting)}");

        public static CallbackFunction delayCall;
        private static DelegateWithPerformanceTracker<CallbackFunction> m_DelayCallEvent = new DelegateWithPerformanceTracker<CallbackFunction>($"{nameof(EditorApplication)}.{nameof(delayCall)}");

        internal static Action CallDelayed(CallbackFunction action, double delaySeconds = 0.0f)
        {
            var startTime = DateTime.UtcNow;
            CallbackFunction delayedHandler = null;
            delayedHandler = new CallbackFunction(() =>
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds < delaySeconds)
                    return;
                tick -= delayedHandler;
                action();
            });
            tick += delayedHandler;
            if (delaySeconds == 0f)
                SignalTick();

            return () => tick -= delayedHandler;
        }

        // Each time an object is (or a group of objects are) created, renamed, parented, unparented or destroyed this callback is raised.
        public static event Action hierarchyChanged
        {
            add => m_HierarchyChangedEvent.Add(value);
            remove => m_HierarchyChangedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Action> m_HierarchyChangedEvent = new EventWithPerformanceTracker<Action>($"{nameof(EditorApplication)}.{nameof(hierarchyChanged)}");

        [Obsolete("Use EditorApplication.hierarchyChanged")]
        public static CallbackFunction hierarchyWindowChanged;

        public static event Action projectChanged
        {
            add => m_ProjectChangedEvent.Add(value);
            remove => m_ProjectChangedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Action> m_ProjectChangedEvent = new EventWithPerformanceTracker<Action>($"{nameof(EditorApplication)}.{nameof(projectChanged)}");

        [Obsolete("Use EditorApplication.projectChanged")]
        public static CallbackFunction projectWindowChanged;

        public static CallbackFunction searchChanged;

        internal static CallbackFunction assetLabelsChanged;

        internal static CallbackFunction assetBundleNameChanged;

        // Delegate for changed keyboard modifier keys.
        public static CallbackFunction modifierKeysChanged;

        public static event Action<PauseState> pauseStateChanged
        {
            add => m_PauseStateChangedEvent.Add(value);
            remove => m_PauseStateChangedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Action<PauseState>> m_PauseStateChangedEvent = new EventWithPerformanceTracker<Action<PauseState>>($"{nameof(EditorApplication)}.{nameof(pauseStateChanged)}");

        public static event Action<PlayModeStateChange> playModeStateChanged
        {
            add => m_PlayModeStateChangedEvent.Add(value);
            remove => m_PlayModeStateChangedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<Action<PlayModeStateChange>> m_PlayModeStateChangedEvent = new EventWithPerformanceTracker<Action<PlayModeStateChange>>($"{nameof(EditorApplication)}.{nameof(playModeStateChanged)}");

        [Obsolete("Use EditorApplication.playModeStateChanged and/or EditorApplication.pauseStateChanged")]
        public static CallbackFunction playmodeStateChanged;

        // Global key up/down event that was not handled by anyone
        internal static CallbackFunction globalEventHandler;

        // Returns true when the pressed keys are defined in the Trigger
        internal static Func<bool> doPressedKeysTriggerAnyShortcut;

        internal static event Action<bool> focusChanged;

        // Windows were reordered
        internal static CallbackFunction windowsReordered;

        // Global contextual menus for inspector values
        public static SerializedPropertyCallbackFunction contextualPropertyMenu;

        internal static event Action<ApplicationTitleDescriptor> updateMainWindowTitle;

        internal static string GetDefaultMainWindowTitle(ApplicationTitleDescriptor desc)
        {
            // de facto dev tool window title conventions:
            // https://unity.slack.com/archives/C06TQ0QMQ/p1550046908037800
            //
            // _windows_ & _linux_
            //
            //   <Project> - <ThingImEditing> [- <MaybeSomeBuildConfigStuff>] - <AppName> [<ProbablyVersionToo>]
            //
            //   this is done to keep the most important data at the front to deal with truncation that happens
            //   in various interfaces like alt-tab and hover taskbar icon.
            //
            // _mac_
            //
            //   <ThingImEditing> - <Project> [- <MaybeSomeBuildConfigStuff>] - <AppName> [<ProbablyVersionToo>]
            //
            //   most macOS apps show the icon of "ThingImEditing" in front of the title, so it makes sense
            //   for the icon to be next to what it represents. (Unity does not currently show the icon though.)
            //

            var title = Application.platform == RuntimePlatform.OSXEditor
                ? $"{desc.activeSceneName} - {desc.projectName}"
                : $"{desc.projectName} - {desc.activeSceneName}";

            // FUTURE: [CODE COVERAGE] and the build target info do not belong in the title bar. they
            // are there now because we want them to be always-visible to user, which normally would be a) buildconfig
            // bar or b) status bar, but we don't have a) and our b) needs work to support such a thing.

            if (!string.IsNullOrEmpty(desc.targetName))
            {
                title += $" - {desc.targetName}";
            }

            title += $" - Unity {desc.unityVersion}";

            if (desc.codeCoverageEnabled)
            {
                title += " " + L10n.Tr("[CODE COVERAGE]");
            }

            return title;
        }

        [RequiredByNativeCode]
        internal static string BuildMainWindowTitle()
        {
            var desc = GetApplicationTitleDescriptor();
            updateMainWindowTitle?.Invoke(desc);

            return desc.title;
        }

        internal static ApplicationTitleDescriptor GetApplicationTitleDescriptor()
        {
            var activeSceneName = L10n.Tr("Untitled");
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            {
                activeSceneName = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path);
            }

            var desc = new ApplicationTitleDescriptor(
                isTemporaryProject ? PlayerSettings.productName : Path.GetFileName(Path.GetDirectoryName(Application.dataPath)),
                InternalEditorUtility.GetUnityDisplayVersion(),
                activeSceneName,
                BuildPipeline.GetBuildTargetGroupDisplayName(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)),
                Coverage.enabled
            );

            desc.title = GetDefaultMainWindowTitle(desc);

            return desc;
        }

        [RequiredByNativeCode]
        internal static void Internal_CallUpdateFunctions()
        {
            if (update == null)
                return;

            foreach (var evt in m_UpdateEvent.UpdateAndInvoke(update))
                evt();
        }

        [RequiredByNativeCode]
        internal static void Internal_InvokeTickEvents()
        {
            tick?.Invoke();
        }

        [RequiredByNativeCode]
        internal static void Internal_CallDelayFunctions()
        {
            CallbackFunction delay = delayCall;
            delayCall = null;
            foreach (var evt in m_DelayCallEvent.UpdateAndInvoke(delay))
                evt();
        }

        internal static void Internal_SwitchSkin()
        {
            EditorGUIUtility.Internal_SwitchSkin();
        }

        internal static void RequestRepaintAllViews()
        {
            foreach (GUIView view in Resources.FindObjectsOfTypeAll(typeof(GUIView)))
                view.Repaint();
        }

        static void Internal_CallHierarchyHasChanged()
        {
            #pragma warning disable 618
            hierarchyWindowChanged?.Invoke();
            #pragma warning restore 618

            foreach (var evt in m_HierarchyChangedEvent)
                evt();
        }

        [RequiredByNativeCode]
        static void Internal_CallProjectHasChanged()
        {
            #pragma warning disable 618
            projectWindowChanged?.Invoke();
            #pragma warning restore 618

            foreach (var evt in m_ProjectChangedEvent)
                evt();
        }

        internal static void Internal_CallSearchHasChanged()
        {
            searchChanged?.Invoke();
        }

        internal static void Internal_CallAssetLabelsHaveChanged()
        {
            assetLabelsChanged?.Invoke();
        }

        internal static void Internal_CallAssetBundleNameChanged()
        {
            assetBundleNameChanged?.Invoke();
        }

        static void Internal_PauseStateChanged(PauseState state)
        {
            #pragma warning disable 618
            playmodeStateChanged?.Invoke();
            #pragma warning restore 618

            foreach (var evt in m_PauseStateChangedEvent)
                evt(state);
        }

        static void Internal_PlayModeStateChanged(PlayModeStateChange state)
        {
            #pragma warning disable 618
            playmodeStateChanged?.Invoke();
            #pragma warning restore 618

            foreach (var evt in m_PlayModeStateChangedEvent)
                evt(state);
        }

        static void Internal_CallKeyboardModifiersChanged()
        {
            modifierKeysChanged?.Invoke();
        }

        static void Internal_CallWindowsReordered()
        {
            windowsReordered?.Invoke();
        }

        [RequiredByNativeCode]
        static bool DoPressedKeysTriggerAnyShortcutHandler()
        {
            if (doPressedKeysTriggerAnyShortcut != null)
                return doPressedKeysTriggerAnyShortcut();
            return false;
        }

        [RequiredByNativeCode]
        static void Internal_CallGlobalEventHandler()
        {
            globalEventHandler?.Invoke();

            // Ensure this is called last in order to make sure no null current events are passed to other handlers
            WindowLayout.MaximizeGestureHandler();

            Event.current = null;
        }

        [RequiredByNativeCode]
        static void Internal_FocusChanged(bool isFocused)
        {
            focusChanged?.Invoke(isFocused);
        }

        [MenuItem("File/New Scene %n", priority = 150)]
        static void FireFileMenuNewScene()
        {
            if (CommandService.Exists("Menu/File/NewSceneTemplate"))
            {
                CommandService.Execute("Menu/File/NewSceneTemplate");
            }
            else
            {
                EditorApplication.FileMenuNewScene();
            }
        }

        internal static void TogglePlaying()
        {
            isPlaying = !isPlaying;
            InternalEditorUtility.RepaintAllViews();
        }

        [RequiredByNativeCode]
        static void Internal_RestoreLastOpenedScenes()
        {
            using (new EditorPerformanceTracker("Application.RestoreLastOpenedScenes"))
            {
                // Unfortunately kLastOpenedScene would more appropriately be named kLastSpecifiedScene as it refers
                // to the last scene file as specified by the user/as chosen using Double-Click on the scene file in
                // the file browser.  However this editor preference has been around for a while, the name stays.
                //
                // Try to open the last specified scene first
                var lastOpenedScene = EditorPrefs.GetString(kLastOpenedScene);

                // Open requested scene if any
                if (!string.IsNullOrEmpty(lastOpenedScene))
                {
                    try
                    {
                        if (EditorSceneManager.CanOpenScene())
                            EditorSceneManager.OpenScene(lastOpenedScene, OpenSceneMode.Single);
                        else
                            InstantiateDefaultScene();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Error while opening specified scene \"{lastOpenedScene}\":\n {e.Message}");
                    }

                    // Regardless of the operation outcome, reset last opened scene so that we don't
                    // force it next time around
                    EditorPrefs.SetString(kLastOpenedScene, "");
                }
                else
                {
                    // Open last opened scenes
                    if (!EditorSceneManager.LoadLastSceneManagerSetup())
                        InstantiateDefaultScene();
                }
            }
        }

        static void InstantiateDefaultScene()
        {
            if (CommandService.Exists("Menu/File/InstantiateDefaultScene"))
            {
                CommandService.Execute("Menu/File/InstantiateDefaultScene");
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
        }
    }
}
