// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor.AdaptivePerformance.Editor.Metadata;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    class AdaptivePerformanceSettingsManager : SettingsProvider
    {
        struct Content
        {
            public static readonly GUIContent k_InitializeOnStart = new GUIContent(L10n.Tr("Initialize Adaptive Performance on Startup"));
            public static readonly GUIContent k_DocText = new GUIContent(L10n.Tr("View documentation"));
            public static readonly Uri k_DocUri = new Uri("https://docs.unity3d.com/Packages/com.unity.adaptiveperformance@latest");
            public static readonly GUIContent k_ExplanatoryText = new GUIContent(L10n.Tr("Selecting an Adaptive Performance provider below enables that provider for the corresponding build target. Unity will download and install the provider package if it is not already present. Disabling a provider will not automatically uninstall packages that are already installed. To uninstall a provider package, use the Package Manager."));
            public static readonly GUIContent k_FrameTimingExplanatoryText = new GUIContent(L10n.Tr("Please enable Frame Timing Stats in the Player Settings. Adaptive Performance requires precise frame time information."));
        }

        internal static string s_SettingsRootTitle = $"Project/{AdaptivePerformanceConstants.kAdaptivePerformanceProviderManagement}";
        static AdaptivePerformanceSettingsManager s_SettingsManager = null;

        static bool s_EnableAdaptivePerformance = false;

        internal static AdaptivePerformanceSettingsManager Instance => s_SettingsManager;

        static AdaptivePerformanceGeneralSettingsPerBuildTarget currentSettings
        {
            get
            {
                AdaptivePerformanceGeneralSettingsPerBuildTarget generalSettings = null;
                EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out generalSettings);
                if (generalSettings == null && s_EnableAdaptivePerformance)
                {
                    lock (AdaptivePerformanceSettingsManager.Instance)
                    {
                        EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out generalSettings);
                        if (generalSettings == null)
                        {
                            string searchText = "t:AdaptivePerformanceGeneralSettings";
                            string[] assets = AssetDatabase.FindAssets(searchText);
                            if (assets.Length > 0)
                            {
                                string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                                generalSettings = AssetDatabase.LoadAssetAtPath(path, typeof(AdaptivePerformanceGeneralSettingsPerBuildTarget)) as AdaptivePerformanceGeneralSettingsPerBuildTarget;
                            }
                        }

                        if (generalSettings == null)
                        {
                            generalSettings = ScriptableObject.CreateInstance(typeof(AdaptivePerformanceGeneralSettingsPerBuildTarget)) as AdaptivePerformanceGeneralSettingsPerBuildTarget;
                            generalSettings.Version = "6000.3";
                            string assetPath = EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultGeneralSettingsPath);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                assetPath = Path.Combine(assetPath, "AdaptivePerformanceGeneralSettings.asset");
                                //generalSettings.hideFlags = HideFlags.HideInInspector;
                                AssetDatabase.CreateAsset(generalSettings, assetPath);
                            }
                        }

                        EditorBuildSettings.AddConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, generalSettings, true);
                    }
                }

                return generalSettings;
            }
        }

        bool resetUi = false;
        internal bool ResetUi
        {
            get
            {
                return resetUi;
            }
            set
            {
                resetUi = value;
                if (resetUi)
                    Repaint();
            }
        }

        SerializedObject m_SettingsWrapper;

        private Dictionary<BuildTargetGroup, AdaptivePerformanceManagerSettingsEditor> CachedSettingsEditor = new Dictionary<BuildTargetGroup, AdaptivePerformanceManagerSettingsEditor>();


        private BuildTargetGroup m_LastBuildTargetGroup = BuildTargetGroup.Unknown;



        [UnityEngine.Internal.ExcludeFromDocs]
        AdaptivePerformanceSettingsManager(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
        }

        [SettingsProvider]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider Create()
        {
            if (s_SettingsManager == null)
            {
                s_SettingsManager = new AdaptivePerformanceSettingsManager(s_SettingsRootTitle);
            }

            return s_SettingsManager;
        }

        [SettingsProviderGroup]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider[] CreateAllChildSettingsProviders()
        {
            List<SettingsProvider> ret = new List<SettingsProvider>();
            if (s_SettingsManager != null)
            {
                var ats = TypeLoaderExtensions.GetAllTypesWithAttribute<AdaptivePerformanceConfigurationDataAttribute>();
                if (currentSettings?.EnableAdaptivePerformance == true)
                {
                    foreach (var at in ats)
                    {
                        if (at.FullName.Contains("UnityEngine.AdaptivePerformance.TestPackage"))
                            continue;

                        AdaptivePerformanceConfigurationDataAttribute apbda = at.GetCustomAttributes(typeof(AdaptivePerformanceConfigurationDataAttribute), true)[0] as AdaptivePerformanceConfigurationDataAttribute;
                        string settingsPath = String.Format("{1}/{0}", apbda.displayName, AdaptivePerformanceSettingsManager.s_SettingsRootTitle);
                        var resProv = new AdaptivePerformanceConfigurationProvider(settingsPath, apbda.buildSettingsKey, at);
                        ret.Add(resProv);
                    }
                }
            }
            return ret.ToArray();
        }

        void InitEditorData(ScriptableObject settings)
        {
            if (settings != null)
            {
                m_SettingsWrapper = new SerializedObject(settings);
            }
        }

        /// <summary>
        /// See <see href="https://docs.unity3d.com/ScriptReference/SettingsProvider.html">SettingsProvider documentation</see>.
        /// </summary>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitEditorData(currentSettings);
        }

        /// <summary>
        /// See <see href="https://docs.unity3d.com/ScriptReference/SettingsProvider.html">SettingsProvider documentation</see>.
        /// </summary>
        public override void OnDeactivate()
        {
            m_SettingsWrapper = null;
            CachedSettingsEditor.Clear();
        }

        private void DisplayLoaderSelectionUI()
        {
            BuildTargetGroup buildTargetGroup = EditorGUILayout.BeginBuildTargetSelectionGrouping();

            try
            {
                bool buildTargetChanged = m_LastBuildTargetGroup != buildTargetGroup;
                if (buildTargetChanged)
                    m_LastBuildTargetGroup = buildTargetGroup;

                AdaptivePerformanceGeneralSettings settings = currentSettings.SettingsForBuildTarget(buildTargetGroup);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<AdaptivePerformanceGeneralSettings>() as AdaptivePerformanceGeneralSettings;
                    settings.hideFlags = HideFlags.HideInInspector;
                    currentSettings.SetSettingsForBuildTarget(buildTargetGroup, settings);
                    settings.name = $"{buildTargetGroup.ToString()} Settings";
                    AssetDatabase.AddObjectToAsset(settings, AssetDatabase.GetAssetOrScenePath(currentSettings));
                }

                var serializedSettingsObject = new SerializedObject(settings);
                serializedSettingsObject.Update();

                EditorGUILayout.LabelField($"Settings for {buildTargetGroup}", EditorStyles.boldLabel);

                SerializedProperty initOnStart = serializedSettingsObject.FindProperty("m_InitManagerOnStart");
                EditorGUIUtility.labelWidth = 260;
                EditorGUILayout.PropertyField(initOnStart, Content.k_InitializeOnStart);
                EditorGUILayout.Space();

                SerializedProperty loaderProp = serializedSettingsObject.FindProperty("m_LoaderManagerInstance");

                if (!CachedSettingsEditor.ContainsKey(buildTargetGroup))
                {
                    CachedSettingsEditor.Add(buildTargetGroup, null);
                }

                if (loaderProp.objectReferenceValue == null)
                {
                    var adaptivePerformanceManagerSettings = ScriptableObject.CreateInstance<AdaptivePerformanceManagerSettings>() as AdaptivePerformanceManagerSettings;
                    //adaptivePerformanceManagerSettings.hideFlags = HideFlags.HideInInspector;
                    adaptivePerformanceManagerSettings.name = $"{buildTargetGroup.ToString()} Providers";
                    AssetDatabase.AddObjectToAsset(adaptivePerformanceManagerSettings, AssetDatabase.GetAssetOrScenePath(currentSettings));
                    loaderProp.objectReferenceValue = adaptivePerformanceManagerSettings;

                    serializedSettingsObject.ApplyModifiedProperties();
                }

                var obj = loaderProp.objectReferenceValue;

                if (obj != null)
                {
                    loaderProp.objectReferenceValue = obj;

                    if (CachedSettingsEditor[buildTargetGroup] == null)
                    {
                        CachedSettingsEditor[buildTargetGroup] = UnityEditor.Editor.CreateEditor(obj) as AdaptivePerformanceManagerSettingsEditor;

                        if (CachedSettingsEditor[buildTargetGroup] == null)
                        {
                            Debug.LogError("Failed to create a view for Adaptive Performance Manager Settings Instance");
                        }
                    }

                    if (CachedSettingsEditor[buildTargetGroup] != null)
                    {
                        if (ResetUi)
                        {
                            ResetUi = false;
                            CachedSettingsEditor[buildTargetGroup].Reload();
                        }

                        CachedSettingsEditor[buildTargetGroup].BuildTarget = buildTargetGroup;
                        CachedSettingsEditor[buildTargetGroup].OnInspectorGUI();
                    }
                }
                else if (obj == null)
                {
                    settings.AssignedSettings = null;
                    loaderProp.objectReferenceValue = null;
                }

                serializedSettingsObject.ApplyModifiedProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error trying to display provider assignment UI : {ex.Message}");
            }

            EditorGUILayout.EndBuildTargetSelectionGrouping();
        }

        private void DisplayLink(GUIContent text, Uri link)
        {
            var labelStyle = EditorStyles.linkLabel;
            var uriRect = GUILayoutUtility.GetRect(text, labelStyle);
            var size = labelStyle.CalcSize(text);
            uriRect.width = size.x;
            if (GUI.Button(uriRect, text, labelStyle))
            {
                System.Diagnostics.Process.Start(link.AbsoluteUri);
            }
            EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
            EditorGUI.DrawRect(new Rect(uriRect.x, uriRect.y + uriRect.height - 1, uriRect.width, 1), labelStyle.normal.textColor);
        }

        private void DisplayDocumentationLink()
        {
            DisplayLink(Content.k_DocText, Content.k_DocUri);
            EditorGUILayout.Space();
        }

        private void DisplayLoadOrderUi()
        {
            EditorGUILayout.HelpBox(Content.k_ExplanatoryText.text, MessageType.Info);
            EditorGUILayout.Space();
            if (!PlayerSettings.enableFrameTimingStats)
                EditorGUILayout.HelpBox(Content.k_FrameTimingExplanatoryText.text, MessageType.Warning);

            EditorGUI.BeginDisabledGroup(AdaptivePerformancePackageMetadataStore.isDoingQueueProcessing || EditorApplication.isPlaying || EditorApplication.isPaused);
            if (m_SettingsWrapper != null && m_SettingsWrapper.targetObject != null)
            {
                m_SettingsWrapper.Update();

                EditorGUILayout.Space();

                DisplayLoaderSelectionUI();

                m_SettingsWrapper.ApplyModifiedProperties();
            }
            EditorGUI.EndDisabledGroup();
        }

        void DisplayEnableToggle()
        {
            float originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200;
            if (currentSettings != null)
            {
                s_EnableAdaptivePerformance = currentSettings.EnableAdaptivePerformance;
            }

            bool preValue = s_EnableAdaptivePerformance;
            s_EnableAdaptivePerformance = EditorGUILayout.Toggle("Enable Adaptive Performance", s_EnableAdaptivePerformance);

            EditorGUIUtility.labelWidth = originalValue;
            if (currentSettings != null)
            {
                currentSettings.EnableAdaptivePerformance = s_EnableAdaptivePerformance;
            }
            if (preValue != s_EnableAdaptivePerformance)
            {
                if (s_EnableAdaptivePerformance)
                {
                    EditorUtilities.CheckEnableFrameTimingState();
                }
                SettingsService.NotifySettingsProviderChanged();
            }
        }

        /// <summary>
        /// See <see href="https://docs.unity3d.com/ScriptReference/SettingsProvider.html">SettingsProvider documentation</see>.
        /// </summary>
        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(6, false);
            EditorGUILayout.BeginVertical();
            DisplayEnableToggle();

            if (currentSettings?.EnableAdaptivePerformance == true)
            {
                DisplayDocumentationLink();
                DisplayLoadOrderUi();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            base.OnGUI(searchContext);
        }
    }
}
