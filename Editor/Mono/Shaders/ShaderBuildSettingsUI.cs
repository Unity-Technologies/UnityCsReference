// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Shaders
{
    internal class ShaderBuildSettingsUI
    {
        private List<ShaderBuildSettings.KeywordDeclarationOverride> m_KeywordDeclarationOverrides = new();
        private bool[] m_LoadedItemInitialized = {};
        private SerializedObject m_SettingsDataStore = null;
        private SerializedProperty m_SettingsProperty = null;
        private bool m_IsTargetingBuildProfile = false;

        //private bool m_StateChanged = false;

        private ListView m_ListView;
        private Button m_ApplyButton;
        private Button m_RevertButton;

        public void Initialize(VisualElement root, SerializedObject settingsDataStore, bool isTargetingBuildProfile)
        {
            m_IsTargetingBuildProfile = isTargetingBuildProfile;
            m_SettingsDataStore = settingsDataStore;
            if (m_SettingsDataStore != null)
                m_SettingsProperty = m_SettingsDataStore.FindProperty("m_ShaderBuildSettings");

            var shaderBuildSettingsUI = root.Q<VisualElement>("ShaderBuildSettings");

            m_ListView = shaderBuildSettingsUI.Q<ListView>();
            m_ListView.itemsSource = m_KeywordDeclarationOverrides;
            m_ListView.bindItem = BindKeywordFoldoutItem;
            m_ListView.makeItem = MakeKeywordFoldoutItem;

            m_ListView.itemsAdded += OnItemsAdded;
            m_ListView.itemsRemoved += OnItemsRemoved;

            m_ApplyButton = shaderBuildSettingsUI.Q<Button>("ApplyButton");
            m_ApplyButton.RegisterCallback<ClickEvent>(OnApplyClicked);

            m_RevertButton = shaderBuildSettingsUI.Q<Button>("RevertButton");
            m_RevertButton.RegisterCallback<ClickEvent>(OnRevertClicked);

            LoadSettingsData();
        }

        private VisualElement MakeKeywordFoldoutItem()
        {
            return new ShaderKeywordDeclarationOverrideFoldout();
        }

        private void BindKeywordFoldoutItem(VisualElement element, int index)
        {
            var customFoldout = element.Q<ShaderKeywordDeclarationOverrideFoldout>();

            customFoldout.ParentShaderBuildSettingsUI = this;
            customFoldout.DataSource = m_KeywordDeclarationOverrides;
            customFoldout.DataIndex = index;

            var dataItem = m_KeywordDeclarationOverrides[index];
            string keywords = "";

            // Build up existing keyword list string for the keyword input field
            if (dataItem.keywords != null)
            {
                int kwCounter = dataItem.keywords.Length;

                foreach (var kwInfo in dataItem.keywords)
                {
                    keywords += kwInfo.name;
                    if (--kwCounter > 0)
                    {
                        keywords += ' ';
                    }
                }
            }

            // Set the keywords for the item (creates also children if needed)
            var keywordsField = customFoldout.Q<TextField>("KeywordListField");
            keywordsField.SetValueWithoutNotify(keywords);
            customFoldout.SetKeywords(dataItem.keywords);

            var variantGenerationModeDropdown = customFoldout.Q<DropdownField>("VariantGenerationModeDropdown");
            variantGenerationModeDropdown.SetIndexWithoutNotify((int)dataItem.variantGenerationMode);

            // If we're binding the first time after loading the data from the settings
            // we want to set the pruning checkbox state based on the settings data.
            // Also make sure that items are not expanded after load.
            if (index < m_LoadedItemInitialized.Length && !m_LoadedItemInitialized[index])
            {
                customFoldout.ResetPruningCheckboxBasedOnLoadedData();
                m_LoadedItemInitialized[index] = true;
            }
            else // If loading has happened already then we trigger an update logic
            {
                customFoldout.UpdatePruningCheckbox();
            }
        }

        public void SettingsChanged()
        {
            m_ApplyButton.SetEnabled(true);
            m_RevertButton.SetEnabled(true);
            //m_StateChanged = true;
        }

        private void ClearSettingsChangedState()
        {
            m_ApplyButton.SetEnabled(false);
            m_RevertButton.SetEnabled(false);
            //m_StateChanged = false;
        }

        private void OnItemsAdded(IEnumerable<int> items)
        {
            SettingsChanged();
        }

        private void OnItemsRemoved(IEnumerable<int> items)
        {
            SettingsChanged();
        }

        private void OnApplyClicked(ClickEvent evt)
        {
            string msg;
            if (ShaderBuildSettings.ValidateKeywordDeclarationOverrides(m_KeywordDeclarationOverrides.ToArray(), out msg))
            {
                SaveSettingsData();
                ClearSettingsChangedState();
            }
            else
            {
                Debug.LogError(msg);
            }
        }

        private void OnRevertClicked(ClickEvent evt)
        {
            LoadSettingsData();
            ClearSettingsChangedState();
        }

        private SerializedProperty GetKeywordDeclarationOverridesProperty()
        {
            return m_SettingsProperty.FindPropertyRelative("keywordDeclarationOverrides");
        }

        private SerializedProperty GetKeywordsProperty(SerializedProperty kwDeclarationOverridesArray, int index)
        {
            var kwoProp = kwDeclarationOverridesArray.GetArrayElementAtIndex(index);
            return kwoProp.FindPropertyRelative("keywords");
        }

        private void GetKeywordInfoProperties(SerializedProperty keywordsArray, int index, out SerializedProperty nameProp, out SerializedProperty keepInBuildProp)
        {
            var kwInfoProp = keywordsArray.GetArrayElementAtIndex(index);
            nameProp = kwInfoProp.FindPropertyRelative("name");
            keepInBuildProp = kwInfoProp.FindPropertyRelative("keepInBuild");
        }

        private SerializedProperty GetVariantGenerationModeProperty(SerializedProperty kwDeclarationOverridesArray, int index)
        {
            var kwoProp = kwDeclarationOverridesArray.GetArrayElementAtIndex(index);
            return kwoProp.FindPropertyRelative("variantGenerationMode");
        }

        private void LoadSettingsData()
        {
            m_ListView.Clear();
            m_KeywordDeclarationOverrides.Clear();

            // When this UI is used for project settings, the serialized object is created from native GraphicsSettings.
            // Therefore the boxedValue etc are not usable here and we need to find the individual serialized properties manually.
            if (m_SettingsProperty != null)
            {
                var keywordDeclarationOverridesProp = GetKeywordDeclarationOverridesProperty();
                if (keywordDeclarationOverridesProp != null)
                {
                    for (int i = 0, n = keywordDeclarationOverridesProp.arraySize; i < n; ++i)
                    {
                        var kwList = new List<ShaderBuildSettings.KeywordOverrideInfo>();
                        var keywordsProp = GetKeywordsProperty(keywordDeclarationOverridesProp, i);

                        for (int j = 0, m = keywordsProp.arraySize; j < m; ++j)
                        {
                            SerializedProperty nameProp;
                            SerializedProperty keepInBuildProp;
                            GetKeywordInfoProperties(keywordsProp, j, out nameProp, out keepInBuildProp);

                            string name = nameProp.stringValue;
                            bool keepInBuild = keepInBuildProp.boolValue;
                            var kwInfo = new ShaderBuildSettings.KeywordOverrideInfo(name, keepInBuild);
                            kwList.Add(kwInfo);
                        }

                        var vgmProp = GetVariantGenerationModeProperty(keywordDeclarationOverridesProp, i);

                        var kwo = new ShaderBuildSettings.KeywordDeclarationOverride();
                        kwo.keywords = kwList.ToArray();
                        kwo.variantGenerationMode = (ShaderBuildSettings.ShaderVariantGenerationMode)vgmProp.intValue;
                        m_KeywordDeclarationOverrides.Add(kwo);
                    }
                }
            }

            m_LoadedItemInitialized = new bool[m_KeywordDeclarationOverrides.Count]; // Defaults to false

            m_ListView.RefreshItems();
        }

        private void SaveSettingsData()
        {
            // The same manual serialized property process and reasoning as for loading the data.
            if (m_SettingsProperty != null)
            {
                var keywordDeclarationOverridesProp = GetKeywordDeclarationOverridesProperty();
                if (keywordDeclarationOverridesProp != null)
                {
                    keywordDeclarationOverridesProp.ClearArray();
                    for (int i = 0, n = m_KeywordDeclarationOverrides.Count; i < n; ++i)
                    {
                        keywordDeclarationOverridesProp.InsertArrayElementAtIndex(i);
                        var keywordsProp = GetKeywordsProperty(keywordDeclarationOverridesProp, i);
                        keywordsProp.ClearArray();

                        for (int j = 0, m = m_KeywordDeclarationOverrides[i].keywords.Length; j < m; ++j)
                        {
                            keywordsProp.InsertArrayElementAtIndex(j);
                            SerializedProperty nameProp;
                            SerializedProperty keepInBuildProp;
                            GetKeywordInfoProperties(keywordsProp, j, out nameProp, out keepInBuildProp);

                            nameProp.stringValue = m_KeywordDeclarationOverrides[i].keywords[j].name;
                            keepInBuildProp.boolValue = m_KeywordDeclarationOverrides[i].keywords[j].keepInBuild;
                        }

                        var vgmProp = GetVariantGenerationModeProperty(keywordDeclarationOverridesProp, i);
                        vgmProp.intValue = (int)m_KeywordDeclarationOverrides[i].variantGenerationMode;
                    }
                }
                m_SettingsDataStore.ApplyModifiedProperties();

                // Ensure that the re-imports are triggered if the currently active settings were touched
                BuildProfile activeBuildProfile = BuildProfile.GetActiveBuildProfile();

                bool commonGraphicsSettingsUIWithoutActiveBuildProfile = (activeBuildProfile == null && !m_IsTargetingBuildProfile);
                bool settingsTargetsActiveBuildProfile = (activeBuildProfile != null && activeBuildProfile.graphicsSettings == m_SettingsDataStore.targetObject);

                if (commonGraphicsSettingsUIWithoutActiveBuildProfile ||
                    settingsTargetsActiveBuildProfile)
                {
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
