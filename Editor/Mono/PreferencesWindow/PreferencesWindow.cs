// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditor.Modules;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Connect;
using UnityEditor.Collaboration;
using UnityEditor.Web;

namespace UnityEditor
{
    internal class PreferencesWindow : EditorWindow
    {
        internal class Constants
        {
            public GUIStyle sectionScrollView = "PreferencesSectionBox";
            public GUIStyle settingsBoxTitle = "OL Title";
            public GUIStyle settingsBox = "OL Box";
            public GUIStyle errorLabel = "WordWrappedLabel";
            public GUIStyle sectionElement = "PreferencesSection";
            public GUIStyle evenRow = "CN EntryBackEven";
            public GUIStyle oddRow = "CN EntryBackOdd";
            public GUIStyle selected = "OL SelectedRow";
            public GUIStyle keysElement = "PreferencesKeysElement";
            public GUIStyle warningIcon = "CN EntryWarn";
            public GUIStyle sectionHeader = new GUIStyle(EditorStyles.largeLabel);
            public GUIStyle cacheFolderLocation = new GUIStyle(GUI.skin.label);

            public Constants()
            {
                sectionScrollView = new GUIStyle(sectionScrollView);
                sectionScrollView.overflow.bottom += 1;

                sectionHeader.fontStyle = FontStyle.Bold;
                sectionHeader.fontSize = 18;
                sectionHeader.margin.top = 10;
                sectionHeader.margin.left += 1;
                if (!EditorGUIUtility.isProSkin)
                    sectionHeader.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
                else
                    sectionHeader.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);

                cacheFolderLocation.wordWrap = true;
            }
        }

        internal class Styles
        {
            // Common
            public static readonly GUIContent browse = EditorGUIUtility.TrTextContent("Browse...");

            // General
            public static readonly GUIContent autoRefresh = EditorGUIUtility.TrTextContent("Auto Refresh");
            public static readonly GUIContent autoRefreshHelpBox = EditorGUIUtility.TrTextContent("Auto Refresh must be set when using Collaboration feature.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent loadPreviousProjectOnStartup = EditorGUIUtility.TrTextContent("Load Previous Project on Startup");
            public static readonly GUIContent compressAssetsOnImport = EditorGUIUtility.TrTextContent("Compress Assets on Import");
            public static readonly GUIContent osxColorPicker = EditorGUIUtility.TrTextContent("macOS Color Picker");
            public static readonly GUIContent disableEditorAnalytics = EditorGUIUtility.TrTextContent("Disable Editor Analytics (Pro Only)");
            public static readonly GUIContent showAssetStoreSearchHits = EditorGUIUtility.TrTextContent("Show Asset Store search hits");
            public static readonly GUIContent verifySavingAssets = EditorGUIUtility.TrTextContent("Verify Saving Assets");
            public static readonly GUIContent editorSkin = EditorGUIUtility.TrTextContent("Editor Skin");
            public static readonly GUIContent[] editorSkinOptions = new GUIContent[] { EditorGUIUtility.TrTextContent("Personal"), EditorGUIUtility.TrTextContent("Professional"), };
            public static readonly GUIContent enableAlphaNumericSorting = EditorGUIUtility.TrTextContent("Enable Alpha Numeric Sorting");

            // External Tools
            public static readonly GUIContent downloadMonoDevelopInstaller = EditorGUIUtility.TrTextContent("Download MonoDevelop Installer");
            public static readonly GUIContent addUnityProjeToSln = EditorGUIUtility.TrTextContent("Add .unityproj's to .sln");
            public static readonly GUIContent editorAttaching = EditorGUIUtility.TrTextContent("Editor Attaching");
            public static readonly GUIContent changingThisSettingRequiresRestart = EditorGUIUtility.TrTextContent("Changing this setting requires a restart to take effect.");
            public static readonly GUIContent revisionControlDiffMerge = EditorGUIUtility.TrTextContent("Revision Control Diff/Merge");
            public static readonly GUIContent externalScriptEditor = EditorGUIUtility.TrTextContent("External Script Editor");
            public static readonly GUIContent imageApplication = EditorGUIUtility.TrTextContent("Image application");

            // Colors
            public static readonly GUIContent userDefaults = EditorGUIUtility.TrTextContent("Use Defaults");

            // GI Cache
            public static readonly GUIContent maxCacheSize = EditorGUIUtility.TrTextContent("Maximum Cache Size (GB)", "The size of the GI Cache folder will be kept below this maximum value when possible. A background job will periodically clean up the oldest unused files.");
            public static readonly GUIContent customCacheLocation = EditorGUIUtility.TrTextContent("Custom cache location", "Specify the GI Cache folder location.");
            public static readonly GUIContent cacheFolderLocation = EditorGUIUtility.TrTextContent("Cache Folder Location", "The GI Cache folder is shared between all projects.");
            public static readonly GUIContent cacheCompression = EditorGUIUtility.TrTextContent("Cache compression", "Use fast realtime compression for the GI cache files to reduce the size of generated data. Disable it and clean the cache if you need access to the raw data generated by Enlighten.");
            public static readonly GUIContent cantChangeCacheSettings = EditorGUIUtility.TrTextContent("Cache settings can't be changed while lightmapping is being computed.");
            public static readonly GUIContent cleanCache = EditorGUIUtility.TrTextContent("Clean Cache");
            public static readonly GUIContent browseGICacheLocation = EditorGUIUtility.TrTextContent("Browse for GI Cache location");
            public static readonly GUIContent cacheSizeIs = EditorGUIUtility.TrTextContent("Cache size is");
            public static readonly GUIContent pleaseWait = EditorGUIUtility.TrTextContent("Please wait...");

            // 2D
            public static readonly GUIContent spriteMaxCacheSize = EditorGUIUtility.TrTextContent("Max Sprite Atlas Cache Size (GB)", "The size of the Sprite Atlas Cache folder will be kept below this maximum value when possible. Change requires Editor restart");

            // Language
            public static readonly GUIContent editorLanguageExperimental = EditorGUIUtility.TrTextContent("Editor Language(Experimental)");
            public static readonly GUIContent editorLanguage = EditorGUIUtility.TrTextContent("Editor language");
        }

        private delegate void OnGUIDelegate();
        private class Section
        {
            public GUIContent content;
            public OnGUIDelegate guiFunc;

            public Section(string name, OnGUIDelegate guiFunc)
            {
                this.content = EditorGUIUtility.TrTextContent(name);
                this.guiFunc = guiFunc;
            }

            public Section(string name, Texture2D icon, OnGUIDelegate guiFunc)
            {
                this.content = EditorGUIUtility.TrTextContent(name, icon);
                this.guiFunc = guiFunc;
            }

            public Section(GUIContent content, OnGUIDelegate guiFunc)
            {
                this.content = content;
                this.guiFunc = guiFunc;
            }
        }

        private List<Section> m_Sections;
        private int m_SelectedSectionIndex;
        private int selectedSectionIndex
        {
            get { return m_SelectedSectionIndex; }
            set
            {
                if (m_SelectedSectionIndex != value)
                {
                    // Reset the valid key indicator when changing section
                    m_ValidKeyChange = true;
                }
                m_SelectedSectionIndex = value;
                if (m_SelectedSectionIndex >= m_Sections.Count)
                    m_SelectedSectionIndex = 0;
                else if (m_SelectedSectionIndex < 0)
                    m_SelectedSectionIndex = m_Sections.Count - 1;
            }
        }
        private Section selectedSection { get { return m_Sections[m_SelectedSectionIndex]; } }

        static Constants constants = null;

        private List<IPreferenceWindowExtension> prefWinExtensions;
        private bool m_AutoRefresh;

        private bool m_ReopenLastUsedProjectOnStartup;
        private bool m_CompressAssetsOnImport;
        private bool m_UseOSColorPicker;
        private bool m_EnableEditorAnalytics;
        private bool m_ShowAssetStoreSearchHits;
        private bool m_VerifySavingAssets;
        private bool m_DeveloperMode;
        private bool m_DeveloperModeDirty;
        private bool m_AllowAttachedDebuggingOfEditor;
        private bool m_AllowAttachedDebuggingOfEditorStateChangedThisSession;
        private string m_GpuDevice;
        private string[] m_CachedGpuDevices;

        private struct GICacheSettings
        {
            public bool m_EnableCustomPath;
            public int m_MaximumSize;
            public string m_CachePath;
            public int m_CompressionLevel; // GICache compression level, corresponds to CompressionLevel in Compression.h
        }
        private GICacheSettings m_GICacheSettings;

        private RefString m_ScriptEditorPath = new RefString("");
        private string m_ScriptEditorArgs = "";
        private bool m_ExternalEditorSupportsUnityProj;
        private RefString m_ImageAppPath = new RefString("");
        private int m_DiffToolIndex;

        // how many menu items come before the actual list of languages
        // (Default + separator)
        private const int k_LangListMenuOffset = 2;

        private string m_SelectedLanguage;
        private GUIContent[] m_EditorLanguageNames;
        private bool m_EnableEditorLocalization;
        private SystemLanguage[] m_stableLanguages = { SystemLanguage.English };

        private bool m_AllowAlphaNumericHierarchy = false;

        private string[] m_ScriptApps;
        private string[] m_ScriptAppsEditions;
        private string[] m_ImageApps;
        private string[] m_DiffTools;

        private string m_noDiffToolsMessage = string.Empty;

        private bool m_RefreshCustomPreferences;
        private string[] m_ScriptAppDisplayNames;
        private string[] m_ImageAppDisplayNames;
        Vector2 m_KeyScrollPos;
        Vector2 m_SectionScrollPos;
        PrefKey m_SelectedKey = null;
        private const string kRecentScriptAppsKey = "RecentlyUsedScriptApp";
        private const string kRecentImageAppsKey = "RecentlyUsedImageApp";

        private const string m_ExpressNotSupportedMessage =
            "Unfortunately Visual Studio Express does not allow itself to be controlled by external applications. " +
            "You can still use it by manually opening the Visual Studio project file, but Unity cannot automatically open files for you when you doubleclick them. " +
            "\n(This does work with Visual Studio Pro)";

        private const int kRecentAppsCount = 10;

        SortedDictionary<string, List<KeyValuePair<string, PrefColor>>> s_CachedColors = null;
        private static Vector2 s_ScrollPosition = Vector2.zero;

        private int m_SpriteAtlasCacheSize;
        private static int kMinSpriteCacheSizeInGigabytes = 1;
        private static int kMaxSpriteCacheSizeInGigabytes = 200;

        private bool m_ValidKeyChange = true;
        private string m_InvalidKeyMessage = string.Empty;

        class RefString
        {
            public RefString(string s) { str = s; }
            public string str;
            public static implicit operator string(RefString s) { return s.str; }
            public override string ToString()
            {
                return str;
            }
        }

        static void ShowPreferencesWindow()
        {
            EditorWindow w = EditorWindow.GetWindow<PreferencesWindow>(true, L10n.Tr("Unity Preferences"));
            w.minSize = new Vector2(540, 400);
            w.maxSize = new Vector2(w.minSize.x, w.maxSize.y); // Limit to only changing the height.
            w.position = new Rect(new Vector2(100, 100), w.minSize);
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        void OnEnable()
        {
            prefWinExtensions = ModuleManager.GetPreferenceWindowExtensions();

            ReadPreferences();

            m_Sections = new List<Section>();

            //@TODO Move these to custom sections
            m_Sections.Add(new Section("General", ShowGeneral));
            m_Sections.Add(new Section("External Tools", ShowExternalApplications));
            m_Sections.Add(new Section("Colors", ShowColors));
            m_Sections.Add(new Section("Keys", ShowKeys));
            m_Sections.Add(new Section("GI Cache", ShowGICache));
            m_Sections.Add(new Section("2D", Show2D));
            SystemLanguage[] editorLanguages = LocalizationDatabase.GetAvailableEditorLanguages();
            if (m_EditorLanguageNames == null || m_EditorLanguageNames.Length != editorLanguages.Length)
            {
                m_EditorLanguageNames = new GUIContent[editorLanguages.Length];

                for (int i = 0; i < editorLanguages.Length; ++i)
                {
                    // not in stable languages list - display it as experimental language
                    if (ArrayUtility.FindIndex(m_stableLanguages, delegate(SystemLanguage v) { return v == editorLanguages[i]; }) < 0)
                    {
                        m_EditorLanguageNames[i] = EditorGUIUtility.TextContent(string.Format("{0} (Experimental)", editorLanguages[i].ToString()));
                    }
                    else
                    {
                        m_EditorLanguageNames[i] = EditorGUIUtility.TextContent(editorLanguages[i].ToString());
                    }
                }
                ArrayUtility.Insert(ref m_EditorLanguageNames, 0, EditorGUIUtility.TextContent(""));
                GUIContent defaultLanguage = EditorGUIUtility.TextContent(string.Format("Default ( {0} )", LocalizationDatabase.GetDefaultEditorLanguage().ToString()));
                ArrayUtility.Insert(ref m_EditorLanguageNames, 0, defaultLanguage);
            }
            if (editorLanguages.Length > 1)
            {
                m_Sections.Add(new Section("Language", ShowLanguage));
            }

            if (Unsupported.IsDeveloperMode() || UnityConnect.preferencesEnabled)
            {
                m_Sections.Add(new Section("Unity Services", ShowUnityConnectPrefs));
            }
            // Workaround for EditorAssemblies not loaded yet during mono assembly reload.
            m_RefreshCustomPreferences = true;
        }

        private void AddCustomSections()
        {
            AttributeHelper.MethodInfoSorter methods = AttributeHelper.GetMethodsWithAttribute<PreferenceItem>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var method in methods.methodsWithAttributes)
            {
                OnGUIDelegate callback = Delegate.CreateDelegate(typeof(OnGUIDelegate), method.info) as OnGUIDelegate;
                if (callback != null)
                {
                    var attributeName = (method.attribute as PreferenceItem).name;
                    // Group Preference sections with the same name
                    int idx = m_Sections.FindIndex(section => section.content.text.Equals(attributeName));
                    if (idx >= 0)
                    {
                        m_Sections[idx].guiFunc += callback;
                    }
                    else
                    {
                        m_Sections.Add(new Section(attributeName, callback));
                    }
                }
            }
        }

        private void OnGUI()
        {
            // Workaround for EditorAssemblies not loaded yet during mono assembly reload.
            if (m_RefreshCustomPreferences)
            {
                AddCustomSections();
                m_RefreshCustomPreferences = false;
            }

            EditorGUIUtility.labelWidth = 200f;

            if (constants == null)
            {
                constants = new Constants();
            }

            HandleKeys();

            GUILayout.BeginHorizontal();
            {
                m_SectionScrollPos = GUILayout.BeginScrollView(m_SectionScrollPos, constants.sectionScrollView, GUILayout.Width(140f));
                {
                    GUILayout.Space(40f);
                    for (int i = 0; i < m_Sections.Count; i++)
                    {
                        var section = m_Sections[i];

                        Rect elementRect = GUILayoutUtility.GetRect(section.content, constants.sectionElement, GUILayout.ExpandWidth(true));

                        if (section == selectedSection && Event.current.type == EventType.Repaint)
                            constants.selected.Draw(elementRect, false, false, false, false);

                        EditorGUI.BeginChangeCheck();
                        if (GUI.Toggle(elementRect, selectedSectionIndex == i, section.content, constants.sectionElement))
                            selectedSectionIndex = i;
                        if (EditorGUI.EndChangeCheck())
                            GUIUtility.keyboardControl = 0;
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(10.0f);

                GUILayout.BeginVertical();
                {
                    GUILayout.Label(selectedSection.content, constants.sectionHeader);
                    GUILayout.Space(10f);
                    s_ScrollPosition = EditorGUILayout.BeginScrollView(s_ScrollPosition);
                    selectedSection.guiFunc();
                    EditorGUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void HandleKeys()
        {
            if (Event.current.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
                return;

            switch (Event.current.keyCode)
            {
                case KeyCode.UpArrow:
                    selectedSectionIndex--;
                    Event.current.Use();
                    break;
                case KeyCode.DownArrow:
                    selectedSectionIndex++;
                    Event.current.Use();
                    break;
            }
        }

        private void ShowExternalApplications()
        {
            // Applications
            FilePopup(Styles.externalScriptEditor, m_ScriptEditorPath, ref m_ScriptAppDisplayNames, ref m_ScriptApps, m_ScriptEditorPath, "internal", OnScriptEditorChanged);

            var scriptEditor = GetSelectedScriptEditor();

            if (scriptEditor == ScriptEditorUtility.ScriptEditor.Other)
            {
                string oldEditorArgs = m_ScriptEditorArgs;
                m_ScriptEditorArgs = EditorGUILayout.TextField("External Script Editor Args", m_ScriptEditorArgs);
                if (oldEditorArgs != m_ScriptEditorArgs)
                    OnScriptEditorArgsChanged();
            }

            DoUnityProjCheckbox();

            bool oldValue = m_AllowAttachedDebuggingOfEditor;
            m_AllowAttachedDebuggingOfEditor = EditorGUILayout.Toggle(Styles.editorAttaching, m_AllowAttachedDebuggingOfEditor);

            if (oldValue != m_AllowAttachedDebuggingOfEditor)
                m_AllowAttachedDebuggingOfEditorStateChangedThisSession = true;

            if (m_AllowAttachedDebuggingOfEditorStateChangedThisSession)
                GUILayout.Label(Styles.changingThisSettingRequiresRestart, EditorStyles.helpBox);

            if (GetSelectedScriptEditor() == ScriptEditorUtility.ScriptEditor.VisualStudioExpress)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("", constants.warningIcon);
                GUILayout.Label(m_ExpressNotSupportedMessage, constants.errorLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);

            FilePopup(Styles.imageApplication, m_ImageAppPath, ref m_ImageAppDisplayNames, ref m_ImageApps, m_ImageAppPath, "internal", null);

            GUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(!InternalEditorUtility.HasTeamLicense()))
            {
                m_DiffToolIndex = EditorGUILayout.Popup(Styles.revisionControlDiffMerge, m_DiffToolIndex, m_DiffTools);
            }

            if (m_noDiffToolsMessage != string.Empty)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("", constants.warningIcon);
                GUILayout.Label(m_noDiffToolsMessage, constants.errorLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                if (extension.HasExternalApplications())
                {
                    GUILayout.Space(10f);
                    extension.ShowExternalApplications();
                }
            }

            ApplyChangesToPrefs();
        }

        private void DoUnityProjCheckbox()
        {
            bool isConfigurable = false;
            bool value = false;

            ScriptEditorUtility.ScriptEditor scriptEditor = GetSelectedScriptEditor();

            if (scriptEditor == ScriptEditorUtility.ScriptEditor.MonoDevelop)
            {
                isConfigurable = true;
                value = m_ExternalEditorSupportsUnityProj;
            }

            using (new EditorGUI.DisabledScope(!isConfigurable))
            {
                value = EditorGUILayout.Toggle(Styles.addUnityProjeToSln, value);
            }

            if (isConfigurable)
                m_ExternalEditorSupportsUnityProj = value;
        }

        private ScriptEditorUtility.ScriptEditor GetSelectedScriptEditor()
        {
            return ScriptEditorUtility.GetScriptEditorFromPath(m_ScriptEditorPath.str);
        }

        private void OnScriptEditorChanged()
        {
            ScriptEditorUtility.SetExternalScriptEditor(m_ScriptEditorPath);
            m_ScriptEditorArgs = ScriptEditorUtility.GetExternalScriptEditorArgs();
            UnityEditor.VisualStudioIntegration.UnityVSSupport.ScriptEditorChanged(m_ScriptEditorPath.str);
        }

        private void OnScriptEditorArgsChanged()
        {
            ScriptEditorUtility.SetExternalScriptEditorArgs(m_ScriptEditorArgs);
        }

        private void ShowUnityConnectPrefs()
        {
            UnityConnectPrefs.ShowPanelPrefUI();
            ApplyChangesToPrefs();
        }

        private void ShowGeneral()
        {
            // Options
            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            using (new EditorGUI.DisabledScope(collabEnabled))
            {
                if (collabEnabled)
                {
                    EditorGUILayout.Toggle(Styles.autoRefresh, true);       // Don't keep toggle value in m_AutoRefresh since we don't want to save the overwritten value
                    EditorGUILayout.HelpBox(Styles.autoRefreshHelpBox);
                }
                else
                    m_AutoRefresh = EditorGUILayout.Toggle(Styles.autoRefresh, m_AutoRefresh);
            }

            m_ReopenLastUsedProjectOnStartup = EditorGUILayout.Toggle(Styles.loadPreviousProjectOnStartup, m_ReopenLastUsedProjectOnStartup);

            bool oldCompressOnImport = m_CompressAssetsOnImport;
            m_CompressAssetsOnImport = EditorGUILayout.Toggle(Styles.compressAssetsOnImport, oldCompressOnImport);

            if (GUI.changed && m_CompressAssetsOnImport != oldCompressOnImport)
                Unsupported.SetApplicationSettingCompressAssetsOnImport(m_CompressAssetsOnImport);

            if (Application.platform == RuntimePlatform.OSXEditor)
                m_UseOSColorPicker = EditorGUILayout.Toggle(Styles.osxColorPicker, m_UseOSColorPicker);

            bool pro = UnityEngine.Application.HasProLicense();
            using (new EditorGUI.DisabledScope(!pro))
            {
                m_EnableEditorAnalytics = !EditorGUILayout.Toggle(Styles.disableEditorAnalytics, !m_EnableEditorAnalytics);
                if (!pro && !m_EnableEditorAnalytics)
                    m_EnableEditorAnalytics = true;
            }

            bool assetStoreSearchChanged = false;
            EditorGUI.BeginChangeCheck();
            m_ShowAssetStoreSearchHits = EditorGUILayout.Toggle(Styles.showAssetStoreSearchHits, m_ShowAssetStoreSearchHits);
            if (EditorGUI.EndChangeCheck())
                assetStoreSearchChanged = true;

            m_VerifySavingAssets = EditorGUILayout.Toggle(Styles.verifySavingAssets, m_VerifySavingAssets);

            // Only show this toggle if this is a source build or we're already in developer mode.
            // We don't want to show this to users yet.
            if (Unsupported.IsSourceBuild() || m_DeveloperMode)
            {
                EditorGUI.BeginChangeCheck();
                m_DeveloperMode = EditorGUILayout.Toggle("Developer Mode", m_DeveloperMode);
                if (EditorGUI.EndChangeCheck())
                    m_DeveloperModeDirty = true;
            }

            using (new EditorGUI.DisabledScope(!pro))
            {
                int newSkin = EditorGUILayout.Popup(Styles.editorSkin, !EditorGUIUtility.isProSkin ? 0 : 1, Styles.editorSkinOptions);
                if ((!EditorGUIUtility.isProSkin ? 0 : 1) != newSkin)
                    InternalEditorUtility.SwitchSkinAndRepaintAllViews();
            }

            bool oldAlphaNumeric = m_AllowAlphaNumericHierarchy;
            m_AllowAlphaNumericHierarchy = EditorGUILayout.Toggle(Styles.enableAlphaNumericSorting, m_AllowAlphaNumericHierarchy);

            if (InternalEditorUtility.IsGpuDeviceSelectionSupported())
            {
                // Cache gpu devices
                if (m_CachedGpuDevices == null)
                {
                    var devices = InternalEditorUtility.GetGpuDevices();
                    m_CachedGpuDevices = new string[devices.Length + 1];
                    m_CachedGpuDevices[0] = "Automatic";
                    Array.Copy(devices, 0, m_CachedGpuDevices, 1, devices.Length);
                }

                // Try to find selected gpu device
                var currentGpuDeviceIndex = Array.FindIndex(m_CachedGpuDevices, (string gpuDevice) => m_GpuDevice == gpuDevice);
                if (currentGpuDeviceIndex == -1)
                    currentGpuDeviceIndex = 0;

                var newGpuDeviceIndex = EditorGUILayout.Popup("Device To Use", currentGpuDeviceIndex, m_CachedGpuDevices);
                if (currentGpuDeviceIndex != newGpuDeviceIndex)
                {
                    m_GpuDevice = m_CachedGpuDevices[newGpuDeviceIndex];
                    InternalEditorUtility.SetGpuDeviceAndRecreateGraphics(newGpuDeviceIndex - 1, m_GpuDevice);
                }
            }
            ApplyChangesToPrefs();

            if (oldAlphaNumeric != m_AllowAlphaNumericHierarchy)
                EditorApplication.DirtyHierarchyWindowSorting();

            if (assetStoreSearchChanged)
            {
                ProjectBrowser.ShowAssetStoreHitsWhileSearchingLocalAssetsChanged();
            }
        }

        public void ApplyChangesToPrefs(bool force = false)
        {
            if (GUI.changed || force)
            {
                WritePreferences();
                ReadPreferences();
                Repaint();
            }
        }

        private void RevertKeys()
        {
            foreach (KeyValuePair<string, PrefKey> kvp in Settings.Prefs<PrefKey>())
            {
                kvp.Value.ResetToDefault();
                EditorPrefs.SetString(kvp.Value.Name, kvp.Value.ToUniqueString());
            }
        }

        private SortedDictionary<string, List<KeyValuePair<string, T>>> OrderPrefs<T>(IEnumerable<KeyValuePair<string, T>> input)
            where T : IPrefType
        {
            SortedDictionary<string, List<KeyValuePair<string, T>>> retval = new SortedDictionary<string, List<KeyValuePair<string, T>>>();

            foreach (KeyValuePair<string, T> kvp in input)
            {
                int idx = kvp.Key.IndexOf('/');
                string first, second;
                if (idx == -1)
                {
                    first = "General";
                    second = kvp.Key;
                }
                else
                {
                    first = kvp.Key.Substring(0, idx);
                    second = kvp.Key.Substring(idx + 1);
                }
                if (!retval.ContainsKey(first))
                {
                    List<KeyValuePair<string, T>> inner = new List<KeyValuePair<string, T>>();
                    inner.Add(new KeyValuePair<string, T>(second, kvp.Value));
                    retval.Add(first, new List<KeyValuePair<string, T>>(inner));
                }
                else
                {
                    retval[first].Add(new KeyValuePair<string, T>(second, kvp.Value));
                }
            }

            return retval;
        }

        static int s_KeysControlHash = "KeysControlHash".GetHashCode();
        private void ShowKeys()
        {
            int id = GUIUtility.GetControlID(s_KeysControlHash, FocusType.Keyboard);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(185f));
            GUILayout.Label("Actions", constants.settingsBoxTitle, GUILayout.ExpandWidth(true));
            m_KeyScrollPos = GUILayout.BeginScrollView(m_KeyScrollPos, constants.settingsBox);

            PrefKey prevKey = null;
            PrefKey nextKey = null;
            bool foundSelectedKey = false;

            foreach (KeyValuePair<string, PrefKey> kvp in Settings.Prefs<PrefKey>())
            {
                if (!foundSelectedKey)
                {
                    if (kvp.Value == m_SelectedKey)
                    {
                        foundSelectedKey = true;
                    }
                    else
                    {
                        prevKey = kvp.Value;
                    }
                }
                else
                {
                    if (nextKey == null) nextKey = kvp.Value;
                }

                EditorGUI.BeginChangeCheck();
                if (GUILayout.Toggle(kvp.Value == m_SelectedKey, kvp.Key, constants.keysElement))
                {
                    if (m_SelectedKey != kvp.Value)
                    {
                        m_ValidKeyChange = true;
                    }
                    m_SelectedKey = kvp.Value;
                }
                if (EditorGUI.EndChangeCheck())
                    GUIUtility.keyboardControl = id;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10.0f);

            GUILayout.BeginVertical();

            if (m_SelectedKey != null)
            {
                Event e = m_SelectedKey.KeyboardEvent;
                GUI.changed = false;
                var splitKey = m_SelectedKey.Name.Split('/');
                System.Diagnostics.Debug.Assert(splitKey.Length == 2, "Unexpected Split: " + m_SelectedKey.Name);
                GUILayout.Label(splitKey[0], "boldLabel");
                GUILayout.Label(splitKey[1], "boldLabel");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Key:");
                e = EditorGUILayout.KeyEventField(e);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Modifiers:");
                GUILayout.BeginVertical();
                if (Application.platform == RuntimePlatform.OSXEditor)
                    e.command = GUILayout.Toggle(e.command, "Command");
                e.control = GUILayout.Toggle(e.control, "Control");
                e.shift = GUILayout.Toggle(e.shift, "Shift");
                e.alt = GUILayout.Toggle(e.alt, "Alt");

                if (GUI.changed)
                {
                    m_ValidKeyChange = true;
                    string selectedToolName = m_SelectedKey.Name.Split('/')[0];

                    foreach (KeyValuePair<string, PrefKey> kvp in Settings.Prefs<PrefKey>())
                    {
                        // Setting the same key to the same action is ok.
                        // Setting the same key to a different action from a different tool is ok too.
                        string toolName = kvp.Key.Split('/')[0];
                        if (kvp.Value.KeyboardEvent.Equals(e) && (toolName == selectedToolName && kvp.Key != m_SelectedKey.Name))
                        {
                            m_ValidKeyChange = false;
                            StringBuilder sb = new StringBuilder();
                            if (Application.platform == RuntimePlatform.OSXEditor && e.command)
                                sb.Append("Command+");
                            if (e.control)
                                sb.Append("Ctrl+");
                            if (e.shift)
                                sb.Append("Shift+");
                            if (e.alt)
                                sb.Append("Alt+");
                            sb.Append(e.keyCode);

                            m_InvalidKeyMessage = string.Format("Key {0} can't be used for action \"{1}\" because it's already used for action \"{2}\"",
                                    sb, m_SelectedKey.Name, kvp.Key);
                            break;
                        }
                    }

                    if (m_ValidKeyChange)
                    {
                        m_SelectedKey.KeyboardEvent = e;
                        Settings.Set(m_SelectedKey.Name, m_SelectedKey);
                    }
                }
                else
                {
                    if (GUIUtility.keyboardControl == id && Event.current.type == EventType.KeyDown)
                    {
                        switch (Event.current.keyCode)
                        {
                            case KeyCode.UpArrow:
                                if (prevKey != null && prevKey != m_SelectedKey)
                                {
                                    m_SelectedKey = prevKey;
                                    m_ValidKeyChange = true;
                                }
                                Event.current.Use();
                                break;
                            case KeyCode.DownArrow:
                                if (nextKey != null && nextKey != m_SelectedKey)
                                {
                                    m_SelectedKey = nextKey;
                                    m_ValidKeyChange = true;
                                }
                                Event.current.Use();
                                break;
                        }
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (!m_ValidKeyChange)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("", constants.warningIcon);
                    GUILayout.Label(m_InvalidKeyMessage, constants.errorLabel);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(10f);

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);

            if (GUILayout.Button(Styles.userDefaults, GUILayout.Width(120)))
            {
                m_ValidKeyChange = true;
                RevertKeys();
            }
        }

        private void RevertColors()
        {
            foreach (KeyValuePair<string, PrefColor> kvp in Settings.Prefs<PrefColor>())
            {
                kvp.Value.ResetToDefault();
                EditorPrefs.SetString(kvp.Value.Name, kvp.Value.ToUniqueString());
            }
        }

        private void ShowColors()
        {
            if (s_CachedColors == null)
            {
                s_CachedColors = OrderPrefs<PrefColor>(Settings.Prefs<PrefColor>());
            }

            var changedColor = false;
            PrefColor ccolor = null;
            foreach (KeyValuePair<string, List<KeyValuePair<string, PrefColor>>> category in s_CachedColors)
            {
                GUILayout.Label(category.Key, EditorStyles.boldLabel);
                foreach (KeyValuePair<string, PrefColor> kvp in category.Value)
                {
                    EditorGUI.BeginChangeCheck();
                    Color c = EditorGUILayout.ColorField(kvp.Key, kvp.Value.Color);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ccolor = kvp.Value;
                        ccolor.Color = c;
                        changedColor = true;
                    }
                }
                if (ccolor != null)
                    Settings.Set(ccolor.Name, ccolor);
            }
            GUILayout.Space(5f);

            if (GUILayout.Button(Styles.userDefaults, GUILayout.Width(120)))
            {
                RevertColors();
                changedColor = true;
            }

            if (changedColor)
                EditorApplication.RequestRepaintAllViews();
        }

        private void Show2D()
        {
            // 2D Settings.
            EditorGUI.BeginChangeCheck();

            m_SpriteAtlasCacheSize = EditorGUILayout.IntSlider(Styles.spriteMaxCacheSize, m_SpriteAtlasCacheSize, kMinSpriteCacheSizeInGigabytes, kMaxSpriteCacheSizeInGigabytes);
            if (EditorGUI.EndChangeCheck())
                WritePreferences();
        }

        private void ShowGICache()
        {
            {
                // Show Gigabytes to the user.
                const int kMinSizeInGigabytes = 5;
                const int kMaxSizeInGigabytes = 200;

                // Write size in GigaBytes.
                m_GICacheSettings.m_MaximumSize = EditorGUILayout.IntSlider(Styles.maxCacheSize, m_GICacheSettings.m_MaximumSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);
                WritePreferences();
            }
            GUILayout.BeginHorizontal();
            {
                if (Lightmapping.isRunning)
                {
                    GUIContent warning = EditorGUIUtility.TextContent(Styles.cantChangeCacheSettings.text);
                    EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                }
            }
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(Lightmapping.isRunning))
            {
                m_GICacheSettings.m_EnableCustomPath = EditorGUILayout.Toggle(Styles.customCacheLocation, m_GICacheSettings.m_EnableCustomPath);

                // browse for cache folder if not per project
                if (m_GICacheSettings.m_EnableCustomPath)
                {
                    GUIStyle style = EditorStyles.miniButton;
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(Styles.cacheFolderLocation, style);
                    Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                    GUIContent guiText = string.IsNullOrEmpty(m_GICacheSettings.m_CachePath) ? Styles.browse : new GUIContent(m_GICacheSettings.m_CachePath);
                    if (EditorGUI.DropdownButton(r, guiText, FocusType.Passive, style))
                    {
                        string pathToOpen = m_GICacheSettings.m_CachePath;
                        string path = EditorUtility.OpenFolderPanel(Styles.browseGICacheLocation.text, pathToOpen, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            m_GICacheSettings.m_CachePath = path;
                            WritePreferences();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                    m_GICacheSettings.m_CachePath = "";

                // We use toggle for now, 0 means kCompressionLevelNone, 1 - kCompressionLevelFastest.
                m_GICacheSettings.m_CompressionLevel = EditorGUILayout.Toggle(Styles.cacheCompression, m_GICacheSettings.m_CompressionLevel == 1) ? 1 : 0;

                if (GUILayout.Button(Styles.cleanCache, GUILayout.Width(120)))
                {
                    EditorUtility.DisplayProgressBar(Styles.cleanCache.text, Styles.pleaseWait.text, 0.0F);
                    Lightmapping.Clear();
                    EditorUtility.DisplayProgressBar(Styles.cleanCache.text, Styles.pleaseWait.text, 0.5F);
                    UnityEditor.Lightmapping.ClearDiskCache();
                    EditorUtility.ClearProgressBar();
                }

                if (UnityEditor.Lightmapping.diskCacheSize >= 0)
                    GUILayout.Label(Styles.cacheSizeIs.text + " " + EditorUtility.FormatBytes(UnityEditor.Lightmapping.diskCacheSize));
                else
                    GUILayout.Label(Styles.cacheSizeIs.text + " is being calculated...");

                GUILayout.Label(Styles.cacheFolderLocation.text + ":");
                GUILayout.Label(UnityEditor.Lightmapping.diskCachePath, constants.cacheFolderLocation);
            }
        }

        private void ShowLanguage()
        {
            var enable_localization = EditorGUILayout.Toggle(Styles.editorLanguageExperimental, m_EnableEditorLocalization);
            if (enable_localization != m_EnableEditorLocalization)
            {
                m_EnableEditorLocalization = enable_localization;
                m_SelectedLanguage = LocalizationDatabase.GetDefaultEditorLanguage().ToString();
            }

            EditorGUI.BeginDisabledGroup(!m_EnableEditorLocalization);
            {
                SystemLanguage[] editorLanguages = LocalizationDatabase.GetAvailableEditorLanguages();

                int idx = 0;
                for (int i = 0; i < editorLanguages.Length; i++)
                {
                    if (editorLanguages[i].ToString().Equals(m_SelectedLanguage))
                    {
                        idx = k_LangListMenuOffset + i;
                        break;
                    }
                }

                int sel = EditorGUILayout.Popup(Styles.editorLanguage, idx, m_EditorLanguageNames);
                m_SelectedLanguage = (sel == 0) ? LocalizationDatabase.GetDefaultEditorLanguage().ToString() :
                    editorLanguages[sel - k_LangListMenuOffset].ToString();
            }
            EditorGUI.EndDisabledGroup();

            if (!m_SelectedLanguage.Equals(LocalizationDatabase.currentEditorLanguage.ToString()))
            {
                SystemLanguage lang = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), m_SelectedLanguage);
                EditorGUIUtility.NotifyLanguageChanged(lang);
                InternalEditorUtility.RequestScriptReload();
            }

            ApplyChangesToPrefs();
        }


        private void WriteRecentAppsList(string[] paths, string path, string prefsKey)
        {
            int appIndex = 0;
            // first write the selected app (if it's not a built-in one)
            if (path.Length != 0)
            {
                EditorPrefs.SetString(prefsKey + appIndex, path);
                ++appIndex;
            }
            // write the other apps
            for (int i = 0; i < paths.Length; ++i)
            {
                if (appIndex >= kRecentAppsCount)
                    break; // stop when we wrote up to the limit
                if (paths[i].Length == 0)
                    continue; // do not write built-in app into recently used list
                if (paths[i] == path)
                    continue; // this is a selected app, do not write it twice
                EditorPrefs.SetString(prefsKey + appIndex, paths[i]);
                ++appIndex;
            }
        }

        private void WritePreferences()
        {
            ScriptEditorUtility.SetExternalScriptEditor(m_ScriptEditorPath);
            ScriptEditorUtility.SetExternalScriptEditorArgs(m_ScriptEditorArgs);
            EditorPrefs.SetBool("kExternalEditorSupportsUnityProj", m_ExternalEditorSupportsUnityProj);

            EditorPrefs.SetString("kImagesDefaultApp", m_ImageAppPath);
            EditorPrefs.SetString("kDiffsDefaultApp", m_DiffTools.Length == 0 ? "" : m_DiffTools[m_DiffToolIndex]);

            WriteRecentAppsList(m_ScriptApps, m_ScriptEditorPath, kRecentScriptAppsKey);
            WriteRecentAppsList(m_ImageApps, m_ImageAppPath, kRecentImageAppsKey);

            EditorPrefs.SetBool("kAutoRefresh", m_AutoRefresh);

            if (Unsupported.IsDeveloperMode() || UnityConnect.preferencesEnabled)
                UnityConnectPrefs.StorePanelPrefs();

            EditorPrefs.SetBool("ReopenLastUsedProjectOnStartup", m_ReopenLastUsedProjectOnStartup);
            EditorPrefs.SetBool("UseOSColorPicker", m_UseOSColorPicker);
            EditorPrefs.SetBool("EnableEditorAnalytics", m_EnableEditorAnalytics);
            EditorPrefs.SetBool("ShowAssetStoreSearchHits", m_ShowAssetStoreSearchHits);
            EditorPrefs.SetBool("VerifySavingAssets", m_VerifySavingAssets);

            // The Preferences window always writes all preferences, we don't want this behavior since we
            // want the default value to just match "IsSourceBuild" until the developer has explicitly changed it.
            if (m_DeveloperModeDirty)
            {
                EditorPrefs.SetBool("DeveloperMode", m_DeveloperMode);

                // Repaint all views to show/hide debug repaint indicator
                InternalEditorUtility.RepaintAllViews();
            }

            EditorPrefs.SetBool("AllowAttachedDebuggingOfEditor", m_AllowAttachedDebuggingOfEditor);

            EditorPrefs.SetBool("Editor.kEnableEditorLocalization", m_EnableEditorLocalization);
            EditorPrefs.SetString("Editor.kEditorLocale", m_SelectedLanguage.ToString());

            EditorPrefs.SetBool("AllowAlphaNumericHierarchy", m_AllowAlphaNumericHierarchy);
            EditorPrefs.SetString("GpuDevice", m_GpuDevice);

            EditorPrefs.SetBool("GICacheEnableCustomPath", m_GICacheSettings.m_EnableCustomPath);
            EditorPrefs.SetInt("GICacheMaximumSizeGB", m_GICacheSettings.m_MaximumSize);
            EditorPrefs.SetString("GICacheFolder", m_GICacheSettings.m_CachePath);
            EditorPrefs.SetInt("GICacheCompressionLevel", m_GICacheSettings.m_CompressionLevel);

            EditorPrefs.SetInt("SpritePackerCacheMaximumSizeGB", m_SpriteAtlasCacheSize);

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                extension.WritePreferences();
            }
            UnityEditor.Lightmapping.UpdateCachePath();
        }

        static private void SetupDefaultPreferences()
        {
        }

        static private string GetProgramFilesFolder()
        {
            string result = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (result != null) return result;
            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private void ReadPreferences()
        {
            m_ScriptEditorPath.str = ScriptEditorUtility.GetExternalScriptEditor();
            m_ScriptEditorArgs = ScriptEditorUtility.GetExternalScriptEditorArgs();

            m_ExternalEditorSupportsUnityProj = EditorPrefs.GetBool("kExternalEditorSupportsUnityProj", false);
            m_ImageAppPath.str = EditorPrefs.GetString("kImagesDefaultApp");

            m_ScriptApps = BuildAppPathList(m_ScriptEditorPath, kRecentScriptAppsKey, "internal");
            m_ScriptAppsEditions = new string[m_ScriptApps.Length];

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                foreach (var vsPaths in SyncVS.InstalledVisualStudios.Values)
                    foreach (var vsPath in vsPaths)
                    {
                        int index = Array.IndexOf(m_ScriptApps, vsPath.Path);
                        if (index == -1)
                        {
                            ArrayUtility.Add(ref m_ScriptApps, vsPath.Path);
                            ArrayUtility.Add(ref m_ScriptAppsEditions, vsPath.Edition);
                        }
                        else
                        {
                            m_ScriptAppsEditions[index] = vsPath.Edition;
                        }
                    }
            }

            var foundScriptEditorPaths = ScriptEditorUtility.GetFoundScriptEditorPaths(Application.platform);

            foreach (var scriptEditorPath in foundScriptEditorPaths)
            {
                ArrayUtility.Add(ref m_ScriptApps, scriptEditorPath);
                ArrayUtility.Add(ref m_ScriptAppsEditions, null);
            }

            m_ImageApps = BuildAppPathList(m_ImageAppPath, kRecentImageAppsKey, "");

            m_ScriptAppDisplayNames = BuildFriendlyAppNameList(m_ScriptApps, m_ScriptAppsEditions,
                    "Open by file extension");

            m_ImageAppDisplayNames = BuildFriendlyAppNameList(m_ImageApps, null,
                    L10n.Tr("Open by file extension"));

            m_DiffTools = InternalEditorUtility.GetAvailableDiffTools();

            // only show warning if has team license
            if ((m_DiffTools == null || m_DiffTools.Length == 0) && InternalEditorUtility.HasTeamLicense())
            {
                m_noDiffToolsMessage = InternalEditorUtility.GetNoDiffToolsDetectedMessage();
            }

            string diffTool = EditorPrefs.GetString("kDiffsDefaultApp");
            m_DiffToolIndex = ArrayUtility.IndexOf(m_DiffTools, diffTool);
            if (m_DiffToolIndex == -1)
                m_DiffToolIndex = 0;

            m_AutoRefresh = EditorPrefs.GetBool("kAutoRefresh");

            m_ReopenLastUsedProjectOnStartup = EditorPrefs.GetBool("ReopenLastUsedProjectOnStartup");

            m_UseOSColorPicker = EditorPrefs.GetBool("UseOSColorPicker");
            m_EnableEditorAnalytics = EditorPrefs.GetBool("EnableEditorAnalytics", true);
            m_ShowAssetStoreSearchHits = EditorPrefs.GetBool("ShowAssetStoreSearchHits", true);
            m_VerifySavingAssets = EditorPrefs.GetBool("VerifySavingAssets", false);
            m_DeveloperMode = Unsupported.IsDeveloperMode();

            m_GICacheSettings.m_EnableCustomPath = EditorPrefs.GetBool("GICacheEnableCustomPath");
            m_GICacheSettings.m_CachePath = EditorPrefs.GetString("GICacheFolder");
            m_GICacheSettings.m_MaximumSize = EditorPrefs.GetInt("GICacheMaximumSizeGB", 10);
            m_GICacheSettings.m_CompressionLevel = EditorPrefs.GetInt("GICacheCompressionLevel");

            m_SpriteAtlasCacheSize = EditorPrefs.GetInt("SpritePackerCacheMaximumSizeGB");

            m_AllowAttachedDebuggingOfEditor = EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true);
            m_EnableEditorLocalization = EditorPrefs.GetBool("Editor.kEnableEditorLocalization", true);
            m_SelectedLanguage = EditorPrefs.GetString("Editor.kEditorLocale", LocalizationDatabase.GetDefaultEditorLanguage().ToString());
            m_AllowAlphaNumericHierarchy = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);

            m_CompressAssetsOnImport = Unsupported.GetApplicationSettingCompressAssetsOnImport();
            m_GpuDevice = EditorPrefs.GetString("GpuDevice");

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                extension.ReadPreferences();
            }
        }

        private string StripMicrosoftFromVisualStudioName(string arg)
        {
            if (!arg.Contains("Visual Studio"))
                return arg;
            if (!arg.StartsWith("Microsoft"))
                return arg;
            return arg.Substring("Microsoft ".Length);
        }

        class AppsListUserData
        {
            public AppsListUserData(string[] paths, RefString str, Action onChanged)
            {
                this.paths = paths;
                this.str = str;
                this.onChanged = onChanged;
            }

            public string[] paths;
            public RefString str;
            public Action onChanged;
        }

        void AppsListClick(object userData, string[] options, int selected)
        {
            AppsListUserData ud = (AppsListUserData)userData;
            if (options[selected] == "Browse...")
            {
                string path = EditorUtility.OpenFilePanel("Browse for application", "", InternalEditorUtility.GetApplicationExtensionForRuntimePlatform(Application.platform));
                if (path.Length != 0)
                {
                    // browsed to new application
                    ud.str.str = path;
                    if (ud.onChanged != null)
                        ud.onChanged();
                }
            }
            else
            {
                // value comes from the list
                ud.str.str = ud.paths[selected];
                if (ud.onChanged != null)
                    ud.onChanged();
            }

            WritePreferences();
            ReadPreferences();
        }

        private void FilePopup(GUIContent label, string selectedString, ref string[] names, ref string[] paths, RefString outString, string defaultString, Action onChanged)
        {
            GUIStyle style = EditorStyles.popup;
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label, style);

            int[] selected = new int[0];
            if (paths.Contains(selectedString))
                selected = new[] { Array.IndexOf(paths, selectedString) };
            GUIContent text = new GUIContent(selected.Length == 0 ? defaultString : names[selected[0]]);
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
            AppsListUserData ud = new AppsListUserData(paths, outString, onChanged);
            if (EditorGUI.DropdownButton(r, text, FocusType.Passive, style))
            {
                ArrayUtility.Add(ref names, Styles.browse.text);
                EditorUtility.DisplayCustomMenu(r, names, selected, AppsListClick, ud, false);
            }
            GUILayout.EndHorizontal();
        }

        private string[] BuildAppPathList(string userAppPath, string recentAppsKey, string stringForInternalEditor)
        {
            // built-in (internal) is always the first
            string[] apps = new string[1];
            apps[0] = stringForInternalEditor;

            // current user setting
            if (userAppPath != null && userAppPath.Length != 0 && Array.IndexOf(apps, userAppPath) == -1)
                ArrayUtility.Add(ref apps, userAppPath);

            // add any recently used apps
            for (int i = 0; i < kRecentAppsCount; ++i)
            {
                string path = EditorPrefs.GetString(recentAppsKey + i);
                if (!File.Exists(path))
                {
                    path = "";
                    EditorPrefs.SetString(recentAppsKey + i, path);
                }

                if (path.Length != 0 && Array.IndexOf(apps, path) == -1)
                    ArrayUtility.Add(ref apps, path);
            }

            return apps;
        }

        private string[] BuildFriendlyAppNameList(string[] appPathList, string[] appEditionList, string defaultBuiltIn)
        {
            var list = new List<string>();
            for (int i = 0; i < appPathList.Length; ++i)
            {
                var appPath = appPathList[i];

                if (appPath == "internal" || appPath == "") // use built-in
                    list.Add(defaultBuiltIn);
                else
                {
                    var friendlyName = StripMicrosoftFromVisualStudioName(OSUtil.GetAppFriendlyName(appPath));

                    if (appEditionList != null && !string.IsNullOrEmpty(appEditionList[i]))
                        friendlyName = string.Format("{0} ({1})", friendlyName, appEditionList[i]);

                    list.Add(friendlyName);
                }
            }

            return list.ToArray();
        }
    }
}
