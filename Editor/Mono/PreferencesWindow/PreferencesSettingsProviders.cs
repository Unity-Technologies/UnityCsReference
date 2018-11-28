// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Modules;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Connect;
using UnityEditor.ShortcutManagement;

using UnityEditor.Collaboration;

namespace UnityEditor
{
    internal class PreferencesProvider : SettingsProvider
    {
        internal static class Constants
        {
            public static GUIStyle sectionScrollView = "PreferencesSectionBox";
            public static GUIStyle settingsBoxTitle = "OL Title";
            public static GUIStyle settingsBox = "OL Box";
            public static GUIStyle errorLabel = "WordWrappedLabel";
            public static GUIStyle sectionElement = "PreferencesSection";
            public static GUIStyle evenRow = "CN EntryBackEven";
            public static GUIStyle oddRow = "CN EntryBackOdd";
            public static GUIStyle selected = "OL SelectedRow";
            public static GUIStyle keysElement = "PreferencesKeysElement";
            public static GUIStyle warningIcon = "CN EntryWarn";
            public static GUIStyle cacheFolderLocation = "CacheFolderLocation";
        }

        internal class Styles
        {
            public static readonly GUIContent browse = EditorGUIUtility.TrTextContent("Browse...");
            public static readonly GUIStyle clearBindingButton = new GUIStyle(GUI.skin.button);

            static Styles()
            {
                clearBindingButton.margin.top = 0;
            }
        }

        internal class GeneralProperties
        {
            public static readonly GUIContent autoRefresh = EditorGUIUtility.TrTextContent("Auto Refresh");
            public static readonly GUIContent autoRefreshHelpBox = EditorGUIUtility.TrTextContent("Auto Refresh must be set when using Collaboration feature.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent loadPreviousProjectOnStartup = EditorGUIUtility.TrTextContent("Load Previous Project on Startup");
            public static readonly GUIContent compressAssetsOnImport = EditorGUIUtility.TrTextContent("Compress Assets on Import");
            public static readonly GUIContent osxColorPicker = EditorGUIUtility.TrTextContent("macOS Color Picker");
            public static readonly GUIContent disableEditorAnalytics = EditorGUIUtility.TrTextContent("Disable Editor Analytics (Pro Only)");
            public static readonly GUIContent showAssetStoreSearchHits = EditorGUIUtility.TrTextContent("Show Asset Store search hits");
            public static readonly GUIContent verifySavingAssets = EditorGUIUtility.TrTextContent("Verify Saving Assets");
            public static readonly GUIContent scriptChangesDuringPlay = EditorGUIUtility.TrTextContent("Script Changes While Playing");
            public static readonly GUIContent editorSkin = EditorGUIUtility.TrTextContent("Editor Skin");
            public static readonly GUIContent[] editorSkinOptions = { EditorGUIUtility.TrTextContent("Personal"), EditorGUIUtility.TrTextContent("Professional") };
            public static readonly GUIContent enableAlphaNumericSorting = EditorGUIUtility.TrTextContent("Enable Alpha Numeric Sorting");
        }

        internal class ExternalProperties
        {
            public static readonly GUIContent addUnityProjeToSln = EditorGUIUtility.TrTextContent("Add .unityproj's to .sln");
            public static readonly GUIContent editorAttaching = EditorGUIUtility.TrTextContent("Editor Attaching");
            public static readonly GUIContent changingThisSettingRequiresRestart = EditorGUIUtility.TrTextContent("Changing this setting requires a restart to take effect.");
            public static readonly GUIContent revisionControlDiffMerge = EditorGUIUtility.TrTextContent("Revision Control Diff/Merge");
            public static readonly GUIContent externalScriptEditor = EditorGUIUtility.TrTextContent("External Script Editor");
            public static readonly GUIContent imageApplication = EditorGUIUtility.TrTextContent("Image application");
        }

        internal class ColorsProperties
        {
            public static readonly GUIContent userDefaults = EditorGUIUtility.TrTextContent("Use Defaults");
        }

        internal class GICacheProperties
        {
            public static readonly GUIContent maxCacheSize = EditorGUIUtility.TrTextContent("Maximum Cache Size (GB)", "The size of the GI Cache folder will be kept below this maximum value when possible. A background job will periodically clean up the oldest unused files.");
            public static readonly GUIContent customCacheLocation = EditorGUIUtility.TrTextContent("Custom cache location", "Specify the GI Cache folder location.");
            public static readonly GUIContent cacheFolderLocation = EditorGUIUtility.TrTextContent("Cache Folder Location", "The GI Cache folder is shared between all projects.");
            public static readonly GUIContent cacheCompression = EditorGUIUtility.TrTextContent("Cache compression", "Use fast realtime compression for the GI cache files to reduce the size of generated data. Disable it and clean the cache if you need access to the raw data generated by Enlighten.");
            public static readonly GUIContent cantChangeCacheSettings = EditorGUIUtility.TrTextContent("Cache settings can't be changed while lightmapping is being computed.");
            public static readonly GUIContent cleanCache = EditorGUIUtility.TrTextContent("Clean Cache");
            public static readonly GUIContent browseGICacheLocation = EditorGUIUtility.TrTextContent("Browse for GI Cache location");
            public static readonly GUIContent cacheSizeIs = EditorGUIUtility.TrTextContent("Cache size is");
            public static readonly GUIContent pleaseWait = EditorGUIUtility.TrTextContent("Please wait...");
        }

        internal class TwoDProperties
        {
            public static readonly GUIContent spriteMaxCacheSize = EditorGUIUtility.TrTextContent("Max Sprite Atlas Cache Size (GB)", "The size of the Sprite Atlas Cache folder will be kept below this maximum value when possible. Change requires Editor restart");
        }

        internal class LanguageProperties
        {
            public static readonly GUIContent editorLanguageExperimental = EditorGUIUtility.TrTextContent("Editor Language(Experimental)");
            public static readonly GUIContent editorLanguage = EditorGUIUtility.TrTextContent("Editor language");
        }

        private List<IPreferenceWindowExtension> prefWinExtensions;
        private bool m_AutoRefresh;

        private bool m_ReopenLastUsedProjectOnStartup;
        private bool m_CompressAssetsOnImport;
        private bool m_UseOSColorPicker;
        private bool m_EnableEditorAnalytics;
        private bool m_ShowAssetStoreSearchHits;
        private bool m_VerifySavingAssets;
        private ScriptChangesDuringPlayOptions m_ScriptCompilationDuringPlay;
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
        private static GUIContent[] m_EditorLanguageNames;
        private bool m_EnableEditorLocalization;
        private static SystemLanguage[] m_stableLanguages = { SystemLanguage.English };

        private bool m_AllowAlphaNumericHierarchy = false;

        private string[] m_ScriptApps;
        private string[] m_ScriptAppsEditions;
        private string[] m_ImageApps;
        private string[] m_DiffTools;

        private string m_noDiffToolsMessage = string.Empty;

        private string[] m_ScriptAppDisplayNames;
        private string[] m_ImageAppDisplayNames;
        Vector2 m_KeyScrollPos;
        int m_SelectedShortcut = -1;
        private const string kRecentScriptAppsKey = "RecentlyUsedScriptApp";
        private const string kRecentImageAppsKey = "RecentlyUsedImageApp";

        private static readonly string k_ExpressNotSupportedMessage = L10n.Tr(
            "Unfortunately Visual Studio Express does not allow itself to be controlled by external applications. " +
            "You can still use it by manually opening the Visual Studio project file, but Unity cannot automatically open files for you when you doubleclick them. " +
            "\n(This does work with Visual Studio Pro)"
        );
        private static readonly string k_KeyCollisionFormat = L10n.Tr("Key {0} can't be used for action \"{1}\" because it's already used for action \"{2}\"");

        private const int kRecentAppsCount = 10;

        SortedDictionary<string, List<KeyValuePair<string, PrefColor>>> s_CachedColors = null;

        private int m_SpriteAtlasCacheSize;
        private static int kMinSpriteCacheSizeInGigabytes = 1;
        private static int kMaxSpriteCacheSizeInGigabytes = 200;

        private bool m_ValidKeyChange = true;
        private string m_InvalidKeyMessage = string.Empty;
        private static readonly string[] kShortcutIdentifierSplitters = { Identifier.kPathSeparator };

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

        public PreferencesProvider(string path, IEnumerable<string> keywords = null)
            : base(path, SettingsScope.User, keywords)
        {
            prefWinExtensions = ModuleManager.GetPreferenceWindowExtensions();
            ReadPreferences();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateGeneralProvider()
        {
            var settings = new PreferencesProvider("Preferences/_General", GetSearchKeywordsFromGUIContentProperties<GeneralProperties>()) { label = "General" };
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowGeneral); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateExternalToolsProvider()
        {
            var settings = new PreferencesProvider("Preferences/External Tools", GetSearchKeywordsFromGUIContentProperties<ExternalProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowExternalApplications); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateColorsProvider()
        {
            var settings = new PreferencesProvider("Preferences/Colors", GetSearchKeywordsFromGUIContentProperties<ColorsProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowColors); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateGICacheProvider()
        {
            var settings = new PreferencesProvider("Preferences/GI Cache", GetSearchKeywordsFromGUIContentProperties<GICacheProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowGICache); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider Create2DProvider()
        {
            var settings = new PreferencesProvider("Preferences/2D", GetSearchKeywordsFromGUIContentProperties<TwoDProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.Show2D); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateKeysProvider()
        {
            var settings = new PreferencesProvider("Preferences/Keys");
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowShortcuts); };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateLanguagesProvider()
        {
            var editorLanguages = LocalizationDatabase.GetAvailableEditorLanguages();
            if (m_EditorLanguageNames == null || m_EditorLanguageNames.Length != editorLanguages.Length)
            {
                m_EditorLanguageNames = new GUIContent[editorLanguages.Length];

                for (int i = 0; i < editorLanguages.Length; ++i)
                {
                    // not in stable languages list - display it as experimental language
                    if (ArrayUtility.FindIndex(m_stableLanguages, v => v == editorLanguages[i]) < 0)
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
                var settings = new PreferencesProvider("Preferences/Languages");
                settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowLanguage); };
                return settings;
            }

            return null;
        }


        [SettingsProvider]
        internal static SettingsProvider CreateUnityServicesProvider()
        {
            if (Unsupported.IsDeveloperMode() || UnityConnect.preferencesEnabled)
            {
                var settings = new PreferencesProvider("Preferences/Unity Services");
                settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowUnityConnectPrefs); };
                return settings;
            }
            return null;
        }

        // Group Preference sections with the same name
        private static void OnGUI(string searchContext, Action<string> drawAction)
        {
            using (new SettingsWindow.GUIScope())
                drawAction(searchContext);
        }

        private void ShowExternalApplications(string searchContext)
        {
            // Applications
            FilePopup(ExternalProperties.externalScriptEditor, m_ScriptEditorPath, ref m_ScriptAppDisplayNames, ref m_ScriptApps, m_ScriptEditorPath, "internal", OnScriptEditorChanged);

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
            m_AllowAttachedDebuggingOfEditor = EditorGUILayout.Toggle(ExternalProperties.editorAttaching, m_AllowAttachedDebuggingOfEditor);

            if (oldValue != m_AllowAttachedDebuggingOfEditor)
                m_AllowAttachedDebuggingOfEditorStateChangedThisSession = true;

            if (m_AllowAttachedDebuggingOfEditorStateChangedThisSession)
                GUILayout.Label(ExternalProperties.changingThisSettingRequiresRestart, EditorStyles.helpBox);

            if (GetSelectedScriptEditor() == ScriptEditorUtility.ScriptEditor.VisualStudioExpress)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("", Constants.warningIcon);
                GUILayout.Label(k_ExpressNotSupportedMessage, Constants.errorLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);

            FilePopup(ExternalProperties.imageApplication, m_ImageAppPath, ref m_ImageAppDisplayNames, ref m_ImageApps, m_ImageAppPath, "internal", null);

            GUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(!InternalEditorUtility.HasTeamLicense()))
            {
                m_DiffToolIndex = EditorGUILayout.Popup(ExternalProperties.revisionControlDiffMerge, m_DiffToolIndex, m_DiffTools);
            }

            if (m_noDiffToolsMessage != string.Empty)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("", Constants.warningIcon);
                GUILayout.Label(m_noDiffToolsMessage, Constants.errorLabel);
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
                value = EditorGUILayout.Toggle(ExternalProperties.addUnityProjeToSln, value);
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

        private void ShowUnityConnectPrefs(string searchContext)
        {
            UnityConnectPrefs.ShowPanelPrefUI();
            ApplyChangesToPrefs();
        }

        private void ShowGeneral(string searchContext)
        {
            // Options
            bool collabEnabled = Collab.instance.IsCollabEnabledForCurrentProject();
            using (new EditorGUI.DisabledScope(collabEnabled))
            {
                if (collabEnabled)
                {
                    EditorGUILayout.Toggle(GeneralProperties.autoRefresh, true);               // Don't keep toggle value in m_AutoRefresh since we don't want to save the overwritten value
                    EditorGUILayout.HelpBox(GeneralProperties.autoRefreshHelpBox);
                }
                else
                    m_AutoRefresh = EditorGUILayout.Toggle(GeneralProperties.autoRefresh, m_AutoRefresh);
            }

            m_ReopenLastUsedProjectOnStartup = EditorGUILayout.Toggle(GeneralProperties.loadPreviousProjectOnStartup, m_ReopenLastUsedProjectOnStartup);

            bool oldCompressOnImport = m_CompressAssetsOnImport;
            m_CompressAssetsOnImport = EditorGUILayout.Toggle(GeneralProperties.compressAssetsOnImport, oldCompressOnImport);

            if (GUI.changed && m_CompressAssetsOnImport != oldCompressOnImport)
                Unsupported.SetApplicationSettingCompressAssetsOnImport(m_CompressAssetsOnImport);

            if (Application.platform == RuntimePlatform.OSXEditor)
                m_UseOSColorPicker = EditorGUILayout.Toggle(GeneralProperties.osxColorPicker, m_UseOSColorPicker);

            bool pro = UnityEngine.Application.HasProLicense();
            using (new EditorGUI.DisabledScope(!pro))
            {
                m_EnableEditorAnalytics = !EditorGUILayout.Toggle(GeneralProperties.disableEditorAnalytics, !m_EnableEditorAnalytics) || !pro && !m_EnableEditorAnalytics;
            }

            bool assetStoreSearchChanged = false;
            EditorGUI.BeginChangeCheck();
            m_ShowAssetStoreSearchHits = EditorGUILayout.Toggle(GeneralProperties.showAssetStoreSearchHits, m_ShowAssetStoreSearchHits);
            if (EditorGUI.EndChangeCheck())
                assetStoreSearchChanged = true;

            m_VerifySavingAssets = EditorGUILayout.Toggle(GeneralProperties.verifySavingAssets, m_VerifySavingAssets);

            m_ScriptCompilationDuringPlay = (ScriptChangesDuringPlayOptions)EditorGUILayout.EnumPopup(GeneralProperties.scriptChangesDuringPlay, m_ScriptCompilationDuringPlay);

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
                int newSkin = EditorGUILayout.Popup(GeneralProperties.editorSkin, !EditorGUIUtility.isProSkin ? 0 : 1, GeneralProperties.editorSkinOptions);
                if ((!EditorGUIUtility.isProSkin ? 0 : 1) != newSkin)
                    InternalEditorUtility.SwitchSkinAndRepaintAllViews();
            }

            bool oldAlphaNumeric = m_AllowAlphaNumericHierarchy;
            m_AllowAlphaNumericHierarchy = EditorGUILayout.Toggle(GeneralProperties.enableAlphaNumericSorting, m_AllowAlphaNumericHierarchy);

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
                var currentGpuDeviceIndex = Array.FindIndex(m_CachedGpuDevices, gpuDevice => m_GpuDevice == gpuDevice);
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
                settingsWindow.Repaint();
            }
        }

        private static void RevertShortcuts()
        {
            ShortcutIntegration.instance.profileManager.ResetToDefault();
        }

        private static void RevertPrefKeys()
        {
            foreach (KeyValuePair<string, PrefKey> kvp in PrefSettings.Prefs<PrefKey>())
            {
                kvp.Value.ResetToDefault();
                EditorPrefs.SetString(kvp.Value.Name, kvp.Value.ToUniqueString());
            }

            // Delete PrefKeys from EditorPrefs based on key given in FormerlyPrefKeyAs attributes
            foreach (var methodInfo in EditorAssemblies.GetAllMethodsWithAttribute<FormerlyPrefKeyAsAttribute>())
            {
                var attribute = (FormerlyPrefKeyAsAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(FormerlyPrefKeyAsAttribute));
                EditorPrefs.DeleteKey(attribute.name);
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
                    List<KeyValuePair<string, T>> inner = new List<KeyValuePair<string, T>> {new KeyValuePair<string, T>(second, kvp.Value)};
                    retval.Add(first, new List<KeyValuePair<string, T>>(inner));
                }
                else
                {
                    retval[first].Add(new KeyValuePair<string, T>(second, kvp.Value));
                }
            }

            return retval;
        }

        private readonly List<KeyValuePair<string, object>> m_SortedKeyEntries =
            new List<KeyValuePair<string, object>>();

        private object ShowShortcutsAndKeys(int controlID)
        {
            int current = 0;
            bool selected = false;
            ShortcutEntry currentShortcutEntry = null;
            PrefKey currentPrefKey = null;
            var shortcutProfileManager = ShortcutIntegration.instance.profileManager;
            IEnumerable<ShortcutEntry> shortcuts = shortcutProfileManager.GetAllShortcuts();
            if (!Unsupported.IsDeveloperMode())
                shortcuts = shortcuts.Where(shortcut => shortcut.type != ShortcutType.Menu);

            m_SortedKeyEntries.Clear();
            m_SortedKeyEntries.AddRange(
                shortcuts.Select(
                    s => new KeyValuePair<string, object>(s.identifier.path, s)
                )
            );
            m_SortedKeyEntries.AddRange(
                PrefSettings.Prefs<PrefKey>().Select(pk => new KeyValuePair<string, object>(pk.Key, pk.Value))
            );
            m_SortedKeyEntries.Sort((k1, k2) => k1.Key.CompareTo(k2.Key));
            foreach (var keyEntry in m_SortedKeyEntries)
            {
                ++current;
                selected = current == m_SelectedShortcut;
                bool legacy = typeof(PrefKey).IsAssignableFrom(keyEntry.Value.GetType());
                if (selected)
                {
                    if (legacy)
                        currentPrefKey = (PrefKey)keyEntry.Value;
                    else
                        currentShortcutEntry = (ShortcutEntry)keyEntry.Value;
                }

                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    if (GUILayout.Toggle(selected, keyEntry.Key, Constants.keysElement))
                    {
                        m_ValidKeyChange = !selected;
                        m_SelectedShortcut = current;
                    }

                    if (changeCheckScope.changed)
                    {
                        GUIUtility.keyboardControl = controlID;
                        m_InvalidKeyMessage = "";
                    }
                }
            }

            return currentShortcutEntry ?? (object)currentPrefKey;
        }

        static string BuildDescription(Event keyEvent)
        {
            var keyDescription = new StringBuilder();
            if (Application.platform == RuntimePlatform.OSXEditor && keyEvent.command)
                keyDescription.Append("Command+");
            if (keyEvent.control)
                keyDescription.Append("Ctrl+");
            if (keyEvent.shift)
                keyDescription.Append("Shift+");
            if (keyEvent.alt)
                keyDescription.Append("Alt+");
            keyDescription.Append(keyEvent.keyCode);
            return keyDescription.ToString();
        }

        private void ProcessArrowKeyInShortcutConfiguration()
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.UpArrow:
                    if (m_SelectedShortcut > 1)
                    {
                        --m_SelectedShortcut;
                        m_ValidKeyChange = true;
                    }

                    Event.current.Use();
                    break;
                case KeyCode.DownArrow:
                    if (m_SelectedShortcut < ShortcutIntegration.instance.profileManager.GetAllShortcuts().Count() + PrefSettings.Prefs<PrefKey>().Count())
                    {
                        m_SelectedShortcut++;
                        m_ValidKeyChange = true;
                    }

                    Event.current.Use();
                    break;
            }
        }

        private void CheckForCollisions(Event e, ShortcutEntry selectedShortcut, PrefKey selectedPrefKey)
        {
            ShortcutController shortcutController = ShortcutIntegration.instance;
            var collisions = new List<ShortcutEntry>();
            m_ValidKeyChange = true;
            m_InvalidKeyMessage = "";
            Type context = selectedShortcut?.context;
            string identifier = selectedShortcut != null
                ? selectedShortcut.identifier.path
                : selectedPrefKey.Name;


            // Check shortcuts
            shortcutController.directory.FindShortcutEntries(
                new List<KeyCombination> {KeyCombination.FromKeyboardInput(e)},
                new[] { context },
                collisions);

            if (collisions.Any() && !Equals(identifier, collisions[0].identifier.path))
            {
                m_ValidKeyChange = false;
                m_InvalidKeyMessage = string.Format(k_KeyCollisionFormat, BuildDescription(e), identifier, collisions[0].identifier);
                return;
            }

            // Check prefkeys
            string selectedToolName = identifier.Split('/')[0];

            // Setting the same key to the same action is ok.
            // Setting the same key to a different action from a different tool is ok too.
            KeyValuePair<string, PrefKey> collision = PrefSettings.Prefs<PrefKey>().FirstOrDefault(kvp =>
                kvp.Value.KeyboardEvent.Equals(e) &&
                kvp.Key.Split('/')[0] == selectedToolName && kvp.Key != identifier
            );

            if (collision.Key != null)
            {
                m_ValidKeyChange = false;
                m_InvalidKeyMessage = string.Format(k_KeyCollisionFormat, BuildDescription(e), identifier, collision.Key);
            }
        }

        private void ShowShortcutConfiguration(int controlID, ShortcutEntry selectedShortcut)
        {
            // TODO: chords
            Event e = selectedShortcut.combinations.FirstOrDefault().ToKeyboardEvent();
            GUI.changed = false;

            // FIXME: Are we going to support/enforce paths like this?
            foreach (var label in selectedShortcut.identifier.path.Split(kShortcutIdentifierSplitters, 2, StringSplitOptions.None))
                GUILayout.Label(label, EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Key:");
                e = EditorGUILayout.KeyEventField(e);

                if (GUILayout.Button("Clear", Styles.clearBindingButton))
                    e.keyCode = KeyCode.None;
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Modifiers:");
                using (new GUILayout.VerticalScope())
                {
                    ShortcutController shortcutController = ShortcutIntegration.instance;
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        e.command = GUILayout.Toggle(e.command, "Command");
                    else
                        e.control = GUILayout.Toggle(e.control, "Control");
                    e.shift = GUILayout.Toggle(e.shift, "Shift");
                    e.alt = GUILayout.Toggle(e.alt, "Alt");

                    if (GUI.changed)
                    {
                        CheckForCollisions(e, selectedShortcut, null);
                        if (m_ValidKeyChange)
                        {
                            // TODO: Don't clobber secondary+ combinations
                            var newCombination = new List<KeyCombination>();
                            if (e.keyCode != KeyCode.None)
                                newCombination.Add(KeyCombination.FromKeyboardInput(e));
                            shortcutController.profileManager.ModifyShortcutEntry(selectedShortcut.identifier, newCombination);
                            shortcutController.profileManager.PersistChanges();
                        }
                    }
                    else if (GUIUtility.keyboardControl == controlID && Event.current.type == EventType.KeyDown)
                        ProcessArrowKeyInShortcutConfiguration();
                }
            }
            if (!m_ValidKeyChange)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("", Constants.warningIcon);
                    GUILayout.Label(m_InvalidKeyMessage, Constants.errorLabel);
                }
            }
        }

        private void ShowPrefKeyConfiguration(int controlID, PrefKey selectedKey)
        {
            Event e = selectedKey.KeyboardEvent;
            GUI.changed = false;
            var splitKey = selectedKey.Name.Split('/');
            System.Diagnostics.Debug.Assert(splitKey.Length == 2, "Unexpected Split: " + selectedKey.Name);
            GUILayout.Label(splitKey[0], EditorStyles.boldLabel);
            GUILayout.Label(splitKey[1], EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Key:");
                e = EditorGUILayout.KeyEventField(e);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Modifiers:");
                using (new GUILayout.VerticalScope())
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        e.command = GUILayout.Toggle(e.command, "Command");
                    e.control = GUILayout.Toggle(e.control, "Control");
                    e.shift = GUILayout.Toggle(e.shift, "Shift");
                    e.alt = GUILayout.Toggle(e.alt, "Alt");

                    if (GUI.changed)
                    {
                        CheckForCollisions(e, null, selectedKey);
                        if (m_ValidKeyChange)
                        {
                            selectedKey.KeyboardEvent = e;
                            PrefSettings.Set(selectedKey.Name, selectedKey);
                        }
                    }
                    else if (GUIUtility.keyboardControl == controlID && Event.current.type == EventType.KeyDown)
                        ProcessArrowKeyInShortcutConfiguration();
                }
            }
            if (!m_ValidKeyChange)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("", Constants.warningIcon);
                    GUILayout.Label(m_InvalidKeyMessage, Constants.errorLabel);
                }
            }
        }

        static int s_KeysControlHash = "KeysControlHash".GetHashCode();
        private void ShowShortcuts(string searchContext)
        {
            // TODO: Some code duplication between Shortcut/PrefKey for now
            // Leaving it this way so that it can be easily removed when we kill PrefKey for 2018.3
            int id = GUIUtility.GetControlID(s_KeysControlHash, FocusType.Keyboard);

            using (new GUILayout.HorizontalScope())
            {
                object selectedItem = null;
                using (new GUILayout.VerticalScope(GUILayout.Width(250f)))
                {
                    GUILayout.Label("Actions", Constants.settingsBoxTitle, GUILayout.ExpandWidth(true));
                    using (var scrollViewScope = new GUILayout.ScrollViewScope(m_KeyScrollPos, Constants.settingsBox))
                    {
                        m_KeyScrollPos = scrollViewScope.scrollPosition;
                        selectedItem = ShowShortcutsAndKeys(id);
                    }
                }
                GUILayout.Space(10.0f);

                using (new GUILayout.VerticalScope())
                {
                    ShortcutEntry shortcut = selectedItem as ShortcutEntry;
                    if (shortcut != null)
                        ShowShortcutConfiguration(id, shortcut);
                    PrefKey prefKey = selectedItem as PrefKey;
                    if (prefKey != null)
                        ShowPrefKeyConfiguration(id, prefKey);
                }

                GUILayout.Space(10f);
            }
            GUILayout.Space(5f);

            if (GUILayout.Button(ColorsProperties.userDefaults, GUILayout.Width(120)))
            {
                m_ValidKeyChange = true;
                m_InvalidKeyMessage = "";
                RevertPrefKeys();
                RevertShortcuts();
            }
        }

        private void RevertColors()
        {
            foreach (KeyValuePair<string, PrefColor> kvp in PrefSettings.Prefs<PrefColor>())
            {
                kvp.Value.ResetToDefault();
                EditorPrefs.SetString(kvp.Value.Name, kvp.Value.ToUniqueString());
            }
        }

        private void ShowColors(string searchContext)
        {
            if (s_CachedColors == null)
            {
                s_CachedColors = OrderPrefs(PrefSettings.Prefs<PrefColor>());
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
                    PrefSettings.Set(ccolor.Name, ccolor);
            }
            GUILayout.Space(5f);

            if (GUILayout.Button(ColorsProperties.userDefaults, GUILayout.Width(120)))
            {
                RevertColors();
                changedColor = true;
            }

            if (changedColor)
                EditorApplication.RequestRepaintAllViews();
        }

        private void Show2D(string searchContext)
        {
            // 2D Settings.
            EditorGUI.BeginChangeCheck();

            m_SpriteAtlasCacheSize = EditorGUILayout.IntSlider(TwoDProperties.spriteMaxCacheSize, m_SpriteAtlasCacheSize, kMinSpriteCacheSizeInGigabytes, kMaxSpriteCacheSizeInGigabytes);
            if (EditorGUI.EndChangeCheck())
                WritePreferences();
        }

        private void ShowGICache(string searchContext)
        {
            {
                // Show Gigabytes to the user.
                const int kMinSizeInGigabytes = 5;
                const int kMaxSizeInGigabytes = 200;

                // Write size in GigaBytes.
                m_GICacheSettings.m_MaximumSize = EditorGUILayout.IntSlider(GICacheProperties.maxCacheSize, m_GICacheSettings.m_MaximumSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);
                WritePreferences();
            }
            GUILayout.BeginHorizontal();
            {
                if (Lightmapping.isRunning)
                {
                    GUIContent warning = EditorGUIUtility.TextContent(GICacheProperties.cantChangeCacheSettings.text);
                    EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                }
            }
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(Lightmapping.isRunning))
            {
                m_GICacheSettings.m_EnableCustomPath = EditorGUILayout.Toggle(GICacheProperties.customCacheLocation, m_GICacheSettings.m_EnableCustomPath);

                // browse for cache folder if not per project
                if (m_GICacheSettings.m_EnableCustomPath)
                {
                    GUIStyle style = EditorStyles.miniButton;
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(GICacheProperties.cacheFolderLocation, style);
                    Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                    GUIContent guiText = string.IsNullOrEmpty(m_GICacheSettings.m_CachePath) ? Styles.browse : new GUIContent(m_GICacheSettings.m_CachePath);
                    if (EditorGUI.DropdownButton(r, guiText, FocusType.Passive, style))
                    {
                        string pathToOpen = m_GICacheSettings.m_CachePath;
                        string path = EditorUtility.OpenFolderPanel(GICacheProperties.browseGICacheLocation.text, pathToOpen, "");
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
                m_GICacheSettings.m_CompressionLevel = EditorGUILayout.Toggle(GICacheProperties.cacheCompression, m_GICacheSettings.m_CompressionLevel == 1) ? 1 : 0;

                if (GUILayout.Button(GICacheProperties.cleanCache, GUILayout.Width(120)))
                {
                    EditorUtility.DisplayProgressBar(GICacheProperties.cleanCache.text, GICacheProperties.pleaseWait.text, 0.0F);
                    Lightmapping.Clear();
                    EditorUtility.DisplayProgressBar(GICacheProperties.cleanCache.text, GICacheProperties.pleaseWait.text, 0.5F);
                    UnityEditor.Lightmapping.ClearDiskCache();
                    EditorUtility.ClearProgressBar();
                }

                if (UnityEditor.Lightmapping.diskCacheSize >= 0)
                    GUILayout.Label(GICacheProperties.cacheSizeIs.text + " " + EditorUtility.FormatBytes(UnityEditor.Lightmapping.diskCacheSize));
                else
                    GUILayout.Label(GICacheProperties.cacheSizeIs.text + " is being calculated...");

                GUILayout.Label(GICacheProperties.cacheFolderLocation.text + ":");
                GUILayout.Label(UnityEditor.Lightmapping.diskCachePath, Constants.cacheFolderLocation);
            }
        }

        private void ShowLanguage(string searchContext)
        {
            var enable_localization = EditorGUILayout.Toggle(LanguageProperties.editorLanguageExperimental, m_EnableEditorLocalization);
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

                int sel = EditorGUILayout.Popup(LanguageProperties.editorLanguage, idx, m_EditorLanguageNames);
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
                    break;     // stop when we wrote up to the limit
                if (paths[i].Length == 0)
                    continue;     // do not write built-in app into recently used list
                if (paths[i] == path)
                    continue;     // this is a selected app, do not write it twice
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
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", (int)m_ScriptCompilationDuringPlay);

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
            EditorPrefs.SetString("Editor.kEditorLocale", m_SelectedLanguage);

            EditorPrefs.SetBool("AllowAlphaNumericHierarchy", m_AllowAlphaNumericHierarchy);
            EditorPrefs.SetString("GpuDeviceName", m_GpuDevice);

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

            foreach (var scriptEditorPath in foundScriptEditorPaths.Keys)
            {
                ArrayUtility.Add(ref m_ScriptApps, scriptEditorPath);
                ArrayUtility.Add(ref m_ScriptAppsEditions, null);
            }

            m_ImageApps = BuildAppPathList(m_ImageAppPath, kRecentImageAppsKey, "");

            m_ScriptAppDisplayNames = BuildFriendlyAppNameList(m_ScriptApps, m_ScriptAppsEditions, foundScriptEditorPaths,
                "Open by file extension");

            m_ImageAppDisplayNames = BuildFriendlyAppNameList(m_ImageApps, null, null,
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
            m_ScriptCompilationDuringPlay = (ScriptChangesDuringPlayOptions)EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0);
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
            m_GpuDevice = EditorPrefs.GetString("GpuDeviceName");

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
            if (options[selected] == L10n.Tr("Browse..."))
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
            if (!String.IsNullOrEmpty(userAppPath) && Array.IndexOf(apps, userAppPath) == -1)
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

        private string[] BuildFriendlyAppNameList(string[] appPathList, string[] appEditionList, Dictionary<string, string> appPathToName, string defaultBuiltIn)
        {
            var list = new List<string>();
            for (int i = 0; i < appPathList.Length; ++i)
            {
                var appPath = appPathList[i];

                if (appPath == "internal" || appPath == "")     // use built-in
                    list.Add(defaultBuiltIn);
                else
                {
                    var friendlyName = StripMicrosoftFromVisualStudioName(OSUtil.GetAppFriendlyName(appPath));

                    if (appEditionList != null && !string.IsNullOrEmpty(appEditionList[i]))
                        friendlyName = string.Format("{0} ({1})", friendlyName, appEditionList[i]);
                    else if (appPathToName != null && appPathToName.ContainsKey(appPath))
                        friendlyName = appPathToName[appPath];

                    list.Add(friendlyName);
                }
            }

            return list.ToArray();
        }
    }
}
