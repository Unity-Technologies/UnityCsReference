// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build.Profile;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal class BuildProfileAdaptivePerformanceProviderUI : VisualElement
    {
        internal struct LoaderInformation
        {
            public string packageId;
            public string loaderName;
            public string loaderType;
            public bool toggled;
            public bool isSettingCreated;
            public bool stateChanged;
            public bool isDeprecated;
        }

        int m_CurrentSelectedProviderIndex = 0;
        internal Dictionary<string, LoaderInformation> m_LoaderMetadata = null;
        BuildProfile m_BuildProfile;
        AdaptivePerformancePackageMetadataStore.LoaderBuildTargetQueryResult m_DefaultLoader;
        Dictionary<string, UnityEditor.Editor> m_Editors = new();
        DropdownField m_DropDown;
        List<string> m_LoaderNameList;
        ReorderableList m_ReorderableList;
        [NoAutoStaticsCleanup] // dialog-guard flag, always restored to false after dialog closes
        static bool s_ShowingDialog = false;

        internal readonly GUIContent k_ViewGuide = new("View Guide");
        internal readonly string k_NoProviderTxt = "NoProviders";
        internal readonly string k_CustomProviderUrl = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance@5.1/manual/provider.html";
        internal static readonly string k_NoProviderMessage = "No Provider available on this platform. You could create and use a custom provider. ";
        public BuildTargetGroup CurrentBuildTargetGroup { get; set; }

        bool IsSimulatorLoader(string loaderName)
        {
            return loaderName == "Device Simulator Provider";
        }

        // Reset the metadata to its original state.
        // Dynamic settings need to be preserved through domain reload.
        internal void UpdateMetadata()
        {
            m_LoaderMetadata = new Dictionary<string, LoaderInformation>();
            m_LoaderNameList = new List<string>();
            foreach (var pmd in AdaptivePerformancePackageMetadataStore.GetLoadersForBuildTarget(CurrentBuildTargetGroup))
            {
                if (IsSimulatorLoader(pmd.loaderName)) continue;
                // use the assigned loader as a check for the toggle and settings.
                bool installedPackage = SessionState.GetBool(pmd.loaderName, false);
                var alreadyAssigned = IsLoaderAssigned(pmd.loaderType);
                var isProviderSettingCreated = IsProviderSettingCreated(pmd.packageId);
                var loaderInfo = new LoaderInformation()
                {
                    packageId = pmd.packageId,
                    loaderName = pmd.loaderName,
                    loaderType = pmd.loaderType,
                    toggled = alreadyAssigned,
                    isSettingCreated = isProviderSettingCreated,
                    isDeprecated = pmd.isDeprecated,
                };
                // Need to recreate the UI when switching back and forth to a custom profile since the setting is already created.
                if (isProviderSettingCreated)
                {
                    CreateProviderSettingUI(loaderInfo);
                }

                if (installedPackage)
                {
                    if (IsPackageInstalled(pmd.packageId))
                    {
                        CreateAdaptivePerformanceSettingsForProvider(loaderInfo);
                        CreateProviderSettingUI(loaderInfo);
                        CreateAndAssignAdaptivePerformanceLoaderSetting(loaderInfo);
                        loaderInfo.isSettingCreated = true;
                        loaderInfo.toggled = true;
                    }
                    SessionState.SetBool(pmd.loaderName, false);
                }

                m_LoaderMetadata[pmd.loaderName] = loaderInfo;
                m_LoaderNameList.Add(pmd.loaderName);
            }
        }

        internal static bool InstallPackageForProvider(string packageId)
        {
            if (!s_ShowingDialog)
            {
                bool Ok = EditorUtility.DisplayDialog(L10n.Tr("Install Package: "), L10n.Tr($"Missing Package {packageId} for provider. Do you want to Install?"),
                    L10n.Tr("Ok"), L10n.Tr("Cancel"));
                s_ShowingDialog = true;
                if (Ok)
                {
                    var request = Client.Add(packageId);
                    while (!request.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    if (request.Status == StatusCode.Failure)
                    {
                        Debug.Log("Error " + request.Error.message + "installing package " + packageId);
                    }
                }
                s_ShowingDialog = false;
                return Ok;
            }

            return false;

        }

        public void SelectDefaultProvider()
        {
            SelectProvider(m_DefaultLoader.loaderName);
        }

        void SelectProvider(string loaderName)
        {
            if (String.IsNullOrEmpty(loaderName) || m_LoaderMetadata.ContainsKey(loaderName) == false) return;
            var loaderInfo = m_LoaderMetadata[loaderName];
            if (!loaderInfo.toggled && IsPackageInstalled(loaderInfo.packageId))
            {
                CreateAdaptivePerformanceSettingsForProvider(loaderInfo);
                CreateProviderSettingUI(loaderInfo);
                CreateAndAssignAdaptivePerformanceLoaderSetting(loaderInfo);
                loaderInfo.toggled = true;
                m_LoaderMetadata[loaderName] = loaderInfo;
                m_DropDown.index = m_CurrentSelectedProviderIndex;
            }
        }

        internal BuildProfileAdaptivePerformanceProviderUI(BuildProfile buildProfile)
        {
            m_BuildProfile = buildProfile;
            CurrentBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_BuildProfile.buildTarget);
            m_DefaultLoader = AdaptivePerformancePackageMetadataStore.GetDefaultLoaderForBuildTarget(CurrentBuildTargetGroup);
            UpdateMetadata();
            viewDataKey = "AdaptivePerformanceProviderUI";
        }

        bool IsGeneralSettingExists()
        {
            return m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>() != null;
        }

        void OnSettingsGUI()
        {
            if (!IsGeneralSettingExists()) return;

            var loaderName = m_DropDown.value;
            if (String.IsNullOrEmpty(loaderName)) return;
            var loaderInfo = m_LoaderMetadata[loaderName];
            if (IsPackageInstalled(loaderInfo.packageId))
            {
                if (loaderInfo.isSettingCreated)
                {
                    if (m_Editors.ContainsKey(loaderInfo.loaderType) && m_Editors[loaderInfo.loaderType] != null)
                    {
                        m_Editors[loaderInfo.loaderType].OnInspectorGUI();
                    }
                }
            }
        }

        private void DisplayLink(GUIContent text, Uri link, float leftMargin, float width, Rect rect)
        {
            var labelStyle = EditorStyles.linkLabel;
            var uriRect = rect;

            uriRect.y += EditorGUIUtility.singleLineHeight;
            uriRect.width = width;

            if (GUI.Button(uriRect, text, labelStyle))
            {
                System.Diagnostics.Process.Start(link.AbsoluteUri);
            }
            EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
            EditorGUI.DrawRect(new Rect(uriRect.x + 2, uriRect.y + uriRect.height - 3, uriRect.width - 3, 1), labelStyle.normal.textColor);
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var loaderName = m_LoaderNameList[index];
            if (loaderName == k_NoProviderTxt)
            {
                EditorGUIUtility.labelWidth = 180;
                EditorGUI.LabelField(rect, k_NoProviderMessage);
                DisplayLink(k_ViewGuide, new Uri(k_CustomProviderUrl), 2, 70, rect);
                return;
            }
            if (loaderName == "") //for link
                return;

            var li = m_LoaderMetadata[loaderName];
            bool preToggledState = li.toggled;
            float toggleHeight = EditorGUIUtility.singleLineHeight;
            rect.y += (rect.height - toggleHeight) * 0.5f;
            rect.height = toggleHeight;
            rect.width *= 0.51f;
            EditorGUIUtility.labelWidth = 250;
            li.toggled = EditorGUI.Toggle(rect, li.loaderName + (li.isDeprecated ? " (deprecated)" : string.Empty), preToggledState);
            if (li.toggled != preToggledState)
            {
                li.stateChanged = true;
                m_LoaderMetadata[m_LoaderNameList[index]] = li;
            }
        }

        float GetElementHeight(int index)
        {
            return m_ReorderableList.elementHeight;
        }

        void OnProviderListGUI()
        {
            if (!IsGeneralSettingExists()) return;
            bool noProvider = false;
            if (m_LoaderNameList.Count == 0)
            {
                noProvider = true;
                m_LoaderNameList.Add(k_NoProviderTxt);
                m_LoaderNameList.Add(""); // For link
            }

            m_ReorderableList = new ReorderableList(m_LoaderNameList, typeof(string), false, true, false, false);
            m_ReorderableList.drawHeaderCallback = (rect) =>
            {
                var labelSize = EditorStyles.label.CalcSize(AdaptivePerformanceLoaderOrderUI.Content.k_LoaderUITitle);
                var labelRect = new Rect(rect);
                labelRect.width = labelSize.x;

                labelSize = EditorStyles.label.CalcSize(AdaptivePerformanceLoaderOrderUI.Content.k_HelpContent);
                var imageRect = new Rect(rect);
                imageRect.xMin = labelRect.xMax + 1;
                imageRect.width = labelSize.x;

                EditorGUI.LabelField(labelRect, AdaptivePerformanceLoaderOrderUI.Content.k_LoaderUITitle, EditorStyles.label);
                EditorGUI.LabelField(imageRect, AdaptivePerformanceLoaderOrderUI.Content.k_HelpContent);
            };

            m_ReorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawElementCallback(rect, index, isActive, isFocused);
            m_ReorderableList.elementHeightCallback = (index) => GetElementHeight(index);
            m_ReorderableList.drawFooterCallback = (rect) =>
            {
                var status = AdaptivePerformancePackageMetadataStore.GetCurrentStatusDisplayText();
                GUI.Label(rect, status, EditorStyles.label);
            };

            m_ReorderableList.DoLayoutList();
            if (noProvider)
            {
                m_LoaderNameList.Clear();
                return;
            }

            for (int i = 0; i < m_LoaderNameList.Count; i++)
            {
                var loaderInfo = m_LoaderMetadata[m_LoaderNameList[i]];

                if (loaderInfo.stateChanged)
                {
                    loaderInfo.stateChanged = false;
                    if (!loaderInfo.toggled)
                    {
                        RemoveLoaderFromManagerSetting(loaderInfo.loaderType);
                    }
                    else
                    {
                        if (!IsPackageInstalled(loaderInfo.packageId))
                        {
                            bool okToInstall = InstallPackageForProvider(loaderInfo.packageId);
                            if (okToInstall)
                            {
                                SessionState.SetBool(m_LoaderNameList[i], true);
                            }
                            else
                            {
                                loaderInfo.toggled = false;
                            }
                        }
                        else
                        {
                            CreateAndAssignAdaptivePerformanceLoaderSetting(loaderInfo);
                        }
                    }
                }
                else
                {
                    if (!IsPackageInstalled(loaderInfo.packageId))
                    {
                        continue;
                    }

                    if (!loaderInfo.isSettingCreated)
                    {
                        CreateAdaptivePerformanceSettingsForProvider(loaderInfo);
                        CreateProviderSettingUI(loaderInfo);
                        loaderInfo.isSettingCreated = true;
                    }
                }

                m_LoaderMetadata[m_LoaderNameList[i]] = loaderInfo;
            }
        }

        public void CreateUI()
        {
            m_DropDown = new DropdownField("Provider Settings");
            var adaptivePerformanceGeneralSettings = m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>();
            if (!adaptivePerformanceGeneralSettings) return;
            m_DropDown.RegisterValueChangedCallback(evt =>
            {
                adaptivePerformanceGeneralSettings.m_LastSelectedProvider = evt.newValue;
            });

            int i = 0;
            foreach (var kv in m_LoaderMetadata)
            {
                var loaderInfo = kv.Value;
                if (!IsPackageInstalled(loaderInfo.packageId)) continue;
                m_DropDown.choices.Add(loaderInfo.loaderName);
                if (!String.IsNullOrEmpty(adaptivePerformanceGeneralSettings.m_LastSelectedProvider)
                    && adaptivePerformanceGeneralSettings.m_LastSelectedProvider == loaderInfo.loaderName)
                {
                    m_CurrentSelectedProviderIndex = i;

                }
                else if (loaderInfo.loaderName == m_DefaultLoader.loaderName)
                {
                    m_CurrentSelectedProviderIndex = i;
                }
                i++;
            }

            var imguiContainerForProviders = new IMGUIContainer();
            imguiContainerForProviders.onGUIHandler = OnProviderListGUI;
            Add(imguiContainerForProviders);

            if (m_DropDown.choices.Count == 0) return;

            var imguiContainerForSettings = new IMGUIContainer();
            imguiContainerForSettings.onGUIHandler = OnSettingsGUI;
            m_DropDown.index = m_CurrentSelectedProviderIndex;
            m_DropDown.AddToClassList("unity-base-field__aligned");
            imguiContainerForSettings.style.marginLeft = imguiContainerForSettings.style.marginBottom = 12;

            var veContainerForSettings = new VisualElement();
            veContainerForSettings.Add(m_DropDown);
            veContainerForSettings.Add(imguiContainerForSettings);
            Add(veContainerForSettings);

        }
        bool IsLoaderAssigned(string loaderType)
        {
            var managerSettings = m_BuildProfile.GetComponent<AdaptivePerformanceManagerSettings>();
            if (managerSettings == null) return false;
            foreach(var loader in managerSettings.loaders)
            {
                if (loader != null && String.Compare(loader.GetType().FullName, loaderType) == 0)
                    return true;
            }
            return false;
        }

        bool IsTheSameProviderSettingType(IAdaptivePerformanceSettings providerSettings, string providerType)
        {
            if (providerSettings != null && providerType == providerSettings.GetType().FullName)
            {
                return true;
            }

            return false;
        }

        void CreateProviderSettingUI(LoaderInformation loaderInfo)
        {
            var packageMetaData = AdaptivePerformancePackageMetadataStore.GetMetadataForPackage(loaderInfo.packageId);
            // only one provider setting per provider type
            var adaptivePerformanceProviderSettingContainer = m_BuildProfile.GetComponent<BuildProfileProviderContainer>();
            if (adaptivePerformanceProviderSettingContainer != null)
            {
                foreach (var providerSetting in adaptivePerformanceProviderSettingContainer.adaptivePerformanceProviderSettings)
                {
                    if (IsTheSameProviderSettingType(providerSetting, packageMetaData.settingsType))
                    {
                        // These providerSetting objects are created per build profile, not shared.
                        var editor = UnityEditor.Editor.CreateEditor(providerSetting) as ProviderSettingsEditor;
                        if (editor == null)
                        {
                            Debug.LogError("Unable to create setting editor for provider setting type : " + packageMetaData.settingsType);
                        }

                        editor.ShowTargetGroupSelection = false;
                        m_Editors[loaderInfo.loaderType] = editor;
                        break;
                    }
                }
            }
        }

        void RemoveLoaderFromManagerSetting(string loaderTypeName)
        {
            var managerSettings = m_BuildProfile.GetComponent<AdaptivePerformanceManagerSettings>();
            if (managerSettings == null) return;
            foreach(var loader in managerSettings.loaders)
            {
                if (loader != null && String.Compare(loader.GetType().FullName, loaderTypeName) == 0)
                {
                    managerSettings.loaders.Remove(loader);
                    AssetDatabase.RemoveObjectFromAsset(loader);
                    return;
                }
            }
        }

        // Check whether the requested package is installed before creating UI and settings.
        // Since the setting type will only be available after installation.
        internal bool IsPackageInstalled(string packageId)
        {
            if (packageId == "com.unity.adaptiveperformance.basic" || packageId == "com.unity.adaptiveperformance") return true;
            return UnityEditor.PackageManager.PackageInfo.IsPackageRegistered(packageId);
        }


        bool IsProviderSettingCreated(string packageId)
        {
            var packageMetaData = AdaptivePerformancePackageMetadataStore.GetMetadataForPackage(packageId);
            var adaptivePerformanceProviderContainer = m_BuildProfile.GetComponent<BuildProfileProviderContainer>();
            // only one provider setting per provider type
            if (adaptivePerformanceProviderContainer != null)
            {
                foreach (var providerSetting in adaptivePerformanceProviderContainer.adaptivePerformanceProviderSettings)
                {
                    if (IsTheSameProviderSettingType(providerSetting, packageMetaData.settingsType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void CreateAdaptivePerformanceProviderSettings(LoaderInformation loaderInfo)
        {
            var providerSettingCreated = IsProviderSettingCreated(loaderInfo.packageId);

            if (!providerSettingCreated)
            {
                var packageMetaData = AdaptivePerformancePackageMetadataStore.GetMetadataForPackage(loaderInfo.packageId);
                var adaptivePerformanceProviderContainer = m_BuildProfile.GetComponent<BuildProfileProviderContainer>();
                if (adaptivePerformanceProviderContainer == null)
                {
                    adaptivePerformanceProviderContainer = ScriptableObject.CreateInstance<BuildProfileProviderContainer>();
                    adaptivePerformanceProviderContainer.hideFlags = HideFlags.HideInHierarchy;
                    m_BuildProfile.AddComponent(adaptivePerformanceProviderContainer);
                }

                var copyObject = EditorUtilities.CloneProviderSettingsFromProjectSettings(packageMetaData.settingsType);
                var providerSetting = copyObject == null ? ScriptableObject.CreateInstance(packageMetaData.settingsType) as IAdaptivePerformanceSettings : copyObject;
                providerSetting.hideFlags = HideFlags.HideInHierarchy;
                adaptivePerformanceProviderContainer.adaptivePerformanceProviderSettings.Add(providerSetting);
                AssetDatabase.AddObjectToAsset(providerSetting, adaptivePerformanceProviderContainer);
                var customScalers = EditorUtilities.CloneCustomScalersFromProjectSettings(providerSetting);
                for (int i = 0; i < customScalers.Count; i++)
                {
                    AssetDatabase.AddObjectToAsset(customScalers[i], adaptivePerformanceProviderContainer);
                }
                AssetDatabase.SaveAssetIfDirty(adaptivePerformanceProviderContainer);
            }
        }

        void CreateAndAssignAdaptivePerformanceLoaderToManagerSetting(string loaderTypeName)
        {
            var managerSettings = m_BuildProfile.GetComponent<AdaptivePerformanceManagerSettings>();
            if(managerSettings == null) return;
            var newLoader = ScriptableObject.CreateInstance(loaderTypeName) as AdaptivePerformanceLoader;
            if (newLoader == null)
            {
                Debug.LogError("Unable to create adaptive performance loader with type " + loaderTypeName);
                return;
            }
            var assignedLoaders = managerSettings.loaders;
            if (assignedLoaders == null)
            {
                managerSettings.loaders = new List<AdaptivePerformanceLoader>();
            }
            // loaders are loaded based on the order they are added. Might need to update the priority so that
            // basic provider takes the lowest priority.
            if (!assignedLoaders.Contains(newLoader))
            {
                newLoader.hideFlags = HideFlags.HideInHierarchy;
                assignedLoaders.Add(newLoader);
                AssetDatabase.AddObjectToAsset(newLoader, managerSettings);
            }
        }

        internal void CreateAndAssignAdaptivePerformanceLoaderSetting(LoaderInformation loaderInfo)
        {
            if (IsLoaderAssigned(loaderInfo.loaderType)) return;
            CreateAndAssignAdaptivePerformanceLoaderToManagerSetting(loaderInfo.loaderType);
        }
        // Would only have one manager setting for multiple provider settings.
        void CreateAdaptivePerformanceManagerSettings()
        {
            var managerSetting = m_BuildProfile.GetComponent<AdaptivePerformanceManagerSettings>();
            if (managerSetting == null)
            {
                managerSetting = ScriptableObject.CreateInstance<AdaptivePerformanceManagerSettings>();
                managerSetting.hideFlags = HideFlags.HideInHierarchy;
                var generalSettings = m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>();
                if (generalSettings != null)
                {
                    generalSettings.AssignedSettings = managerSetting;
                }
                m_BuildProfile.AddComponent(managerSetting);
            }
        }

        /// <summary>
        /// Create all adaptive performance runtime settings per build profile
        /// Number of provider settings == number of providers.
        /// Only 1 manager setting per build profile.
        /// Number of loaders == number of providers.
        /// </summary>
        /// <param name="loaderInfo"></param>
        internal void CreateAdaptivePerformanceSettingsForProvider(LoaderInformation loaderInfo)
        {
            CreateAdaptivePerformanceManagerSettings();
            // only 1 loader and 1 provider per loader type
            CreateAdaptivePerformanceProviderSettings(loaderInfo);
        }

    }
}
