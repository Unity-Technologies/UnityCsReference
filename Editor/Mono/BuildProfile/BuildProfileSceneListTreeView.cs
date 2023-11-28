// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

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
        readonly bool m_IsEditorBuildSettingsSceneList;
        readonly BuildProfile m_Target;

        public BuildProfileSceneTreeView(TreeViewState state, BuildProfile target) : base(state)
        {
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

                GUID newGUID;
                GUID.TryParse(scene.guid, out newGUID);
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

        protected override EditorBuildSettingsScene[] GetScenes() => (m_IsEditorBuildSettingsSceneList)
            ? EditorBuildSettings.GetEditorBuildSettingsSceneIgnoreProfile()
            : m_Target.scenes;

        protected override void SetScenes(EditorBuildSettingsScene[] scenes)
        {
            if (!m_IsEditorBuildSettingsSceneList)
            {
                Undo.RecordObject(m_Target, "Scene list");
                m_Target.scenes = scenes;
                EditorUtility.SetDirty(m_Target);
                return;
            }

            // Classic platforms scene list can only be changed through this component
            // and write data directly to EditorBuildSettings.
            EditorBuildSettings.SetEditorBuildSettingsSceneIgnoreProfile(scenes);
            if (BuildProfileContext.instance.activeProfile is null)
                EditorBuildSettings.SceneListChanged();
        }
    }
}
