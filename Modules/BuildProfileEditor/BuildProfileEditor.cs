// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    internal class BuildProfileEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileEditor.uxml";
        const string k_PlatformSettingPropertyName = "m_PlatformBuildProfile";

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject.targetObject is not BuildProfile profile)
            {
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");
            }

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            var noModuleFoundHelpbox = root.Q<HelpBox>("platform-warning-help-box");
            var buildSettingsLabel = root.Q<Label>("build-settings-label");
            var platformSettingsBaseRoot = root.Q<VisualElement>("platform-settings-base-root");
            var buildDataLabel = root.Q<Label>("build-data-label");
            var sharedSettingsInfoHelpbox = root.Q<HelpBox>("shared-settings-info-helpbox");

            buildSettingsLabel.text = TrText.buildSettings;
            buildDataLabel.text = TrText.buildData;
            sharedSettingsInfoHelpbox.text = TrText.sharedSettingsInfo;

            if (BuildProfileModuleUtil.IsModuleInstalled(profile.buildTarget, profile.subtarget))
            {
                noModuleFoundHelpbox.style.display = DisplayStyle.None;
                var platformProperties = serializedObject.FindProperty(k_PlatformSettingPropertyName);
                var platformExtension = BuildProfileModuleUtil.GetBuildProfileExtension(profile.buildTarget);
                if (platformExtension != null)
                {
                    var settings = platformExtension.CreateSettingsGUI(serializedObject, platformProperties);
                    settings.AddToClassList(Util.k_PY_MediumUssClass);
                    platformSettingsBaseRoot.Add(settings);
                }
            }
            else
            {
                noModuleFoundHelpbox.text = string.Empty;
                noModuleFoundHelpbox.Add(
                    BuildProfileModuleUtil.CreateModuleNotInstalledElement(profile.buildTarget, profile.subtarget));
                noModuleFoundHelpbox.style.display = DisplayStyle.Flex;
            }

            return root;
        }
    }
}
