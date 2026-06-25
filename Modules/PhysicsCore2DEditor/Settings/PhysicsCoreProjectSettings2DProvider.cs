// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.U2D.Physics.Editor
{
    sealed class PhysicsCoreProjectSettings2DProvider
    {
        static internal class ProjectSettingPath
        {
            public const string PhysicsCoreModule = "Project/Physics Core 2D";
        }

        static internal class AssetPath
        {
            public const string PhysicsCoreSettingsAsset = "ProjectSettings/PhysicsCoreProjectSettings2D.asset";
            public static readonly string PhysicsCoreSettingsAssetError = $"{nameof(CreateProjectSettingsProvider)} failed to load asset {PhysicsCoreSettingsAsset}.";
        }

        static internal class StyleSheetPath
        {
            public const string projectSettingsSheet = "PhysicsCore2D/StyleSheets/ProjectSettings.uss";
            public const string projectSettingsCommonSheet = "StyleSheets/ProjectSettings/ProjectSettingsCommon.uss";
            public const string commonSheet = "StyleSheets/Extensions/base/common.uss";
            public const string darkSheet = "StyleSheets/Extensions/base/dark.uss";
            public const string lightSheet = "StyleSheets/Extensions/base/light.uss";
        }

        static internal class UXMLPath
        {
            public const string physicsCoreProjectSettings2D = "PhysicsCore2D/UXML/PhysicsCoreProjectSettings2D.uxml";
            public const string physicsCoreSettings2D = "PhysicsCore2D/UXML/PhysicsCoreSettings2D.uxml";
        }

        static SerializedObject LoadPhysicsCoreSettingsAsset()
        {
            var found = AssetDatabase.LoadAllAssetsAtPath(AssetPath.PhysicsCoreSettingsAsset);
            if (found == null)
                return null;

            return new SerializedObject(found[0]);
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new SettingsProvider(ProjectSettingPath.PhysicsCoreModule, SettingsScope.Project)
            {
                label = "Physics Core 2D",
                keywords = SettingsProvider.GetSearchKeywordsFromPath(AssetPath.PhysicsCoreSettingsAsset),
                activateHandler = (searchContext, root) =>
                {
                    var serializedObject = LoadPhysicsCoreSettingsAsset();
                    if (serializedObject == null)
                    {
                        Debug.LogError(AssetPath.PhysicsCoreSettingsAssetError);
                        return;
                    }

                    // Create settings root.
                    var physicsCoreProjectSettingsUXML = EditorGUIUtility.Load(UXMLPath.physicsCoreProjectSettings2D) as VisualTreeAsset;
                    physicsCoreProjectSettingsUXML.CloneTree(root);

                    // Add styles.
                    var content = root.Q<ScrollView>(className: "project-settings-section-content");
                    content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.projectSettingsSheet) as StyleSheet);
                    content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.projectSettingsCommonSheet) as StyleSheet);
                    content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.commonSheet) as StyleSheet);
                    content.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? StyleSheetPath.darkSheet : StyleSheetPath.lightSheet) as StyleSheet);

                    // Add core settings property.
                    {
                        var coreProjectSettingsField = new ObjectField
                        {
                            label = "Physics Core Settings",
                            tooltip = "The active Physics Core Settings 2D.",
                            objectType = typeof(PhysicsCoreSettings2D),
                            bindingPath = "m_PhysicsCoreSettings"
                        };

                        // Increase the margin.
                        coreProjectSettingsField.style.marginLeft = 10;
                        coreProjectSettingsField.style.marginRight = 10;

                        // Show as inspector class.
                        coreProjectSettingsField.AddToClassList(InspectorElement.ussClassName);
                        content.Add(coreProjectSettingsField);

                        // Ensure we read the change immediately.
                        coreProjectSettingsField.RegisterValueChangedCallback(_ =>
                        {
                            PhysicsEditorOnly.ReadProjectSettings();
                            PhysicsCoreSettings2DProvider.RefreshActiveSettingContent();
                        });
                    }

                    // Bind the project settings object.
                    root.Bind(serializedObject);
                }
            };

            return provider;
        }
    }
}
