// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Hardware;
using UnityEditor.Collaboration;
using UnityEditor.Experimental;

namespace UnityEditor
{
    [CustomEditor(typeof(EditorSettings))]
    internal class EditorSettingsInspector : ProjectSettingsBaseEditor
    {
        class Content
        {
            public static GUIContent unityRemote = EditorGUIUtility.TrTextContent("Unity Remote");
            public static GUIContent device = EditorGUIUtility.TrTextContent("Device");
            public static GUIContent compression = EditorGUIUtility.TrTextContent("Compression");
            public static GUIContent resolution = EditorGUIUtility.TrTextContent("Resolution");
            public static GUIContent joystickSource = EditorGUIUtility.TrTextContent("Joystick Source");

            public static GUIContent mode = EditorGUIUtility.TrTextContent("Mode");

            public static GUIContent assetPipeline = EditorGUIUtility.TrTextContent("Asset Pipeline");
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

            public static GUIContent lineEndingForNewScripts = EditorGUIUtility.TrTextContent("Line Endings For New Scripts");

            public static GUIContent streamingSettings = EditorGUIUtility.TrTextContent("Streaming Settings");
            public static GUIContent enablePlayModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Play Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Play Mode");
            public static GUIContent enableEditModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Edit Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Edit Mode");

            private const string activeAssetPipelineVersionTooltip = "The active asset import pipeline is chosen at startup by inspecting the following sources in order: Environment variable, command line argument (-adb2), local per project editor settings (the dropdown above)";
            public static readonly GUIContent activeAssetPipelineVersionLabel = EditorGUIUtility.TrTextContent("Active version", activeAssetPipelineVersionTooltip);
            public static readonly GUIContent activeAssetPipelineVersion = new GUIContent("2", activeAssetPipelineVersionTooltip);

            public static GUIContent shaderCompilation = EditorGUIUtility.TrTextContent("Shader Compilation");
            public static GUIContent asyncShaderCompilation = EditorGUIUtility.TrTextContent("Asynchronous Shader Compilation", "Enables async shader compilation in Game and Scene view. Async compilation for custom editor tools can be achieved via script API and is not affected by this option.");
            public static GUIContent cachingShaderPreprocessor = EditorGUIUtility.TrTextContent("Caching Preprocessor (Experimental)", "Enables caching shader preprocessor with advanced functionality.");

            public static readonly GUIContent enterPlayModeSettings = EditorGUIUtility.TrTextContent("Enter Play Mode Settings");
            public static readonly GUIContent enterPlayModeOptionsEnabled = EditorGUIUtility.TrTextContent("Enter Play Mode Options (Experimental)", "Enables options when Entering Play Mode");
            public static readonly GUIContent enterPlayModeOptionsEnableDomainReload = EditorGUIUtility.TrTextContent("Reload Domain", "Enables Domain Reload when Entering Play Mode. Domain reload reinitializes game completely making loading behavior very close to the Player");
            public static readonly GUIContent enterPlayModeOptionsEnableSceneReload = EditorGUIUtility.TrTextContent("Reload Scene", "Enables Scene Reload when Entering Play Mode. Scene reload makes loading behavior and performance characteristics very close to the Player");

            public static readonly GUIContent numberingScheme = EditorGUIUtility.TrTextContent("Numbering Scheme");

            public static readonly GUIContent[] numberingSchemeNames =
            {
                EditorGUIUtility.TrTextContent("Prefab (1)", "Number in parentheses"),
                EditorGUIUtility.TrTextContent("Prefab.1", "Number after dot"),
                EditorGUIUtility.TrTextContent("Prefab_1", "Number after underscore")
            };
            public static readonly int[] numberingSchemeValues =
            {
                (int)EditorSettings.NamingScheme.SpaceParenthesis,
                (int)EditorSettings.NamingScheme.Dot,
                (int)EditorSettings.NamingScheme.Underscore
            };
            public static readonly GUIContent numberingHierarchyScheme = EditorGUIUtility.TrTextContent("Game Object Naming");
            public static readonly GUIContent numberingHierarchyDigits = EditorGUIUtility.TrTextContent("Game Object Digits");
            public static readonly GUIContent numberingProjectSpace = EditorGUIUtility.TrTextContent("Space Before Number in Asset Names");
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

        SerializedProperty m_UseLegacyProbeSampleCount;
        SerializedProperty m_DisableCookiesInLightmapper;

        SerializedProperty m_EnableTextureStreamingInPlayMode;
        SerializedProperty m_EnableTextureStreamingInEditMode;

        SerializedProperty m_GameObjectNamingDigits;
        SerializedProperty m_GameObjectNamingScheme;
        SerializedProperty m_AssetNamingUsesSpace;

        SerializedProperty m_AsyncShaderCompilation;
        SerializedProperty m_CachingShaderPreprocessor;

        enum CacheServerConnectionState { Unknown, Success, Failure }
        private CacheServerConnectionState m_CacheServerConnectionState;
        private static string s_ForcedAssetPipelineWarning;

        public void OnEnable()
        {
            DevDeviceList.Changed += OnDeviceListChanged;
            BuildRemoteDeviceList();

            m_UseLegacyProbeSampleCount = serializedObject.FindProperty("m_UseLegacyProbeSampleCount");
            m_DisableCookiesInLightmapper = serializedObject.FindProperty("m_DisableCookiesInLightmapper");

            m_EnableTextureStreamingInPlayMode = serializedObject.FindProperty("m_EnableTextureStreamingInPlayMode");
            m_EnableTextureStreamingInEditMode = serializedObject.FindProperty("m_EnableTextureStreamingInEditMode");

            m_GameObjectNamingDigits = serializedObject.FindProperty("m_GameObjectNamingDigits");
            m_GameObjectNamingScheme = serializedObject.FindProperty("m_GameObjectNamingScheme");
            m_AssetNamingUsesSpace = serializedObject.FindProperty("m_AssetNamingUsesSpace");

            m_AsyncShaderCompilation = serializedObject.FindProperty("m_AsyncShaderCompilation");
            m_CachingShaderPreprocessor = serializedObject.FindProperty("m_CachingShaderPreprocessor");

            m_CacheServerConnectionState = CacheServerConnectionState.Unknown;
            s_ForcedAssetPipelineWarning = null;
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

            ShowUnityRemoteGUI(editorEnabled);

            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            GUILayout.Space(10);

            int index = (int)EditorSettings.serializationMode;
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

            if (EditorSettings.serializationMode != SerializationMode.ForceBinary)
                EditorSettings.serializeInlineMappingsOnOneLine = EditorGUILayout.Toggle(Content.textSerializeMappingsOnOneLine, EditorSettings.serializeInlineMappingsOnOneLine);

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.defaultBehaviorMode, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp((int)EditorSettings.defaultBehaviorMode, 0, behaviorPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, behaviorPopupList, index, SetDefaultBehaviorMode);

            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = true;

                DoAssetPipelineSettings();

                if (EditorSettings.assetPipelineMode == AssetPipelineMode.Version2)
                    DoCacheServerSettings();

                GUI.enabled = wasEnabled;
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label("Prefab Editing Environments", EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            {
                EditorGUI.BeginChangeCheck();
                SceneAsset scene = EditorSettings.prefabRegularEnvironment;
                scene = (SceneAsset)EditorGUILayout.ObjectField("Regular Environment", scene, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                    EditorSettings.prefabRegularEnvironment = scene;
            }
            {
                EditorGUI.BeginChangeCheck();
                SceneAsset scene = EditorSettings.prefabUIEnvironment;
                scene = (SceneAsset)EditorGUILayout.ObjectField("UI Environment", scene, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                    EditorSettings.prefabUIEnvironment = scene;
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.graphics, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UseLegacyProbeSampleCount, Content.useLegacyProbeSampleCount);
            if (EditorGUI.EndChangeCheck())
                EditorApplication.RequestRepaintAllViews();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Content.enableCookiesInLightmapper, m_DisableCookiesInLightmapper);
            EditorGUI.BeginChangeCheck();
            bool enableCookiesInLightmapperValue = !m_DisableCookiesInLightmapper.boolValue;
            enableCookiesInLightmapperValue = EditorGUI.Toggle(rect, Content.enableCookiesInLightmapper, enableCookiesInLightmapperValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_DisableCookiesInLightmapper.boolValue = !enableCookiesInLightmapperValue;
                EditorApplication.RequestRepaintAllViews();
            }
            EditorGUI.EndProperty();

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.spritePacker, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            // Legacy Packer has been deprecated.
            index = Mathf.Clamp((int)(EditorSettings.spritePackerMode) - spritePackDeprecatedEnums, 0, spritePackerPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, spritePackerPopupList, index, SetSpritePackerMode);

            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
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
            DoNumberingSchemeSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DoProjectGenerationSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.cSharpProjectGeneration, EditorStyles.boldLabel);

            var old = EditorSettings.Internal_ProjectGenerationUserExtensions;
            string newvalue = EditorGUILayout.TextField(Content.additionalExtensionsToInclude, old);
            if (newvalue != old)
                EditorSettings.Internal_ProjectGenerationUserExtensions = newvalue;

            old = EditorSettings.projectGenerationRootNamespace;
            newvalue = EditorGUILayout.TextField(Content.rootNamespace, old);
            if (newvalue != old)
                EditorSettings.projectGenerationRootNamespace = newvalue;
        }

        private void DoEtcTextureCompressionSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label(Content.etcTextureCompressor, EditorStyles.boldLabel);

            int index = Mathf.Clamp((int)EditorSettings.etcTextureCompressorBehavior, 0, etcTextureCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.behavior.text, etcTextureCompressorPopupList, index, SetEtcTextureCompressorBehavior);

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(index < 2);

            index = Mathf.Clamp((int)EditorSettings.etcTextureFastCompressor, 0, etcTextureFastCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.fast.text, etcTextureFastCompressorPopupList, index, SetEtcTextureFastCompressor);

            index = Mathf.Clamp((int)EditorSettings.etcTextureNormalCompressor, 0, etcTextureNormalCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.normal.text, etcTextureNormalCompressorPopupList, index, SetEtcTextureNormalCompressor);

            index = Mathf.Clamp((int)EditorSettings.etcTextureBestCompressor, 0, etcTextureBestCompressorPopupList.Length - 1);
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

        private void DoAssetPipelineSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.assetPipeline, EditorStyles.boldLabel);

            var assetPipelineWarning = GetForcedAssetPipelineWarning();

            EditorGUILayout.LabelField(Content.activeAssetPipelineVersionLabel, Content.activeAssetPipelineVersion);
        }

        private void DoCacheServerSettings()
        {
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
                    isCacheServerEnabled = false;
                    if (CacheServerPreferences.IsCacheServerV2Enabled)
                    {
                        var cacheServerIP = CacheServerPreferences.CachesServerV2Address;
                        cacheServerIP = string.IsNullOrEmpty(cacheServerIP) ? "Not set in preferences" : cacheServerIP;
                        EditorGUILayout.HelpBox(cacheServerIP, MessageType.None, false);
                    }
                    else
                    {
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

            int index = (int)EditorSettings.lineEndingsForNewScripts;
            CreatePopupMenu(Content.mode.text, lineEndingsPopupList, index, SetLineEndingsForNewScripts);
        }

        private void DoStreamingSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.streamingSettings, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_EnableTextureStreamingInPlayMode, Content.enablePlayModeTextureStreaming);
            EditorGUILayout.PropertyField(m_EnableTextureStreamingInEditMode, Content.enableEditModeTextureStreaming);
        }

        EditorSettings.NamingScheme m_PrevGoNamingScheme;
        int m_PrevGoNamingDigits = -1;
        string m_GoNamingHelpText;
        static string GetNewName(string name, List<string> names)
        {
            var newName = ObjectNames.GetUniqueName(names.ToArray(), name);
            names.Add(newName);
            return newName;
        }

        void DoNumberingSchemeSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.numberingScheme, EditorStyles.boldLabel);
            EditorGUILayout.IntPopup(m_GameObjectNamingScheme, Content.numberingSchemeNames, Content.numberingSchemeValues, Content.numberingHierarchyScheme);
            EditorGUILayout.IntSlider(m_GameObjectNamingDigits, 1, 5, Content.numberingHierarchyDigits);
            if (m_PrevGoNamingDigits != EditorSettings.gameObjectNamingDigits ||
                m_PrevGoNamingScheme != EditorSettings.gameObjectNamingScheme ||
                m_GoNamingHelpText == null)
            {
                var names = new List<string>();
                var n1 = "Clap";
                var n2 = "High5";
                m_GoNamingHelpText = $"Instances of prefab '{n1}' will become '{GetNewName(n1, names)}', '{GetNewName(n1, names)}', '{GetNewName(n1, names)}'\nInstances of prefab '{n2}' will become '{GetNewName(n2, names)}', '{GetNewName(n2, names)}', '{GetNewName(n2, names)}'";
                m_PrevGoNamingDigits = EditorSettings.gameObjectNamingDigits;
                m_PrevGoNamingScheme = EditorSettings.gameObjectNamingScheme;
            }
            EditorGUILayout.HelpBox(m_GoNamingHelpText, MessageType.Info, true);

            EditorGUILayout.PropertyField(m_AssetNamingUsesSpace, Content.numberingProjectSpace);
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

            EditorSettings.enterPlayModeOptionsEnabled = EditorGUILayout.Toggle(Content.enterPlayModeOptionsEnabled, EditorSettings.enterPlayModeOptionsEnabled);

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!EditorSettings.enterPlayModeOptionsEnabled))
            {
                EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
                options = ToggleEnterPlayModeOptions(options, EnterPlayModeOptions.DisableDomainReload, Content.enterPlayModeOptionsEnableDomainReload);
                options = ToggleEnterPlayModeOptions(options, EnterPlayModeOptions.DisableSceneReload, Content.enterPlayModeOptionsEnableSceneReload);
                EditorSettings.enterPlayModeOptions = options;
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

        private static void CreatePopupMenu(string title, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            CreatePopupMenu(title, elements[selectedIndex].content, elements, selectedIndex, func);
        }

        internal static void CreatePopupMenu(string title, GUIContent content, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent(title));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, elements, selectedIndex, func);
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

            EditorSettings.defaultBehaviorMode = (EditorBehaviorMode)popupIndex;
        }

        private void SetSpritePackerMode(object data)
        {
            int popupIndex = (int)data;

            // Legacy Packer has been obsoleted (1 & 2). Disabled (0) is still valid.
            popupIndex = (popupIndex != 0) ? (popupIndex + spritePackDeprecatedEnums) : 0;
            EditorSettings.spritePackerMode = (SpritePackerMode)popupIndex;

            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
                UnityEditor.U2D.SpriteAtlasImporter.MigrateAllSpriteAtlases();
        }

        private void SetSpritePackerPaddingPower(object data)
        {
            int popupIndex = (int)data;

            EditorSettings.spritePackerPaddingPower = popupIndex + 1;
        }

        [Obsolete("SetAssetPipelineMode is deprecated, as Asset Pipeline V2 is the only mode available.")]
        private void SetAssetPipelineMode(object data) {}

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

            if (EditorSettings.etcTextureCompressorBehavior == newValue)
                return;

            EditorSettings.etcTextureCompressorBehavior = newValue;

            if (newValue == 0)
                EditorSettings.SetEtcTextureCompressorLegacyBehavior();
            else
                EditorSettings.SetEtcTextureCompressorDefaultBehavior();
        }

        private void SetEtcTextureFastCompressor(object data)
        {
            EditorSettings.etcTextureFastCompressor = (int)data;
        }

        private void SetEtcTextureNormalCompressor(object data)
        {
            EditorSettings.etcTextureNormalCompressor = (int)data;
        }

        private void SetEtcTextureBestCompressor(object data)
        {
            EditorSettings.etcTextureBestCompressor = (int)data;
        }

        private void SetLineEndingsForNewScripts(object data)
        {
            int popupIndex = (int)data;

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
