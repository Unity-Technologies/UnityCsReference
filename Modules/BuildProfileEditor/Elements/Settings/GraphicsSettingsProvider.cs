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

        public void OnAdd(BuildProfile profile) => BuildProfileModuleUtil.CreateGraphicsSettings(profile);

        public void OnRemove(BuildProfile profile) => BuildProfileModuleUtil.RemoveGraphicsSettings(profile);

        public Action<BuildProfile> GetResetAction() => OnReset;

        public Action<BuildProfile> OnCopy() => OnCopy;

        public Action<BuildProfile> OnPaste() => OnPaste;

        void OnCopy(BuildProfile profile) => BuildProfileModuleUtil.CopySubAssetValues(profile.graphicsSettings);

        void OnPaste(BuildProfile profile) => BuildProfileModuleUtil.PasteSubAssetValues(profile.graphicsSettings);

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new EditorAsVisualElement(profile.graphicsSettings, true);
        }

        void OnReset(BuildProfile profile) => profile.graphicsSettings.ResetToGlobalValues();
    }
}
