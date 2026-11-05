// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Handles mapping the existing <see cref="BuildPlayerSceneTreeView"/> component
    /// to build profile. Classic platforms specifically target scenes stored in
    /// <see cref="EditorBuildSettings"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfileSceneTreeView : BuildPlayerSceneTreeView
    {
        const string k_SceneListUndoName = "Scene list";

        readonly bool m_IsEditorBuildSettingsSceneList;
        readonly BuildProfile m_Target;

#pragma warning disable UAL0015 // chains into BuildPlayerSceneTreeView's ctor, whose subscription is unsubscribed by UnsubscribeListChange before this view can outlive the current CodeLoaded scope
        public BuildProfileSceneTreeView(TreeViewState state, BuildProfile target) : base(state)
        {
#pragma warning restore UAL0015
            showBorder = false;
            m_Target = target;
            m_IsEditorBuildSettingsSceneList = target == null;
        }

        /// <summary>
        /// Exported from <see cref="BuildPlayerWindow"/>.
        /// </summary>
        public void AddOpenScenes()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(GetScenes());

            bool isSceneAdded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (EditorSceneManager.IsAuthoringScene(scene))
                    continue;

                if (scene.path.Length == 0 && !EditorSceneManager.SaveScene(scene, "", false))
                    continue;

                if (list.Exists(s => s.path == scene.path))
                    continue;

                var newGUID = scene.guid;
                var buildSettingsScene = (newGUID == default(GUID)) ?
                    new EditorBuildSettingsScene(scene.path, true) :
                    new EditorBuildSettingsScene(newGUID, true);
                list.Add(buildSettingsScene);
                isSceneAdded = true;
            }

            if (!isSceneAdded)
                return;

            SetScenes(list.ToArray());
            Reload();
            GUIUtility.ExitGUI();
        }

        /// <summary>
        /// Appends the given scenes to the end of the list, skipping the ones already in it.
        /// </summary>
        public void AddScenes(IList<GUID> guids)
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(GetScenes());

            bool isSceneAdded = false;
            foreach (var guid in guids)
            {
                if (list.Exists(s => s.guid == guid))
                    continue;

                list.Add(new EditorBuildSettingsScene(guid, true));
                isSceneAdded = true;
            }

            if (!isSceneAdded)
                return;

            SetScenes(list.ToArray());
            Reload();
            Repaint();
        }

        /// <summary>
        /// GUIDs of the scenes currently in the list.
        /// </summary>
        public HashSet<GUID> GetSceneGuids()
        {
            var guids = new HashSet<GUID>();
            foreach (var scene in GetScenes())
                guids.Add(scene.guid);

            return guids;
        }

        protected override EditorBuildSettingsScene[] GetScenes() => (m_IsEditorBuildSettingsSceneList)
            ? EditorBuildSettings.GetEditorBuildSettingsSceneIgnoreProfile()
            : m_Target.scenes;

        protected override void SetScenes(EditorBuildSettingsScene[] scenes)
        {
            if (!m_IsEditorBuildSettingsSceneList)
            {
                Undo.RecordObject(m_Target, k_SceneListUndoName);
                m_Target.scenes = scenes;
                EditorUtility.SetDirty(m_Target);
                return;
            }

            // Classic platforms scene list can only be changed through this component
            // and write data directly to EditorBuildSettings.
            Undo.RecordObject(EditorBuildSettings.GetEditorBuildSettings(), k_SceneListUndoName);
            EditorBuildSettings.SetEditorBuildSettingsSceneIgnoreProfile(scenes);
            if (BuildProfileContext.activeProfile is null || !BuildProfileContext.activeProfile.overrideGlobalScenes)
                EditorBuildSettings.SceneListChanged();
        }
    }
}
