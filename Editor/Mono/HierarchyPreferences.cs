// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [VisibleToOtherModules("HierarchyModule", "UnityEditor.UIToolkitAuthoringModule", "MultiplayerEditorModule")]
    internal static partial class HierarchyPreferences
    {
        const bool kUseNewHierarchy = false;

        public static PrefabStage.Mode DefaultPrefabModeFromHierarchy
        {
            get
            {
                return (PrefabStage.Mode)EditorPrefs.GetInt("DefaultPrefabModeFromHierarchy", (int)PrefabStage.Mode.InContext);
            }
            set
            {
                EditorPrefs.SetInt("DefaultPrefabModeFromHierarchy", (int)value);
            }
        }

        public static readonly SavedBool RenameNewObjects = new SavedBool("SceneHierarchyWindow.RenameNewObjects", true);
        public static readonly SavedBool UseQueryBuilder = new SavedBool("HierarchyWindow.UseQueryBuilder", true);
        public static readonly SavedBool AlternatingRowBackground = new SavedBool("HierarchyWindow.AlternatingRowBackground", true);
        public static readonly SavedBool UseNewHierarchy = new SavedBool("HierarchyWindow.UseNewHierarchy", kUseNewHierarchy);

        public static bool UseNewHierarchyWindowEnabled
        {
            get => UseNewHierarchy.value;
            set => UseNewHierarchy.value = value;
        }

        /// <summary>
        /// Fired whenever any tracked hierarchy preference value changes.
        /// Subscribers receive no argument; query the specific preference for the new value.
        /// </summary>
        [VisibleToOtherModules("HierarchyModule", "MultiplayerEditorModule")]
        internal static event Action AnyPreferenceChanged;

        static void FireAnyPreferenceChanged() => AnyPreferenceChanged?.Invoke();

        static HierarchyPreferences()
        {
            RenameNewObjects.valueChanged         += FireAnyPreferenceChanged;
            UseQueryBuilder.valueChanged          += FireAnyPreferenceChanged;
            AlternatingRowBackground.valueChanged += FireAnyPreferenceChanged;
            UseNewHierarchy.valueChanged          += FireAnyPreferenceChanged;
        }

        public static void EnsureCorrectHierarchyIsInUse(EditorWindow window)
        {
            var windowIsLegacy = window is SceneHierarchyWindow;
            if (UseNewHierarchy != windowIsLegacy)
                return;

            var wndType = UseNewHierarchy ? HierarchyV2WindowType : typeof(SceneHierarchyWindow);
            var replacementWindow = (EditorWindow)ScriptableObject.CreateInstance(wndType);

            if (window.m_Parent is DockArea dockParent)
                dockParent.AddTab(dockParent.m_Panes.IndexOf(window), replacementWindow);
            else
            {
                replacementWindow.position = window.position;
                replacementWindow.Show();
            }
            window.Close();
        }

        /// <summary>
        /// Re-reads all hierarchy preferences from the EditorPrefs store and fires
        /// <see cref="SavedValue{T}.valueChanged"/> for any value that has changed since
        /// the last read. Intended for use by virtual-player clones after
        /// <see cref="EditorPrefs.Sync"/> to pick up changes made in the main editor
        /// without bypassing the cached <see cref="SavedValue{T}"/> layer.
        /// </summary>
        [VisibleToOtherModules("HierarchyModule", "MultiplayerEditorModule")]
        internal static void RefreshPreferences()
        {
            RenameNewObjects.Refresh();
            UseQueryBuilder.Refresh();
            AlternatingRowBackground.Refresh();
            UseNewHierarchy.Refresh();
        }

        [VisibleToOtherModules("HierarchyModule", "MultiplayerEditorModule")]
        [AutoStaticsCleanupOnCodeReload]
        internal static Type HierarchyV2WindowType;
    }
}
