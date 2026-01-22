// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using Unity.PlayMode.Editor;
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
        internal const string k_StylePath = "Multiplayer/UI/ScenarioConfigEditor.uss";
        internal const string k_LocalInstanceListName = "local-instance-list";
        internal const string k_RemoteInstanceListName = "remote-instance-list";
        internal const string k_EditorInstancesContainerName = "editor-instances-container";
        internal const string k_RemoteInstancesFoldoutName = "remote-instances-foldout";
        internal const string k_InstallMissingPackagesButtonName = "install-missing-packages-button";
        internal const string k_ListViewRemoveButton = "unity-list-view__remove-button";
        internal const string k_ListViewAddButton ="unity-list-view__add-button";
        internal const string k_VirtualEditorInstanceFoldoutName = "virtual-editor-instance-foldout";
        internal const string k_DisabledInstanceHelpBoxName = "main-multiplayer-instance-DisabledHelpBox";
        internal const string k_DisabledInstanceHelpBoxText = "An instance is currently running. To modify any settings for editor instances, please terminate this instance in the Active Scenario Window.";

        internal const int MaxServerCount = 1;
        internal const int MaxEditorInstanceCount = 3;
        internal const int MaxLocalInstanceCount = 4;

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

        Foldout CreateMissingPackageHelpbox(List<string> missingPacks)
        {
            var foldout = new Foldout() { text = "Remote Instances", name = k_RemoteInstancesFoldoutName };
            foldout.AddToClassList("missing-packages-foldout");

            var helpBox = new HelpBox("Remote server will be deployed to Unity Game Server Hosting. Make sure you have the necessary packages installed.\n\t" +
                string.Join("\n\t", missingPacks)
                , HelpBoxMessageType.Info);
            var dashboardButton = new Button(() => Application.OpenURL("https://cloud.unity.com/")) { text = "Open Dashboard" };
            var installPackages = new Button(async void () =>  await OrchestratedScenario.LoadPackagesAsync())
            {
                name = k_InstallMissingPackagesButtonName,
                text = "Install missing packages"
            };

            var buttonContainer = new VisualElement() { name = "missing-packages-button-container" };
            buttonContainer.Add(dashboardButton);
            buttonContainer.Add(installPackages);
            helpBox.Add(buttonContainer);
            foldout.Add(helpBox);
            return foldout;
        }

        // We have to override the default behaviour of the list view, because we need some custom logic in it.
        void SetupListView<TController, TSettings>(ListView listView, SerializedProperty listProperty, int maxInstanceCount, string tooltip)
            where TController : InstanceController<TController, TSettings>
        {
            if (listView == null)
            {
                return;
            }

            // listView.showFoldoutHeader = false;
            listView.showBoundCollectionSize = false;
            listView.reorderable = false;
            listView.canStartDrag += args => false;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            listView.onAdd = _ =>
            {
                listProperty.serializedObject.Update();

                if (listProperty.arraySize >= maxInstanceCount)
                {
                    EditorUtility.DisplayDialog("Warning", $"You can't have more than {maxInstanceCount} {(maxInstanceCount>1?"instances":"instance")}", "Ok");
                    return;
                }

                listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
                var instanceProperty = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                var instanceSettings = InstanceController<TController, TSettings>.GetDefaultSettings() as InstanceDescription;
                Assert.IsNotNull(instanceSettings, $"Default settings for {typeof(TController).Name} returned null or is not of type InstanceDescription.");
                instanceSettings.Name = GenerateInstanceName(listProperty);
                instanceProperty.boxedValue = instanceSettings;
                listProperty.serializedObject.ApplyModifiedProperties();
                listProperty.serializedObject.Update();
            };

            RefreshListViewAddRemoveToggles<TController>(listView, listProperty);

            var foldout = listView.Q<Foldout>();
            var toggle = foldout?.Q<Toggle>();
            if (toggle != null)
            {
                toggle.tooltip = tooltip;
            }
        }

        // Mimic the naming that is used in Unity.
        static string GenerateInstanceName(SerializedProperty instanceArrayProperty)
        {
            var instanceList = new List<InstanceDescription>();
            for (var i = 0; i < instanceArrayProperty.arraySize; i++)
            {
                if (instanceArrayProperty.GetArrayElementAtIndex(i).boxedValue != null && instanceArrayProperty.GetArrayElementAtIndex(i).boxedValue is InstanceDescription instanceDescription)
                    instanceList.Add(instanceDescription);
            }

            var configName = "Instance";
            var counter = 1;
            bool nameExists = true;
            while (nameExists)
            {
                nameExists = false;
                foreach (var c in instanceList)
                {
                    if (c.Name == configName)
                    {
                        nameExists = true;
                        configName = "Instance" + $"({counter})";
                        counter++;
                        break;
                    }
                }
            }

            return configName;
        }

        private VisualElement CreateEditorInstancesElement()
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
            var multiplayerPlaymodeProperty = serializedObject.FindProperty("m_MainEditorInstance");
            var mainEditorField = new PropertyField(multiplayerPlaymodeProperty);
            content.Add(mainEditorField);

            // Editor Instances List
            var editorInstancesProperty = serializedObject.FindProperty("m_EditorInstances");
            var additionalEditors = new PropertyField(editorInstancesProperty) { label = "Additional Editor Instances", name = k_EditorInstancesContainerName };
            additionalEditors.AddToClassList("instances-group");

            //Todo: Found no better way for now, AttachToPanelEvent is not working.
            additionalEditors.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var listView = additionalEditors.Q<ListView>();

                // Use the reorderable flag to check if the setup already happened.
                if (listView == null || listView.reorderable == false)
                    return;

                SetupListView<CloneEditorController, VirtualEditorInstanceDescription>(listView, serializedObject.FindProperty("m_EditorInstances"), MaxEditorInstanceCount,
                    "Initial Editor Instances when entering playmode. Editor Instances will only have limited authoring capabilities.");
            });

            content.Add(additionalEditors);
            return container;
        }

        private VisualElement CreateLocalInstancesElement()
        {
            var container = new VisualElement();
            container.AddToClassList("instances-group");

            var localInstancesProperty = serializedObject.FindProperty("m_LocalInstances");
            var localInstanceUI = new PropertyField(localInstancesProperty) { name = k_LocalInstanceListName };

            localInstanceUI.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var listView = localInstanceUI.Q<ListView>();

                // Use the reorderable flag to check if the setup already happened.
                if (listView == null || listView.reorderable == false)
                    return;
                SetupListView<LocalPlayerController, LocalInstanceDescription>(listView, serializedObject.FindProperty("m_LocalInstances"), MaxLocalInstanceCount,
                    "Local Instances are builds that will run on the same machine as the editor.");
            });

            container.Add(localInstanceUI);
            return container;
        }

        private void RefreshListViewAddRemoveToggles<TController>(ListView listView, SerializedProperty listProperty)
            where TController : InstanceController
        {
            if (listProperty?.arraySize == 0)
                return;

            // Disable removal from the list view if a virtual instance is active.
            var scenario = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
            var isFreeRunningActive = scenario != null &&
                                      scenario.Scenario != null &&
                                      scenario.Scenario.HasActiveFreeRunInstanceOfType<TController>();

            var toolTipText  = isFreeRunningActive ? k_DisabledInstanceHelpBoxText : "";
            listView.allowRemove = !isFreeRunningActive;
            listView.allowAdd = !isFreeRunningActive;
            listView.Q<Button>(k_ListViewRemoveButton).tooltip = toolTipText;
            listView.Q<Button>(k_ListViewAddButton).tooltip = toolTipText;
        }
    }
}
