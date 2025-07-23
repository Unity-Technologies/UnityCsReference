// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Graphic setting override stored as scriptable object asset within
    /// a build profile.
    /// </summary>
    class GraphicsSettingsProvider : IBuildProfileSettingsProvider
    {
        public string GetDisplayName() => TrText.graphicsSettings;

        public string GetTooltip() => string.Empty;

        public bool HasSettings(BuildProfile profile) => profile.graphicsSettings != null;

        public void OnAdd(BuildProfile profile)
        {
            BuildProfileModuleUtil.CreateGraphicsSettings(profile);
            OnReset(profile);

            if (profile == BuildProfileContext.activeProfile)
                BuildProfileModuleUtil.OnActiveProfileGraphicsSettingsChanged(true);
        }

        public void OnRemove(BuildProfile profile)
        {
            BuildProfileModuleUtil.RemoveGraphicsSettings(profile);

            if (profile == BuildProfileContext.activeProfile)
                BuildProfileModuleUtil.OnActiveProfileGraphicsSettingsChanged(false);
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new EditorAsVisualElement(profile.graphicsSettings);
        }

        void OnReset(BuildProfile profile)
        {
            var editor = Editor.CreateEditor(profile.graphicsSettings) as BuildProfileGraphicsSettingsEditor;
            editor.ResetToGlobalGraphicsSettingsValues();
            UnityEngine.Object.DestroyImmediate(editor);
            EditorUtility.SetDirty(profile.graphicsSettings);
        }
    }
}
