// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Hardware;
using UnityEditor.Collaboration;
using UnityEngine.Assertions;

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
            public static GUIContent parallelImport = EditorGUIUtility.TrTextContent("Parallel Import", "During an asset database refresh some asset imports can be performed in parallel in sub processes.");
            public static GUIContent parallelImportLearnMore = EditorGUIUtility.TrTextContent("Learn more...", "During an asset database refresh some asset imports can be performed in parallel in sub processes.");
            public static GUIContent desiredImportWorkerCountOverride = EditorGUIUtility.TrTextContent("Override Desired Worker Count", "Override the desired worker count specified in the preferences.");
            public static GUIContent desiredImportWorkerCount = EditorGUIUtility.TrTextContent("Desired Import Worker Count", "The desired number of import worker processes to use for importing. The actual number of worker processes on the system can be both lower or higher that this, but the system will seek towards this number when importing.");
            public static GUIContent standbyImportWorkerCount = EditorGUIUtility.TrTextContent("Standby Import Worker Count", "The number of import worker processes to keep around in standby and ready for importing. The actual number of worker processes on the system can be both lower or higher that this, but the system will seek towards this number when worker processes are idle.");
            public static GUIContent idleWorkerShutdownDelay = EditorGUIUtility.TrTextContent("Idle Import Worker Shutdown Delay", "When an importer worker has been idle for this amount of seconds in will be shutdown unless it would take the worker count below the standby worker count setting.");

            public static GUIContent cacheServer = EditorGUIUtility.TrTextContent("Cache Server (project specific)");
            public static GUIContent assetPipeline = EditorGUIUtility.TrTextContent("Asset Pipeline");
            public static GUIContent artifactGarbageCollection = EditorGUIUtility.TrTextContent("Remove unused Artifacts on Restart", "By default, when you start the Editor, Unity removes unused artifact files in the Library folder, and removes their entries in the asset database. This is a form of \"garbage collection\". This setting allows you to turn off the asset database garbage collection, so that previous artifact revisions which are no longer used are still preserved after restarting the Editor. This is useful if you need to debug unexpected import results.");
            public static GUIContent cacheServerIPLabel = EditorGUIUtility.TrTextContent("IP address");
            public static GUIContent cacheServerNamespacePrefixLabel = EditorGUIUtility.TrTextContent("Namespace prefix", "The namespace used for looking up and storing values on the cache server");
            public static GUIContent cacheServerEnableDownloadLabel = EditorGUIUtility.TrTextContent("Download", "Enables downloads from the cache server.");
            public static GUIContent cacheServerEnableUploadLabel = EditorGUIUtility.TrTextContent("Upload", "Enables uploads to the cache server.");
            public static GUIContent cacheServerEnableTlsLabel = EditorGUIUtility.TrTextContent("TLS/SSL", "Enabled encryption on the cache server connection.");
            public static GUIContent cacheServerEnableAuthLabel = EditorGUIUtility.TrTextContent("Authentication (using Unity ID)", "Enable authentication for cache server using Unity ID. Also forces TLS/SSL encryption.");
            public static GUIContent cacheServerAuthUserLabel = EditorGUIUtility.TrTextContent("User");
            public static GUIContent cacheServerAuthPasswordLabel = EditorGUIUtility.TrTextContent("Password");
            public static GUIContent cacheServerValidationLabel = EditorGUIUtility.TrTextContent("Content Validation");
            public static GUIContent cacheServerDownloadBatchSizeLabel = EditorGUIUtility.TrTextContent("Download Batch Size");
            public static readonly GUIContent cacheServerLearnMore = new GUIContent("Learn more...", "Go to cacheserver documentation.");

            public static GUIContent assetSerialization = EditorGUIUtility.TrTextContent("Asset Serialization");
            public static GUIContent textSerializeMappingsOnOneLine = EditorGUIUtility.TrTextContent("Force Serialize References On One Line", "Forces Unity to write references and other inline mappings on one line, to help reduce version control noise");
            public static GUIContent defaultBehaviorMode = EditorGUIUtility.TrTextContent("Default Behaviour Mode");

            public static GUIContent buildPipelineHeader = EditorGUIUtility.TrTextContent("Build Pipeline");
            public static GUIContent ucbpEnableAssetBundles = EditorGUIUtility.TrTextContent("Multi-Process AssetBundle Building", "Enable experimental improvements to the AssetBundle Build Pipeline aimed at reducing build times with multi-process importing and providing more efficient incremental content building");
            public static readonly GUIContent ucbpLearnMore = new GUIContent("Learn more...", "Review official Unity documentation for important considerations around these experimental improvements.");

            public static GUIContent graphics = EditorGUIUtility.TrTextContent("Graphics");
            public static GUIContent showLightmapResolutionOverlay = EditorGUIUtility.TrTextContent("Show Lightmap Resolution Overlay");
            public static GUIContent useLegacyProbeSampleCount = EditorGUIUtility.TrTextContent("Use legacy Light Probe sample counts", "Uses fixed Light Probe sample counts for baking with the Progressive Lightmapper. The sample counts are: 64 direct samples, 2048 indirect samples and 2048 environment samples.");
            public static GUIContent enableCookiesInLightmapper = EditorGUIUtility.TrTextContent("Enable baked cookies support", "Determines whether cookies should be evaluated by the Progressive Lightmapper during Global Illumination calculations. Introduced in version 2020.1. ");

            public static GUIContent spritePacker = EditorGUIUtility.TrTextContent("Sprite Atlas");
            public static readonly GUIContent spriteMaxCacheSize = EditorGUIUtility.TrTextContent("Max SpriteAtlas Cache Size (GB)", "The size of the Sprite Atlas Cache folder will be kept below this maximum value when possible. Change requires Editor restart.");

            public static GUIContent cSharpProjectGeneration = EditorGUIUtility.TrTextContent("C# Project Generation");
            public static GUIContent additionalExtensionsToInclude = EditorGUIUtility.TrTextContent("Additional extensions to include");
            public static GUIContent rootNamespace = EditorGUIUtility.TrTextContent("Root namespace");

            public static GUIContent textureCompressors = EditorGUIUtility.TrTextContent("Texture Compressors");
            public static GUIContent bc7Compressor = EditorGUIUtility.TrTextContent("BC7 Compressor", "Compressor to use for BC7 format texture compression");
            public static GUIContent etcCompressor = EditorGUIUtility.TrTextContent("ETC Compressor", "Compressors to use for ETC/ETC2/EAC format texture compression");
            public static GUIContent fast = EditorGUIUtility.TrTextContent("Fast");
            public static GUIContent normal = EditorGUIUtility.TrTextContent("Normal");
            public static GUIContent best = EditorGUIUtility.TrTextContent("Best");

            public static GUIContent lineEndingForNewScripts = EditorGUIUtility.TrTextContent("Line Endings For New Scripts");

            public static GUIContent streamingSettings = EditorGUIUtility.TrTextContent("Streaming Settings");
            public static GUIContent enablePlayModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Play Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Play Mode. This reduces GPU memory by streaming mips in and out as needed.");
            public static GUIContent enableEditModeTextureStreaming = EditorGUIUtility.TrTextContent("Enable Texture Streaming In Edit Mode", "Texture Streaming must be enabled in Quality Settings for mipmap streaming to function in Edit Mode. This reduces GPU memory by streaming mips in and out as needed.");
            public static GUIContent enableEditorAsyncCPUTextureLoading = EditorGUIUtility.TrTextContent("Load texture data on demand", "While in Editor, load CPU side texture data for streaming textures from disk asynchronously on demand (will avoid some stalls and reduce CPU memory usage). Change requires Editor restart.");

            public static GUIContent shaderCompilation = EditorGUIUtility.TrTextContent("Shader Compilation");
            public static GUIContent asyncShaderCompilation = EditorGUIUtility.TrTextContent("Asynchronous Shader Compilation", "Enables async shader compilation in Game and Scene view. Async compilation for custom editor tools can be achieved via script API and is not affected by this option.");

            public static GUIContent prefabMode = EditorGUIUtility.TrTextContent("Prefab Mode");
            public static GUIContent prefabModeAllowAutoSave = EditorGUIUtility.TrTextContent("Allow Auto Save", "When enabled, an Auto Save toggle is displayed in Prefab Mode which you can turn on or off. This is the default. When disabled, there is no Auto Save in Prefab Mode in this project and the toggle is not displayed.");
            public static GUIContent prefabModeEditingEnvironments = EditorGUIUtility.TrTextContent("Editing Environments");
            public static GUIContent prefabModeRegularEnvironment = EditorGUIUtility.TrTextContent("Regular Environment");
            public static GUIContent prefabModeUIEnvironment = EditorGUIUtility.TrTextContent("UI Environment");

            public static readonly GUIContent enterPlayModeSettings = EditorGUIUtility.TrTextContent("Enter Play Mode Settings");
            public static readonly GUIContent enterPlayModeOptionsEnabled = EditorGUIUtility.TrTextContent("Enter Play Mode Options", "Enables options when Entering Play Mode");
            public static readonly GUIContent enterPlayModeOptionsEnableDomainReload = EditorGUIUtility.TrTextContent("Reload Domain", "Enables Domain Reload when Entering Play Mode. Domain reload reinitializes game completely making loading behavior very close to the Player");
            public static readonly GUIContent enterPlayModeOptionsEnableSceneReload = EditorGUIUtility.TrTextContent("Reload Scene", "Enables Scene Reload when Entering Play Mode. Scene reload makes loading behavior and performance characteristics very close to the Player");
            public static readonly GUIContent enterPlayModeOptionsEnableSceneBackup = EditorGUIUtility.TrTextContent("Scene Backup", "Force writing a backup of all the open scenes to disk even if scenes are not dirty when entering Play mode. Only scenes that are modified in-memory actually need to be backed up, but making modifications from script may change the scene without setting the scene's dirty flag.");

            public static readonly GUIContent numberingScheme = EditorGUIUtility.TrTextContent("Numbering Scheme");

            public static readonly GUIContent inspectorSettings = EditorGUIUtility.TrTextContent("Inspector");
            public static readonly GUIContent inspectorUseIMGUIDefaultInspector = EditorGUIUtility.TrTextContent("Use IMGUI Default Inspector", "Revert to using IMGUI to generate Default Inspectors where no custom Inspector/Editor was defined.");

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

            public PopupElement(string id, string content)
            {
                this.id = id;
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
            new PopupElement("Sprite Atlas V2 - Enabled"),
            new PopupElement("Sprite Atlas V2 - Enabled for Builds"),
        };
        private static readonly int spritePackDeprecatedEnums = 2;

        private PopupElement[] lineEndingsPopupList =
        {
            new PopupElement("OS Native"),
            new PopupElement("Unix"),
            new PopupElement("Windows"),
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

        private PopupElement[] cacheServerModePopupList =
        {
            new PopupElement("Use global settings (stored in preferences)"),
            new PopupElement("Enabled"),
            new PopupElement("Disabled"),
        };

        private PopupElement[] refreshImportModePopupList =
        {
            new PopupElement("In process"),
            new PopupElement("Out of process by queue"),
        };

        private PopupElement[] cacheServerAuthMode =
        {
            new PopupElement("Basic")
        };

        private GUIContent[] cacheServerValidationPopupList =
        {
            EditorGUIUtility.TrTextContent("Disabled", "Content hashes are not calculated for uploaded artifacts and are not validated for downloaded artifacts."),
            EditorGUIUtility.TrTextContent("Upload Only", "Content hashes are calculated for uploaded artifacts and sent to the Accelerator. Content hashes are not validated for downloaded artifacts." ),
            EditorGUIUtility.TrTextContent("Enabled", "Content hashes are calculated for uploaded artifacts and sent to the Accelerator. Content hashes, if provided by the Accelerator, are validated for downloaded artifacts."),
            EditorGUIUtility.TrTextContent("Required", "Content hashes are calculated for uploaded artifacts and sent to the Accelerator. Content hashes are required and validated for downloaded artifacts."),
        };

        private GUIContent[] bc7TextureCompressorOptions =
        {
            EditorGUIUtility.TrTextContent("Default", "Use default BC7 compressor (currently bc7e)"),
            EditorGUIUtility.TrTextContent("ISPC (legacy)", "Use Intel ISPCTextureCompressor (legacy pre-2021.2 behavior)"),
            EditorGUIUtility.TrTextContent("bc7e", "Use Binomial bc7e compressor"),
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
        SerializedProperty m_EnableEditorAsyncCPUTextureLoading;

        SerializedProperty m_GameObjectNamingDigits;
        SerializedProperty m_GameObjectNamingScheme;
        SerializedProperty m_AssetNamingUsesSpace;

        SerializedProperty m_AsyncShaderCompilation;
        SerializedProperty m_DefaultBehaviorMode;
        SerializedProperty m_SerializationMode;
        SerializedProperty m_SerializeInlineMappingsOnOneLine;
        SerializedProperty m_PrefabRegularEnvironment;
        SerializedProperty m_PrefabUIEnvironment;
        SerializedProperty m_PrefabModeAllowAutoSave;
        SerializedProperty m_UseLegacyProbeSampleCount;
        SerializedProperty m_DisableCookiesInLightmapper;
        SerializedProperty m_SpritePackerMode;
        SerializedProperty m_SpritePackerCacheSize;
        SerializedProperty m_Bc7TextureCompressor;
        SerializedProperty m_EtcTextureCompressorBehavior;
        SerializedProperty m_EtcTextureFastCompressor;
        SerializedProperty m_EtcTextureNormalCompressor;
        SerializedProperty m_EtcTextureBestCompressor;
        SerializedProperty m_LineEndingsForNewScripts;
        SerializedProperty m_EnterPlayModeOptionsEnabled;
        SerializedProperty m_EnterPlayModeOptions;
        SerializedProperty m_ProjectGenerationIncludedExtensions;
        SerializedProperty m_ProjectGenerationRootNamespace;
        SerializedProperty m_CacheServerValidationMode;
        SerializedProperty m_InspectorUseIMGUIDefaultInspector;

        bool m_IsGlobalSettings;

        const string kRefreshImportModeKeyArgs = "-refreshImportMode";
        const string kStandbyWorkerCountKeyArgs = "-standbyWorkerCount";
        const string kIdleWorkerShutdownDelayKeyArgs = "-idleWorkerShutdownDelay";
        const string kDesiredImportWorkerCountKeyArgs = "-desiredWorkerCount";

        private const string kCacheServerDownloadBatchSizeCmdArg = "-cacheServerDownloadBatchSize";

        enum CacheServerConnectionState { Unknown, Success, Failure }
        private CacheServerConnectionState m_CacheServerConnectionState;

        public void OnEnable()
        {
            DevDeviceList.Changed += OnDeviceListChanged;
            BuildRemoteDeviceList();

            m_EnableTextureStreamingInPlayMode = serializedObject.FindProperty("m_EnableTextureStreamingInPlayMode");
            m_EnableTextureStreamingInEditMode = serializedObject.FindProperty("m_EnableTextureStreamingInEditMode");
            m_EnableEditorAsyncCPUTextureLoading = serializedObject.FindProperty("m_EnableEditorAsyncCPUTextureLoading");

            m_GameObjectNamingDigits = serializedObject.FindProperty("m_GameObjectNamingDigits");
            m_GameObjectNamingScheme = serializedObject.FindProperty("m_GameObjectNamingScheme");
            m_AssetNamingUsesSpace = serializedObject.FindProperty("m_AssetNamingUsesSpace");

            m_AsyncShaderCompilation = serializedObject.FindProperty("m_AsyncShaderCompilation");

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

            m_PrefabModeAllowAutoSave = serializedObject.FindProperty("m_PrefabModeAllowAutoSave");
            Assert.IsNotNull(m_PrefabModeAllowAutoSave);

            m_UseLegacyProbeSampleCount = serializedObject.FindProperty("m_UseLegacyProbeSampleCount");
            Assert.IsNotNull(m_UseLegacyProbeSampleCount);

            m_DisableCookiesInLightmapper = serializedObject.FindProperty("m_DisableCookiesInLightmapper");
            Assert.IsNotNull(m_DisableCookiesInLightmapper);

            m_SpritePackerMode = serializedObject.FindProperty("m_SpritePackerMode");
            Assert.IsNotNull(m_SpritePackerMode);

            m_SpritePackerCacheSize = serializedObject.FindProperty("m_SpritePackerCacheSize");
            Assert.IsNotNull(m_SpritePackerCacheSize);

            m_Bc7TextureCompressor = serializedObject.FindProperty("m_Bc7TextureCompressor");
            Assert.IsNotNull(m_Bc7TextureCompressor);

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

            m_CacheServerValidationMode = serializedObject.FindProperty("m_CacheServerValidationMode");
            Assert.IsNotNull(m_ProjectGenerationRootNamespace);

            m_CacheServerConnectionState = CacheServerConnectionState.Unknown;

            m_InspectorUseIMGUIDefaultInspector = serializedObject.FindProperty("m_InspectorUseIMGUIDefaultInspector");
            Assert.IsNotNull(m_InspectorUseIMGUIDefaultInspector);

            m_IsGlobalSettings = EditorSettings.GetEditorSettings() == target;
        }

        public void OnDisable()
        {
            DevDeviceList.Changed -= OnDeviceListChanged;
            AssetDatabase.RefreshSettings();
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

            GUILayout.BeginHorizontal();
            GUI.enabled = true;
            GUILayout.Label(Content.buildPipelineHeader, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;
            if (GUILayout.Button(Content.ucbpLearnMore, EditorStyles.linkLabel))
            {
                var help = Help.FindHelpNamed("AssetBundles-Building");
                Application.OpenURL(help);
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            bool parallelAssetBundleBuilding = EditorBuildSettings.UseParallelAssetBundleBuilding;
            parallelAssetBundleBuilding = EditorGUILayout.Toggle(Content.ucbpEnableAssetBundles, parallelAssetBundleBuilding);
            if (EditorGUI.EndChangeCheck())
                EditorBuildSettings.UseParallelAssetBundleBuilding = parallelAssetBundleBuilding;
            if(parallelAssetBundleBuilding)
                EditorGUILayout.HelpBox("Please review official documentation before building any content with these experimental improvements enabled. These improvements apply only to AssetBundles built with BuildPipeline.BuildAssetBundles() and do not apply to AssetBundles built with Scriptable Build Pipeline or Addressables.", MessageType.Info);

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.defaultBehaviorMode, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp(m_DefaultBehaviorMode.intValue, 0, behaviorPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, behaviorPopupList, index, SetDefaultBehaviorMode);

            DoAssetPipelineSettings();

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
            GUILayout.Label(Content.prefabMode, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_PrefabModeAllowAutoSave, Content.prefabModeAllowAutoSave);
                if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
                {
                    EditorSettings.prefabModeAllowAutoSave = m_PrefabModeAllowAutoSave.boolValue;
                }
            }

            GUILayout.Label(Content.prefabModeEditingEnvironments, EditorStyles.label);

            EditorGUI.indentLevel++;
            {
                EditorGUI.BeginChangeCheck();
                var scene = m_PrefabRegularEnvironment.objectReferenceValue as SceneAsset;
                scene = (SceneAsset)EditorGUILayout.ObjectField(Content.prefabModeRegularEnvironment, scene, typeof(SceneAsset), false);
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
                scene = (SceneAsset)EditorGUILayout.ObjectField(Content.prefabModeUIEnvironment, scene, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PrefabUIEnvironment.objectReferenceValue = scene;
                    if (m_IsGlobalSettings)
                    {
                        EditorSettings.prefabUIEnvironment = scene;
                    }
                }
            }
            EditorGUI.indentLevel--;

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
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2 && EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2Build && EditorSettings.spritePackerMode != SpritePackerMode.Disabled)
                EditorGUILayout.IntSlider(m_SpritePackerCacheSize, 1, 200, Content.spriteMaxCacheSize);

            DoProjectGenerationSettings();
            var compressorsChanged = DoTextureCompressorSettings();
            DoLineEndingsSettings();
            DoStreamingSettings();
            DoShaderCompilationSettings();
            DoEnterPlayModeSettings();
            DoNumberingSchemeSettings();
            DoEnterInspectorSettings();

            serializedObject.ApplyModifiedProperties();
            if (compressorsChanged)
                AssetDatabase.Refresh(); // note: needs to be done after ApplyModifiedProperties call
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

        private bool DoTextureCompressorSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label(Content.textureCompressors, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // BC7
            EditorGUILayout.Popup(m_Bc7TextureCompressor, bc7TextureCompressorOptions, Content.bc7Compressor);

            // ETC
            int index = Mathf.Clamp(m_IsGlobalSettings ? EditorSettings.etcTextureCompressorBehavior : m_EtcTextureCompressorBehavior.intValue, 0, etcTextureCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.etcCompressor.text, etcTextureCompressorPopupList, index, SetEtcTextureCompressorBehavior);

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(index < 2);

            index = Mathf.Clamp(m_IsGlobalSettings ? EditorSettings.etcTextureFastCompressor : m_EtcTextureFastCompressor.intValue, 0, etcTextureFastCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.fast.text, etcTextureFastCompressorPopupList, index, SetEtcTextureFastCompressor);

            index = Mathf.Clamp(m_IsGlobalSettings ? EditorSettings.etcTextureNormalCompressor : m_EtcTextureNormalCompressor.intValue, 0, etcTextureNormalCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.normal.text, etcTextureNormalCompressorPopupList, index, SetEtcTextureNormalCompressor);

            index = Mathf.Clamp(m_IsGlobalSettings ? EditorSettings.etcTextureBestCompressor : m_EtcTextureBestCompressor.intValue, 0, etcTextureBestCompressorPopupList.Length - 1);
            CreatePopupMenu(Content.best.text, etcTextureBestCompressorPopupList, index, SetEtcTextureBestCompressor);

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            return EditorGUI.EndChangeCheck();
        }

        private void DoAssetPipelineSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label(Content.assetPipeline, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool enableArtifactGarbageCollection = EditorUserSettings.artifactGarbageCollection;
            enableArtifactGarbageCollection = EditorGUILayout.Toggle(Content.artifactGarbageCollection, enableArtifactGarbageCollection);
            if (EditorGUI.EndChangeCheck())
                EditorUserSettings.artifactGarbageCollection = enableArtifactGarbageCollection;

            var overrideMode = GetCommandLineOverride(kRefreshImportModeKeyArgs);
            if (overrideMode != null)
            {
                EditorGUILayout.HelpBox($"Refresh Import mode forced to {overrideMode} via command line argument. To use the mode specified here please restart Unity without the -refreshImportMode command line argument.", MessageType.Info, true);
            }

            using (new EditorGUI.DisabledScope(overrideMode != null))
            {
                GUILayout.BeginHorizontal();
                    var refreshMode = EditorSettings.refreshImportMode;
                    var parallelImportEnabledOld = refreshMode == AssetDatabase.RefreshImportMode.OutOfProcessPerQueue;
                    var parallelImportEnabledNew = EditorGUILayout.Toggle(Content.parallelImport, parallelImportEnabledOld);

                    if (parallelImportEnabledOld != parallelImportEnabledNew)
                        EditorSettings.refreshImportMode = parallelImportEnabledNew ? AssetDatabase.RefreshImportMode.OutOfProcessPerQueue : AssetDatabase.RefreshImportMode.InProcess;
                    if (GUILayout.Button(Content.parallelImportLearnMore, EditorStyles.linkLabel))
                    {
                        // Known issue with Docs redirect - versioned pages might not open offline docs
                        var help = Help.FindHelpNamed("ParallelImport");
                        Help.BrowseURL(help);
                    }
                GUILayout.EndHorizontal();
            }

            var overrideDesiredCount = GetCommandLineOverride(kDesiredImportWorkerCountKeyArgs);
            if (overrideDesiredCount != null)
            {
                EditorGUILayout.HelpBox($"Desired import worker count forced to {overrideDesiredCount} via command line argument. To use the worker count specified here please restart Unity without the -desiredWorkerCount command line argument.", MessageType.Info, true);
            }

            // This min/max worker count is enforced here and in EditorUserSettings.cpp
            // Please keep them in sync.
            const int minWorkerCount = 1;
            const int maxWorkerCount = 128;

            using (new EditorGUI.DisabledScope(overrideDesiredCount != null))
            {
                var oldCount = EditorUserSettings.desiredImportWorkerCount;
                int newCount = EditorGUILayout.IntField(Content.desiredImportWorkerCount, oldCount);
                newCount = Mathf.Clamp(newCount, minWorkerCount, maxWorkerCount);

                if (oldCount != newCount)
                    EditorUserSettings.desiredImportWorkerCount = newCount;
            }

            var overrideStandbyCount = GetCommandLineOverride(kStandbyWorkerCountKeyArgs);
            if (overrideStandbyCount != null)
            {
                EditorGUILayout.HelpBox($"Standby import worker count forced to {overrideStandbyCount} via command line argument. To use the standby worker count specified here please restart Unity without the -standbyWorkerCount command line argument.", MessageType.Info, true);
            }

            using (new EditorGUI.DisabledScope(overrideStandbyCount != null))
            {
                var oldCount = EditorUserSettings.standbyImportWorkerCount;
                var newCount = EditorGUILayout.IntField(Content.standbyImportWorkerCount, oldCount);
                int desiredWorkerCount = EditorUserSettings.desiredImportWorkerCount;
                newCount = Mathf.Clamp(newCount, 0, desiredWorkerCount);

                if (oldCount != newCount)
                {
                    EditorUserSettings.standbyImportWorkerCount = newCount;
                }
            }

            var overridekIdleWorkerShutdownDelay = GetCommandLineOverride(kIdleWorkerShutdownDelayKeyArgs);
            if (overridekIdleWorkerShutdownDelay != null)
            {
                EditorGUILayout.HelpBox($"Idle import worker shutdown delay forced to {overridekIdleWorkerShutdownDelay} ms. via command line argument. To use the settings specified here please restart Unity without the -idleWorkerShutdownDelay command line argument.", MessageType.Info, true);
            }

            using (new EditorGUI.DisabledScope(overridekIdleWorkerShutdownDelay != null))
            {
                var oldSeconds = EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds / 1000.0f;
                var newSeconds = EditorGUILayout.FloatField(Content.idleWorkerShutdownDelay, oldSeconds);
                newSeconds = Mathf.Max(0, newSeconds);

                if (oldSeconds != newSeconds)
                {
                    EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = (int)(newSeconds * 1000.0f);
                }
            }
        }

        private void DoCacheServerSettings()
        {
            Assert.IsTrue(m_IsGlobalSettings);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Content.cacheServer, EditorStyles.boldLabel);

            if (GUILayout.Button(Content.cacheServerLearnMore, EditorStyles.linkLabel))
            {
                // Known issue with Docs redirect - versioned pages might not open offline docs
                var help = Help.FindHelpNamed("UnityAccelerator");
                Help.BrowseURL(help);
            }
            GUILayout.EndHorizontal();

            var overrideAddress = AssetPipelinePreferences.GetCommandLineRemoteAddressOverride();
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
                    if (AssetPipelinePreferences.IsCacheServerEnabled)
                    {
                        var cacheServerIP = AssetPipelinePreferences.CacheServerAddress;
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
                        var address = EditorSettings.cacheServerEndpoint.Split(':');
                        var ip = address[0];
                        UInt16 port = 0; // If 0, will use the default set port
                        if (address.Length == 2)
                            port = Convert.ToUInt16(address[1]);

                        if (AssetDatabase.CanConnectToCacheServer(ip, port))
                            m_CacheServerConnectionState = CacheServerConnectionState.Success;
                        else
                            m_CacheServerConnectionState = CacheServerConnectionState.Failure;
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

                    var oldPrefix = EditorSettings.cacheServerNamespacePrefix;
                    var newPrefix = EditorGUILayout.TextField(Content.cacheServerNamespacePrefixLabel, oldPrefix);
                    if (newPrefix != oldPrefix)
                    {
                        EditorSettings.cacheServerNamespacePrefix = newPrefix;
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

                    EditorGUI.BeginChangeCheck();
                    enableAuth = EditorGUILayout.Toggle(Content.cacheServerEnableAuthLabel, enableAuth);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorSettings.cacheServerEnableAuth = enableAuth;
                        if (enableAuth)
                        {
                            EditorSettings.cacheServerEnableTls = true;
                        }
                    }

                    int validationIndex = Mathf.Clamp((int)EditorSettings.cacheServerValidationMode, 0, cacheServerValidationPopupList.Length - 1);
                    EditorGUILayout.Popup(m_CacheServerValidationMode, cacheServerValidationPopupList, Content.cacheServerValidationLabel);

                    var cacheServerDownloadBatchSizeOverride = GetCommandLineOverride(kCacheServerDownloadBatchSizeCmdArg);
                    if (cacheServerDownloadBatchSizeOverride != null)
                        EditorGUILayout.HelpBox($"Forced via command line argument. To use the setting, please restart Unity without the {kCacheServerDownloadBatchSizeCmdArg} command line argument.", MessageType.Info, true);

                    using (new EditorGUI.DisabledScope(cacheServerDownloadBatchSizeOverride != null))
                    {
                        var oldDownloadBatchSize = cacheServerDownloadBatchSizeOverride != null ? Int32.Parse(cacheServerDownloadBatchSizeOverride) : EditorSettings.cacheServerDownloadBatchSize;
                        var newDownloadBatchSize = EditorGUILayout.IntField(Content.cacheServerDownloadBatchSizeLabel, oldDownloadBatchSize);
                        newDownloadBatchSize = Mathf.Max(0, newDownloadBatchSize);
                        if (newDownloadBatchSize != oldDownloadBatchSize)
                            EditorSettings.cacheServerDownloadBatchSize = newDownloadBatchSize;
                    }
                }
            }
        }

        private static string GetCommandLineOverride(string key)
        {
            string address = null;
            var argv = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(argv, key);
            if (index >= 0 && argv.Length > index + 1)
                address = argv[index + 1];

            return address;
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
            EditorGUILayout.PropertyField(m_EnableEditorAsyncCPUTextureLoading, Content.enableEditorAsyncCPUTextureLoading);
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
                options = ToggleEnterPlayModeOptions(options, EnterPlayModeOptions.DisableSceneBackupUnlessDirty, Content.enterPlayModeOptionsEnableSceneBackup);

                if (m_EnterPlayModeOptions.intValue != (int)options)
                {
                    m_EnterPlayModeOptions.intValue = (int)options;
                    if (m_IsGlobalSettings)
                        EditorSettings.enterPlayModeOptions = options;
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DoEnterInspectorSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label(Content.inspectorSettings, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_InspectorUseIMGUIDefaultInspector, Content.inspectorUseIMGUIDefaultInspector);
            if (EditorGUI.EndChangeCheck() && m_IsGlobalSettings)
            {
                EditorSettings.inspectorUseIMGUIDefaultInspector = m_InspectorUseIMGUIDefaultInspector.boolValue;

                // Needs to be delayCall because it forces redrawing of UI which messes with the current IMGUI context of the Settings window.
                EditorApplication.delayCall += ClearEditorsAndRebuildInspectors;
            }
        }

        static void ClearEditorsAndRebuildInspectors()
        {
            // Cannot use something like EditorUtility.ForceRebuildInspectors() because this only refreshes
            // the inspector's values and IMGUI state, but otherwise, if the target did not change we
            // re-use the Editors. We need a special clear function to properly recreate the UI using
            // the new setting.
            var propertyEditors = Resources.FindObjectsOfTypeAll<PropertyEditor>();
            foreach (var propertyEditor in propertyEditors)
                propertyEditor.ClearEditorsAndRebuild();
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
            CreatePopupMenu(serializedObject, new GUIContent(title), elements[selectedIndex].content, elements, selectedIndex, func);
        }

        internal static void CreatePopupMenu(SerializedObject obj, GUIContent titleContent, GUIContent content, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, titleContent);
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

            if (m_SerializationMode.intValue == popupIndex) return;

            if (!EditorUtility.DisplayDialog("Change Asset Serialization Mode?",
                    "Changing the serialization method for assets may force a reimport of some or all assets immediately in the project.\n\nAre you sure you wish to change the asset serialization mode?",
                    "Yes", "No")) return;

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
                if (popupIndex >= (int)SpritePackerMode.SpriteAtlasV2)
                {
                    UnityEditor.U2D.SpriteAtlasImporter.MigrateAllSpriteAtlases();
                }
            }
        }

        private void SetRefreshImportMode(object data)
        {
            EditorSettings.refreshImportMode = (AssetDatabase.RefreshImportMode)data;
        }

        private void SetCacheServerMode(object data)
        {
            EditorSettings.cacheServerMode = (CacheServerMode)data;
        }

        private void SetCacheServerAuthMode(object data)
        {
            EditorUserSettings.SetConfigValue("cacheServerAuthMode", $"{(int)data}");
        }

        private void SetCacheServerValidationMode(object data)
        {
            EditorSettings.cacheServerValidationMode = (CacheServerValidationMode)data;
        }

        private void SetEtcTextureCompressorBehavior(object data)
        {
            int newValue = (int)data;
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
            else
            {
                m_EtcTextureCompressorBehavior.intValue = newValue;
            }
        }

        private void SetEtcTextureFastCompressor(object data)
        {
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureFastCompressor = (int)data;
            else
                m_EtcTextureFastCompressor.intValue = (int)data;
        }

        private void SetEtcTextureNormalCompressor(object data)
        {
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureNormalCompressor = (int)data;
            else
                m_EtcTextureNormalCompressor.intValue = (int)data;
        }

        private void SetEtcTextureBestCompressor(object data)
        {
            if (m_IsGlobalSettings)
                EditorSettings.etcTextureBestCompressor = (int)data;
            else
                m_EtcTextureBestCompressor.intValue = (int)data;
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
