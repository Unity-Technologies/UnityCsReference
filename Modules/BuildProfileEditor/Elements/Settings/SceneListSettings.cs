// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    internal class SceneListProvider : IBuildProfileSettingsProvider
    {
        /// <summary>
        /// Scene list settings displays a list of <see cref="EditorBuildSettingsScene"/> stored
        /// directly in the build profile under <see cref="BuildProfile.scenes"/>. The flag
        /// <see cref="BuildProfile.overrideGlobalScenes"/>
        /// </summary>
        class SceneListVisualElement : VisualElement
        {
            const string k_Uxml = "BuildProfile/UXML/VisualElement/SceneListSettings.uxml";

            BuildProfileSceneList m_SceneList;

            public SceneListVisualElement(BuildProfile profile)
            {
                var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
                uxml.CloneTree(this);

                m_SceneList = new BuildProfileSceneList(profile);

                var root = this.Q<VisualElement>("scene-list-foldout-root");
                var addOpenSceneListButton = this.Q<Button>("scene-list-foldout-add-open-button");

                root.Add(m_SceneList.GetSceneListGUI());

                addOpenSceneListButton.text = TrText.addOpenScenes;
                addOpenSceneListButton.clicked += () => m_SceneList.AddOpenScenes();

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                Undo.undoRedoEvent += m_SceneList.OnUndoRedo;
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                Undo.undoRedoEvent -= m_SceneList.OnUndoRedo;
            }
        }

        public string GetDisplayName()
        {
            return TrText.sceneList;
        }

        public string GetTooltip() => string.Empty;

        public bool HasSettings(BuildProfile profile)
        {
            return profile.overrideGlobalScenes;
        }

        public void OnAdd(BuildProfile profile)
        {
            profile.overrideGlobalScenes = true;
            EditorUtility.SetDirty(profile);
        }

        public void OnRemove(BuildProfile profile)
        {
            profile.overrideGlobalScenes = false;
            EditorUtility.SetDirty(profile);
        }

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new SceneListVisualElement(profile);
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        void OnReset(BuildProfile profile)
        {
            profile.overrideGlobalScenes = true;
            profile.scenes = Array.Empty<EditorBuildSettingsScene>();
            EditorUtility.SetDirty(profile);
        }
    }
}
