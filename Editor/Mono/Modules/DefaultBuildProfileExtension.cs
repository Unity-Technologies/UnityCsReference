// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Modules
{
    internal abstract class DefaultBuildProfileExtension : IBuildProfileExtension
    {
        public abstract BuildProfilePlatformSettingsBase CreateBuildProfilePlatformSettings();

        public virtual VisualElement CreateSettingsGUI(SerializedObject serializedObject, SerializedProperty rootProperty)
        {
            // Default implementation will render all platform settings defined in
            // BuildProfilePlatformSettingsBase as a PropertyField. Enumerators are
            // shown as-is.
            var field = new PropertyField(rootProperty);
            field.BindProperty(rootProperty);
            return field;
        }

        public virtual void CopyPlatformSettingsToBuildProfile(BuildProfilePlatformSettingsBase platformSettingsBase)
        {
        }
    }
}
