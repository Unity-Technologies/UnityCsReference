// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEditorInternal;

namespace UnityEditor
{
    public enum PlayModeStateChange
    {
        EnteredEditMode,
        ExitingEditMode,
        EnteredPlayMode,
        ExitingPlayMode,
    }

    public enum PauseState
    {
        Paused,
        Unpaused,
    }

    public sealed partial class EditorApplication
    {
        internal static UnityAction projectWasLoaded;
        internal static UnityAction editorApplicationQuit;

        static void Internal_ProjectWasLoaded()
        {
            if (projectWasLoaded != null)
                projectWasLoaded();
        }

        [RequiredByNativeCode]
        static bool Internal_EditorApplicationWantsToQuit()
        {
            if (wantsToQuit == null)
                return true;

            foreach (Func<bool> continueQuit in wantsToQuit.GetInvocationList())
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
            if (quitting != null)
                quitting();

            if (editorApplicationQuit != null)
                editorApplicationQuit();
        }

        internal static bool supportsHiDPI { get { return Application.platform == RuntimePlatform.OSXEditor; } }

        // Delegate to be called for every visible list item in the ProjectWindow on every OnGUI event.
        public delegate void ProjectWindowItemCallback(string guid, Rect selectionRect);

        // Delegate for OnGUI events for every visible list item in the ProjectWindow.
        public static ProjectWindowItemCallback projectWindowItemOnGUI;

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

        // Can be used to ensure repaint of the HierarchyWindow.
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

        // Delegate to be called from [[EditorApplication]] callbacks.
        public delegate void CallbackFunction();

        // Delegate to be called from [[EditorApplication]] contextual inspector callbacks.
        public delegate void SerializedPropertyCallbackFunction(GenericMenu menu, SerializedProperty property);

        // Delegate for generic updates.
        public static CallbackFunction update;

        public static event Func<bool> wantsToQuit;

        public static event Action quitting;

        public static CallbackFunction delayCall;

        // Each time an object is (or a group of objects are) created, renamed, parented, unparented or destroyed this callback is raised.
        public static event Action hierarchyChanged;

        [Obsolete("Use EditorApplication.hierarchyChanged")]
        public static CallbackFunction hierarchyWindowChanged;

        public static event Action projectChanged;

        [Obsolete("Use EditorApplication.projectChanged")]
        public static CallbackFunction projectWindowChanged;

        public static CallbackFunction searchChanged;

        internal static CallbackFunction assetLabelsChanged;

        internal static CallbackFunction assetBundleNameChanged;

        // Delegate for changed keyboard modifier keys.
        public static CallbackFunction modifierKeysChanged;

        public static event Action<PauseState> pauseStateChanged;

        public static event Action<PlayModeStateChange> playModeStateChanged;

        [Obsolete("Use EditorApplication.playModeStateChanged and/or EditorApplication.pauseStateChanged")]
        public static CallbackFunction playmodeStateChanged;

        // Global key up/down event that was not handled by anyone
        internal static CallbackFunction globalEventHandler;

        // Windows were reordered
        internal static CallbackFunction windowsReordered;

        // Global contextual menus for inspector values
        public static SerializedPropertyCallbackFunction contextualPropertyMenu;

        static void Internal_CallUpdateFunctions()
        {
            if (update != null)
                update();
        }

        static void Internal_CallDelayFunctions()
        {
            CallbackFunction delay = delayCall;
            delayCall = null;

            if (delay != null)
                delay();
        }

        static void Internal_SwitchSkin()
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
            if (hierarchyWindowChanged != null)
                hierarchyWindowChanged();
            #pragma warning restore 618

            if (hierarchyChanged != null)
                hierarchyChanged();
        }

        static void Internal_CallProjectHasChanged()
        {
            #pragma warning disable 618
            if (projectWindowChanged != null)
                projectWindowChanged();
            #pragma warning restore 618

            if (projectChanged != null)
                projectChanged();
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

        // Single use case for now ONLY!
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

        static void Internal_PauseStateChanged(PauseState state)
        {
            #pragma warning disable 618
            if (playmodeStateChanged != null)
                playmodeStateChanged();
            #pragma warning restore 618

            if (pauseStateChanged != null)
                pauseStateChanged(state);
        }

        static void Internal_PlayModeStateChanged(PlayModeStateChange state)
        {
            #pragma warning disable 618
            if (playmodeStateChanged != null)
                playmodeStateChanged();
            #pragma warning restore 618

            if (playModeStateChanged != null)
                playModeStateChanged(state);
        }

        static void Internal_CallKeyboardModifiersChanged()
        {
            if (modifierKeysChanged != null)
                modifierKeysChanged();
        }

        static void Internal_CallWindowsReordered()
        {
            if (windowsReordered != null)
                windowsReordered();
        }

        [RequiredByNativeCode]
        static void Internal_CallGlobalEventHandler()
        {
            if (globalEventHandler != null)
                globalEventHandler();

            // Ensure this is called last in order to make sure no null current events are passed to other handlers
            WindowLayout.MaximizeKeyHandler();

            Event.current = null;
        }
    }
}
