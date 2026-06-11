// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Shaders
{
    internal class ShaderBuildSettingsUI
    {
        private List<ShaderBuildSettings.KeywordDeclarationOverride> m_KeywordDeclarationOverrides = new();
        private List<string> m_ConstantDefines = new();
        private List<string> m_InternalConstantDefines = new();
        private List<ShaderBuildSettings.ShaderCompilerSettings> m_CompilerBackendSettings = new();
        private bool[] m_LoadedItemInitialized = Array.Empty<bool>();
        private SerializedObject m_SettingsDataStore = null;
        private SerializedProperty m_SettingsProperty = null;
        private bool m_IsTargetingBuildProfile = false;
        private bool m_HasUnsavedChanges = false;
        private BuildProfile m_CachedBuildProfile = null;
        private BuildTarget? m_LastBuildTarget = null;
        private List<GraphicsDeviceType> m_SelectableApisCache = null;

        private ListView m_KeywordDeclarationOverridesListView;
        private ListView m_ConstantDefinesListView;
        private ListView m_CompilerBackendListView;
        private HelpBox m_CompilerBackendEmptyApisHelpBox;
        private Button m_ApplyButton;
        private Button m_RevertButton;

        private VisualTreeAsset m_ConstantDefineUXML;
        private VisualTreeAsset m_CompilerBackendRowUXML;

        private static string CompilerDisplayName(ShaderBuildSettings.ShaderCompilerToolchain compiler)
        {
            switch (compiler)
            {
                case ShaderBuildSettings.ShaderCompilerToolchain.FXC: return L10n.Tr("DirectX 11 Shader Compiler (FXC)");
                case ShaderBuildSettings.ShaderCompilerToolchain.DXC: return L10n.Tr("DirectX 12 Shader Compiler (DXC)");
                case ShaderBuildSettings.ShaderCompilerToolchain.Default: return L10n.Tr("Default");
                default: return compiler.ToString();
            }
        }

        private static string CompilerTooltip(ShaderBuildSettings.ShaderCompilerToolchain compiler)
        {
            switch (compiler)
            {
                case ShaderBuildSettings.ShaderCompilerToolchain.FXC:
                case ShaderBuildSettings.ShaderCompilerToolchain.DXC:
                    return L10n.Tr("Compiler used for shaders targeting the selected graphics API. Per-shader '#pragma use_dxc' / '#pragma never_use_dxc' directives override this project setting.");
                case ShaderBuildSettings.ShaderCompilerToolchain.Default:
                default:
                    return L10n.Tr("Compiler used for shaders targeting the selected graphics API.");
            }
        }

        public bool HasUnsavedChanges => m_HasUnsavedChanges;

        public void Initialize(VisualElement root, SerializedObject settingsDataStore, bool isTargetingBuildProfile)
        {
            m_IsTargetingBuildProfile = isTargetingBuildProfile;
            m_SettingsDataStore = settingsDataStore;
            if (m_SettingsDataStore != null)
                m_SettingsProperty = m_SettingsDataStore.FindProperty("m_ShaderBuildSettings");

            m_CachedBuildProfile = null;
            m_LastBuildTarget = null;
            m_SelectableApisCache = null;
            if (m_IsTargetingBuildProfile && m_SettingsDataStore != null && m_SettingsDataStore.targetObject != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(m_SettingsDataStore.targetObject);
                if (!string.IsNullOrEmpty(assetPath))
                    m_CachedBuildProfile = AssetDatabase.LoadMainAssetAtPath(assetPath) as BuildProfile;
            }

            var shaderBuildSettingsUI = root.Q<VisualElement>("ShaderBuildSettings");

            m_ConstantDefineUXML = EditorGUIUtility.Load("ShaderBuildSettings/UXML/ShaderConstantDefine.uxml") as VisualTreeAsset;
            m_CompilerBackendRowUXML = EditorGUIUtility.Load("ShaderBuildSettings/UXML/ShaderCompilerBackendRow.uxml") as VisualTreeAsset;

            m_KeywordDeclarationOverridesListView = shaderBuildSettingsUI.Q<ListView>("KeywordDeclarationOverrides");
            m_KeywordDeclarationOverridesListView.itemsSource = m_KeywordDeclarationOverrides;
            m_KeywordDeclarationOverridesListView.bindItem = BindKeywordFoldoutItem;
            m_KeywordDeclarationOverridesListView.unbindItem = UnbindKeywordFoldoutItem;
            m_KeywordDeclarationOverridesListView.makeItem = MakeKeywordFoldoutItem;

            m_KeywordDeclarationOverridesListView.itemsAdded += OnItemsAdded;
            m_KeywordDeclarationOverridesListView.itemsRemoved += OnItemsRemoved;
            m_KeywordDeclarationOverridesListView.itemIndexChanged += OnItemIndexChanged;

            m_ConstantDefinesListView = shaderBuildSettingsUI.Q<ListView>("ShaderConstDefines");
            m_ConstantDefinesListView.itemsSource = m_ConstantDefines;
            m_ConstantDefinesListView.makeItem = MakeConstantDefineItem;
            m_ConstantDefinesListView.bindItem = BindConstantDefineItem;
            m_ConstantDefinesListView.unbindItem = UnbindConstantDefineItem;
            m_ConstantDefinesListView.itemsAdded += OnItemsAdded;
            m_ConstantDefinesListView.itemsRemoved += OnItemsRemoved;
            m_ConstantDefinesListView.itemIndexChanged += OnItemIndexChanged;

            var compilerBackendFoldout = shaderBuildSettingsUI.Q<Foldout>("CompilerBackendFoldout");
            if (m_IsTargetingBuildProfile)
            {
                m_CompilerBackendListView = shaderBuildSettingsUI.Q<ListView>("CompilerBackendList");
                m_CompilerBackendListView.itemsSource = m_CompilerBackendSettings;
                m_CompilerBackendListView.makeItem = MakeCompilerBackendItem;
                m_CompilerBackendListView.bindItem = BindCompilerBackendItem;
                m_CompilerBackendListView.unbindItem = UnbindCompilerBackendItem;
                m_CompilerBackendListView.itemsRemoved += OnCompilerBackendItemsRemoved;
                m_CompilerBackendListView.overridingAddButtonBehavior = AddCompilerBackendRow;

                m_CompilerBackendEmptyApisHelpBox = new HelpBox(
                    L10n.Tr("Shader compiler toolchain selection is not available for this build target. Unity will use the default toolchain."),
                    HelpBoxMessageType.Info);
                m_CompilerBackendEmptyApisHelpBox.style.display = DisplayStyle.None;
                compilerBackendFoldout?.Insert(0, m_CompilerBackendEmptyApisHelpBox);
            }
            else if (compilerBackendFoldout != null)
            {
                var listView = shaderBuildSettingsUI.Q<ListView>("CompilerBackendList");
                if (listView != null)
                    listView.style.display = DisplayStyle.None;
                var helpBox = new HelpBox(
                    L10n.Tr("Shader compiler toolchain selection is configured per Build Profile."),
                    HelpBoxMessageType.Info);
                compilerBackendFoldout.Insert(0, helpBox);
            }

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
            keywordsField.value = keywords;
            customFoldout.SetKeywords(dataItem.keywords);

            var variantGenerationModeDropdown = customFoldout.Q<DropdownField>("VariantGenerationModeDropdown");
            variantGenerationModeDropdown.index = (int)dataItem.variantGenerationMode;

            customFoldout.RegisterChangeEventCallbacks();
        }

        private void UnbindKeywordFoldoutItem(VisualElement element, int index)
        {
            var customFoldout = element.Q<ShaderKeywordDeclarationOverrideFoldout>();
            customFoldout.UnregisterChangeEventCallbacks();
        }

        private VisualElement MakeConstantDefineItem()
        {
            return m_ConstantDefineUXML.Instantiate();
        }

        private void ValidateConstantDefineAndUpdateErrorBox(VisualElement element, string define)
        {
            string identifier, value, validationMsg = "";
            bool isValid = true;

            // Do in-place validation only after user has typed at least one character after a whitespace.
            // This is so that the start of the typing would not show the validation errors. Apply-time validation
            // will catch any invalid input that this does not.
            if (define != null)
            {
                int spaceIndex = define.IndexOf(' ');
                if (spaceIndex >= 0 && spaceIndex < (define.Length - 1)) 
                    isValid = ShaderBuildSettings.SplitAndValidateDefine(define, out identifier, out value, out validationMsg);
            }

            var errorBox = element.Q<HelpBox>("DefineError");
            if (errorBox != null)
            {
                if (!isValid)
                {
                    errorBox.text = validationMsg;
                    errorBox.style.display = DisplayStyle.Flex;
                }
                else
                {
                    errorBox.style.display = DisplayStyle.None;
                }
            }
        }

        private void BindConstantDefineItem(VisualElement element, int index)
        {
            var textField = element.Q<TextField>("DefineField");
            if (textField == null)
                return;

            if (index < m_ConstantDefines.Count)
            {
                textField.value = m_ConstantDefines[index];
                textField.userData = index;
                ValidateConstantDefineAndUpdateErrorBox(element, m_ConstantDefines[index]);
            }

            textField.RegisterValueChangedCallback(OnConstantDefineValueChanged);
        }

        private void UnbindConstantDefineItem(VisualElement element, int index)
        {
            var textField = element.Q<TextField>("DefineField");
            if (textField != null)
            {
                textField.UnregisterValueChangedCallback(OnConstantDefineValueChanged);
            }
        }

        private void OnConstantDefineValueChanged(ChangeEvent<string> evt)
        {
            var textField = evt.target as TextField;
            if (textField != null && textField.userData is int index)
            {
                if (index >= 0 && index < m_ConstantDefines.Count)
                {
                    m_ConstantDefines[index] = evt.newValue;
                    ValidateConstantDefineAndUpdateErrorBox(textField.parent, evt.newValue);
                    SettingsChanged();
                }
            }
        }

        public void SettingsChanged()
        {
            m_ApplyButton.SetEnabled(true);
            m_RevertButton.SetEnabled(true);
            m_HasUnsavedChanges = true;
        }

        private void ClearSettingsChangedState()
        {
            m_HasUnsavedChanges = false;
            if (m_ApplyButton == null)
                return;
            m_ApplyButton.SetEnabled(false);
            m_RevertButton.SetEnabled(false);
        }

        private void OnItemsAdded(IEnumerable<int> items)
        {
            SettingsChanged();
        }

        private void OnItemsRemoved(IEnumerable<int> items)
        {
            SettingsChanged();
        }

        private void OnItemIndexChanged(int oldIndex, int newIndex)
        {
            SettingsChanged();
        }

        public void HandleUnsavedChangesDialog(string buildProfileName = null)
        {
            if (!m_HasUnsavedChanges)
                return;

            string message = string.IsNullOrEmpty(buildProfileName)
                ? L10n.Tr("Shader Build Settings have been modified.\nDo you want to apply changes?")
                : string.Format(L10n.Tr("Shader Build Settings have been modified in build profile \"{0}\".\nDo you want to apply changes?"), buildProfileName);

            if (EditorUtility.DisplayDialog(
                L10n.Tr("Unapplied Changes"),
                message,
                L10n.Tr("Apply"),
                L10n.Tr("Revert")))
            {
                ApplySettings();
            }
            else
            {
                LoadSettingsData();
                ClearSettingsChangedState();
            }
        }

        private void ApplySettings()
        {
            string kdoValidationErrorMsg, defineValidationErrorMsg, compilerValidationErrorMsg;
            int internalDefineCount = m_InternalConstantDefines.Count;
            m_InternalConstantDefines.InsertRange(m_InternalConstantDefines.Count, m_ConstantDefines); // temporarily combine the two define lists
            bool isValidData = ShaderBuildSettings.ValidateKeywordDeclarationOverrides(m_KeywordDeclarationOverrides.ToArray(), out kdoValidationErrorMsg);
            isValidData &= ShaderBuildSettings.ValidateDefinesInternal(m_InternalConstantDefines.ToArray(), (uint)internalDefineCount, out defineValidationErrorMsg);
            compilerValidationErrorMsg = "";
            if (m_IsTargetingBuildProfile)
                isValidData &= ShaderBuildSettings.ValidateShaderCompilerSettings(m_CompilerBackendSettings.ToArray(), out compilerValidationErrorMsg);
            m_InternalConstantDefines.RemoveRange(internalDefineCount, m_InternalConstantDefines.Count - internalDefineCount); // revert the list back

            if (isValidData)
            {
                SaveSettingsData();
                ClearSettingsChangedState();
            }
            else
            {
                if (kdoValidationErrorMsg.Length > 0)
                    Debug.LogError(kdoValidationErrorMsg);

                if (defineValidationErrorMsg.Length > 0)
                    Debug.LogError(defineValidationErrorMsg);

                if (compilerValidationErrorMsg.Length > 0)
                    Debug.LogError(compilerValidationErrorMsg);
            }
        }

        private void OnApplyClicked(ClickEvent evt)
        {
            ApplySettings();
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

        private SerializedProperty GetConstantDefinesProperty(out int firstUserDefineIndex)
        {
            var indexProp = m_SettingsProperty.FindPropertyRelative("numInternalDefines");
            if (indexProp != null && indexProp.intValue >= 0)
            {
                firstUserDefineIndex = indexProp.intValue;
            }
            else
            {
                firstUserDefineIndex = 0;
            }

            return m_SettingsProperty.FindPropertyRelative("defines");
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

        private SerializedProperty GetCompilerSettingsProperty()
        {
            return m_SettingsProperty?.FindPropertyRelative("compilerSettings");
        }

        private void LoadSettingsData()
        {
            // Clean stale selections before shrinking the list of keywords.
            m_KeywordDeclarationOverridesListView.ClearSelection();
            m_ConstantDefinesListView.ClearSelection();

            m_KeywordDeclarationOverrides.Clear();
            m_KeywordDeclarationOverridesListView.RefreshItems();
            m_InternalConstantDefines.Clear();
            m_ConstantDefines.Clear();
            m_ConstantDefinesListView.RefreshItems();
            m_CompilerBackendSettings.Clear();
            m_CompilerBackendListView?.RefreshItems();

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

                int firstUserDefineIndex;
                var constantDefinesProp = GetConstantDefinesProperty(out firstUserDefineIndex);
                if (constantDefinesProp != null)
                {
                    for (int i = 0, n = firstUserDefineIndex; i < n; ++i)
                    {
                        var element = constantDefinesProp.GetArrayElementAtIndex(i);
                        m_InternalConstantDefines.Add(element.stringValue);
                    }

                    for (int i = firstUserDefineIndex, n = constantDefinesProp.arraySize; i < n; ++i)
                    {
                        var element = constantDefinesProp.GetArrayElementAtIndex(i);
                        m_ConstantDefines.Add(element.stringValue);
                    }
                }

                if (m_IsTargetingBuildProfile)
                {
                    var compilerSettingsProp = GetCompilerSettingsProperty();
                    if (compilerSettingsProp != null)
                    {
                        for (int i = 0, n = compilerSettingsProp.arraySize; i < n; ++i)
                        {
                            var element = compilerSettingsProp.GetArrayElementAtIndex(i);
                            var apiProp = element.FindPropertyRelative("graphicsAPI");
                            var compilerProp = element.FindPropertyRelative("compilerToolchainOverride");
                            m_CompilerBackendSettings.Add(new ShaderBuildSettings.ShaderCompilerSettings
                            {
                                graphicsAPI = (GraphicsDeviceType)apiProp.intValue,
                                compilerToolchainOverride = (ShaderBuildSettings.ShaderCompilerToolchain)compilerProp.intValue,
                            });
                        }
                    }
                }
            }

            m_LoadedItemInitialized = new bool[m_KeywordDeclarationOverrides.Count]; // Defaults to false

            m_KeywordDeclarationOverridesListView.RefreshItems();
            m_ConstantDefinesListView.RefreshItems();
            m_CompilerBackendListView?.RefreshItems();
            UpdateAddCompilerBackendButtonState();
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

                int firstUserDefineIndex;
                var constantDefinesProp = GetConstantDefinesProperty(out firstUserDefineIndex);
                if (constantDefinesProp != null)
                {
                    // resize the array to match current data amount
                    constantDefinesProp.arraySize = m_ConstantDefines.Count + firstUserDefineIndex;

                    // then set the user define values (leave the internal ones untouched)
                    for (int i = 0, n = m_ConstantDefines.Count; i < n; ++i)
                    {
                        var element = constantDefinesProp.GetArrayElementAtIndex(firstUserDefineIndex + i);
                        element.stringValue = m_ConstantDefines[i];
                    }
                }

                if (m_IsTargetingBuildProfile)
                {
                    var compilerSettingsProp = GetCompilerSettingsProperty();
                    if (compilerSettingsProp != null)
                    {
                        compilerSettingsProp.arraySize = m_CompilerBackendSettings.Count;
                        for (int i = 0, n = m_CompilerBackendSettings.Count; i < n; ++i)
                        {
                            var element = compilerSettingsProp.GetArrayElementAtIndex(i);
                            element.FindPropertyRelative("graphicsAPI").intValue = (int)m_CompilerBackendSettings[i].graphicsAPI;
                            element.FindPropertyRelative("compilerToolchainOverride").intValue = (int)m_CompilerBackendSettings[i].compilerToolchainOverride;
                        }
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

        // --- Shader Compiler Backend Selection -----------------------------------------------

        private VisualElement MakeCompilerBackendItem()
        {
            var element = m_CompilerBackendRowUXML.Instantiate();
            FillSlotWithPopup<GraphicsDeviceType>(element, "GraphicsAPIDropdown",
                "compiler-backend-api-dropdown", v => v.ToString(), OnCompilerBackendApiChanged);
            FillSlotWithPopup<ShaderBuildSettings.ShaderCompilerToolchain>(element, "CompilerDropdown",
                "compiler-backend-compiler-dropdown", CompilerDisplayName, OnCompilerBackendCompilerChanged);
            return element;
        }

        // UXML can't express PopupField<T> for non-string T; the row template uses an empty VisualElement
        // slot that this swaps for the typed PopupField at the same parent/index.
        private static void FillSlotWithPopup<T>(VisualElement root, string name, string ussClass,
            Func<T, string> formatter, EventCallback<ChangeEvent<T>> changedCallback)
        {
            var slot = root.Q<VisualElement>(name);
            if (slot == null)
                return;
            var parent = slot.parent;
            int idx = parent.IndexOf(slot);
            parent.Remove(slot);
            var popup = new PopupField<T>
            {
                name = name,
                choices = new List<T>(),
                formatListItemCallback = formatter,
                formatSelectedValueCallback = formatter,
            };
            popup.AddToClassList(ussClass);
            popup.RegisterValueChangedCallback(changedCallback);
            parent.Insert(idx, popup);
        }

        private BuildTarget GetCurrentBuildTarget()
        {
            if (m_IsTargetingBuildProfile && m_CachedBuildProfile != null)
                return m_CachedBuildProfile.buildTarget;
            return EditorUserBuildSettings.activeBuildTarget;
        }

        // Cached; invalidated only on build target change.
        private List<GraphicsDeviceType> GetSelectableApisForCurrentTarget()
        {
            var currentTarget = GetCurrentBuildTarget();
            if (m_SelectableApisCache != null && m_LastBuildTarget == currentTarget)
                return m_SelectableApisCache;

            m_LastBuildTarget = currentTarget;
            var result = new List<GraphicsDeviceType>();
            GraphicsDeviceType[] targetSupported;
            try
            {
                targetSupported = PlayerSettings.GetSupportedGraphicsAPIs(currentTarget)
                    ?? Array.Empty<GraphicsDeviceType>();
            }
            catch
            {
                targetSupported = Array.Empty<GraphicsDeviceType>();
            }

            for (int i = 0; i < targetSupported.Length; ++i)
            {
                var api = targetSupported[i];
                if (ShaderBuildSettings.SupportsCompilerToolchainOverride(api))
                    result.Add(api);
            }
            m_SelectableApisCache = result;
            return result;
        }

        private void BindCompilerBackendItem(VisualElement element, int index)
        {
            var apiPopup = element.Q<PopupField<GraphicsDeviceType>>("GraphicsAPIDropdown");
            var compilerPopup = element.Q<PopupField<ShaderBuildSettings.ShaderCompilerToolchain>>("CompilerDropdown");
            if (apiPopup == null || compilerPopup == null)
                return;

            if (index < 0 || index >= m_CompilerBackendSettings.Count)
                return;

            var row = m_CompilerBackendSettings[index];
            var selectable = GetSelectableApisForCurrentTarget();

            var inUseByOthers = new HashSet<GraphicsDeviceType>();
            for (int j = 0, n = m_CompilerBackendSettings.Count; j < n; ++j)
            {
                if (j != index)
                    inUseByOthers.Add(m_CompilerBackendSettings[j].graphicsAPI);
            }

            var apiChoices = new List<GraphicsDeviceType>(selectable.Count);
            for (int i = 0; i < selectable.Count; ++i)
            {
                var candidate = selectable[i];
                if (!inUseByOthers.Contains(candidate))
                    apiChoices.Add(candidate);
            }

            apiPopup.choices = apiChoices;
            apiPopup.SetValueWithoutNotify(row.graphicsAPI);
            apiPopup.tooltip = L10n.Tr("Select the graphics API this compiler choice applies to.");

            var supportedCompilers = ShaderBuildSettings.GetSupportedCompilerToolchainsForAPI(row.graphicsAPI)
                ?? new[] { ShaderBuildSettings.ShaderCompilerToolchain.Default };
            compilerPopup.choices = new List<ShaderBuildSettings.ShaderCompilerToolchain>(supportedCompilers);
            compilerPopup.SetValueWithoutNotify(row.compilerToolchainOverride);
            compilerPopup.SetEnabled(supportedCompilers.Length > 1);
            compilerPopup.tooltip = CompilerTooltip(row.compilerToolchainOverride);

            // Callbacks are registered once in MakeCompilerBackendItem; userData carries the row index they read.
            apiPopup.userData = index;
            compilerPopup.userData = index;
        }

        private void UnbindCompilerBackendItem(VisualElement element, int index)
        {
            var apiPopup = element.Q<PopupField<GraphicsDeviceType>>("GraphicsAPIDropdown");
            var compilerPopup = element.Q<PopupField<ShaderBuildSettings.ShaderCompilerToolchain>>("CompilerDropdown");
            if (apiPopup != null) apiPopup.userData = null;
            if (compilerPopup != null) compilerPopup.userData = null;
        }

        private static bool TryGetRowIndex(EventBase evt, int max, out int rowIndex)
        {
            rowIndex = -1;
            if ((evt.target as VisualElement)?.userData is not int idx)
                return false;
            if (idx < 0 || idx >= max)
                return false;
            rowIndex = idx;
            return true;
        }

        private void OnCompilerBackendApiChanged(ChangeEvent<GraphicsDeviceType> evt)
        {
            if (!TryGetRowIndex(evt, m_CompilerBackendSettings.Count, out int rowIndex))
                return;

            var api = evt.newValue;
            var row = m_CompilerBackendSettings[rowIndex];
            row.graphicsAPI = api;
            // Pre-select the recommended compiler for the new API, matching AddCompilerBackendRow.
            row.compilerToolchainOverride = PickRecommendedCompilerToolchainForAPI(api);
            m_CompilerBackendSettings[rowIndex] = row;
            m_CompilerBackendListView.RefreshItems();
            SettingsChanged();
            UpdateAddCompilerBackendButtonState();
        }

        private void OnCompilerBackendCompilerChanged(ChangeEvent<ShaderBuildSettings.ShaderCompilerToolchain> evt)
        {
            if (!TryGetRowIndex(evt, m_CompilerBackendSettings.Count, out int rowIndex))
                return;

            var compiler = evt.newValue;
            var row = m_CompilerBackendSettings[rowIndex];
            row.compilerToolchainOverride = compiler;
            m_CompilerBackendSettings[rowIndex] = row;
            if (evt.target is VisualElement el)
                el.tooltip = CompilerTooltip(compiler);
            SettingsChanged();
        }

        // Recommended = first non-Default in the native list; Default if the API has no override entry.
        private static ShaderBuildSettings.ShaderCompilerToolchain PickRecommendedCompilerToolchainForAPI(GraphicsDeviceType api)
        {
            var supported = ShaderBuildSettings.GetSupportedCompilerToolchainsForAPI(api);
            if (supported != null)
            {
                for (int i = 0; i < supported.Length; ++i)
                {
                    if (supported[i] != ShaderBuildSettings.ShaderCompilerToolchain.Default)
                        return supported[i];
                }
            }
            return ShaderBuildSettings.ShaderCompilerToolchain.Default;
        }

        private GraphicsDeviceType? FindFirstUnusedSelectableApi()
        {
            var selectable = GetSelectableApisForCurrentTarget();
            var inUse = new HashSet<GraphicsDeviceType>();
            for (int j = 0, n = m_CompilerBackendSettings.Count; j < n; ++j)
                inUse.Add(m_CompilerBackendSettings[j].graphicsAPI);

            for (int i = 0; i < selectable.Count; ++i)
            {
                if (!inUse.Contains(selectable[i]))
                    return selectable[i];
            }
            return null;
        }

        private void AddCompilerBackendRow(BaseListView listView, Button addButton)
        {
            var apiOpt = FindFirstUnusedSelectableApi();
            // Defensive: add button is gated by allowAdd, but a programmatic add could bypass it.
            if (apiOpt == null)
                return;

            var api = apiOpt.Value;
            m_CompilerBackendSettings.Add(new ShaderBuildSettings.ShaderCompilerSettings
            {
                graphicsAPI = api,
                compilerToolchainOverride = PickRecommendedCompilerToolchainForAPI(api),
            });
            m_CompilerBackendListView.RefreshItems();
            UpdateAddCompilerBackendButtonState();
            SettingsChanged();
        }

        private void OnCompilerBackendItemsRemoved(IEnumerable<int> indices)
        {
            SettingsChanged();
            // itemsRemoved fires before itemsSource updates; defer one frame to read post-removal count.
            m_CompilerBackendListView?.schedule.Execute(UpdateAddCompilerBackendButtonState);
        }

        private void UpdateAddCompilerBackendButtonState()
        {
            if (m_CompilerBackendListView == null)
                return;
            // Drives footer add-button via the public API; avoids the internal "unity-list-view__add-button" class.
            m_CompilerBackendListView.allowAdd = FindFirstUnusedSelectableApi() != null;

            bool hasSelectableApis = GetSelectableApisForCurrentTarget().Count > 0;
            m_CompilerBackendListView.style.display = hasSelectableApis ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_CompilerBackendEmptyApisHelpBox != null)
                m_CompilerBackendEmptyApisHelpBox.style.display = hasSelectableApis ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
