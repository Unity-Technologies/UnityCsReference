// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Listview to render all available configurations
    /// </summary>
    class PlayModeListView : VisualElement
    {
        ListView m_ListView;
        internal Action<PlayModeScenario> OnConfigSelected;
        readonly LabelWithIcon m_NewItemTextField;

        private const string k_Stylesheet = "PlayMode/UI/Framework.uss";

        internal PlayModeScenario ConfigurationToEdit => m_ListView.selectedItem as PlayModeScenario;

        internal PlayModeListView()
        {
            name = "playmode-list-view";

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_ListView = new ListView { fixedItemHeight = 16, selectionType = SelectionType.Single };
            m_ListView.selectionChanged += OnItemSelected;

            m_NewItemTextField = LabelWithIcon.Create("PlaymodeConfig", "");
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_NewItemTextField.name = "add-new-item-textfield";

            m_NewItemTextField.OnFinishEdit += s =>
            {
                m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                var uniqueName = MakeUniqueScenarioName(s);
                var newConfig = PlayModeScenarioUtils.CreatePlayModeConfig(uniqueName, m_NewItemTextField.userData as Type);
                if (newConfig != null)
                {
                    TrySelect(newConfig);
                }
            };

            m_NewItemTextField.OnCancel += () => m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            Add(m_NewItemTextField);
            Add(m_ListView);
            RefreshList();
            styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            PlayModeScenarioUtils.AssetsChanged -= RefreshList;
            PlayModeScenarioUtils.AssetsChanged += RefreshList;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            PlayModeScenarioUtils.AssetsChanged -= RefreshList;
        }

        internal bool TrySelect(PlayModeScenario scenario)
        {
            var index = m_ListView.itemsSource.IndexOf(scenario);
            if (index != -1)
            {
                m_ListView.SetSelection(index);
                return true;
            }
            return false;
        }

        void OnItemSelected(IEnumerable<object> selection)
        {
            PlayModeScenario selectedConfig = null;
            foreach (var item in selection)
            {
                selectedConfig = item as PlayModeScenario;
                break;
            }
            OnConfigSelected?.Invoke(selectedConfig);
        }

        void RefreshList()
        {
            m_ListView.itemsSource = PlayModeScenarioUtils.GetAllConfigs();
            m_ListView.Rebuild();
            m_ListView.makeItem = () =>
            {
                var element = new LabelWithIcon("", "", "_Popup");
                element.AddManipulator(InstanceListItemContextMenu(element));
                element.AddToClassList("unity-scenarios-playmode-list-view__item--scenario-window");
                element.OnFinishEdit += s =>
                {
                    var path = AssetDatabase.GetAssetPath(element.userData as PlayModeScenario);
                    AssetDatabase.RenameAsset(path, s);
                    this.schedule.Execute(() => RefreshList());
                };

                element.OnEdit += s =>
                {
                    element.InputIsValid = true;
                };

                return element;
            };

            m_ListView.bindItem = (element, i) =>
            {
                var labelAndIcon = (LabelWithIcon)element;
                var config = (PlayModeScenario)m_ListView.itemsSource[i];

                // The scenario could have been deleted
                if (config == null)
                {
                    labelAndIcon.Text = "Missing PlayModeScenario";
                    labelAndIcon.SetIcon(null);
                    labelAndIcon.userData = null;
                    labelAndIcon.tooltip = "The PlayModeScenario asset is missing or could not be loaded.";
                    return;
                }

                SerializedObject so = new SerializedObject(config);
                labelAndIcon.Unbind();
                labelAndIcon.TrackSerializedObjectValue(so, _ =>
                {
                    // If the config was manually removed from file system, return.
                    if (config == null)
                        return;

                    var configIsValid = config.IsValid(out var tooltipText);
                    labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                });
                var configIsValid = config.IsValid(out var tooltipText);
                labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                labelAndIcon.SetIcon(config.Icon);
                labelAndIcon.Text = config.name;
                labelAndIcon.userData = config;
                labelAndIcon.tooltip = config.Description;
            };

            var currentConfig = PlayModeScenarioManager.ActiveScenario;

            // Select the config that is currently selected if not available (because maybe it just got deleted) select first
            if (currentConfig != null)
                m_ListView.selectedIndex = m_ListView.itemsSource.IndexOf(currentConfig);

            var selectedIndex = m_ListView.selectedIndex;
            PlayModeScenario selectedConfig = (selectedIndex >= 0 && selectedIndex < m_ListView.itemsSource.Count)
                ? (PlayModeScenario)m_ListView.itemsSource[selectedIndex]
                : null;

            // Null is allowed here, it means that the we will show the not seleccted view.
            OnConfigSelected?.Invoke(selectedConfig);
        }

        ContextualMenuManipulator InstanceListItemContextMenu(LabelWithIcon element)
        {
            return new ContextualMenuManipulator(evt =>
            {
                var scenario = element.userData as PlayModeScenario;
                var persistentAssetActionStatus = EditorUtility.IsPersistent(scenario)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;

                evt.menu.AppendAction("Delete", _ =>
                {
                    var delete = EditorUtility.DisplayDialog($"{scenario.name}", $"Do you really want to delete {scenario.name}", "Delete", "Cancel");

                    if (delete)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(scenario));
                    }
                }, persistentAssetActionStatus);

                evt.menu.AppendAction("Rename", _ => element.EnableEditMode(), persistentAssetActionStatus);

                evt.menu.AppendAction("Duplicate", _ => PlayModeScenarioUtils.CopyPlayModeConfiguration(element.userData as PlayModeScenario), persistentAssetActionStatus);
            });
        }

        internal void ShowAddTextField(Type type, string newItemName)
        {
            m_NewItemTextField.Text = MakeUniqueScenarioName(newItemName);
            m_NewItemTextField.userData = type;
            m_NewItemTextField.InputIsValid = true;
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            m_NewItemTextField.EnableEditMode();
        }

        static string MakeUniqueScenarioName(string desiredName)
        {
            var allConfigs = PlayModeScenarioUtils.GetAllConfigs();
            var existingNames = new string[allConfigs.Count];
            for (int i = 0; i < allConfigs.Count; i++)
                existingNames[i] = allConfigs[i].name;
            return ObjectNames.GetUniqueName(existingNames, desiredName);
        }
    }
}
