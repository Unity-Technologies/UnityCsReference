// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Custom Editor for ScenarioConfigurations. Used inside PlayModeConfigurationsWindow.
    /// </summary>
    [CustomEditor(typeof(OrchestratedScenario))]
    class ScenarioConfigEditor : Editor
    {
        internal const string k_InstancesListPropertyPath = $"{OrchestratedScenario.k_SettingsPropertyName}.{OrchestratedScenarioSettings.k_InstanceItemsPropertyName}";
        internal const string k_StylePath = "Multiplayer/UI/ScenarioConfigEditor.uss";
        internal const string k_LocalInstanceListName = "local-instance-list";
        internal const string k_EditorInstancesContainerName = "editor-instances-container";
        internal const string k_VirtualEditorInstanceFoldoutName = "virtual-editor-instance-foldout";
        const string k_CloneEditorsLabel = "Additional Editor Instances";
        const string k_CloneEditorsTooltip = "Initial Editor Instances when entering play mode. Editor Instances will only have limited authoring capabilities.";
        const string k_LocalInstancesLabel = "Local Instances";
        const string k_LocalInstancesTooltip = "Local Instances are builds that will run on the same machine as the editor.";

        internal const int MaxServerCount = 1;

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.styleSheets.Add(EditorGUIUtility.LoadRequired(k_StylePath) as StyleSheet);

            // Description Field
            var descriptionField = serializedObject.FindProperty("m_Description");
            var descriptionText = new TextField("Description");
            descriptionText.AddToClassList("unity-base-field__aligned");
            descriptionText.multiline = true;
            descriptionText.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            descriptionText.maxLength = 500;
            descriptionText.BindProperty(descriptionField);
            descriptionText.Bind(serializedObject);
            container.Add(descriptionText);

            container.Add(CreateEditorInstancesElement());
            container.Add(CreateLocalInstancesElement());

            return container;
        }

        bool TryGetMainEditorProperty(out SerializedProperty mainEditorProperty)
        {
            var instancesProperty = serializedObject.FindProperty($"{OrchestratedScenario.k_SettingsPropertyName}.{OrchestratedScenarioSettings.k_InstanceItemsPropertyName}");
            mainEditorProperty = default(SerializedProperty);

            for (int i = 0; i < instancesProperty.arraySize; i++)
            {
                var instanceProperty = instancesProperty.GetArrayElementAtIndex(i);
                if (instanceProperty.boxedValue is not IInstanceItem instanceItem)
                    continue;

                if (instanceItem.IsInstanceType(typeof(MainEditorController)))
                {
                    mainEditorProperty = instanceProperty;
                    return true;
                }
            }

            return false;
        }

        VisualElement CreateEditorInstancesElement()
        {
            var container = new VisualElement();
            container.AddToClassList("instances-group");
            var enableEditorsProperty = serializedObject.FindProperty("m_EnableEditors");

            var editorsToggle = new Toggle("Editor") { name = "EditorInstancesToggle" };
            container.Add(editorsToggle);
            editorsToggle.BindProperty(enableEditorsProperty);

            var content = new VisualElement() { name = k_VirtualEditorInstanceFoldoutName };
            content.AddToClassList("unity-foldout__content");
            container.Add(content);

            editorsToggle.RegisterValueChangedCallback(evt =>
            {
                content.style.display = evt.newValue
                    ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                    : new StyleEnum<DisplayStyle>(DisplayStyle.None);
            });

            // Main editor instance
            if (TryGetMainEditorProperty(out var mainEditorProperty))
            {
                var mainEditorField = new PropertyField(mainEditorProperty);
                content.Add(mainEditorField);
            }

            var cloneEditorsList = new FilteredInstancesListProperty<CloneEditorController, CloneEditorController.InstanceSettings>
                ((OrchestratedScenario)target, OrchestratedScenario.k_MaxCloneEditorInstances);
            cloneEditorsList.text = k_CloneEditorsLabel;
            cloneEditorsList.name = k_EditorInstancesContainerName;
            cloneEditorsList.tooltip = k_CloneEditorsTooltip;
            cloneEditorsList.AddToClassList("instances-group");
            cloneEditorsList.viewDataKey = $"{nameof(ScenarioConfigEditor)}.{k_EditorInstancesContainerName}";
            content.Add(cloneEditorsList);

            return container;
        }

        VisualElement CreateLocalInstancesElement()
        {
            var localInstancesList = new FilteredInstancesListProperty<LocalPlayerController, LocalPlayerController.InstanceSettings>
                ((OrchestratedScenario)target, OrchestratedScenario.k_MaxPlayerInstances);
            localInstancesList.text = k_LocalInstancesLabel;
            localInstancesList.name = k_LocalInstanceListName;
            localInstancesList.tooltip = k_LocalInstancesTooltip;
            localInstancesList.AddToClassList("instances-group");
            localInstancesList.viewDataKey = $"{nameof(ScenarioConfigEditor)}.{k_LocalInstanceListName}";

            return localInstancesList;
        }
    }
}
