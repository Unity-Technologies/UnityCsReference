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

        private Toggle m_EnableDebugSymbolsToggle;
        private PopupField<ShaderBuildSettings.ShaderOptimizationLevel> m_OptimizationLevelDropdown;
        private HelpBox m_DebugSymbolsWarningHelpBox;
        private HelpBox m_OptimizationLevelWarningHelpBox;
        private ShaderBuildSettings.ShaderOptimizationLevel m_OptLevelBeforeDebugForced = ShaderBuildSettings.ShaderOptimizationLevel.Default;
        private string m_DebugSymbolsBaseTooltip = string.Empty;
        private List<GraphicsDeviceType> m_LastWarnedEnabledApis;
        private BuildTarget? m_LastLoadedDebugOptTarget = null;
        private static readonly ShaderBuildSettings.ShaderOptimizationLevel[] s_OptimizationLevels =
            (ShaderBuildSettings.ShaderOptimizationLevel[])Enum.GetValues(typeof(ShaderBuildSettings.ShaderOptimizationLevel));

        private VisualTreeAsset m_ConstantDefineUXML;
        private VisualTreeAsset m_CompilerBackendRowUXML;

        private static string CompilerDisplayName(ShaderBuildSettings.ShaderCompilerToolchain compiler)
        {
            switch (compiler)
            {
                case ShaderBuildSettings.ShaderCompilerToolchain.FXC: return L10n.Tr("Effect-Compiler Tool (FXC)", null);
                case ShaderBuildSettings.ShaderCompilerToolchain.DXC: return L10n.Tr("DirectX Shader Compiler (DXC)", null);
                case ShaderBuildSettings.ShaderCompilerToolchain.Default: return L10n.Tr("Default", null);
                default: return compiler.ToString();
            }
        }

        private static string PlatformOrShared(string platformText, string sharedText)
            => string.IsNullOrEmpty(platformText) ? sharedText : platformText;

        private static string CompilerDisplayNameFor(GraphicsDeviceType api, ShaderBuildSettings.ShaderCompilerToolchain compiler)
            => PlatformOrShared(ShaderBuildSettings.GetCompilerDisplayNameForAPI(api, compiler), CompilerDisplayName(compiler));

        private static string CompilerTooltipFor(GraphicsDeviceType api, ShaderBuildSettings.ShaderCompilerToolchain compiler)
            => PlatformOrShared(ShaderBuildSettings.GetCompilerTooltipForAPI(api, compiler), CompilerTooltip(compiler));

        private static string CompilerTooltip(ShaderBuildSettings.ShaderCompilerToolchain compiler)
        {
            switch (compiler)
            {
                case ShaderBuildSettings.ShaderCompilerToolchain.FXC:
                case ShaderBuildSettings.ShaderCompilerToolchain.DXC:
                    return L10n.Tr("Compiler used for shaders targeting the selected graphics API. Per-shader '#pragma use_dxc' / '#pragma never_use_dxc' directives override this project setting.", null);
                case ShaderBuildSettings.ShaderCompilerToolchain.Default:
                default:
                    return L10n.Tr("Compiler used for shaders targeting the selected graphics API.", null);
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
            m_LastLoadedDebugOptTarget = null;
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
                    L10n.Tr("Shader compiler toolchain selection is not available for this build target. Unity will use the default toolchain.", null),
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
                    L10n.Tr("Shader compiler toolchain selection is configured per Build Profile.", null),
                    HelpBoxMessageType.Info);
                compilerBackendFoldout.Insert(0, helpBox);
            }

            SetupDebugOptControls(shaderBuildSettingsUI);

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
                ? L10n.Tr("Shader Build Settings have been modified.\nDo you want to apply changes?", null)
                : string.Format(L10n.Tr("Shader Build Settings have been modified in build profile \"{0}\".\nDo you want to apply changes?", null), buildProfileName);

            if (EditorUtility.DisplayDialog(
                L10n.Tr("Unapplied Changes", null),
                message,
                L10n.Tr("Apply", null),
                L10n.Tr("Revert", null)))
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
            var mergedCompilerSettings = BuildMergedCompilerSettings();
            isValidData &= ShaderBuildSettings.ValidateShaderCompilerSettings(mergedCompilerSettings, out compilerValidationErrorMsg);
            m_InternalConstantDefines.RemoveRange(internalDefineCount, m_InternalConstantDefines.Count - internalDefineCount); // revert the list back

            if (isValidData)
            {
                SaveSettingsData(mergedCompilerSettings);
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
                    // Opt/debug-only rows (e.g. D3D11) would render with no selectable API; LoadDebugOptAndRefreshWarnings covers them.
                    foreach (var row in ReadPersistedCompilerRows())
                    {
                        if (!ShaderBuildSettings.SupportsCompilerToolchainOverride(row.graphicsAPI))
                            continue;
                        m_CompilerBackendSettings.Add(new ShaderBuildSettings.ShaderCompilerSettings
                        {
                            graphicsAPI = row.graphicsAPI,
                            compilerToolchainOverride = row.compilerToolchainOverride,
                        });
                    }
                }
            }

            m_LoadedItemInitialized = new bool[m_KeywordDeclarationOverrides.Count]; // Defaults to false

            m_KeywordDeclarationOverridesListView.RefreshItems();
            m_ConstantDefinesListView.RefreshItems();
            m_CompilerBackendListView?.RefreshItems();
            UpdateAddCompilerBackendButtonState();

            LoadDebugOptAndRefreshWarnings();
        }

        private void SaveSettingsData(ShaderBuildSettings.ShaderCompilerSettings[] merged)
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

                var compilerSettingsProp = GetCompilerSettingsProperty();
                if (compilerSettingsProp != null)
                {
                    compilerSettingsProp.arraySize = merged.Length;
                    for (int i = 0, n = merged.Length; i < n; ++i)
                    {
                        var element = compilerSettingsProp.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("graphicsAPI").intValue = (int)merged[i].graphicsAPI;
                        element.FindPropertyRelative("compilerToolchainOverride").intValue = (int)merged[i].compilerToolchainOverride;
                        element.FindPropertyRelative("optimizationLevel").intValue = (int)merged[i].optimizationLevel;
                        element.FindPropertyRelative("enableDebugSymbols").boolValue = merged[i].enableDebugSymbols;
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

        // UXML can't express PopupField<T> for non-string T, so the template leaves an empty slot to swap out here.
        private static PopupField<T> FillSlotWithPopup<T>(VisualElement root, string name, string ussClass,
            Func<T, string> formatter, EventCallback<ChangeEvent<T>> changedCallback)
        {
            var slot = root.Q<VisualElement>(name);
            if (slot == null)
                return null;
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
            return popup;
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
            apiPopup.tooltip = L10n.Tr("Select the graphics API this compiler choice applies to.", null);

            var supportedCompilers = ShaderBuildSettings.GetSupportedCompilerToolchainsForAPI(row.graphicsAPI)
                ?? new[] { ShaderBuildSettings.ShaderCompilerToolchain.Default };
            compilerPopup.choices = new List<ShaderBuildSettings.ShaderCompilerToolchain>(supportedCompilers);
            compilerPopup.SetValueWithoutNotify(row.compilerToolchainOverride);
            compilerPopup.SetEnabled(supportedCompilers.Length > 1);
            compilerPopup.tooltip = CompilerTooltipFor(row.graphicsAPI, row.compilerToolchainOverride);

            var rowApi = row.graphicsAPI;
            Func<ShaderBuildSettings.ShaderCompilerToolchain, string> displayNameForRow = c => CompilerDisplayNameFor(rowApi, c);
            compilerPopup.formatListItemCallback = displayNameForRow;
            compilerPopup.formatSelectedValueCallback = displayNameForRow;

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
                el.tooltip = CompilerTooltipFor(row.graphicsAPI, compiler);
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
            m_CompilerBackendListView?.schedule.Execute(UpdateAddCompilerBackendButtonState);
        }

        private void UpdateAddCompilerBackendButtonState()
        {
            if (m_CompilerBackendListView == null)
                return;

            m_CompilerBackendListView.allowAdd = FindFirstUnusedSelectableApi() != null;
            bool hasSelectableApis = GetSelectableApisForCurrentTarget().Count > 0;
            m_CompilerBackendListView.style.display = hasSelectableApis ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_CompilerBackendEmptyApisHelpBox != null)
                m_CompilerBackendEmptyApisHelpBox.style.display = hasSelectableApis ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // --- Debug Symbols & Optimisation Level -----------------------------------------------

        private void SetupDebugOptControls(VisualElement shaderBuildSettingsUI)
        {
            var container = shaderBuildSettingsUI.Q<VisualElement>("CompilerDebugOptSettings");
            if (container == null)
                return;

            m_EnableDebugSymbolsToggle = shaderBuildSettingsUI.Q<Toggle>("EnableDebugSymbolsToggle");
            if (m_EnableDebugSymbolsToggle != null)
                m_DebugSymbolsBaseTooltip = m_EnableDebugSymbolsToggle.tooltip ?? string.Empty;

            m_OptimizationLevelDropdown = FillSlotWithPopup<ShaderBuildSettings.ShaderOptimizationLevel>(
                shaderBuildSettingsUI, "OptimizationLevelDropdown", "optimization-level-dropdown",
                OptimizationLevelDisplayName, OnOptimizationLevelChanged);
            if (m_OptimizationLevelDropdown != null)
            {
                m_OptimizationLevelDropdown.label = L10n.Tr("Optimisation Level", null);
                m_OptimizationLevelDropdown.choices = new List<ShaderBuildSettings.ShaderOptimizationLevel>(s_OptimizationLevels);
                m_OptimizationLevelDropdown.AddToClassList(BaseField<ShaderBuildSettings.ShaderOptimizationLevel>.alignedFieldUssClassName);
            }

            m_DebugSymbolsWarningHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_DebugSymbolsWarningHelpBox.style.display = DisplayStyle.None;
            m_OptimizationLevelWarningHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_OptimizationLevelWarningHelpBox.style.display = DisplayStyle.None;

            if (m_EnableDebugSymbolsToggle != null)
                container.Insert(container.IndexOf(m_EnableDebugSymbolsToggle) + 1, m_DebugSymbolsWarningHelpBox);
            if (m_OptimizationLevelDropdown != null)
                container.Insert(container.IndexOf(m_OptimizationLevelDropdown) + 1, m_OptimizationLevelWarningHelpBox);

            m_EnableDebugSymbolsToggle?.RegisterValueChangedCallback(OnDebugSymbolsChanged);

            container.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // Unsubscribe first so a detach/reattach can't register the handler twice.
                ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
                ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            });
            container.RegisterCallback<DetachFromPanelEvent>(_ => ObjectChangeEvents.changesPublished -= OnObjectChangesPublished);
        }

        private void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (ReloadDebugOptIfBuildTargetChanged())
                return;

            RefreshApiDependentUIIfApisChanged();
        }

        private bool ReloadDebugOptIfBuildTargetChanged()
        {
            if (m_HasUnsavedChanges || m_LastLoadedDebugOptTarget == GetCurrentBuildTarget())
                return false;

            LoadDebugOptAndRefreshWarnings();
            return true;
        }

        private void OnDebugSymbolsChanged(ChangeEvent<bool> evt)
        {
            if (m_OptimizationLevelDropdown != null)
            {
                if (evt.newValue)
                    m_OptLevelBeforeDebugForced = m_OptimizationLevelDropdown.value;
                else
                    m_OptimizationLevelDropdown.SetValueWithoutNotify(m_OptLevelBeforeDebugForced);
            }
            UpdateOptimizationLevelEnabledState();
            RefreshApiDependentUI(GetEnabledApisForCurrentTarget());
            SettingsChanged();
        }

        private void OnOptimizationLevelChanged(ChangeEvent<ShaderBuildSettings.ShaderOptimizationLevel> evt)
        {
            RefreshApiDependentUI(GetEnabledApisForCurrentTarget());
            SettingsChanged();
        }


        private void UpdateOptimizationLevelEnabledState()
        {
            if (m_OptimizationLevelDropdown == null || m_EnableDebugSymbolsToggle == null)
                return;

            bool debugOn = m_EnableDebugSymbolsToggle.value;
            if (debugOn && m_OptimizationLevelDropdown.value != ShaderBuildSettings.ShaderOptimizationLevel.Disabled)
                m_OptimizationLevelDropdown.SetValueWithoutNotify(ShaderBuildSettings.ShaderOptimizationLevel.Disabled);
            m_OptimizationLevelDropdown.SetEnabled(!debugOn);
            m_OptimizationLevelDropdown.tooltip = debugOn
                ? L10n.Tr("Optimisation is disabled while debug symbols are enabled.", null)
                : L10n.Tr("Optimisation level is applied to shaders for graphics APIs that support it.", null);
        }

        private static string OptimizationLevelDisplayName(ShaderBuildSettings.ShaderOptimizationLevel level)
        {
            switch (level)
            {
                case ShaderBuildSettings.ShaderOptimizationLevel.Default: return L10n.Tr("Default", null);
                case ShaderBuildSettings.ShaderOptimizationLevel.Disabled: return L10n.Tr("Disabled", null);
                case ShaderBuildSettings.ShaderOptimizationLevel.Low: return L10n.Tr("Low", null);
                case ShaderBuildSettings.ShaderOptimizationLevel.Medium: return L10n.Tr("Medium", null);
                case ShaderBuildSettings.ShaderOptimizationLevel.High: return L10n.Tr("High", null);
                default: return level.ToString();
            }
        }

        private static string ApiDisplayNameFor(GraphicsDeviceType api)
            => PlatformOrShared(ShaderBuildSettings.GetDisplayNameForAPI(api), ApiDisplayName(api));

        private static string ApiDisplayName(GraphicsDeviceType api)
        {
            switch (api)
            {
                case GraphicsDeviceType.Direct3D11: return L10n.Tr("DirectX 11 (FXC)", null);
                case GraphicsDeviceType.Direct3D12: return L10n.Tr("DirectX 12 (DXC)", null);
                case GraphicsDeviceType.Vulkan: return L10n.Tr("Vulkan", null);
                case GraphicsDeviceType.Metal: return L10n.Tr("Metal", null);
                case GraphicsDeviceType.OpenGLCore: return L10n.Tr("OpenGL (GLSL)", null);
                case GraphicsDeviceType.OpenGLES3: return L10n.Tr("OpenGL ES (GLSL)", null);
                case GraphicsDeviceType.WebGPU: return L10n.Tr("WebGPU", null);
                default: return api.ToString();
            }
        }

        // Warnings compare the single chosen value against the APIs the project actually builds for.
        private List<GraphicsDeviceType> GetEnabledApisForCurrentTarget()
        {
            var result = new List<GraphicsDeviceType>();
            try
            {
                var target = GetCurrentBuildTarget();
                // GetGraphicsAPIs reflects the active/global profile, not the one being edited, so prefer the edited profile's own override.
                GraphicsDeviceType[] apis = null;
                if (m_IsTargetingBuildProfile && m_CachedBuildProfile != null && m_CachedBuildProfile.buildProfilePlayerSettings != null)
                    apis = m_CachedBuildProfile.buildProfilePlayerSettings.GetGraphicsAPIs(target);

                apis ??= GetProjectSettingsGraphicsApis(target);
                apis ??= PlayerSettings.GetGraphicsAPIs(target);
                result.AddRange(apis ?? Array.Empty<GraphicsDeviceType>());
            }
            catch
            {
            }
            return result;
        }

        private static GraphicsDeviceType[] GetProjectSettingsGraphicsApis(BuildTarget target)
        {
            var projectSettingsPlayerSettings = PlayerSettings.GetProjectSettingsPlayerSettings();
            return projectSettingsPlayerSettings != null ? projectSettingsPlayerSettings.GetGraphicsAPIs_Internal(target) : null;
        }

        private void RefreshCapabilityWarnings(List<GraphicsDeviceType> enabledApis)
        {
            if (m_DebugSymbolsWarningHelpBox == null || m_OptimizationLevelWarningHelpBox == null)
                return;

            bool debugOn = m_EnableDebugSymbolsToggle != null && m_EnableDebugSymbolsToggle.value;
            var currentOptLevel = m_OptimizationLevelDropdown != null
                ? m_OptimizationLevelDropdown.value
                : ShaderBuildSettings.ShaderOptimizationLevel.Default;
            bool customOptOn = !debugOn && currentOptLevel != ShaderBuildSettings.ShaderOptimizationLevel.Default;

            var debugUnsupported = new List<GraphicsDeviceType>();
            var optUnsupported = new List<GraphicsDeviceType>();
            foreach (var api in enabledApis)
            {
                if (debugOn && !ShaderBuildSettings.SupportsDebugSymbols(api))
                    debugUnsupported.Add(api);
                if (customOptOn && !ShaderBuildSettings.SupportsOptimizationLevel(api))
                    optUnsupported.Add(api);
            }

            SetCapabilityWarning(m_DebugSymbolsWarningHelpBox, debugUnsupported, isDebug: true);
            SetCapabilityWarning(m_OptimizationLevelWarningHelpBox, optUnsupported, isDebug: false);
        }

        private void RefreshApiDependentUI(List<GraphicsDeviceType> enabledApis)
        {
            RefreshCapabilityWarnings(enabledApis);
            UpdateDebugSymbolsTooltip(enabledApis);
            m_LastWarnedEnabledApis = enabledApis;
        }

        private void UpdateDebugSymbolsTooltip(List<GraphicsDeviceType> enabledApis)
        {
            if (m_EnableDebugSymbolsToggle == null)
                return;

            m_EnableDebugSymbolsToggle.tooltip = PlatformOrShared(FindPlatformDebugSymbolsTooltip(enabledApis), m_DebugSymbolsBaseTooltip);
        }

        private static string FindPlatformDebugSymbolsTooltip(List<GraphicsDeviceType> enabledApis)
        {
            foreach (var api in enabledApis)
            {
                var platformTooltip = ShaderBuildSettings.GetDebugSymbolsTooltipForAPI(api);
                if (!string.IsNullOrEmpty(platformTooltip))
                    return platformTooltip;
            }

            return null;
        }

        // The enabled-API list can change from another inspector, so refresh only when the set actually differs.
        private void RefreshApiDependentUIIfApisChanged()
        {
            var enabledApis = GetEnabledApisForCurrentTarget();
            if (!EnabledApisEqual(enabledApis, m_LastWarnedEnabledApis))
                RefreshApiDependentUI(enabledApis);
        }

        private static bool EnabledApisEqual(List<GraphicsDeviceType> a, List<GraphicsDeviceType> b)
        {
            if (a == null || b == null)
                return a == b;
            if (a.Count != b.Count)
                return false;
            for (int i = 0, n = a.Count; i < n; ++i)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        private static void SetCapabilityWarning(HelpBox box, List<GraphicsDeviceType> unsupported, bool isDebug)
        {
            if (unsupported.Count == 0)
            {
                box.style.display = DisplayStyle.None;
                return;
            }

            var names = string.Join(", ", unsupported.ConvertAll(ApiDisplayNameFor));
            string msg;
            if (unsupported.Count == 1)
                msg = isDebug
                    ? string.Format(L10n.Tr("{0} doesn't support debug symbols and will skip this setting.", null), names)
                    : string.Format(L10n.Tr("{0} doesn't support the selected optimisation level and will use its default optimisation level.", null), names);
            else
                msg = isDebug
                    ? string.Format(L10n.Tr("Some Graphics APIs don't support debug symbols and will skip this setting: {0}.", null), names)
                    : string.Format(L10n.Tr("Some Graphics APIs don't support the selected optimisation level and will use their default optimisation level: {0}.", null), names);

            box.text = msg;
            box.style.display = DisplayStyle.Flex;
        }

        private List<ShaderBuildSettings.ShaderCompilerSettings> ReadPersistedCompilerRows()
        {
            var rows = new List<ShaderBuildSettings.ShaderCompilerSettings>();
            var prop = GetCompilerSettingsProperty();
            if (prop != null)
            {
                for (int i = 0, n = prop.arraySize; i < n; ++i)
                {
                    var element = prop.GetArrayElementAtIndex(i);
                    rows.Add(new ShaderBuildSettings.ShaderCompilerSettings
                    {
                        graphicsAPI = (GraphicsDeviceType)element.FindPropertyRelative("graphicsAPI").intValue,
                        compilerToolchainOverride = (ShaderBuildSettings.ShaderCompilerToolchain)element.FindPropertyRelative("compilerToolchainOverride").intValue,
                        optimizationLevel = (ShaderBuildSettings.ShaderOptimizationLevel)element.FindPropertyRelative("optimizationLevel").intValue,
                        enableDebugSymbols = element.FindPropertyRelative("enableDebugSymbols").boolValue,
                    });
                }
            }
            return rows;
        }

        private ShaderBuildSettings.ShaderCompilerSettings[] ReadSanitizedCompilerRows()
        {
            return ShaderBuildSettings.SanitizeShaderCompilerSettings(ReadPersistedCompilerRows().ToArray());
        }

        private void LoadDebugOptAndRefreshWarnings()
        {
            bool debug = false;
            var level = ShaderBuildSettings.ShaderOptimizationLevel.Default;
            var enabledApis = GetEnabledApisForCurrentTarget();
            m_LastLoadedDebugOptTarget = GetCurrentBuildTarget();

            var enabledForTarget = new HashSet<GraphicsDeviceType>(enabledApis);
            foreach (var row in ReadSanitizedCompilerRows())
            {
                var api = row.graphicsAPI;
                if (!enabledForTarget.Contains(api))
                    continue;
                if (!ShaderBuildSettings.SupportsOptimizationLevel(api) && !ShaderBuildSettings.SupportsDebugSymbols(api))
                    continue;
                debug = row.enableDebugSymbols;
                level = row.optimizationLevel;
                break;
            }

            m_OptLevelBeforeDebugForced = level;
            m_EnableDebugSymbolsToggle?.SetValueWithoutNotify(debug);
            m_OptimizationLevelDropdown?.SetValueWithoutNotify(level);
            UpdateOptimizationLevelEnabledState();
            RefreshApiDependentUI(enabledApis);
        }

        private ShaderBuildSettings.ShaderCompilerSettings[] BuildMergedCompilerSettings()
        {
            var byApi = new Dictionary<GraphicsDeviceType, ShaderBuildSettings.ShaderCompilerSettings>();
            foreach (var row in ReadSanitizedCompilerRows())
                byApi[row.graphicsAPI] = row;

            if (m_IsTargetingBuildProfile)
            {
                foreach (var api in new List<GraphicsDeviceType>(byApi.Keys))
                {
                    if (!ShaderBuildSettings.SupportsCompilerToolchainOverride(api))
                        continue;
                    var s = byApi[api];
                    s.compilerToolchainOverride = ShaderBuildSettings.ShaderCompilerToolchain.Default;
                    byApi[api] = s;
                }
                foreach (var row in m_CompilerBackendSettings)
                {
                    if (!byApi.TryGetValue(row.graphicsAPI, out var s))
                        s = new ShaderBuildSettings.ShaderCompilerSettings
                        {
                            graphicsAPI = row.graphicsAPI,
                            optimizationLevel = ShaderBuildSettings.ShaderOptimizationLevel.Default,
                            enableDebugSymbols = false,
                        };
                    s.compilerToolchainOverride = row.compilerToolchainOverride;
                    byApi[row.graphicsAPI] = s;
                }
            }

            bool debug = m_EnableDebugSymbolsToggle != null && m_EnableDebugSymbolsToggle.value;
            var level = m_OptimizationLevelDropdown != null
                ? m_OptimizationLevelDropdown.value
                : ShaderBuildSettings.ShaderOptimizationLevel.Default;

            if (debug)
                level = m_OptLevelBeforeDebugForced;

            return ShaderBuildSettings.MergeCompilerSettings(byApi.Values, debug, level, GetEnabledApisForCurrentTarget());
        }
    }
}
