// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Hardware;
using UnityEditor.VersionControl;
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

            public static GUIContent versionControl = EditorGUIUtility.TrTextContent("Version Control");
            public static GUIContent mode = EditorGUIUtility.TrTextContent("Mode");
            public static GUIContent logLevel = EditorGUIUtility.TrTextContent("Log Level");
            public static GUIContent automaticAdd = EditorGUIUtility.TrTextContent("Automatic Add", "Automatically add newly created assets to version control.");
            public static GUIContent smartMerge = EditorGUIUtility.TrTextContent("Smart merge");

            public static GUIContent vcsConnect = EditorGUIUtility.TrTextContent("Connect");
            public static GUIContent vcsReconnect = EditorGUIUtility.TrTextContent("Reconnect");
            public static GUIContent workOffline = EditorGUIUtility.TrTextContent("Work Offline", "Enable asset modifications even when not connected to a version control server. Requires manual integration into VCS system afterwards.");
            public static GUIContent allowAsyncUpdate = EditorGUIUtility.TrTextContent("Allow Async Update", "Enable asynchronous file status queries (use with slow server connections).");
            public static GUIContent showFailedCheckouts = EditorGUIUtility.TrTextContent("Show Failed Checkouts", "Show dialogs for failed 'Check Out' operations.");
            public static GUIContent overwriteFailedCheckoutAssets = EditorGUIUtility.TrTextContent("Overwrite Failed Checkout Assets", "When on, assets that can not be checked out will get saved anyway.");
            public static GUIContent overlayIcons = EditorGUIUtility.TrTextContent("Overlay Icons", "Should version control status icons be shown in project view.");

            public static GUIContent assetPipeline = EditorGUIUtility.TrTextContent("Asset Pipeline");
            public static GUIContent cacheServer = EditorGUIUtility.TrTextContent("Cache Server (project specific)");
            public static GUIContent cacheServerIPLabel = EditorGUIUtility.TrTextContent("IP address");
            public static GUIContent cacheServerNamespacePrefixLabel = EditorGUIUtility.TrTextContent("Namespace prefix");
            public static GUIContent cacheServerEnableDownloadLabel = EditorGUIUtility.TrTextContent("Download");
            public static GUIContent cacheServerEnableUploadLabel = EditorGUIUtility.TrTextContent("Upload");
            public static GUIContent assetSerialization = EditorGUIUtility.TrTextContent("Asset Serialization");
            public static GUIContent defaultBehaviorMode = EditorGUIUtility.TrTextContent("Default Behaviour Mode");

            public static GUIContent graphics = EditorGUIUtility.TrTextContent("Graphics");
            public static GUIContent showLightmapResolutionOverlay = EditorGUIUtility.TrTextContent("Show Lightmap Resolution Overlay");
            public static GUIContent useLegacyProbeSampleCount = EditorGUIUtility.TrTextContent("Use legacy Light Probe sample counts", "Uses fixed Light Probe sample counts for baking with the Progressive Lightmapper. The sample counts are: 64 direct samples, 2048 indirect samples and 2048 environment samples.");

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

            private const string activeAssetPipelineVersionTooltip = "The active asset import pipeline is chosen at startup by inspecting the following sources in order: Environment variable, command line argument (-adb1 or -adb2), local per project editor settings (the dropdown above)";
            public static readonly GUIContent activeAssetPipelineVersionLabel = EditorGUIUtility.TrTextContent("Active version", activeAssetPipelineVersionTooltip);
            public static readonly GUIContent activeAssetPipelineVersion = new GUIContent(AssetDatabase.IsV1Enabled() ? "1" : "2", activeAssetPipelineVersionTooltip);

            public static GUIContent shaderCompilation = EditorGUIUtility.TrTextContent("Shader Compilation");
            public static GUIContent asyncShaderCompilation = EditorGUIUtility.TrTextContent("Asynchronous Shader Compilation", "Enables async shader compilation in Game and Scene view. Async compilation for custom editor tools can be achieved via script API and is not affected by this option.");

            public static readonly GUIContent enterPlayModeSettings = EditorGUIUtility.TrTextContent("Enter Play Mode Settings");
            public static readonly GUIContent enterPlayModeOptionsEnabled = EditorGUIUtility.TrTextContent("Enter Play Mode Options (Experimental)", "Enables options when Entering Play Mode");
            public static readonly GUIContent enterPlayModeOptionsEnableDomainReload = EditorGUIUtility.TrTextContent("Reload Domain", "Enables Domain Reload when Entering Play Mode. Domain reload reinitializes game completely making loading behavior very close to the Player");
            public static readonly GUIContent enterPlayModeOptionsEnableSceneReload = EditorGUIUtility.TrTextContent("Reload Scene", "Enables Scene Reload when Entering Play Mode. Scene reload makes loading behavior and performance characteristics very close to the Player");
        }

        struct PopupElement
        {
            public readonly string id;
            public readonly bool requiresTeamLicense;
            public readonly GUIContent content;

            public bool Enabled
            {
                get { return (!requiresTeamLicense || InternalEditorUtility.HasTeamLicense()); }
            }

            public PopupElement(string content)
                : this(content, false)
            {
            }

            public PopupElement(string content, bool requiresTeamLicense)
            {
                this.id = content;
                this.content = new GUIContent(content);
                this.requiresTeamLicense = requiresTeamLicense;
            }
        }

        private PopupElement[] vcDefaultPopupList =
        {
            new PopupElement(ExternalVersionControl.Disabled),
            new PopupElement(ExternalVersionControl.Generic),
        };

        private PopupElement[] vcPopupList = null;

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
            new PopupElement("Enabled For Builds(Legacy Sprite Packer)"),
            new PopupElement("Always Enabled(Legacy Sprite Packer)"),
            new PopupElement("Enabled For Builds"),
            new PopupElement("Always Enabled"),
        };

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

        private string[] logLevelPopupList =
        {
            "Verbose", "Info", "Notice", "Fatal"
        };

        private string[] semanticMergePopupList =
        {
            "Off", "Premerge", "Ask"
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

        SerializedProperty m_AsyncShaderCompilation;

        enum CacheServerConnectionState { Unknown, Success, Failure }
        private CacheServerConnectionState m_CacheServerConnectionState;
        private static string s_ForcedAssetPipelineWarning;

        const int kVCFieldRecentCount = 10;
        const string kVCFieldRecentPrefix = "vcs_ConfigField";
        Dictionary<string, string[]> m_VCConfigFieldsRecentValues = new Dictionary<string, string[]>();
        bool m_NeedToSaveValuesOnConnect;

        public void OnEnable()
        {
            Plugin[] availvc = Plugin.availablePlugins;

            List<PopupElement> popupArray = new List<PopupElement>(vcDefaultPopupList);
            foreach (var plugin in availvc)
            {
                popupArray.Add(new PopupElement(plugin.name, true));
            }


            vcPopupList = popupArray.ToArray();

            DevDeviceList.Changed += OnDeviceListChanged;
            BuildRemoteDeviceList();

            m_EnableTextureStreamingInPlayMode = serializedObject.FindProperty("m_EnableTextureStreamingInPlayMode");
            m_EnableTextureStreamingInEditMode = serializedObject.FindProperty("m_EnableTextureStreamingInEditMode");

            m_AsyncShaderCompilation = serializedObject.FindProperty("m_AsyncShaderCompilation");

            m_CacheServerConnectionState = CacheServerConnectionState.Unknown;
            s_ForcedAssetPipelineWarning = null;
        }

        public void OnDisable()
        {
            DevDeviceList.Changed -= OnDeviceListChanged;
            if (EditorSettings.assetPipelineMode == AssetPipelineMode.Version2)
            {
                AssetDatabaseExperimental.RefreshCacheServerNamespacePrefix();
                AssetDatabaseExperimental.RefreshConnectionToCacheServer();
            }
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

        string[] GetVCConfigFieldRecentValues(string fieldName)
        {
            if (m_VCConfigFieldsRecentValues.ContainsKey(fieldName))
                return m_VCConfigFieldsRecentValues[fieldName];

            var res = new List<string>();
            for (var i = 0; i < kVCFieldRecentCount; ++i)
            {
                var prefName = $"{kVCFieldRecentPrefix}{fieldName}{i}";
                var prefValue = EditorPrefs.GetString(prefName);
                if (!string.IsNullOrEmpty(prefValue))
                    res.Add(prefValue);
            }

            var arr = res.ToArray();
            m_VCConfigFieldsRecentValues[fieldName] = arr;
            return arr;
        }

        void UpdateVCConfigFieldRecentValue(string fieldName, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            var arr = GetVCConfigFieldRecentValues(fieldName);
            var newVal = new[] {value};
            // put newly used value in front
            arr = newVal.Concat(arr.Except(newVal)).Take(kVCFieldRecentCount).ToArray();
            m_VCConfigFieldsRecentValues[fieldName] = arr;

            for (var i = 0; i < arr.Length; ++i)
            {
                var prefName = $"{kVCFieldRecentPrefix}{fieldName}{i}";
                EditorPrefs.SetString(prefName, arr[i]);
            }
        }

        void UpdateVCConfigFieldRecentValues(ConfigField[] fields)
        {
            if (fields == null)
                return;
            foreach (var field in fields)
            {
                if (field.isPassword)
                    continue;
                var val = EditorUserSettings.GetConfigValue(field.name);
                if (string.IsNullOrEmpty(val))
                    continue;
                UpdateVCConfigFieldRecentValue(field.name, val);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // GUI.enabled hack because we don't want some controls to be disabled if the EditorSettings.asset is locked
            // since some of the controls are not dependent on the Editor Settings asset. Unfortunately, this assumes
            // that the editor will only be disabled because of version control locking which may change in the future.
            var editorEnabled = GUI.enabled;

            ShowUnityRemoteGUI(editorEnabled);

            GUILayout.Space(10);
            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            using (new EditorGUI.DisabledScope(!collabEnabled))
            {
                GUI.enabled = !collabEnabled;
                GUILayout.Label(Content.versionControl, EditorStyles.boldLabel);

                ExternalVersionControl selvc = EditorSettings.externalVersionControl;
                CreatePopupMenuVersionControl(Content.mode.text, vcPopupList, selvc, SetVersionControlSystem);
                GUI.enabled = editorEnabled && !collabEnabled;
            }
            if (collabEnabled)
            {
                EditorGUILayout.HelpBox("Version Control not available when using Collaboration feature.", MessageType.Warning);
            }

            ConfigField[] configFields = null;

            if (VersionControlSystemHasGUI())
            {
                GUI.enabled = true;
                bool hasRequiredFields = false;

                if (EditorSettings.externalVersionControl == ExternalVersionControl.Generic ||
                    EditorSettings.externalVersionControl == ExternalVersionControl.Disabled)
                {
                    // no specific UI for these VCS types
                }
                else
                {
                    configFields = Provider.GetActiveConfigFields();

                    hasRequiredFields = true;

                    foreach (ConfigField field in configFields)
                    {
                        string newVal;
                        string oldVal = EditorUserSettings.GetConfigValue(field.name);
                        if (field.isPassword)
                        {
                            newVal = EditorGUILayout.PasswordField(GUIContent.Temp(field.label, field.description), oldVal);
                            if (newVal != oldVal)
                                EditorUserSettings.SetPrivateConfigValue(field.name, newVal);
                        }
                        else
                        {
                            var recentValues = GetVCConfigFieldRecentValues(field.name);
                            newVal = EditorGUILayout.TextFieldDropDown(GUIContent.Temp(field.label, field.description), oldVal, recentValues);
                            if (newVal != oldVal)
                                EditorUserSettings.SetConfigValue(field.name, newVal);
                        }

                        if (field.isRequired && string.IsNullOrEmpty(newVal))
                            hasRequiredFields = false;
                    }
                }

                // Log level popup
                string logLevel = EditorUserSettings.GetConfigValue("vcSharedLogLevel");
                int idx = System.Array.FindIndex(logLevelPopupList, (item) => item.ToLower() == logLevel);
                if (idx == -1)
                {
                    logLevel = "notice";
                    idx = System.Array.FindIndex(logLevelPopupList, (item) => item.ToLower() == logLevel);
                    if (idx == -1)
                    {
                        idx = 0;
                    }
                    logLevel = logLevelPopupList[idx];
                    EditorUserSettings.SetConfigValue("vcSharedLogLevel", logLevel);
                }
                int newIdx = EditorGUILayout.Popup(Content.logLevel, idx, logLevelPopupList);
                if (newIdx != idx)
                {
                    EditorUserSettings.SetConfigValue("vcSharedLogLevel", logLevelPopupList[newIdx].ToLower());
                }

                if (Provider.onlineState == OnlineState.Offline)
                {
                    var text = "Not Connected. " + (Provider.offlineReason ?? "");
                    EditorGUILayout.HelpBox(text, MessageType.Error);
                }
                else if (Provider.onlineState == OnlineState.Updating)
                {
                    var text = "Connecting...";
                    EditorGUILayout.HelpBox(text, MessageType.Info);
                }
                else if (EditorUserSettings.WorkOffline)
                {
                    var text = "Working Offline. Manually integrate your changes using a version control client, and uncheck 'Work Offline' setting below to get back to regular state.";
                    EditorGUILayout.HelpBox(text, MessageType.Warning);
                }
                else if (Provider.onlineState == OnlineState.Online)
                {
                    var text = "Connected";
                    EditorGUILayout.HelpBox(text, MessageType.Info);
                }

                GUI.enabled = editorEnabled;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = hasRequiredFields && Provider.onlineState != OnlineState.Updating;
                if (GUILayout.Button(Provider.onlineState != OnlineState.Offline ? Content.vcsReconnect : Content.vcsConnect, EditorStyles.miniButton))
                {
                    m_NeedToSaveValuesOnConnect = true;
                    Provider.UpdateSettings();
                }
                GUILayout.EndHorizontal();

                if (m_NeedToSaveValuesOnConnect && Provider.onlineState == OnlineState.Online)
                {
                    // save connection field settings if we got online with them successfully
                    m_NeedToSaveValuesOnConnect = false;
                    UpdateVCConfigFieldRecentValues(configFields);
                }

                if (Provider.requiresNetwork)
                {
                    bool workOfflineNew = EditorGUILayout.Toggle(Content.workOffline, EditorUserSettings.WorkOffline); // Enabled has a slightly different behaviour
                    if (workOfflineNew != EditorUserSettings.WorkOffline)
                    {
                        // On toggling on show a warning
                        if (workOfflineNew && !EditorUtility.DisplayDialog("Confirm working offline", "Working offline and making changes to your assets means that you will have to manually integrate changes back into version control using your standard version control client before you stop working offline in Unity. Make sure you know what you are doing.", "Work offline", "Cancel"))
                        {
                            workOfflineNew = false; // User cancelled working offline
                        }
                        EditorUserSettings.WorkOffline = workOfflineNew;
                        EditorApplication.RequestRepaintAllViews();
                    }
                }

                EditorUserSettings.AutomaticAdd = EditorGUILayout.Toggle(Content.automaticAdd, EditorUserSettings.AutomaticAdd);

                if (Provider.requiresNetwork)
                    EditorUserSettings.allowAsyncStatusUpdate = EditorGUILayout.Toggle(Content.allowAsyncUpdate, EditorUserSettings.allowAsyncStatusUpdate);

                if (Provider.hasCheckoutSupport)
                {
                    EditorUserSettings.showFailedCheckout = EditorGUILayout.Toggle(Content.showFailedCheckouts, EditorUserSettings.showFailedCheckout);
                    EditorUserSettings.overwriteFailedCheckoutAssets = EditorGUILayout.Toggle(Content.overwriteFailedCheckoutAssets, EditorUserSettings.overwriteFailedCheckoutAssets);
                }

                GUI.enabled = editorEnabled;

                EditorUserSettings.semanticMergeMode = (SemanticMergeMode)EditorGUILayout.Popup(Content.smartMerge, (int)EditorUserSettings.semanticMergeMode, semanticMergePopupList);

                var newOverlayIcons = EditorGUILayout.Toggle(Content.overlayIcons, EditorUserSettings.overlayIcons);
                if (newOverlayIcons != EditorUserSettings.overlayIcons)
                {
                    EditorUserSettings.overlayIcons = newOverlayIcons;
                    EditorApplication.RequestRepaintAllViews();
                }
                if (newOverlayIcons)
                    DrawOverlayDescriptions();
            }

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
            bool showRes = LightmapVisualization.showResolution;
            showRes = EditorGUILayout.Toggle(Content.showLightmapResolutionOverlay, showRes);
            if (EditorGUI.EndChangeCheck())
                LightmapVisualization.showResolution = showRes;

            EditorGUI.BeginChangeCheck();
            bool useLegacyProbeSampleCountValue = EditorSettings.useLegacyProbeSampleCount;
            useLegacyProbeSampleCountValue = EditorGUILayout.Toggle(Content.useLegacyProbeSampleCount, useLegacyProbeSampleCountValue);
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.RequestRepaintAllViews();
                EditorSettings.useLegacyProbeSampleCount = useLegacyProbeSampleCountValue;
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label(Content.spritePacker, EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp((int)EditorSettings.spritePackerMode, 0, spritePackerPopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, spritePackerPopupList, index, SetSpritePackerMode);

            if (EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOn
                || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnly)
            {
                index = Mathf.Clamp((int)(EditorSettings.spritePackerPaddingPower - 1), 0, 2);
                CreatePopupMenu("Padding Power (Legacy Sprite Packer)", spritePackerPaddingPowerPopupList, index, SetSpritePackerPaddingPower);
            }

            DoProjectGenerationSettings();
            DoEtcTextureCompressionSettings();
            DoLineEndingsSettings();
            DoStreamingSettings();
            DoShaderCompilationSettings();
            DoEnterPlayModeSettings();

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
                    s_ForcedAssetPipelineWarning = "Asset pipeline mode was forced via command line argument using the -adb1 or -adb2 command line argument. The above setting is not in effect before restarting without the command line argument.";
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

            int index = Mathf.Clamp((int)EditorSettings.assetPipelineMode, 0, assetPipelineModePopupList.Length - 1);
            CreatePopupMenu(Content.mode.text, assetPipelineModePopupList, index, SetAssetPipelineMode);

            EditorGUILayout.LabelField(Content.activeAssetPipelineVersionLabel, Content.activeAssetPipelineVersion);

            bool isAssetPipelineVersion1 = EditorSettings.assetPipelineMode == AssetPipelineMode.Version1;

            if (!string.IsNullOrEmpty(assetPipelineWarning))
                EditorGUILayout.HelpBox(assetPipelineWarning, MessageType.Info, true);
            else if (isAssetPipelineVersion1 != AssetDatabase.IsV1Enabled())
            {
                var message = "Changes in Asset Pipeline Version will take effect after saving and restarting the project.";

                if (isAssetPipelineVersion1)
                    message += "\nPlease note that Asset Pipeline Version 1 is now deprecated.";

                EditorGUILayout.HelpBox(message, MessageType.Info, true);
            }
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
                            if (EditorSettings.cacheServerEndpoint.Length > 0)
                            {
                                var address = EditorSettings.cacheServerEndpoint.Split(':');
                                var ip = address[0];
                                var port = Convert.ToUInt16(address[1]);
                                if (AssetDatabaseExperimental.CanConnectToCacheServer(ip, port))
                                    m_CacheServerConnectionState = CacheServerConnectionState.Success;
                                else
                                    m_CacheServerConnectionState = CacheServerConnectionState.Failure;
                            }
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

        void DrawOverlayDescriptions()
        {
            Texture2D atlas = Provider.overlayAtlas;
            if (atlas == null)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawOverlayDescription(Asset.States.Local);
            DrawOverlayDescription(Asset.States.OutOfSync);
            DrawOverlayDescription(Asset.States.CheckedOutLocal);
            DrawOverlayDescription(Asset.States.CheckedOutRemote);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            DrawOverlayDescription(Asset.States.DeletedLocal);
            DrawOverlayDescription(Asset.States.DeletedRemote);
            DrawOverlayDescription(Asset.States.AddedLocal);
            DrawOverlayDescription(Asset.States.AddedRemote);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            DrawOverlayDescription(Asset.States.Conflicted);
            DrawOverlayDescription(Asset.States.LockedLocal);
            DrawOverlayDescription(Asset.States.LockedRemote);
            DrawOverlayDescription(Asset.States.Updating);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void DrawOverlayDescription(Asset.States state)
        {
            Rect atlasUV = Provider.GetAtlasRectForState((int)state);
            if (atlasUV.width == 0f)
                return; // no overlay

            Texture2D atlas = Provider.overlayAtlas;
            if (atlas == null)
                return;

            GUILayout.Label("    " + Asset.StateToString(state), EditorStyles.miniLabel);
            Rect r = GUILayoutUtility.GetLastRect();
            r.width = 16f;
            GUI.DrawTextureWithTexCoords(r, atlas, atlasUV);
        }

        private void CreatePopupMenuVersionControl(string title, PopupElement[] elements, string selectedValue, GenericMenu.MenuFunction2 func)
        {
            var selectedIndex = System.Array.FindIndex(elements, (PopupElement typeElem) => (typeElem.id == selectedValue));
            var content = new GUIContent(elements[selectedIndex].content);
            CreatePopupMenu(title, content, elements, selectedIndex, func);
        }

        private void CreatePopupMenu(string title, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            CreatePopupMenu(title, elements[selectedIndex].content, elements, selectedIndex, func);
        }

        private void CreatePopupMenu(string title, GUIContent content, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent(title));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, elements, selectedIndex, func);
        }

        private void DoPopup(Rect popupRect, PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];

                if (element.Enabled)
                    menu.AddItem(element.content, i == selectedIndex, func, i);
                else
                    menu.AddDisabledItem(element.content);
            }
            menu.DropDown(popupRect);
        }

        private bool VersionControlSystemHasGUI()
        {
            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            if (!collabEnabled)
            {
            ExternalVersionControl system = EditorSettings.externalVersionControl;
            return
                system != ExternalVersionControl.Disabled &&
                system != ExternalVersionControl.AutoDetect &&
                system != ExternalVersionControl.Generic;
        }

        return false;
        }

        private void SetVersionControlSystem(object data)
        {
            int popupIndex = (int)data;
            if (popupIndex < 0 || popupIndex >= vcPopupList.Length)
                return;

            PopupElement el = vcPopupList[popupIndex];
            string oldVC = EditorSettings.externalVersionControl;

            EditorSettings.externalVersionControl = el.id;
            Provider.UpdateSettings();
            AssetDatabase.Refresh();

            if (oldVC != el.id)
            {
                if (el.content.text == ExternalVersionControl.Disabled ||
                    el.content.text == ExternalVersionControl.Generic
                )
                {
                    // Close the normal version control window
                    WindowPending.CloseAllWindows();
                }
            }
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

            EditorSettings.spritePackerMode = (SpritePackerMode)popupIndex;
        }

        private void SetSpritePackerPaddingPower(object data)
        {
            int popupIndex = (int)data;

            EditorSettings.spritePackerPaddingPower = popupIndex + 1;
        }

        private void SetAssetPipelineMode(object data)
        {
            EditorSettings.assetPipelineMode = (AssetPipelineMode)data;
        }

        private void SetCacheServerMode(object data)
        {
            EditorSettings.cacheServerMode = (CacheServerMode)data;
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
