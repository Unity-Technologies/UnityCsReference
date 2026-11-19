// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build Automation settings require cloud build package.
    /// </summary>
    internal class BuildAutomationSettingsProvider : IBuildProfileSettingsProvider
    {
        public string GetDisplayName() => BuildAutomationSettingsEditor.buildAutomationLabelText;

        public string GetTooltip() => string.Empty;

        public bool CanAddSettings(BuildProfile profile) => BuildAutomationSettingsEditor.IsBuildAutomationPackagePresent();

        public bool HasSettings(BuildProfile profile) => profile.GetComponent<BuildAutomationSettings>() is not null;

        public void OnAdd(BuildProfile profile) => BuildAutomationSettingsEditor.AddBuildAutomationSettings(profile);

        public void OnRemove(BuildProfile profile) => profile.RemoveComponent<BuildAutomationSettings>();

        public Action<BuildProfile> GetResetAction() => null;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new BuildAutomationSettingsEditor(profile);
        }
    }
}
