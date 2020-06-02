// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Hardware;
using UnityEditor.Collaboration;
using UnityEditor.Experimental;
using UnityEngine.Assertions;

namespace UnityEditor
{
    [CustomEditor(typeof(EditorSettings))]
    internal class EditorSettingsInspector : ProjectSettingsBaseEditor
    {
        class Content
        {
            public static GUIContent unityRemote = EditorGUIUtility.TrTextContent("Unity Remote (Deprecated)");
            public static GUIContent device = EditorGUIUtility.TrTextContent("Device");
            public static GUIContent compression = EditorGUIUtility.TrTextContent("Compression");
            public static GUIContent resolution = EditorGUIUtility.TrTextContent("Resolution");
            public static GUIContent joystickSource = EditorGUIUtility.TrTextContent("Joystick Source");

            public static GUIContent mode = EditorGUIUtility.TrTextContent("Mode");

            public static GUIContent cacheServer = EditorGUIUtility.TrTextContent("Cache Server (project specific)");
            public static GUIContent cacheServerIPLabel = EditorGUIUtility.TrTextContent("IP address");
            public static GUIContent cacheServerNamespacePrefixLabel = EditorGUIUtility.TrTextContent("Namespace prefix", "The namespace used for looking up and storing values on the cache server");
            public static GUIContent cacheServerEnableDownloadLabel = EditorGUIUtility.TrTextContent("Download", "Enables downloads from the cache server.");
            public static GUIContent cacheServerEnableUploadLabel = EditorGUIUtility.TrTextContent("Upload", "Enables uploads to the cache server.");
            public static GUIContent cacheServerEnableTlsLabel = EditorGUIUtility.TrTextContent("TLS/SSL", "Enabled encryption on the cache server connection.");
            public static GUIContent cacheServerEnableAuthLabel = EditorGUIUtility.TrTextContent("Authentication", "Enable authentication for cache server. Also forces TLS/SSL encryption.");
            public static GUIContent cacheServerAuthUserLabel = EditorGUIUtility.TrTextContent("User");
            public static GUIContent cacheServerAuthPasswordLabel = EditorGUIUtility.TrTextContent("Password");

            public static GUIContent assetSerialization = EditorGUIUtility.TrTextContent("Asset Serialization");
            public static GUIContent textSerializeMappingsOnOneLine = EditorGUIUtility.TrTextContent("Force Serialize References On One Line", "Forces Unity to write references and other inline mappings on one line, to help reduce version control noise");
            public static GUIContent defaultBehaviorMode = EditorGUIUtility.TrTextContent("Default Behaviour Mode");

            public static GUIContent graphics = EditorGUIUtility.TrTextContent("Graphics");
            public static GUIContent showLightmapResolutionOverlay = EditorGUIUtility.TrTextContent("Show Lightmap Resolution Overlay");
            public static GUIContent useLegacyProbeSampleCount = EditorGUIUtility.TrTextContent("Use legacy Light Probe sample counts", "Uses fixed Light Probe sample counts for baking with the Progressive Lightmapper. The sample counts are: 64 direct samples, 2048 indirect samples and 2048 environment samples.");
            public static GUIContent enableCookiesInLightmapper = EditorGUIUtility.TrTextContent("Enable baked cookies support", "Determines whether cookies should be evaluated by the Progressive Lightmapper during Global Illumination calculations. Introduced in version 2020.1. ");

            public static GUIContent spritePacker = EditorGUIUtility.TrTextContent("Sprite Packer");

            public static GUIContent cSharpProjectGeneration = EditorGUIUtility.TrTextContent("C# Project Generation");
            public static GUIContent additionalExtensionsToInclude = EditorGUIUtility.TrTextContent("Additional extensions to include");
            public static GUIContent rootNamespace = EditorGUIUtility.TrTextContent("Root namespace");

            public static GUIContent etcTextureCompressor = EditorGUIUtility.TrTextContent("ETC Texture Compressor");
            public static GUIContent behavior = EditorGUIUtility.TrTextContent("Behaviour");
            public static GUIContent fast = EditorGUIUtility.TrTextContent("Fast");
            public static GUIContent normal = EditorGUIUtility.TrTextContent("Normal");
            public static GUIContent best = EditorGUIUtility.TrTextContent("Best");

            public static GUIContent internalSettings = EditorGUIUtility.TrTextContent("Internal Settings");
            public static GUIContent internalSettingsVisible = EditorGUIUtility.TrTextContent("Internals visible in user scripts");

            public static GUIContent lineEndingForNewScripts = EditorGUIUtility.TrTextContent("Line Endings For New Scripts");

            public static GUIContent streamingSettings = EditorGUIUtility.TrTextContent("Streaming Settings");
            public static GUIContent enablePlayModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Play Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Play Mode");
            public static GUIContent enableEditModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Edit Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Edit Mode");

            public static GUIContent roslynAnalyzerSettings = EditorGUIUtility.TrTextContent("Roslyn Analyzer Settings");
            public static GUIContent enableRoslynAnalyzers = EditorGUIUtility.TrTextContent("Enable Roslyn Analyzers");

            public static GUIContent shaderCompilation = EditorGUIUtility.TrTextContent("Shader Compilation");
            public static GUIContent asyncShaderCompilation = EditorGUIUtility.TrTextContent("Asynchronous Shader Compilation", "Enables async shader compilation in Game and Scene view. Async compilation for custom editor tools can be achieved via script API and is not affected by this option.");
            public static GUIContent cachingShaderPreprocessor = EditorGUIUtility.TrTextContent("Caching Preprocessor", "Enables caching shader preprocessor with advanced functionality.");

            public static readonly GUIContent enterPlayModeSettings = EditorGUIUtility.TrTextContent("Enter Play Mode Settings");
            public static readonly GUIContent enterPlayModeOptionsEnabled = EditorGUIUtility.TrTextContent("Enter Play Mode Options", "Enables options when Entering Play Mode");
            public static readonly GUIContent enterPlayModeOptionsEnableDomainReload = EditorGUIUtility.TrTextContent("Reload Domain", "Enables Domain Reload when Entering Play Mode. Domain reload reinitializes game completely making loading behavior very close to the Player");
            public static readonly GUIContent enterPlayModeOptionsEnableSceneReload = EditorGUIUtility.TrTextContent("Reload Scene", "Enables Scene Reload when Entering Play Mode. Scene reload makes loading behavior and performance characteristics very close to the Player");
        }

        internal struct PopupElement
        {
            public readonly string id;
            public readonly GUIContent content;

            public PopupElement(string content)
            {
                this.id = content;
                this.content = new GUIContent(content);
            }
        }

        private PopupElement[] serializationPopupList =
        {
            new PopupElement("Mixed"),
            new PopupElement("Force Binary"),
            new PopupElement("Force Text"),
        };

        private PopupElement[] behaviorPopupList =
        {
            new PopupElement("3D"),
            new PopupElement("2D"),
        };

        private PopupElement[] spritePackerPopupList =
        {
            new PopupElement("Disabled"),
            new PopupElement("Sprite Atlas V1 - Enabled For Builds"),
            new PopupElement("Sprite Atlas V1 - Always Enabled"),
            new PopupElement("Sprite Atlas V2 (Experimental) - Enabled"),
        };
        private static readonly int spritePackDeprecatedEnums = 2;

        private PopupElement[] lineEndingsPopupList =
        {
            new PopupElement("OS Native"),
            new PopupElement("Unix"),
            new PopupElement("Windows"),
        };

        private PopupElement[] spritePackerPaddingPowerPopupList =
        {
            new PopupElement("1"),
            new PopupElement("2"),
            new PopupElement("3"),
        };

        private PopupElement[] remoteDevicePopupList;
        private DevDevice[]    remoteDeviceList;
        private PopupElement[] remoteCompressionList =
        {
            new PopupElement("JPEG"),
            new PopupElement("PNG"),
        };
        private PopupElement[] remoteResolutionList =
        {
            new PopupElement("Downsize"),
            new PopupElement("Normal"),
        };
        private PopupElement[] remoteJoystickSourceList =
        {
            new PopupElement("Remote"),
            new PopupElement("Local"),
        };

        private PopupElement[] assetPipelineModePopupList =
        {
            new PopupElement("Version 1 (deprecated)"),
            new PopupElement("Version 2"),
        };

        private PopupElement[] cacheServerModePopupList =
        {
            new PopupElement("Use global settings (stored in preferences)"),
            new PopupElement("Enabled"),
            new PopupElement("Disabled"),
        };

        private PopupElement[] cacheServerAuthMode =
        {
            new PopupElement("Basic")
        };

        private PopupElement[] etcTextureCompressorPopupList =
        {
            new PopupElement("Legacy"),
            new PopupElement("Default"),
            new PopupElement("Custom"),
        };

        private PopupElement[] etcTextureFastCompressorPopupList =
        {
            new PopupElement("etcpak"),
            new PopupElement("ETCPACK Fast"),
        };

        private PopupElement[] etcTextureNormalCompressorPopupList =
        {
            new PopupElement("etcpak"),
            new PopupElement("ETCPACK Fast"),
            new PopupElement("Etc2Comp Fast"),
            new PopupElement("Etc2Comp Best"),
        };

        private PopupElement[] etcTextureBestCompressorPopupList =
        {
            new PopupElement("Etc2Comp Fast"),
            new PopupElement("Etc2Comp Best"),
            new PopupElement("ETCPACK Best"),
        };

        SerializedProperty m_EnableTextureStreamingInPlayMode;
        SerializedProperty m_EnableTextureStreamingInEditMode;

        SerializedProperty m_EnableRoslynAnalyzers;

        SerializedProperty m_AsyncShaderCompilation;
        SerializedProperty m_CachingShaderPreprocessor;
        SerializedProperty m_DefaultBehaviorMode;
        SerializedProperty m_SerializationMode;
        SerializedProperty m_SerializeInlineMappingsOnOneLine;
        SerializedProperty m_PrefabRegularEnvironment;
        SerializedProperty m_PrefabUIEnvironment;
        SerializedProperty m_UseLegacyProbeSampleCount;
        SerializedProperty m_DisableCookiesInLightmapper;
        SerializedProperty m_SpritePackerMode;
        SerializedProperty m_EtcTextureCompressorBehavior;
        SerializedProperty m_EtcTextureFastCompressor;
        SerializedProperty m_EtcTextureNormalCompressor;
        SerializedProperty m_EtcTextureBestCompressor;
        SerializedProperty m_LineEndingsForNewScripts;
        SerializedProperty m_EnterPlayModeOptionsEnabled;
        SerializedProperty m_EnterPlayModeOptions;
        SerializedProperty m_ProjectGenerationIncludedExtensions;
        SerializedProperty m_ProjectGenerationRootNamespace;

        bool m_IsGlobalSettings;

        enum CacheServerConnectionState { Unknown, Success, Failure }
        private CacheServerConnectionState m_CacheServerConnectionState;
        private static string s_ForcedAssetPipelineWarning;

        public void OnEnable()
        {
            DevDeviceList.Changed += OnDeviceListChanged;
            BuildRemoteDeviceList();

            m_EnableTextureStreamingInPlayMode = serializedObject.FindProperty("m_EnableTextureStreamingInPlayMode");
            m_EnableTextureStreamingInEditMode = serializedObject.FindProperty("m_EnableTextureStreamingInEditMode");

            m_EnableRoslynAnalyzers = serializedObject.FindProperty("m_EnableRoslynAnalyzers");

            m_AsyncShaderCompilation = serializedObject.FindProperty("m_AsyncShaderCompilation");
            m_CachingShaderPreprocessor = serializedObject.FindProperty("m_CachingShaderPreprocessor");

            m_DefaultBehaviorMode = serializedObject.FindProperty("m_DefaultBehaviorMode");
            Assert.IsNotNull(m_DefaultBehaviorMode);

            m_SerializationMode = serializedObject.FindProperty("m_SerializationMode");
            Assert.IsNotNull(m_SerializationMode);

            m_SerializeInlineMappingsOnOneLine = serializedObject.FindProperty("m_SerializeInlineMappingsOnOneLine");
            Assert.IsNotNull(m_SerializeInlineMappingsOnOneLine);

            m_PrefabRegularEnvironment = serializedObject.FindProperty("m_PrefabRegularEnvironment");
            Assert.IsNotNull(m_PrefabRegularEnvironment);

            m_PrefabUIEnvironment = serializedObject.FindProperty("m_PrefabUIEnvironment");
            Assert.IsNotNull(m_PrefabUIEnvironment);

            m_UseLegacyProbeSampleCount = serializedObject.FindProperty("m_UseLegacyProbeSampleCount");
            Assert.IsNotNull(m_UseLegacyProbeSampleCount);

            m_DisableCookiesInLightmapper = serializedObject.FindProperty("m_DisableCookiesInLightmapper");
            Assert.IsNotNull(m_DisableCookiesInLightmapper);

            m_SpritePackerMode = serializedObject.FindProperty("m_SpritePackerMode");
            Assert.IsNotNull(m_SpritePackerMode);

            m_EtcTextureCompressorBehavior = serializedObject.FindProperty("m_EtcTextureCompressorBehavior");
            Assert.IsNotNull(m_EtcTextureCompressorBehavior);

            m_EtcTextureFastCompressor = serializedObject.FindProperty("m_EtcTextureFastCompressor");
            Assert.IsNotNull(m_EtcTextureFastCompressor);

            m_EtcTextureNormalCompressor = serializedObject.FindProperty("m_EtcTextureNormalCompressor");
            Assert.IsNotNull(m_EtcTextureNormalCompressor);

            m_EtcTextureBestCompressor = serializedObject.FindProperty("m_EtcTextureBestCompressor");
            Assert.IsNotNull(m_EtcTextureBestCompressor);

            m_LineEndingsForNewScripts = serializedObject.FindProperty("m_LineEndingsForNewScripts");
            Assert.IsNotNull(m_LineEndingsForNewScripts);

            m_EnterPlayModeOptionsEnabled = serializedObject.FindProperty("m_EnterPlayModeOptionsEnabled");
            Assert.IsNotNull(m_EnterPlayModeOptionsEnabled);

            m_EnterPlayModeOptions = serializedObject.FindProperty("m_EnterPlayModeOptions");
            Assert.IsNotNull(m_EnterPlayModeOptions);

            m_ProjectGenerationIncludedExtensions = serializedObject.FindProperty("m_ProjectGenerationIncludedExtensions");
            Assert.IsNotNull(m_ProjectGenerationIncludedExtensions);

            m_ProjectGenerationRootNamespace = serializedObject.FindProperty("m_ProjectGenerationRootNamespace");
            Assert.IsNotNull(m_ProjectGenerationRootNamespace);

            m_CacheServerConnectionState = CacheServerConnectionState.Unknown;
            s_ForcedAssetPipelineWarning = null;

            m_IsGlobalSettings = EditorSettings.GetEditorSettings() == target;
        }

        public void OnDisable()
        {
            DevDeviceList.Changed -= OnDeviceListChanged;
            AssetDatabaseExperimental.RefreshSettings();
        }

        void OnDeviceListChanged()
        {
            BuildRemoteDeviceList();
        }

        void BuildRemoteDeviceList()
        {
            var devices = new List<DevDevice>();
            var popupList = new List<PopupElement>();

            devices.Add(DevDevice.none);
            popupList.Add(new PopupElement("None"));

            // TODO: move Android stuff to editor extension
            devices.Add(new DevDevice("Any Android Device", "Any Android Device",
                "virtual", "Android", DevDeviceState.Connected,
                DevDeviceFeatures.RemoteConnection));
            popupList.Add(new PopupElement("Any Android Device"));

            foreach (var device in DevDeviceList.GetDevices())
            {
                bool supportsRemote = (device.features & DevDeviceFeatures.RemoteConnection) != 0;
                if (!device.isConnected || !supportsRemote)
                    continue;

                devices.Add(device);
                popupList.Add(new PopupElement(device.name));
            }

            remoteDeviceList = devices.ToArray();
            remoteDevicePopupList = popupList.ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // GUI.enabled hack because we don't want some controls to be disabled if the EditorSettings.asset is locked
            // since some of the controls are not dependent on the Editor Settings asset. Unfortunately, this assumes
            // that the editor will only be disabled because of version control locking which may change in the future.
            var editorEnabled = GUI.enabled;

            // Remove Settings are taken from preferences and NOT from the EditorSettings Asset.
            // Only show them when editing the "global" settings
            if (m_IsGlobalSettings)
                ShowUnityRemoteGUI(editorEnabled);

            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            GUILayout.Space(10);

            int index = m_SerializationMode.intValue;
            using (new EditorGUI.DisabledScope(!collabEnabled))
            {
                GUI.enabled = !collabEnabled;
                GUILayout.Label(Content.assetSerialization, EditorStyles.boldLabel);
                GUI.enabled = editorEnabled && !collabEnabled;
                CreatePopupMenu("Mode", serializationPopupList, index, SetAssetSerializationMode);
            }
            if (collabEnabled)
            {
                EditorGUILayout.HelpBox("Asset Serialization is forced to Text when using Collaboration feature.", MessageType.Warning);
            }

            if (m_SerializationMode.intValue != (int)SerializationMode.ForceBinary)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SerializeInlineMappingsOnOneLine);
                if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
                {
                    EditorSettings.serializeInlineMappingsOnOneLine = m_SerializeInlineMappingsOnOneLine.boolValue;
                }
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.defaultBehaviorMode, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp(m_DefaultBehaviorMode.intValue, 0, behaviorPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, behaviorPopupList, index, SetDefaultBehaviorMode);

            // CacheServer is part asset and preferences. Only show UI in case of Global Settings editing.
            if (m_IsGlobalSettings)
            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = true;

                DoCacheServerSettings();

                GUI.enabled = wasEnabled;
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label("Prefab Editing Environments", EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            {
                EditorGUI.BeginChangeCheck();
                var scene = m_PrefabRegularEnvironment.objectReferenceValue as SceneAsset;
                scene = (SceneAsset)EditorGUILayout.ObjectField("Regular Environment", scene, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PrefabRegularEnvironment.objectReferenceValue = scene;
                    if (m_IsGlobalSettings)
                    {
                        EditorSettings.prefabRegularEnvironment = scene;
                    }
                }
            }
            {
                EditorGUI.BeginChangeCheck();
                var scene = m_PrefabUIEnvironment.objectReferenceValue as SceneAsset;
                scene = (SceneAsset)EditorGUILayout.ObjectField("UI Environment", scene, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PrefabUIEnvironment.objectReferenceValue = scene;
                    if (m_IsGlobalSettings)
                    {
                        EditorSettings.prefabUIEnvironment = scene;
                    }
                }
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.graphics, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            if (m_IsGlobalSettings)
            {
                EditorGUI.BeginChangeCheck();
                bool showRes = LightmapVisualization.showResolution;
                showRes = EditorGUILayout.Toggle(Content.showLightmapResolutionOverlay, showRes);
                if (EditorGUI.EndChangeCheck())
                    LightmapVisualization.showResolution = showRes;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UseLegacyProbeSampleCount, Content.useLegacyProbeSampleCount);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_IsGlobalSettings)
                    EditorSettings.useLegacyProbeSampleCount = m_UseLegacyProbeSampleCount.boolValue;

                EditorApplication.RequestRepaintAllViews();
            }

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Content.enableCookiesInLightmapper, m_DisableCookiesInLightmapper);
            EditorGUI.BeginChangeCheck();
            bool enableCookiesInLightmapperValue = !m_DisableCookiesInLightmapper.boolValue;
            enableCookiesInLightmapperValue = EditorGUI.Toggle(rect, Content.enableCookiesInLightmapper, enableCookiesInLightmapperValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_DisableCookiesInLightmapper.boolValue = !enableCookiesInLightmapperValue;

                if (m_IsGlobalSettings)
                    EditorSettings.enableCookiesInLightmapper = enableCookiesInLightmapperValue;

                EditorApplication.RequestRepaintAllViews();
            }
            EditorGUI.EndProperty();

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.spritePacker, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            // Legacy Packer has been deprecated.
            index = Mathf.Clamp(m_SpritePackerMode.intValue - spritePackDeprecatedEnums, 0, spritePackerPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, spritePackerPopupList, index, SetSpritePackerMode);

            if (m_SpritePackerMode.intValue == (int)SpritePackerMode.SpriteAtlasV2)
            {
                var message = "Sprite Atlas V2 (Experimental) supports CacheServer with Importer workflow. Please take a backup of your project before switching to V2.";
                EditorGUILayout.HelpBox(message, MessageType.Info, true);
            }

            DoProjectGenerationSettings();
            DoEtcTextureCompressionSettings();
            DoLineEndingsSettings();
            DoStreamingSettings();
            DoShaderCompilationSettings();
            DoEnterPlayModeSettings();
            DoRoslynAnalyzerSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DoProjectGenerationSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.cSharpProjectGeneration, EditorStyles.boldLabel);


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ProjectGenerationIncludedExtensions, Content.additionalExtensionsToInclude);
            if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
            {
                EditorSettings.Internal_ProjectGenerationUserExtensions = m_ProjectGenerationIncludedExtensions.stringValue;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ProjectGenerationRootNamespace, Content.rootNamespace);
            if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
            {
                EditorSettings.projectGenerationRootNamespace = m_ProjectGenerationRootNamespace.stringValue;
            }
        }

        private void DoEtcTextureCompressionSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label(Content.etcTextureCompressor, EditorStyles.boldLabel);

            int index = Mathf.Clamp(m_EtcTextureCompressorBehavior.intValue, 0, etcTextureCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.behavior.text, etcTextureCompressorPopupList, index, SetEtcTextureCompressorBehavior);

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(index < 2);

            index = Mathf.Clamp(m_EtcTextureFastCompressor.intValue, 0, etcTextureFastCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.fast.text, etcTextureFastCompressorPopupList, index, SetEtcTextureFastCompressor);

            index = Mathf.Clamp(m_EtcTextureNormalCompressor.intValue, 0, etcTextureNormalCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.normal.text, etcTextureNormalCompressorPopupList, index, SetEtcTextureNormalCompressor);

            index = Mathf.Clamp(m_EtcTextureBestCompressor.intValue, 0, etcTextureBestCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.best.text, etcTextureBestCompressorPopupList, index, SetEtcTextureBestCompressor);

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        private static string GetForcedAssetPipelineWarning()
        {
            if (s_ForcedAssetPipelineWarning == null)
            {
                if (CacheServerPreferences.GetEnvironmentAssetPipelineOverride())
                    s_ForcedAssetPipelineWarning = "Asset pipeline mode was forced via the UNITY_ASSETS_V2_KATANA_TESTS environment variable. The above setting is not in effect before restarting without the environment variable set.";
                else if (CacheServerPreferences.GetCommandLineAssetPipelineOverride() != 0)
                    s_ForcedAssetPipelineWarning = "Asset pipeline mode was forced via command line argument using -adb2 command line argument. The above setting is not in effect before restarting without the command line argument.";
                else if (CacheServerPreferences.GetMagicFileAssetPipelineOverride())
                    s_ForcedAssetPipelineWarning = "Asset pipeline mode was forced via via magic adb2.txt file in project root. The above setting is not in effect before restarting without the magic file.";
                else
                    s_ForcedAssetPipelineWarning = string.Empty;
            }
            return s_ForcedAssetPipelineWarning;
        }

        private void DoCacheServerSettings()
        {
            Assert.IsTrue(m_IsGlobalSettings);
            GUILayout.Space(10);
            GUILayout.Label(Content.cacheServer, EditorStyles.boldLabel);

            var overrideAddress = CacheServerPreferences.GetCommandLineRemoteAddressOverride();
            if (overrideAddress != null)
            {
                EditorGUILayout.HelpBox("Cache Server remote address forced via command line argument. To use the cache server address specified here please restart Unity without the -CacheServerIPAddress command line argument.", MessageType.Info, true);
            }

            int index = Mathf.Clamp((int)EditorSettings.cacheServerMode, 0, cacheServerModePopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, cacheServerModePopupList, index, SetCacheServerMode);

            if (index != (int)CacheServerMode.Disabled)
            {
                bool isCacheServerEnabled = true;

                if (index == (int)CacheServerMode.AsPreferences)
                {
                    if (CacheServerPreferences.IsCacheServerV2Enabled)
                    {
                        var cacheServerIP = CacheServerPreferences.CachesServerV2Address;
                        cacheServerIP = string.IsNullOrEmpty(cacheServerIP) ? "Not set in preferences" : cacheServerIP;
                        EditorGUILayout.HelpBox(cacheServerIP, MessageType.None, false);
                    }
                    else
                    {
                        isCacheServerEnabled = false;
                        EditorGUILayout.HelpBox("Disabled", MessageType.None, false);
                    }
                }

                if (isCacheServerEnabled)
                {
                    var oldEndpoint = EditorSettings.cacheServerEndpoint;
                    var newEndpoint = EditorGUILayout.TextField(Content.cacheServerIPLabel, oldEndpoint);
                    if (newEndpoint != oldEndpoint)
                    {
                        EditorSettings.cacheServerEndpoint = newEndpoint;
                    }

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Check Connection", GUILayout.Width(150)))
                    {
                        if (AssetDatabase.IsV2Enabled())
                        {
                            var address = EditorSettings.cacheServerEndpoint.Split(':');
                            var ip = address[0];
                            UInt16 port = 0; // If 0, will use the default set port
                            if (address.Length == 2)
                                port = Convert.ToUInt16(address[1]);

                            if (AssetDatabaseExperimental.CanConnectToCacheServer(ip, port))
                                m_CacheServerConnectionState = CacheServerConnectionState.Success;
                            else
                                m_CacheServerConnectionState = CacheServerConnectionState.Failure;
                        }
                        else
                        {
                            if (InternalEditorUtility.CanConnectToCacheServer())
                                m_CacheServerConnectionState = CacheServerConnectionState.Success;
                            else
                                m_CacheServerConnectionState = CacheServerConnectionState.Failure;
                        }
                    }

                    GUILayout.Space(25);

                    switch (m_CacheServerConnectionState)
                    {
                        case CacheServerConnectionState.Success:
                            EditorGUILayout.HelpBox("Connection successful.", MessageType.Info, true);
                            break;

                        case CacheServerConnectionState.Failure:
                            EditorGUILayout.HelpBox("Connection failed.", MessageType.Warning, true);
                            break;

                        case CacheServerConnectionState.Unknown:
                            GUILayout.Space(44);
                            break;
                    }

                    EditorGUILayout.EndHorizontal();

                    var old = EditorSettings.cacheServerNamespacePrefix;
                    var newvalue = EditorGUILayout.TextField(Content.cacheServerNamespacePrefixLabel, old);
                    if (newvalue != old)
                    {
                        EditorSettings.cacheServerNamespacePrefix = newvalue;
                    }

                    EditorGUI.BeginChangeCheck();
                    bool enableDownload = EditorSettings.cacheServerEnableDownload;
                    enableDownload = EditorGUILayout.Toggle(Content.cacheServerEnableDownloadLabel, enableDownload);
                    if (EditorGUI.EndChangeCheck())
                        EditorSettings.cacheServerEnableDownload = enableDownload;

                    EditorGUI.BeginChangeCheck();
                    bool enableUpload = EditorSettings.cacheServerEnableUpload;
                    enableUpload = EditorGUILayout.Toggle(Content.cacheServerEnableUploadLabel, enableUpload);
                    if (EditorGUI.EndChangeCheck())
                        EditorSettings.cacheServerEnableUpload = enableUpload;

                    bool enableAuth = EditorSettings.cacheServerEnableAuth;
                    using (new EditorGUI.DisabledScope(enableAuth))
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enableTls = EditorSettings.cacheServerEnableTls;
                        enableTls = EditorGUILayout.Toggle(Content.cacheServerEnableTlsLabel, enableTls);
                        if (EditorGUI.EndChangeCheck())
                            EditorSettings.cacheServerEnableTls = enableTls;
                    }

                }
            }
        }

        private void DoLineEndingsSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.lineEndingForNewScripts, EditorStyles.boldLabel);

            int index = m_LineEndingsForNewScripts.intValue;
            CreatePopupMenu(Content.mode.text, lineEndingsPopupList, index, SetLineEndingsForNewScripts);
        }

        private void DoStreamingSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.streamingSettings, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_EnableTextureStreamingInPlayMode, Content.enablePlayModeTextureStreaming);
            EditorGUILayout.PropertyField(m_EnableTextureStreamingInEditMode, Content.enableEditModeTextureStreaming);
        }

        private void DoRoslynAnalyzerSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.roslynAnalyzerSettings, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_EnableRoslynAnalyzers, Content.enableRoslynAnalyzers);
        }

        private void DoShaderCompilationSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.shaderCompilation, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_AsyncShaderCompilation, Content.asyncShaderCompilation);
            EditorGUILayout.PropertyField(m_CachingShaderPreprocessor, Content.cachingShaderPreprocessor);
        }

        private void DoEnterPlayModeSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.enterPlayModeSettings, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnterPlayModeOptionsEnabled, Content.enterPlayModeOptionsEnabled);
            if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
                EditorSettings.enterPlayModeOptionsEnabled = m_EnterPlayModeOptionsEnabled.boolValue;

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_EnterPlayModeOptionsEnabled.boolValue))
            {
                EnterPlayModeOptions options = (EnterPlayModeOptions)m_EnterPlayModeOptions.intValue;
                options = ToggleEnterPlayModeOptions(options, EnterPlayModeOptions.DisableDomainReload, Content.enterPlayModeOptionsEnableDomainReload);
                options = ToggleEnterPlayModeOptions(options, EnterPlayModeOptions.DisableSceneReload, Content.enterPlayModeOptionsEnableSceneReload);

                if (m_EnterPlayModeOptions.intValue != (int)options)
                {
                    m_EnterPlayModeOptions.intValue = (int)options;
                    if (m_IsGlobalSettings)
                        EditorSettings.enterPlayModeOptions = options;
                }
            }
            EditorGUI.indentLevel--;
        }

        static int GetIndexById(DevDevice[] elements, string id, int defaultIndex)
        {
            for (int i = 0; i < elements.Length; i++)
                if (elements[i].id == id)
                    return i;

            return defaultIndex;
        }

        static int GetIndexById(PopupElement[] elements, string id, int defaultIndex)
        {
            for (int i = 0; i < elements.Length; i++)
                if (elements[i].id == id)
                    return i;

            return defaultIndex;
        }

        private void ShowUnityRemoteGUI(bool editorEnabled)
        {
            // This is a global Settings persisted in preferences
            Assert.IsTrue(m_IsGlobalSettings);
            GUI.enabled = true;
            GUILayout.Label(Content.unityRemote, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            // Find selected device index
            string id = EditorSettings.unityRemoteDevice;
            // We assume first device to be "None", and default to it, hence 0
            int index = GetIndexById(remoteDeviceList, id, 0);

            var content = new GUIContent(remoteDevicePopupList[index].content);
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, Content.device);
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteDevicePopupList, index, SetUnityRemoteDevice);

            int compression = GetIndexById(remoteCompressionList, EditorSettings.unityRemoteCompression, 0);
            content = new GUIContent(remoteCompressionList[compression].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, Content.compression);
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteCompressionList, compression, SetUnityRemoteCompression);

            int resolution = GetIndexById(remoteResolutionList, EditorSettings.unityRemoteResolution, 0);
            content = new GUIContent(remoteResolutionList[resolution].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, Content.resolution);
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteResolutionList, resolution, SetUnityRemoteResolution);

            int joystickSource = GetIndexById(remoteJoystickSourceList, EditorSettings.unityRemoteJoystickSource, 0);
            content = new GUIContent(remoteJoystickSourceList[joystickSource].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, Content.joystickSource);
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteJoystickSourceList, joystickSource, SetUnityRemoteJoystickSource);
        }

        private void CreatePopupMenu(string title, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            CreatePopupMenu(serializedObject, title, elements[selectedIndex].content, elements, selectedIndex, func);
        }

        internal static void CreatePopupMenu(SerializedObject obj, string title, GUIContent content, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent(title));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
            {
                DoPopup(popupRect, elements, selectedIndex, data =>
                {
                    func(data);
                    obj?.ApplyModifiedProperties();
                });
            }
        }

        internal static void DoPopup(Rect popupRect, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                menu.AddItem(element.content, i == selectedIndex, func, i);
            }
            menu.DropDown(popupRect);
        }

        private void SetAssetSerializationMode(object data)
        {
            int popupIndex = (int)data;
            m_SerializationMode.intValue = popupIndex;
            if (m_IsGlobalSettings)
                EditorSettings.serializationMode = (SerializationMode)popupIndex;
        }

        private void SetUnityRemoteDevice(object data)
        {
            EditorSettings.unityRemoteDevice = remoteDeviceList[(int)data].id;
        }

        private void SetUnityRemoteCompression(object data)
        {
            EditorSettings.unityRemoteCompression = remoteCompressionList[(int)data].id;
        }

        private void SetUnityRemoteResolution(object data)
        {
            EditorSettings.unityRemoteResolution = remoteResolutionList[(int)data].id;
        }

        private void SetUnityRemoteJoystickSource(object data)
        {
            EditorSettings.unityRemoteJoystickSource = remoteJoystickSourceList[(int)data].id;
        }

        private void SetDefaultBehaviorMode(object data)
        {
            int popupIndex = (int)data;
            m_DefaultBehaviorMode.intValue = popupIndex;
            if (m_IsGlobalSettings)
            {
                EditorSettings.defaultBehaviorMode = (EditorBehaviorMode)popupIndex;
            }
        }

        private void SetSpritePackerMode(object data)
        {
            int popupIndex = (int)data;

            // Legacy Packer has been obsoleted (1 & 2). Disabled (0) is still valid.
            popupIndex = (popupIndex != 0) ? (popupIndex + spritePackDeprecatedEnums) : 0;
            m_SpritePackerMode.intValue = popupIndex;

            if (m_IsGlobalSettings)
            {
                EditorSettings.spritePackerMode = (SpritePackerMode)popupIndex;
                if (popupIndex == (int)SpritePackerMode.SpriteAtlasV2)
                {
                    UnityEditor.U2D.SpriteAtlasImporter.MigrateAllSpriteAtlases();
                }
            }
        }

        private void SetCacheServerMode(object data)
        {
            EditorSettings.cacheServerMode = (CacheServerMode)data;
        }

        private void SetCacheServerAuthMode(object data)
        {
            EditorUserSettings.SetConfigValue("cacheServerAuthMode", $"{(int)data}");
        }


        private void SetEtcTextureCompressorBehavior(object data)
        {
            int newValue = (int)data;
            m_EtcTextureCompressorBehavior.intValue = newValue;

            if (m_IsGlobalSettings)
            {
                if (EditorSettings.etcTextureCompressorBehavior == newValue)
                    return;

                EditorSettings.etcTextureCompressorBehavior = newValue;

                if (newValue == 0)
                    EditorSettings.SetEtcTextureCompressorLegacyBehavior();
                else
                    EditorSettings.SetEtcTextureCompressorDefaultBehavior();
            }
        }

        private void SetEtcTextureFastCompressor(object data)
        {
            m_EtcTextureFastCompressor.intValue = (int)data;
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureFastCompressor = (int)data;
        }

        private void SetEtcTextureNormalCompressor(object data)
        {
            m_EtcTextureNormalCompressor.intValue = (int)data;
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureNormalCompressor = (int)data;
        }

        private void SetEtcTextureBestCompressor(object data)
        {
            m_EtcTextureBestCompressor.intValue = (int)data;
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureBestCompressor = (int)data;
        }

        private void SetLineEndingsForNewScripts(object data)
        {
            int popupIndex = (int)data;
            m_LineEndingsForNewScripts.intValue = popupIndex;
            if (m_IsGlobalSettings)
                EditorSettings.lineEndingsForNewScripts = (LineEndingsMode)popupIndex;
        }

        private EnterPlayModeOptions ToggleEnterPlayModeOptions(EnterPlayModeOptions options, EnterPlayModeOptions flag, GUIContent content)
        {
            bool isSet = ((options & flag) == flag);
            isSet = EditorGUILayout.Toggle(content, !isSet);

            if (isSet)
            {
                options &= ~flag;
            }
            else
            {
                options |= flag;
            }

            return options;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Editor", "ProjectSettings/EditorSettings.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>());
            return provider;
        }
    }
}
