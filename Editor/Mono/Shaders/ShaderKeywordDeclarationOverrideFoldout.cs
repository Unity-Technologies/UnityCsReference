// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Shaders
{
    [UxmlElement]
    internal partial class ShaderKeywordDeclarationOverrideFoldout : Foldout
    {
        public ShaderBuildSettingsUI ParentShaderBuildSettingsUI { get; set; }
        public List<ShaderBuildSettings.KeywordDeclarationOverride> DataSource { get; set; }
        public int DataIndex { get; set; } = -1;

        private VisualElement m_Header;
        private TextField m_KeywordsField;
        private DropdownField m_VariantGenerationModeDropdown;
        private Toggle m_PruneKeywordsCheckbox;
        private VisualElement m_FoldoutArrow;
        private HelpBox m_ErrorBox;

        public ShaderKeywordDeclarationOverrideFoldout() : base()
        {
            VisualTreeAsset overrideItem = EditorGUIUtility.Load("ShaderBuildSettings/UXML/ShaderKeywordDeclarationOverride.uxml") as VisualTreeAsset;
            m_Header = overrideItem.Instantiate();

            // The Foldout's header is a Toggle element.
            var toggleElement = this.Q<Toggle>();

            // Register a callback that prevents foldout from opening if the prune keywords toggle is unchecked
            toggleElement.RegisterValueChangedCallback(delegate (ChangeEvent<bool> evt)
            {
                if (m_PruneKeywordsCheckbox.value == false && toggleElement.value != false)
                    toggleElement.value = false;
            });

            m_FoldoutArrow = toggleElement.Q(className: checkmarkUssClassName);

            // The Toggle element contains an "input" VisualElement containing the checkmark (arrow) 
            // and the original text label. We want to add our custom header into this "input" container.
            var inputContainer = toggleElement.Q(className: inputUssClassName);

            // Remove the default label element
            var defaultLabel = inputContainer.Q<Label>(className: textUssClassName);
            if (defaultLabel != null)
            {
                inputContainer.Remove(defaultLabel);
            }

            // Add our custom header
            inputContainer.Add(m_Header);
            m_Header.style.flexGrow = 1f;
            m_Header.style.flexShrink = 1f;
            m_Header.style.minWidth = 0;

            m_ErrorBox = m_Header.Q<HelpBox>("KeywordDeclarationOverrideError");

            // Register state change callbacks on the UI elements in the header
            m_KeywordsField = m_Header.Q<TextField>("KeywordListField");
            m_KeywordsField.RegisterCallback<ChangeEvent<string>>(OnKeywordListChanged);
            m_VariantGenerationModeDropdown = m_Header.Q<DropdownField>("VariantGenerationModeDropdown");
            m_VariantGenerationModeDropdown.RegisterCallback<ChangeEvent<string>>(OnVariantGenerationModeDropdownChanged);
            m_PruneKeywordsCheckbox = m_Header.Q<Toggle>("PruneKeywordsToggle");
            m_PruneKeywordsCheckbox.RegisterCallback<ChangeEvent<bool>>(OnPruneKeywordsCheckboxChanged);
        }

        public void SetKeywords(ShaderBuildSettings.KeywordOverrideInfo[] keywords)
        {
            var dataItem = DataSource[DataIndex];
            if (keywords == null)
                keywords = new ShaderBuildSettings.KeywordOverrideInfo[0];

            dataItem.keywords = keywords;
            DataSource[DataIndex] = dataItem;
            bool pruneKeywords = false;

            for (int i = 0, n = keywords.Length; i < n; ++i)
            {
                Toggle toggle;
                if (contentContainer.childCount > i)
                {
                    var element = contentContainer.ElementAt(i);
                    var label = element.Q<Label>();
                    label.text = keywords[i].name;
                    toggle = element.Q<Toggle>();
                }
                else
                {
                    var element = new VisualElement();
                    element.style.flexDirection = FlexDirection.Row;
                    toggle = new Toggle("");
                    var label = new Label(keywords[i].name);
                    element.Add(toggle);
                    element.Add(label);
                    contentContainer.Add(element);
                }
                toggle.userData = i;
                toggle.SetValueWithoutNotify(keywords[i].keepInBuild);
                toggle.RegisterCallback<ChangeEvent<bool>>(OnKeywordIncludedToggleChanged);
                pruneKeywords |= !keywords[i].keepInBuild;
            }

            while (contentContainer.childCount > keywords.Length)
            {
                contentContainer.RemoveAt(contentContainer.childCount - 1);
            }

            string validationMsg;
            if (!dataItem.IsValid(out validationMsg))
            {
                m_ErrorBox.text = validationMsg;
                m_ErrorBox.style.display = DisplayStyle.Flex;

            }
            else
            {
                m_ErrorBox.style.display = DisplayStyle.None;
            }
        }

        private void OnKeywordListChanged(ChangeEvent<string> evt)
        {
            var dataItem = DataSource[DataIndex];
            string[] keywords = evt.newValue.Trim().Split(' ');
            var kwInfoList = new ShaderBuildSettings.KeywordOverrideInfo[keywords.Length];
            
            int i = 0;
            foreach (string keyword in keywords)
            {
                bool keepKw = true;
                if (dataItem.keywords != null && dataItem.keywords.Length > i
                    && keyword == dataItem.keywords[i].name)
                {
                    keepKw = dataItem.keywords[i].keepInBuild;
                }

                kwInfoList[i] = new ShaderBuildSettings.KeywordOverrideInfo(keyword, keepKw);
                i++;
            }

            SetKeywords(kwInfoList);
            ParentShaderBuildSettingsUI.SettingsChanged();
        }

        private void OnVariantGenerationModeDropdownChanged(ChangeEvent<string> evt)
        {
            var dataItem = DataSource[DataIndex];
            int index = m_VariantGenerationModeDropdown.choices.IndexOf(evt.newValue);
            Debug.Assert(index >= 0 && index <= (int)ShaderBuildSettings.ShaderVariantGenerationMode.SingleVariantWithDynamicBranching);
            dataItem.variantGenerationMode = (ShaderBuildSettings.ShaderVariantGenerationMode)index;
            DataSource[DataIndex] = dataItem;

            ParentShaderBuildSettingsUI.SettingsChanged();
        }

        private void ShowFoldoutArrow(bool show)
        {
            m_FoldoutArrow.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        public void UpdatePruningCheckbox()
        {
            // If the checkbox is disabled we want to ensure that the foldout is hidden
            // and minimized. In the enabled case we let the item be as it is.
            if (!m_PruneKeywordsCheckbox.value)
            {
                ShowFoldoutArrow(false);
                value = false;
            }
        }

        public void ResetPruningCheckboxBasedOnLoadedData()
        {
            var dataItem = DataSource[DataIndex];
            value = false; // Always load items with foldout minimized

            foreach (var keyword in dataItem.keywords)
            {
                if (!keyword.keepInBuild)
                {
                    m_PruneKeywordsCheckbox.SetValueWithoutNotify(true);
                    ShowFoldoutArrow(true);
                    return;
                }
            }

            m_PruneKeywordsCheckbox.SetValueWithoutNotify(false);
            ShowFoldoutArrow(false);
        }

        private void OnPruneKeywordsCheckboxChanged(ChangeEvent<bool> evt)
        {
            ShowFoldoutArrow(evt.newValue);
            if (evt.newValue != evt.previousValue)
                value = evt.newValue;

            ParentShaderBuildSettingsUI.SettingsChanged();
        }

        private void OnKeywordIncludedToggleChanged(ChangeEvent<bool> evt)
        {
            var dataIitem = DataSource[DataIndex];
            var toggle = evt.target as Toggle;
            int kwIndex = (int)toggle.userData;
            dataIitem.keywords[kwIndex].keepInBuild = evt.newValue;

            ParentShaderBuildSettingsUI.SettingsChanged();
        }
    }
}
