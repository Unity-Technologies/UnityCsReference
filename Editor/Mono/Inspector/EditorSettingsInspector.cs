// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Hardware;
using UnityEditor.VersionControl;
using UnityEditor.Collaboration;
using UnityEditor.Web;

namespace UnityEditor
{
    [CustomEditor(typeof(EditorSettings))]
    internal class EditorSettingsInspector : ProjectSettingsBaseEditor
    {
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
        }

        public void OnDisable()
        {
            DevDeviceList.Changed -= OnDeviceListChanged;
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
                GUILayout.Label("Version Control", EditorStyles.boldLabel);

                ExternalVersionControl selvc = EditorSettings.externalVersionControl;
                CreatePopupMenuVersionControl("Mode", vcPopupList, selvc, SetVersionControlSystem);
                GUI.enabled = editorEnabled && !collabEnabled;
            }
            if (collabEnabled)
            {
                EditorGUILayout.HelpBox("Version Control not available when using Collaboration feature.", MessageType.Warning);
            }

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
                    ConfigField[] configFields = Provider.GetActiveConfigFields();

                    hasRequiredFields = true;

                    foreach (ConfigField field in configFields)
                    {
                        string newVal;
                        string oldVal = EditorUserSettings.GetConfigValue(field.name);
                        if (field.isPassword)
                        {
                            newVal = EditorGUILayout.PasswordField(field.label, oldVal);
                            if (newVal != oldVal)
                                EditorUserSettings.SetPrivateConfigValue(field.name, newVal);
                        }
                        else
                        {
                            newVal = EditorGUILayout.TextField(field.label, oldVal);
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
                int newIdx = EditorGUILayout.Popup("Log Level", idx, logLevelPopupList);
                if (newIdx != idx)
                {
                    EditorUserSettings.SetConfigValue("vcSharedLogLevel", logLevelPopupList[newIdx].ToLower());
                }

                GUI.enabled = editorEnabled;

                string osState = "Connected";
                if (Provider.onlineState == OnlineState.Updating)
                    osState = "Connecting...";
                else if (Provider.onlineState == OnlineState.Offline)
                    osState = "Disconnected";

                EditorGUILayout.LabelField("Status", osState);

                if (Provider.onlineState != OnlineState.Online && !string.IsNullOrEmpty(Provider.offlineReason))
                {
                    GUI.enabled = false;
                    GUILayout.TextArea(Provider.offlineReason);
                    GUI.enabled = editorEnabled;
                }

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = hasRequiredFields && Provider.onlineState != OnlineState.Updating;
                if (GUILayout.Button("Connect", EditorStyles.miniButton))
                    Provider.UpdateSettings();
                GUILayout.EndHorizontal();

                EditorUserSettings.AutomaticAdd = EditorGUILayout.Toggle("Automatic add", EditorUserSettings.AutomaticAdd);

                if (Provider.requiresNetwork)
                {
                    bool workOfflineNew = EditorGUILayout.Toggle("Work Offline", EditorUserSettings.WorkOffline); // Enabled has a slightly different behaviour
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

                    EditorUserSettings.allowAsyncStatusUpdate = EditorGUILayout.Toggle("Allow Async Update", EditorUserSettings.allowAsyncStatusUpdate);
                }

                if (Provider.hasCheckoutSupport)
                    EditorUserSettings.showFailedCheckout = EditorGUILayout.Toggle("Show failed checkouts", EditorUserSettings.showFailedCheckout);

                GUI.enabled = editorEnabled;

                // Semantic merge popup
                EditorUserSettings.semanticMergeMode = (SemanticMergeMode)EditorGUILayout.Popup("Smart merge", (int)EditorUserSettings.semanticMergeMode, semanticMergePopupList);

                DrawOverlayDescriptions();
            }

            GUILayout.Space(10);

            int index = (int)EditorSettings.serializationMode;
            using (new EditorGUI.DisabledScope(!collabEnabled))
            {
                GUI.enabled = !collabEnabled;
                GUILayout.Label("Asset Serialization", EditorStyles.boldLabel);
                GUI.enabled = editorEnabled && !collabEnabled;


                CreatePopupMenu("Mode", serializationPopupList, index, SetAssetSerializationMode);
            }
            if (collabEnabled)
            {
                EditorGUILayout.HelpBox("Asset Serialization is forced to Text when using Collaboration feature.", MessageType.Warning);
            }

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label("Default Behavior Mode", EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp((int)EditorSettings.defaultBehaviorMode, 0, behaviorPopupList.Length - 1);
            CreatePopupMenu("Mode", behaviorPopupList, index, SetDefaultBehaviorMode);

            GUILayout.Space(10);

            GUI.enabled = true;
            GUILayout.Label("Sprite Packer", EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            index = Mathf.Clamp((int)EditorSettings.spritePackerMode, 0, spritePackerPopupList.Length - 1);
            CreatePopupMenu("Mode", spritePackerPopupList, index, SetSpritePackerMode);

            index = Mathf.Clamp((int)(EditorSettings.spritePackerPaddingPower - 1), 0, 2);
            CreatePopupMenu("Padding Power (Legacy Sprite Packer)", spritePackerPaddingPowerPopupList, index, SetSpritePackerPaddingPower);

            DoProjectGenerationSettings();
            DoEtcTextureCompressionSettings();
            DoInternalSettings();
            DoLineEndingsSettings();
        }

        private void DoProjectGenerationSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label("C# Project Generation", EditorStyles.boldLabel);

            var old = EditorSettings.Internal_ProjectGenerationUserExtensions;
            string newvalue = EditorGUILayout.TextField("Additional extensions to include", old);
            if (newvalue != old)
                EditorSettings.Internal_ProjectGenerationUserExtensions = newvalue;

            old = EditorSettings.projectGenerationRootNamespace;
            newvalue = EditorGUILayout.TextField("Root namespace", old);
            if (newvalue != old)
                EditorSettings.projectGenerationRootNamespace = newvalue;
        }

        private void DoInternalSettings()
        {
            if (!EditorPrefs.GetBool("InternalMode", false))
                return;

            GUILayout.Space(10);
            GUILayout.Label("Internal Settings", EditorStyles.boldLabel);

            var postfix = "-testable";
            EditorGUI.BeginChangeCheck();
            var isEnabled = EditorSettings.Internal_UserGeneratedProjectSuffix == postfix;
            isEnabled = EditorGUILayout.Toggle("Internals visible in user scripts", isEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                EditorSettings.Internal_UserGeneratedProjectSuffix = isEnabled ? postfix : "";
            }
            if (isEnabled)
            {
                EditorGUILayout.HelpBox("If you want this to be set for other people, remember to manually add ProjectSettings/EditorSettings.asset to the repository", MessageType.Info);
            }
        }

        private void DoEtcTextureCompressionSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label("ETC Texture Compressor", EditorStyles.boldLabel);

            int index = Mathf.Clamp((int)EditorSettings.etcTextureCompressorBehavior, 0, etcTextureCompressorPopupList.Length - 1);
            CreatePopupMenu("Behavior", etcTextureCompressorPopupList, index, SetEtcTextureCompressorBehavior);

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(index < 2);

            index = Mathf.Clamp((int)EditorSettings.etcTextureFastCompressor, 0, etcTextureFastCompressorPopupList.Length - 1);
            CreatePopupMenu("Fast", etcTextureFastCompressorPopupList, index, SetEtcTextureFastCompressor);

            index = Mathf.Clamp((int)EditorSettings.etcTextureNormalCompressor, 0, etcTextureNormalCompressorPopupList.Length - 1);
            CreatePopupMenu("Normal", etcTextureNormalCompressorPopupList, index, SetEtcTextureNormalCompressor);

            index = Mathf.Clamp((int)EditorSettings.etcTextureBestCompressor, 0, etcTextureBestCompressorPopupList.Length - 1);
            CreatePopupMenu("Best", etcTextureBestCompressorPopupList, index, SetEtcTextureBestCompressor);

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        private void DoLineEndingsSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label("Line Endings For New Scripts", EditorStyles.boldLabel);

            int index = (int)EditorSettings.lineEndingsForNewScripts;
            CreatePopupMenu("Mode", lineEndingsPopupList, index, SetLineEndingsForNewScripts);
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
            GUILayout.Label("Unity Remote", EditorStyles.boldLabel);
            GUI.enabled = editorEnabled;

            // Find selected device index
            string id = EditorSettings.unityRemoteDevice;
            // We assume first device to be "None", and default to it, hence 0
            int index = GetIndexById(remoteDeviceList, id, 0);

            var content = new GUIContent(remoteDevicePopupList[index].content);
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent("Device"));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteDevicePopupList, index, SetUnityRemoteDevice);

            int compression = GetIndexById(remoteCompressionList, EditorSettings.unityRemoteCompression, 0);
            content = new GUIContent(remoteCompressionList[compression].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent("Compression"));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteCompressionList, compression, SetUnityRemoteCompression);

            int resolution = GetIndexById(remoteResolutionList, EditorSettings.unityRemoteResolution, 0);
            content = new GUIContent(remoteResolutionList[resolution].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent("Resolution"));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteResolutionList, resolution, SetUnityRemoteResolution);

            int joystickSource = GetIndexById(remoteJoystickSourceList, EditorSettings.unityRemoteJoystickSource, 0);
            content = new GUIContent(remoteJoystickSourceList[joystickSource].content);
            popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent("Joystick Source"));
            if (EditorGUI.DropdownButton(popupRect, content, FocusType.Passive, EditorStyles.popup))
                DoPopup(popupRect, remoteJoystickSourceList, joystickSource, SetUnityRemoteJoystickSource);
        }

        private void DrawOverlayDescriptions()
        {
            Texture2D atlas = Provider.overlayAtlas;
            if (atlas == null)
                return;

            GUILayout.Space(10);
            GUILayout.Label("Overlay legends", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawOverlayDescription(Asset.States.Local);
            DrawOverlayDescription(Asset.States.OutOfSync);
            DrawOverlayDescription(Asset.States.CheckedOutLocal);
            DrawOverlayDescription(Asset.States.CheckedOutRemote);
            DrawOverlayDescription(Asset.States.DeletedLocal);
            DrawOverlayDescription(Asset.States.DeletedRemote);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            DrawOverlayDescription(Asset.States.AddedLocal);
            DrawOverlayDescription(Asset.States.AddedRemote);
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
            if (popupIndex < 0 && popupIndex >= vcPopupList.Length)
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
    }
}
