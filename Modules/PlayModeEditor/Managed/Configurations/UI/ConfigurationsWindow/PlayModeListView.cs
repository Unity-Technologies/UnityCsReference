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
        internal Action<PlayModeConfiguration> OnConfigSelected;
        readonly LabelWithIcon m_NewItemTextField;

        private const string k_Stylesheet = "PlayMode/UI/Framework.uss";

        internal PlayModeConfiguration ConfigurationToEdit => m_ListView.selectedItem as PlayModeConfiguration;

        internal PlayModeListView()
        {
            name = "playmode-list-view";

            PlayModeConfigurationUtils.ConfigurationAddedOrRemoved -= RefreshList;
            PlayModeConfigurationUtils.ConfigurationAddedOrRemoved += RefreshList;

            m_ListView = new ListView { fixedItemHeight = 16, selectionType = SelectionType.Single };
            m_ListView.selectionChanged += OnItemSelected;

            m_NewItemTextField = LabelWithIcon.Create("PlaymodeConfig", "");
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_NewItemTextField.name = "add-new-item-textfield";

            m_NewItemTextField.OnFinishEdit += s =>
            {
                m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                var newConfig = PlayModeConfigurationUtils.CreatePlayModeConfig(s, m_NewItemTextField.userData as Type);
                if (newConfig != null)
                {
                    TrySelect(newConfig);
                }
            };

            m_NewItemTextField.OnEdit += s =>
            {
                bool nameExists = false;
                foreach (var c in PlayModeConfigurationUtils.GetAllConfigs())
                {
                    if (c.name == s)
                    {
                        nameExists = true;
                        break;
                    }
                }
                m_NewItemTextField.InputIsValid = !nameExists;
            };

            m_NewItemTextField.OnCancel += () => m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            Add(m_NewItemTextField);
            Add(m_ListView);
            RefreshList();
            styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        }

        void OnItemSelected(IEnumerable<object> selection)
        {
            PlayModeConfiguration selectedConfig = null;
            foreach (var item in selection)
            {
                selectedConfig = item as PlayModeConfiguration;
                break;
            }
            OnConfigSelected?.Invoke(selectedConfig);
        }

        internal bool TrySelect(PlayModeConfiguration config)
        {
            var index = m_ListView.itemsSource.IndexOf(config);
            if (index != -1)
            {
                m_ListView.SetSelection(index);
                return true;
            }
            return false;
        }

        void RefreshList()
        {
            m_ListView.itemsSource = PlayModeConfigurationUtils.GetAllConfigs();
            m_ListView.Rebuild();
            m_ListView.makeItem = () =>
            {
                var element = new LabelWithIcon("", "", "_Popup");
                element.AddManipulator(InstanceListItemContextMenu(element));
                element.AddToClassList("unity-scenarios-playmode-list-view__item--scenario-window");
                element.OnFinishEdit += s =>
                {
                    var path = AssetDatabase.GetAssetPath(element.userData as PlayModeConfiguration);
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
                var config = PlayModeConfigurationUtils.GetAllConfigs()[i];
                SerializedObject so = new SerializedObject(config);
                labelAndIcon.Unbind();
                labelAndIcon.TrackSerializedObjectValue(so, _ =>
                {
                    // If the config was manually removed from file system, return.
                    if (config == null)
                        return;

                    var configIsValid = config.IsConfigurationValid(out var tooltipText);
                    labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                });
                var configIsValid = config.IsConfigurationValid(out var tooltipText);
                labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                labelAndIcon.SetIcon(config.Icon);
                labelAndIcon.Text = config.name;
                labelAndIcon.userData = config;
                labelAndIcon.tooltip = config.Description;
            };

            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig;

            // Select the config that is currently selected if not available (because maybe it just got deleted) select first
            var allConfigs = PlayModeConfigurationUtils.GetAllConfigs();
            if (currentConfig != null)
                m_ListView.selectedIndex = allConfigs.IndexOf(currentConfig);

            var selectedIndex = m_ListView.selectedIndex;
            PlayModeConfiguration selectedConfig = (selectedIndex >= 0 && selectedIndex < allConfigs.Count)
                ? allConfigs[selectedIndex]
                : null;

            // Null is allowed here, it means that the we will show the not seleccted view.
            OnConfigSelected?.Invoke(selectedConfig);
        }

        ContextualMenuManipulator InstanceListItemContextMenu(LabelWithIcon element)
        {
            return new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", _ =>
                {
                    var config = element.userData as PlayModeConfiguration;

                    var delete = EditorUtility.DisplayDialog($"{config.name}", $"Do you really want to delete {config.name}", "Delete", "Cancel");

                    if (delete)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(config));
                    }
                });

                evt.menu.AppendAction("Rename", _ => element.EnableEditMode());

                evt.menu.AppendAction("Duplicate", _ => PlayModeConfigurationUtils.CopyPlayModeConfiguration(element.userData as PlayModeConfiguration));
            });
        }

        internal void ShowAddTextField(Type type, string newItemName)
        {
            var allConfigs = PlayModeConfigurationUtils.GetAllConfigs();
            var finalName = newItemName;
            int counter = 1;
            bool nameExists = true;
            while (nameExists)
            {
                nameExists = false;
                foreach (var c in allConfigs)
                {
                    if (c.name == finalName)
                    {
                        nameExists = true;
                        finalName = newItemName + $"({counter})";
                        counter++;
                        break;
                    }
                }
            }

            m_NewItemTextField.Text = finalName;
            m_NewItemTextField.userData = type;
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            m_NewItemTextField.EnableEditMode();
        }
    }
}
