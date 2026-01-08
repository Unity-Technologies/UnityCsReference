// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

[assembly: InternalsVisibleTo("UnityEditor.Rendering.ShaderBuildSettings.Tests")]

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
        private Label m_NumKeywordsLabel;
        private HelpBox m_ErrorBox;

        public ShaderKeywordDeclarationOverrideFoldout() : base()
        {
            VisualTreeAsset overrideItem = EditorGUIUtility.Load("ShaderBuildSettings/UXML/ShaderKeywordDeclarationOverride.uxml") as VisualTreeAsset;
            // Add the stylesheets from the asset here so that all the children of this get them too
            foreach (var ss in overrideItem.stylesheets)
            {
                styleSheets.Add(ss);
            }
            m_Header = overrideItem.Instantiate();

            // The Foldout's header is a Toggle element.
            var toggleElement = this.Q<Toggle>();

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
            m_NumKeywordsLabel = m_Header.Q<Label>("SelectedKeywordsLabel");

            // Collapse foldout by default
            value = false;

            // Add help text inside the foldout
            var helpTextLabel = new Label("Select keywords to keep:");
            helpTextLabel.AddToClassList("keyword-toggles-help-text");
            helpTextLabel.tooltip = "Defines the list of keywords included at this stage. This list can be further filtered by other processes, such as scriptable stripping, before the final build.";
            contentContainer.Add(helpTextLabel);
        }

        public void SetKeywords(ShaderBuildSettings.KeywordOverrideInfo[] keywords)
        {
            var dataItem = DataSource[DataIndex];
            if (keywords == null)
                keywords = new ShaderBuildSettings.KeywordOverrideInfo[0];

            dataItem.keywords = keywords;
            DataSource[DataIndex] = dataItem;
            int numIncludedKeywords = 0;

            for (int i = 0, n = keywords.Length; i < n; ++i)
            {
                Toggle toggle;
                int elementIndex = i + 1; //first element is the help text

                if (contentContainer.childCount > elementIndex)
                {
                    var element = contentContainer.ElementAt(elementIndex);
                    var label = element.Q<Label>();
                    label.text = keywords[i].name;
                    toggle = element.Q<Toggle>();
                }
                else
                {
                    var element = new VisualElement();
                    element.style.flexDirection = FlexDirection.Row;
                    toggle = new Toggle("");
                    toggle.AddToClassList("keep-keyword-toggle");
                    var label = new Label(keywords[i].name);
                    label.AddToClassList("keep-keyword-label");
                    element.Add(toggle);
                    element.Add(label);
                    contentContainer.Add(element);
                }
                toggle.userData = i;
                toggle.SetValueWithoutNotify(keywords[i].keepInBuild);
                toggle.RegisterCallback<ChangeEvent<bool>>(OnKeywordIncludedToggleChanged);

                if (keywords[i].keepInBuild)
                    numIncludedKeywords++;
            }

            while (contentContainer.childCount > (keywords.Length + 1))
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

            m_NumKeywordsLabel.text = numIncludedKeywords + "/" + keywords.Length;
        }

        internal ShaderBuildSettings.KeywordOverrideInfo[] BuildKeywordOverrideInfoArray(string keywordList, ShaderBuildSettings.KeywordDeclarationOverride previousData)
        {
            string[] keywords = keywordList.Trim().Split(' ');
            var kwInfoList = new List<ShaderBuildSettings.KeywordOverrideInfo>(keywords.Length);

            int i = 0;
            foreach (string keyword in keywords)
            {
                // Ignore empty strings (can happen due to consecutive whitespaces)
                if (keyword.Length == 0)
                    continue;

                // Preserve the previous selection state if possible by searching for a matching item
                bool keepKw = true;
                ShaderBuildSettings.KeywordOverrideInfo foundMatch ;
                if (previousData.FindMatchingKeyword(keyword, out foundMatch))
                {
                    keepKw = foundMatch.keepInBuild;
                }

                kwInfoList.Add(new ShaderBuildSettings.KeywordOverrideInfo(keyword, keepKw));
                i++;
            }

            return kwInfoList.ToArray();
        }

        private void OnKeywordListChanged(ChangeEvent<string> evt)
        {
            SetKeywords(BuildKeywordOverrideInfoArray(evt.newValue, DataSource[DataIndex]));
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

        private void OnKeywordIncludedToggleChanged(ChangeEvent<bool> evt)
        {
            var dataIitem = DataSource[DataIndex];
            var toggle = evt.target as Toggle;
            int kwIndex = (int)toggle.userData;
            dataIitem.keywords[kwIndex].keepInBuild = evt.newValue;

            SetKeywords(dataIitem.keywords);
            ParentShaderBuildSettingsUI.SettingsChanged();
        }
    }
}
