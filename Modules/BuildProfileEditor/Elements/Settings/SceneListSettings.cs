// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: BuildSettingsWindow not yet converted
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    internal class SceneListProvider : IBuildProfileSettingsProvider
    {

        /// <summary>
        /// Wrapper used to serialize the scene list to JSON. <see cref="EditorBuildSettingsScene"/>
        /// holds a GUID that the generic serialized property clipboard cannot round-trip, and
        /// <see cref="EditorJsonUtility"/> cannot serialize a top-level array, so the scenes are
        /// nested in this wrapper.
        /// </summary>
        [Serializable]
        class SceneListClipboard
        {
            public string sceneListClipboardId = k_ClipboardId;
            public EditorBuildSettingsScene[] scenes = Array.Empty<EditorBuildSettingsScene>();
        }

        const string k_ClipboardId = "UnityEditor.Build.Profile.SceneList";

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

        public Action<BuildProfile> OnCopy() => OnCopy;

        public Action<BuildProfile> OnPaste() => OnPaste;

        void OnCopy(BuildProfile profile)
        {
            var clipboard = new SceneListClipboard { scenes = profile.scenes };
            GUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(clipboard);
        }

        void OnPaste(BuildProfile profile)
        {
            string systemBuffer = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(systemBuffer))
                return;

            var clipboard = new SceneListClipboard { sceneListClipboardId = null };
            try
            {
                EditorJsonUtility.FromJsonOverwrite(systemBuffer, clipboard);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Clipboard does not contain valid scene list data for pasting.");
                return;
            }

            if (clipboard.sceneListClipboardId != k_ClipboardId)
            {
                Debug.LogWarning("Clipboard does not contain valid scene list data for pasting.");
                return;
            }

            Undo.RecordObject(profile, "Paste Sub-Asset Values");
            profile.scenes = clipboard.scenes ?? Array.Empty<EditorBuildSettingsScene>();
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
