// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Collaboration;

namespace UnityEditor
{
    [CustomEditor(typeof(VersionControlSettings))]
    internal class VersionControlSettingsProvider : ProjectSettingsBaseEditor
    {
        class Styles
        {
            public static GUIContent mode = new GUIContent("Mode");
            public static GUIContent logLevel = new GUIContent("Log Level");
            public static GUIContent automaticAdd = new GUIContent("Automatic Add",
                "Automatically add newly created assets to version control.");
            public static GUIContent smartMerge = new GUIContent("Smart merge");
            public static GUIContent vcsConnect = new GUIContent("Connect");
            public static GUIContent vcsReconnect = new GUIContent("Reconnect");
            public static GUIContent workOffline = new GUIContent("Work Offline",
                "Enable asset modifications even when not connected to a version control server. Requires manual integration into VCS system afterwards.");
            public static GUIContent allowAsyncUpdate = new GUIContent("Async Status",
                "Enable asynchronous file status queries (useful with slow server connections).");
            public static GUIContent showFailedCheckouts = new GUIContent("Show Failed Checkouts",
                "Show dialogs for failed 'Check Out' operations.");
            public static GUIContent overwriteFailedCheckoutAssets =
                new GUIContent("Overwrite Failed Checkout Assets",
                    "When on, assets that can not be checked out will get saved anyway.");
            public static GUIContent overlayIcons = new GUIContent("Overlay Icons",
                "Should version control status icons be shown.");
            public static GUIContent projectOverlayIcons = new GUIContent("Project Window",
                "Should version control status icons be shown in the Project window.");
            public static GUIContent hierarchyOverlayIcons = new GUIContent("Hierarchy Window",
                "Should version control status icons be shown in the Hierarchy window.");
            public static GUIContent otherOverlayIcons = new GUIContent("Other Windows",
                "Should version control status icons be shown in other windows.");

            // these are required to have correct search keywords
            public static GUIContent password = new GUIContent("Password");
            public static GUIContent username = new GUIContent("Username");
            public static GUIContent server = new GUIContent("Server");
        }

        const int kVCFieldRecentCount = 10;
        const string kVCFieldRecentPrefix = "vcs_ConfigField";
        Dictionary<string, string[]> m_VCConfigFieldsRecentValues = new Dictionary<string, string[]>();

        bool m_NeedToSaveValuesOnConnect;

        [StructLayout(LayoutKind.Sequential)]
        public struct ExternalVersionControl
        {
            private readonly string m_Value;

            public static readonly string Disabled = "Hidden Meta Files";
            public static readonly string AutoDetect = "Auto detect";
            public static readonly string Generic = "Visible Meta Files";


            [Obsolete("Asset Server VCS support has been removed.")]
            public static readonly string AssetServer = "Asset Server";

            public ExternalVersionControl(string value)
            {
                m_Value = value;
            }

            // User-defined conversion
            public static implicit operator string(ExternalVersionControl d)
            {
                return d.ToString();
            }

            // User-defined conversion
            public static implicit operator ExternalVersionControl(string d)
            {
                return new ExternalVersionControl(d);
            }

            public override string ToString()
            {
                return m_Value;
            }
        }

        private string[] logLevelPopupList =
        {
            "Verbose", "Info", "Notice", "Fatal"
        };

        private string[] semanticMergePopupList =
        {
            "Off", "Premerge", "Ask"
        };

        private EditorSettingsInspector.PopupElement[] vcDefaultPopupList =
        {
            new EditorSettingsInspector.PopupElement(ExternalVersionControl.Disabled),
            new EditorSettingsInspector.PopupElement(ExternalVersionControl.Generic),
        };

        private EditorSettingsInspector.PopupElement[] vcPopupList = null;

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
            r.width = r.height;
            GUI.DrawTextureWithTexCoords(r, atlas, atlasUV);
        }

        private void CreatePopupMenuVersionControl(string title, EditorSettingsInspector.PopupElement[] elements, string selectedValue,
            GenericMenu.MenuFunction2 func)
        {
            var selectedIndex =
                System.Array.FindIndex(elements, (EditorSettingsInspector.PopupElement typeElem) => (typeElem.id == selectedValue));
            var content = new GUIContent(elements[selectedIndex].content);
            EditorSettingsInspector.CreatePopupMenu(null, title, content, elements, selectedIndex, func);
        }

        private void SetVersionControlSystem(object data)
        {
            int popupIndex = (int)data;
            if (popupIndex < 0 || popupIndex >= vcPopupList.Length)
                return;

            EditorSettingsInspector.PopupElement el = vcPopupList[popupIndex];
            string oldVC = VersionControlSettings.mode;

            VersionControlSettings.mode = el.id;
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

        private bool VersionControlSystemHasGUI()
        {
            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            if (!collabEnabled)
            {
            ExternalVersionControl system = VersionControlSettings.mode;
            return
                system != ExternalVersionControl.Disabled &&
                system != ExternalVersionControl.AutoDetect &&
                system != ExternalVersionControl.Generic;
        }

        return false;
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

        public void OnEnable()
        {
            Plugin[] availvc = Plugin.availablePlugins;

            List<EditorSettingsInspector.PopupElement> popupArray = new List<EditorSettingsInspector.PopupElement>(vcDefaultPopupList);
            foreach (var plugin in availvc)
            {
                popupArray.Add(new EditorSettingsInspector.PopupElement(plugin.name));
            }

            vcPopupList = popupArray.ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);
            GUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            using (new EditorGUI.DisabledScope(!collabEnabled))
            {
                GUI.enabled = !collabEnabled;

                ExternalVersionControl selvc = VersionControlSettings.mode;
                CreatePopupMenuVersionControl(Styles.mode.text, vcPopupList, selvc, SetVersionControlSystem);
                GUI.enabled = !collabEnabled;
            }

            if (collabEnabled)
            {
                EditorGUILayout.HelpBox("Version Control not available when using Collaboration feature.",
                    MessageType.Warning);
            }

            GUI.enabled = true;
            ConfigField[] configFields = null;

            if (VersionControlSystemHasGUI())
            {
                bool hasRequiredFields = false;

                if (VersionControlSettings.mode == ExternalVersionControl.Generic ||
                    VersionControlSettings.mode == ExternalVersionControl.Disabled)
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
                            newVal = EditorGUILayout.PasswordField(GUIContent.Temp(field.label, field.description),
                                oldVal);
                            if (newVal != oldVal)
                                EditorUserSettings.SetPrivateConfigValue(field.name, newVal);
                        }
                        else
                        {
                            var recentValues = GetVCConfigFieldRecentValues(field.name);
                            newVal = EditorGUILayout.TextFieldDropDown(GUIContent.Temp(field.label, field.description),
                                oldVal, recentValues);
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

                int newIdx = EditorGUILayout.Popup(Styles.logLevel, idx, logLevelPopupList);
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
                    var text =
                        "Working Offline. Manually integrate your changes using a version control client, and uncheck 'Work Offline' setting below to get back to regular state.";
                    EditorGUILayout.HelpBox(text, MessageType.Warning);
                }
                else if (Provider.onlineState == OnlineState.Online)
                {
                    var text = "Connected";
                    EditorGUILayout.HelpBox(text, MessageType.Info);
                }

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = hasRequiredFields && Provider.onlineState != OnlineState.Updating;
                if (GUILayout.Button(
                    Provider.onlineState != OnlineState.Offline ? Styles.vcsReconnect : Styles.vcsConnect,
                    EditorStyles.miniButton))
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
                    bool workOfflineNew =
                        EditorGUILayout.Toggle(Styles.workOffline,
                            EditorUserSettings.WorkOffline); // Enabled has a slightly different behaviour
                    if (workOfflineNew != EditorUserSettings.WorkOffline)
                    {
                        // On toggling on show a warning
                        if (workOfflineNew && !EditorUtility.DisplayDialog("Confirm working offline",
                            "Working offline and making changes to your assets means that you will have to manually integrate changes back into version control using your standard version control client before you stop working offline in Unity. Make sure you know what you are doing.",
                            "Work offline", "Cancel"))
                        {
                            workOfflineNew = false; // User cancelled working offline
                        }

                        EditorUserSettings.WorkOffline = workOfflineNew;
                        EditorApplication.RequestRepaintAllViews();
                    }
                }

                EditorUserSettings.AutomaticAdd =
                    EditorGUILayout.Toggle(Styles.automaticAdd, EditorUserSettings.AutomaticAdd);

                if (Provider.requiresNetwork)
                    EditorUserSettings.allowAsyncStatusUpdate = EditorGUILayout.Toggle(Styles.allowAsyncUpdate,
                        EditorUserSettings.allowAsyncStatusUpdate);

                if (Provider.hasCheckoutSupport)
                {
                    EditorUserSettings.showFailedCheckout = EditorGUILayout.Toggle(Styles.showFailedCheckouts,
                        EditorUserSettings.showFailedCheckout);
                    EditorUserSettings.overwriteFailedCheckoutAssets = EditorGUILayout.Toggle(
                        Styles.overwriteFailedCheckoutAssets, EditorUserSettings.overwriteFailedCheckoutAssets);
                }

                EditorUserSettings.semanticMergeMode = (SemanticMergeMode)EditorGUILayout.Popup(Styles.smartMerge,
                    (int)EditorUserSettings.semanticMergeMode, semanticMergePopupList);

                GUILayout.Space(10);
                GUILayout.Label(Styles.overlayIcons);

                EditorGUI.indentLevel++;
                var newProjectOverlayIcons = EditorGUILayout.Toggle(Styles.projectOverlayIcons, EditorUserSettings.overlayIcons);
                if (newProjectOverlayIcons != EditorUserSettings.overlayIcons)
                {
                    EditorUserSettings.overlayIcons = newProjectOverlayIcons;
                    EditorApplication.RequestRepaintAllViews();
                }

                var newHierarchyOverlayIcons = EditorGUILayout.Toggle(Styles.hierarchyOverlayIcons, EditorUserSettings.hierarchyOverlayIcons);
                if (newHierarchyOverlayIcons != EditorUserSettings.hierarchyOverlayIcons)
                {
                    EditorUserSettings.hierarchyOverlayIcons = newHierarchyOverlayIcons;
                    EditorApplication.RequestRepaintAllViews();
                }

                var newOtherOverlayIcons = EditorGUILayout.Toggle(Styles.otherOverlayIcons, EditorUserSettings.otherOverlayIcons);
                if (newOtherOverlayIcons != EditorUserSettings.otherOverlayIcons)
                {
                    EditorUserSettings.otherOverlayIcons = newOtherOverlayIcons;
                    EditorApplication.RequestRepaintAllViews();
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(10);

                GUI.enabled = true;
                if (newProjectOverlayIcons || newHierarchyOverlayIcons || newOtherOverlayIcons)
                    DrawOverlayDescriptions();
            }
            GUILayout.EndVertical();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Version Control", "ProjectSettings/VersionControlSettings.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
    }
}
