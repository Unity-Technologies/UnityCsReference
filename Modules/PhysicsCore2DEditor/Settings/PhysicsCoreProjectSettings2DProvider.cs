// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    sealed class PhysicsCoreProjectSettings2DProvider : SettingsProvider
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

        // Editor SettingsProvider singleton; assigned by the settings system when the provider is (re)created, so it is safe to persist across a code reload.
        [NoAutoStaticsCleanup]
        public static PhysicsCoreProjectSettings2DProvider Instance { get; private set; }

        static readonly string EmptySettingsLabel = $"Select a {ObjectNames.NicifyVariableName(nameof(PhysicsCoreSettings2D))} asset to edit.";

        // The faint grey used by the 2D physics package's rounded panels, so both settings pages read as one family.
        static readonly Color PanelBorderColor = new(0.5f, 0.5f, 0.5f, 0.40f);

        // The element the settings-asset content is rendered into; null while the provider page is not open.
        VisualElement m_SettingsContentRoot;

        PhysicsCoreProjectSettings2DProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        public static void SetSettingsObject() => Instance?.RefreshContent();

        public static void ClearSettingsObject() => Instance?.RefreshContent();

        // Refresh the shown settings when the active settings asset changes.
        // The refresh re-picks a selected asset, so when the shown asset is the one just assigned or cleared its header suffix stays correct.
        public static void RefreshActiveSettingContent() => Instance?.RefreshContent();

        // Rebuild the settings-asset content, showing a selected settings asset in preference to the active one, or an empty prompt when neither is available.
        void RefreshContent()
        {
            if (m_SettingsContentRoot == null)
                return;

            m_SettingsContentRoot.Clear();

            if (Selection.activeObject is PhysicsCoreSettings2D selectedSettings)
            {
                ShowSettings(selectedSettings);
                return;
            }

            if (PhysicsEditorOnly.physicsSettings != null)
            {
                ShowSettings(PhysicsEditorOnly.physicsSettings);
                return;
            }

            m_SettingsContentRoot.Add(CreateEmptyPropertyGUI());

            void ShowSettings(PhysicsCoreSettings2D settings)
            {
                var serializedSettings = new SerializedObject(settings);
                var contentRoot = PhysicsCoreSettings2DEditor.CreatePropertyGUI(serializedSettings);
                m_SettingsContentRoot.Add(contentRoot);

                // Keep the page searchable by both the project-settings field and the shown asset's own properties.
                var combinedKeywords = new List<string>(GetSearchKeywordsFromPath(AssetPath.PhysicsCoreSettingsAsset));
                combinedKeywords.AddRange(GetSearchKeywordsFromSerializedObject(serializedSettings));
                keywords = combinedKeywords;
            }
        }

        static VisualElement CreateEmptyPropertyGUI()
        {
            var root = new VisualElement();
            root.Add(new HelpBox(EmptySettingsLabel, HelpBoxMessageType.Info));
            return root;
        }

        // Apply the 2D physics package's rounded bordered panel treatment to an element.
        // The gap between one panel on the project-settings page and the next, used at the window edges too so a panel is inset by the same amount all round.
        const float PanelMargin = 10.0f;

        static internal void ApplyPanelStyle(VisualElement element)
        {
            element.style.paddingTop = element.style.paddingBottom = 8;
            element.style.paddingLeft = element.style.paddingRight = 8;
            element.style.borderTopWidth = element.style.borderBottomWidth = 1;
            element.style.borderLeftWidth = element.style.borderRightWidth = 1;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = 4;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 4;
            element.style.borderTopColor = element.style.borderBottomColor = PanelBorderColor;
            element.style.borderLeftColor = element.style.borderRightColor = PanelBorderColor;
        }

        // Create a new Physics Core Settings 2D asset via a save dialog, then assign it into the given field.
        static void CreateAndAssignSettingsAsset(SerializedObject serializedObject, ObjectField field)
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Physics Core Settings 2D", "PhysicsCoreSettings2D", "asset",
                "Choose where to save the new Physics Core Settings 2D asset.");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<PhysicsCoreSettings2D>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            field.value = asset;
            serializedObject.ApplyModifiedProperties();
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
            Instance = new PhysicsCoreProjectSettings2DProvider(ProjectSettingPath.PhysicsCoreModule, SettingsScope.Project)
            {
                label = "Physics Core 2D",
                keywords = GetSearchKeywordsFromPath(AssetPath.PhysicsCoreSettingsAsset),
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

                    // The settings-asset content renders below the core settings property, inside its own panel.
                    // The horizontal margin keeps the panel's side borders off the window edges, which they otherwise sit directly on.
                    var settingsContentPane = content.Q("project-section-content-pane");
                    ApplyPanelStyle(settingsContentPane);
                    settingsContentPane.style.marginLeft = settingsContentPane.style.marginRight = PanelMargin;

                    // Add core settings property, in its own panel.
                    {
                        // The inspector-element class makes the aligned field inside share the inspector's label column.
                        var assetPanel = new VisualElement();
                        ApplyPanelStyle(assetPanel);
                        assetPanel.style.marginTop = 4;
                        assetPanel.style.marginBottom = PanelMargin;
                        assetPanel.style.marginLeft = assetPanel.style.marginRight = PanelMargin;
                        assetPanel.AddToClassList(InspectorElement.ussClassName);

                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                        var coreProjectSettingsField = new ObjectField
                        {
                            label = "Physics Core Settings",
                            tooltip = "The active Physics Core Settings 2D.",
                            objectType = typeof(PhysicsCoreSettings2D),
                            bindingPath = "m_PhysicsCoreSettings",
                            style = { flexGrow = 1 }
                        };

                        // Align the label to the shared inspector column.
                        coreProjectSettingsField.AddToClassList(ObjectField.alignedFieldUssClassName);
                        row.Add(coreProjectSettingsField);

                        // Shown only while the slot is empty; creates a new asset via a save dialog and assigns it.
                        var makeAssetButton = new Button(() => CreateAndAssignSettingsAsset(serializedObject, coreProjectSettingsField))
                        {
                            text = "Make Asset",
                            tooltip = "Create a new Physics Core Settings 2D asset and assign it here."
                        };
                        row.Add(makeAssetButton);

                        assetPanel.Add(row);
                        content.Insert(content.IndexOf(settingsContentPane), assetPanel);

                        void UpdateMakeAssetVisibility() =>
                            makeAssetButton.style.display = coreProjectSettingsField.value == null ? DisplayStyle.Flex : DisplayStyle.None;

                        // Ensure we read the change immediately.
                        coreProjectSettingsField.RegisterValueChangedCallback(_ =>
                        {
                            UpdateMakeAssetVisibility();
                            PhysicsEditorOnly.ReadProjectSettings();
                            RefreshActiveSettingContent();
                        });

                        // Bind the project settings object, then set the initial visibility from the bound value.
                        // Binding assigns the field's value directly rather than raising a ChangeEvent, so the
                        // visibility must be calculated after binding rather than before it.
                        root.Bind(serializedObject);
                        UpdateMakeAssetVisibility();
                    }

                    Instance.m_SettingsContentRoot = settingsContentPane;
                    Instance.RefreshContent();
                },

                deactivateHandler = () => Instance.m_SettingsContentRoot = null,
            };

            return Instance;
        }
    }
}
