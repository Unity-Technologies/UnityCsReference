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
using JetBrains.Annotations;
using Unity.CodeEditor;
using UnityEditor.VisualStudioIntegration;
using UnityEditor.Connect;
using UnityEngine.UIElements;
using UnityEditor.Experimental;
using UnityEngine.TestTools;
using UnityEditor.Compilation;
using UnityEditor.Collaboration;

namespace UnityEditor
{
    internal class PreferencesProvider : SettingsProvider
    {
        internal enum CodeOptimization
        {
            Debug,
            Release
        }

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
            public static readonly GUIContent directoryMonitoring = EditorGUIUtility.TrTextContent("Directory Monitoring", "Monitor directories instead of periodically scanning all project files to detect asset changes.");
            public static readonly GUIContent autoRefreshHelpBox = EditorGUIUtility.TrTextContent("Auto Refresh must be set when using Collaboration feature.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent loadPreviousProjectOnStartup = EditorGUIUtility.TrTextContent("Load Previous Project on Startup");
            public static readonly GUIContent compressAssetsOnImport = EditorGUIUtility.TrTextContent("Compress Assets on Import");
            public static readonly GUIContent disableEditorAnalytics = EditorGUIUtility.TrTextContent("Disable Editor Analytics (Pro Only)");
            public static readonly GUIContent showAssetStoreSearchHits = EditorGUIUtility.TrTextContent("Show Asset Store search hits");
            public static readonly GUIContent verifySavingAssets = EditorGUIUtility.TrTextContent("Verify Saving Assets");
            public static readonly GUIContent scriptChangesDuringPlay = EditorGUIUtility.TrTextContent("Script Changes While Playing");
            public static readonly GUIContent editorFont = EditorGUIUtility.TrTextContent("Editor Font");
            public static readonly GUIContent editorSkin = EditorGUIUtility.TrTextContent("Editor Theme");
            public static readonly GUIContent[] editorSkinOptions = { EditorGUIUtility.TrTextContent("Personal"), EditorGUIUtility.TrTextContent("Professional") };
            public static readonly GUIContent enableAlphaNumericSorting = EditorGUIUtility.TrTextContent("Enable Alphanumeric Sorting");
            public static readonly GUIContent asyncShaderCompilation = EditorGUIUtility.TrTextContent("Asynchronous Shader Compilation");
            public static readonly GUIContent codeCoverageEnabled = EditorGUIUtility.TrTextContent("Enable Code Coverage", "Check this to enable Code Coverage. Code Coverage lets you see how much of your code is executed when it is run. Note that Code Coverage lowers Editor performance.");
            public static readonly GUIContent createObjectsAtWorldOrigin = EditorGUIUtility.TrTextContent("Create Objects at Origin", "Enable this preference to instantiate new 3D objects at World coordinates 0,0,0. Disable it to instantiate them at the Scene pivot (in front of the Scene view Camera).");
            public static readonly GUIContent applicationFrameThrottling = EditorGUIUtility.TrTextContent("Frame Throttling (milliseconds)", "The number of milliseconds the Editor can idle between frames.");
            public static readonly GUIContent interactionMode = EditorGUIUtility.TrTextContent("Interaction Mode", "Specifies how long the Editor can idle before it updates.");
            public static readonly GUIContent[] interactionModes =
            {
                EditorGUIUtility.TrTextContent("Default", "The Editor can idle up to 4 ms per frame."),
                EditorGUIUtility.TrTextContent("No Throttling", "The Editor does not idle. It runs as fast as possible."),
                EditorGUIUtility.TrTextContent("Monitor Refresh Rate", "The Editor can idle up to whatever the monitor's refresh rate is, in milliseconds."),
                EditorGUIUtility.TrTextContent("Custom", "You specify how many milliseconds per frame the Editor can idle."),
            };
            public static readonly GUIContent progressDialogDelay = EditorGUIUtility.TrTextContent("Busy Progress Delay", "Delay in seconds before 'Unity is busy' progress bar shows up.");
            public static readonly GUIContent enableSnapping = EditorGUIUtility.TrTextContent("Graph Snapping", "If enabled, GraphElements in Graph Views (such as Shader Graph) align with one another when you move them. If disabled, GraphElements move freely.");
        }

        internal class ExternalProperties
        {
            public static readonly GUIContent codeOptimizationOnStartup = EditorGUIUtility.TrTextContent("Code Optimization On Startup");
            public static readonly GUIContent changingThisSettingRequiresRestart = EditorGUIUtility.TrTextContent("Changing this setting requires a restart to take effect.");
            public static readonly GUIContent revisionControlDiffMerge = EditorGUIUtility.TrTextContent("Revision Control Diff/Merge");
            public static readonly GUIContent externalScriptEditor = EditorGUIUtility.TrTextContent("External Script Editor");
            public static readonly GUIContent imageApplication = EditorGUIUtility.TrTextContent("Image application");
        }

        internal class UIScalingProperties
        {
            public static readonly GUIContent editorContentScaling = EditorGUIUtility.TrTextContent("Editor icons and text scaling");
            public static readonly GUIContent defaultContentScaling = EditorGUIUtility.TrTextContent("Use default desktop setting");
            public static readonly GUIContent currentContentScaling = EditorGUIUtility.TrTextContent("Current scaling");
            public static readonly GUIContent customContentScaling = EditorGUIUtility.TrTextContent("Use custom scaling value");
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

        internal class SceneViewProperties
        {
            public static readonly GUIContent enableFilteringWhileSearching = EditorGUIUtility.TrTextContent("Enable filtering while searching", "If enabled, searching will cause non-matching items in the scene view to be greyed out");
            public static readonly GUIContent enableFilteringWhileLodGroupEditing = EditorGUIUtility.TrTextContent("Enable filtering while editing LOD groups", "If enabled, editing LOD groups will cause other objects in the scene view to be greyed out");
            public static readonly GUIContent handlesLineThickness = EditorGUIUtility.TrTextContent("Line Thickness", "Thickness of manipulator tool handle lines in UI points (0 = single pixel)");
        }

        internal class LanguageProperties
        {
            public static readonly GUIContent editorLanguageExperimental = EditorGUIUtility.TrTextContent("Editor Language (Experimental)");
            public static readonly GUIContent editorLanguage = EditorGUIUtility.TrTextContent("Editor language");
        }

        internal class DeveloperModeProperties
        {
            public static readonly GUIContent developerMode = EditorGUIUtility.TrTextContent("Developer Mode", "Enable or disable developer mode features.");
            public static readonly GUIContent showRepaintDots = EditorGUIUtility.TrTextContent("Show Repaint Dots", "Enable or disable the colored dots that flash when an EditorWindow repaints.");
        }

        private List<IPreferenceWindowExtension> prefWinExtensions;
        private bool m_AutoRefresh;
        private bool m_DirectoryMonitoring;

        private bool m_ReopenLastUsedProjectOnStartup;
        private bool m_CompressAssetsOnImport;
        private bool m_EnableEditorAnalytics;
        private bool m_ShowAssetStoreSearchHits;
        private bool m_VerifySavingAssets;
        private ScriptChangesDuringPlayOptions m_ScriptCompilationDuringPlay;
        private bool m_DeveloperMode;
        private bool m_ShowRepaintDots;
        private bool m_DeveloperModeDirty;
        private bool m_ScriptDebugInfoEnabled;
        private string m_GpuDeviceInUse;
        private string m_GpuDevice;
        private string[] m_CachedGpuDevices;
        private bool m_ContentScaleChangedThisSession;
        private int m_ContentScalePercentValue;
        private string[] m_CustomScalingLabels = {"100%", "125%", "150%", "175%", "200%", "225%", "250%", "300%", "350%"};
        private int[] m_CustomScalingValues = { 100, 125, 150, 175, 200, 225, 250, 300, 350 };

        private readonly string kContentScalePrefKey = "CustomEditorUIScale";

        private struct GICacheSettings
        {
            public bool m_EnableCustomPath;
            public int m_MaximumSize;
            public string m_CachePath;
            public int m_CompressionLevel; // GICache compression level, corresponds to CompressionLevel in Compression.h
        }
        private GICacheSettings m_GICacheSettings;

        private RefString m_ScriptEditorPath = new RefString("");
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
        private bool m_EnableCodeCoverage = false;
        private readonly string kCodeCoverageEnabledMessage = L10n.Tr("Code Coverage collection is enabled for this Unity session. Note that Code Coverage lowers Editor performance.");
        private bool m_Create3DObjectsAtOrigin = false;
        private float m_ProgressDialogDelay = 3.0f;
        private bool m_GraphSnapping;

        private string[] m_ScriptApps;
        private string[] m_ScriptAppsEditions;
        private string[] m_ImageApps;
        private string[] m_DiffTools;

        private string m_CustomDiffToolPath = "";
        private string[] m_CustomDiffToolArguments = new[] {"", "", ""};

        private string m_noDiffToolsMessage = string.Empty;

        private string[] m_ScriptAppDisplayNames;
        private string[] m_ImageAppDisplayNames;
        private const string kRecentScriptAppsKey = "RecentlyUsedScriptApp";
        private const string kRecentImageAppsKey = "RecentlyUsedImageApp";

        private static readonly string k_ExpressNotSupportedMessage = L10n.Tr(
            "Unfortunately Visual Studio Express does not allow itself to be controlled by external applications. " +
            "You can still use it by manually opening the Visual Studio project file, but Unity cannot automatically open files for you when you doubleclick them. " +
            "\n(This does work with Visual Studio Pro)"
        );

        private const int kRecentAppsCount = 10;

        SortedDictionary<string, List<KeyValuePair<string, PrefColor>>> s_CachedColors = null;

        private int m_SpriteAtlasCacheSize;
        private static int kMinSpriteCacheSizeInGigabytes = 1;
        private static int kMaxSpriteCacheSizeInGigabytes = 200;

        private List<GUIContent> m_SystemFonts = new List<GUIContent>();
        private const int k_browseButtonWidth = 80;

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
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            prefWinExtensions = ModuleManager.GetPreferenceWindowExtensions();
            ReadPreferences();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateGeneralProvider()
        {
            var settings = new PreferencesProvider("Preferences/_General", GetSearchKeywordsFromGUIContentProperties<GeneralProperties>()) { label = "General" };
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowGeneral); };
            settings.activateHandler = (searchContext, rootElement) => { settings.EnableGeneral(); };
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
            var settings = new PreferencesProvider("Preferences/Colors", GetSearchKeywordsFromGUIContentProperties<ColorsProperties>().Concat(OrderPrefs(PrefSettings.Prefs<PrefColor>()).Values.SelectMany(l => l).Select(pair => pair.Key)));
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowColors); };
            return settings;
        }

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider CreateGICacheProvider()
        {
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Slave && !UnityEditor.MPE.ProcessService.HasCapability("enable-gi"))
                return null;
            var settings = new PreferencesProvider("Preferences/GI Cache", GetSearchKeywordsFromGUIContentProperties<GICacheProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowGICache); };
            return settings;
        }

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider Create2DProvider()
        {
            var settings = new PreferencesProvider("Preferences/2D", GetSearchKeywordsFromGUIContentProperties<TwoDProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.Show2D); };
            return settings;
        }

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider CreateSceneViewProvider()
        {
            var settings = new PreferencesProvider("Preferences/Scene View", GetSearchKeywordsFromGUIContentProperties<SceneViewProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowSceneView); };
            return settings;
        }

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider CreateLanguagesProvider()
        {
            var editorLanguages = LocalizationDatabase.GetAvailableEditorLanguages();
            if (m_EditorLanguageNames == null || m_EditorLanguageNames.Length != editorLanguages.Length)
            {
                m_EditorLanguageNames = new GUIContent[editorLanguages.Length];

                for (int i = 0; i < editorLanguages.Length; ++i)
                {
                    var culture = LocalizationDatabase.GetCulture(editorLanguages[i]);
                    var langName = new System.Globalization.CultureInfo(culture).NativeName;

                    // Due to the issue 1088990, workaround for both Chinese is necessary.
                    // This workaround should be removed just after the fix.
                    if (editorLanguages[i] == SystemLanguage.ChineseSimplified)
                    {
                        byte[] letters = { 0xE7, 0xAE, 0x80, 0xE4, 0xBD, 0x93, 0xE4, 0xB8, 0xAD, 0xE6, 0x96, 0x87 };
                        langName = System.Text.Encoding.UTF8.GetString(letters);
                    }
                    else if (editorLanguages[i] == SystemLanguage.ChineseTraditional)
                    {
                        byte[] letters = { 0xE7, 0xB9, 0x81, 0xE9, 0xAB, 0x94, 0xE4, 0xB8, 0xAD, 0xE6, 0x96, 0x87 };
                        langName = System.Text.Encoding.UTF8.GetString(letters);
                    }

                    // not in stable languages list - display it as experimental language
                    if (ArrayUtility.FindIndex(m_stableLanguages, v => v == editorLanguages[i]) < 0)
                    {
                        m_EditorLanguageNames[i] = EditorGUIUtility.TextContent(string.Format("{0} (Experimental)", langName));
                    }
                    else
                    {
                        m_EditorLanguageNames[i] = EditorGUIUtility.TextContent(langName);
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

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider CreateUIScalingProvider()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var settings = new PreferencesProvider("Preferences/UIScaling", GetSearchKeywordsFromGUIContentProperties<UIScalingProperties>()) { label = "UI Scaling" };
                settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowUIScaling); };
                return settings;
            }

            return null;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateDeveloperModeProvider()
        {
            // Only show this section if this is a source build or we're already in developer mode.
            if (!(Unsupported.IsSourceBuild() || Unsupported.IsDeveloperMode()))
                return null;
            var settings = new PreferencesProvider("Preferences/Developer Mode", GetSearchKeywordsFromGUIContentProperties<DeveloperModeProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowDeveloperMode); };
            return settings;
        }

        // Group Preference sections with the same name
        private static void OnGUI(string searchContext, Action<string> drawAction)
        {
            using (new SettingsWindow.GUIScope())
                drawAction(searchContext);
        }

        static void SettingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip)
        {
            var prevValue = (SyncVS.Synchronizer.AssemblyNameProvider.ProjectGenerationFlag & preference) == preference;
            var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
            if (newValue != prevValue)
            {
                SyncVS.Synchronizer.AssemblyNameProvider.ToggleProjectGeneration(preference);
            }
        }

        private void ShowExternalApplications(string searchContext)
        {
            // Applications
            FilePopup(ExternalProperties.externalScriptEditor, ScriptEditorUtility.GetExternalScriptEditor(), ref m_ScriptAppDisplayNames, ref m_ScriptApps, m_ScriptEditorPath, CodeEditor.SystemDefaultPath, OnScriptEditorChanged);

            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.Other
                || ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.SystemDefault)
            {
                CodeEditor.Editor.Current.OnGUI();
            }
            else
            {
                EditorGUILayout.LabelField("Generate .csproj files for:");
                EditorGUI.indentLevel++;

                SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
                SettingsButton(ProjectGenerationFlag.Local, "Local packages", "");
                SettingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
                SettingsButton(ProjectGenerationFlag.Git, "Git packages", "");
                SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
                SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
                SettingsButton(ProjectGenerationFlag.PlayerAssemblies, "Player projects", "For each player project generate an additional csproj with the name 'project-player.csproj'");
                RegenerateProjectFiles();
                EditorGUI.indentLevel--;
            }

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

            {
                m_DiffToolIndex = EditorGUILayout.Popup(ExternalProperties.revisionControlDiffMerge, m_DiffToolIndex, m_DiffTools);
                if (m_DiffToolIndex == m_DiffTools.Length - 1)
                {
                    GUILayout.BeginHorizontal();
                    m_CustomDiffToolPath = EditorGUILayout.DelayedTextField("Tool Path", m_CustomDiffToolPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(k_browseButtonWidth)))
                    {
                        string path = EditorUtility.OpenFilePanel("Browse for application", "", InternalEditorUtility.GetApplicationExtensionForRuntimePlatform(Application.platform));
                        if (path.Length != 0)
                        {
                            m_CustomDiffToolPath = path;
                        }
                    }

                    GUILayout.EndHorizontal();
                    m_CustomDiffToolArguments[0] = EditorGUILayout.DelayedTextField("Two-way diff command line", m_CustomDiffToolArguments[0]);
                    m_CustomDiffToolArguments[1] = EditorGUILayout.DelayedTextField("Three-way diff command line", m_CustomDiffToolArguments[1]);
                    m_CustomDiffToolArguments[2] = EditorGUILayout.DelayedTextField("Merge arguments", m_CustomDiffToolArguments[2]);
                }
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

        private void RegenerateProjectFiles()
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(new GUILayoutOption[] {}));
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
            {
                SyncVS.Synchronizer.Sync();
            }
        }

        #pragma warning disable 618
        private ScriptEditorUtility.ScriptEditor GetSelectedScriptEditor()
        {
            return ScriptEditorUtility.GetScriptEditorFromPath(m_ScriptEditorPath.str);
        }

        private void OnScriptEditorChanged()
        {
            CodeEditor.SetExternalScriptEditor(m_ScriptEditorPath);
            UnityEditor.VisualStudioIntegration.UnityVSSupport.ScriptEditorChanged(m_ScriptEditorPath.str);
        }

        private void EnableGeneral()
        {
            var fontNames = new List<string>(EditorResources.supportedFontNames);

            fontNames.Sort();

            // Remove the default font and prepend it with the '(Default)' suffix
            fontNames.Remove(EditorResources.GetDefaultFont());
            fontNames.Insert(0, EditorResources.GetDefaultFont() + " (Default)");

            foreach (var fontName in fontNames)
            {
                m_SystemFonts.Add(new GUIContent(fontName));
            }
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
            DoDirectoryMonitoring();

            m_ReopenLastUsedProjectOnStartup = EditorGUILayout.Toggle(GeneralProperties.loadPreviousProjectOnStartup, m_ReopenLastUsedProjectOnStartup);

            bool oldCompressOnImport = m_CompressAssetsOnImport;
            m_CompressAssetsOnImport = EditorGUILayout.Toggle(GeneralProperties.compressAssetsOnImport, oldCompressOnImport);

            if (GUI.changed && m_CompressAssetsOnImport != oldCompressOnImport)
                Unsupported.SetApplicationSettingCompressAssetsOnImport(m_CompressAssetsOnImport);

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

            CodeOptimization codeOptimization = (CodeOptimization)EditorGUILayout.EnumPopup(ExternalProperties.codeOptimizationOnStartup, m_ScriptDebugInfoEnabled ? CodeOptimization.Debug : CodeOptimization.Release);
            m_ScriptDebugInfoEnabled = (codeOptimization == CodeOptimization.Debug ? true : false);

            using (new EditorGUI.DisabledScope(!pro))
            {
                int newSkin = EditorGUILayout.Popup(GeneralProperties.editorSkin, !EditorGUIUtility.isProSkin ? 0 : 1, GeneralProperties.editorSkinOptions);
                if ((!EditorGUIUtility.isProSkin ? 0 : 1) != newSkin)
                    InternalEditorUtility.SwitchSkinAndRepaintAllViews();
            }

            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                EditorGUI.BeginChangeCheck();
                var userFontName = EditorPrefs.GetString("user_editor_font", null);
                int selectedFontIndex = Math.Max(0, m_SystemFonts.FindIndex(f => f.text == userFontName));
                selectedFontIndex = EditorGUILayout.Popup(GeneralProperties.editorFont, selectedFontIndex, m_SystemFonts.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    if (selectedFontIndex == 0)
                    {
                        EditorPrefs.DeleteKey("user_editor_font");
                    }
                    else
                    {
                        var selectedFontName = m_SystemFonts[selectedFontIndex].text;
                        EditorPrefs.SetString("user_editor_font", selectedFontName);
                    }

                    // Refresh skin to get new font
                    Unsupported.ClearSkinCache();
                    InternalEditorUtility.RequestScriptReload();
                    InternalEditorUtility.RepaintAllViews();
                }
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

                if (string.IsNullOrEmpty(m_GpuDeviceInUse))
                {
                    m_GpuDeviceInUse = m_CachedGpuDevices[currentGpuDeviceIndex];

                    if (string.IsNullOrEmpty(m_GpuDevice))
                    {
                        m_GpuDevice = m_GpuDeviceInUse;
                    }
                }

                var newGpuDeviceIndex = EditorGUILayout.Popup("Device To Use", currentGpuDeviceIndex, m_CachedGpuDevices);
                if (currentGpuDeviceIndex != newGpuDeviceIndex)
                {
                    m_GpuDevice = m_CachedGpuDevices[newGpuDeviceIndex];
                }

                if (m_GpuDevice != m_GpuDeviceInUse)
                {
                    EditorGUILayout.HelpBox(ExternalProperties.changingThisSettingRequiresRestart.text, MessageType.Warning);
                }
            }

            EditorGUI.BeginChangeCheck();
            m_EnableCodeCoverage = EditorGUILayout.Toggle(GeneralProperties.codeCoverageEnabled, m_EnableCodeCoverage);
            if (EditorGUI.EndChangeCheck())
            {
                // This sets the CodeCoverageEnabled EditorPref in ScriptingCoverage::SetEnabled
                Coverage.enabled = m_EnableCodeCoverage;
            }

            if (m_EnableCodeCoverage)
            {
                EditorGUILayout.HelpBox(kCodeCoverageEnabledMessage, MessageType.Warning);
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var progressDialogDelay = EditorGUILayout.DelayedFloatField(GeneralProperties.progressDialogDelay, m_ProgressDialogDelay);
                progressDialogDelay = Mathf.Clamp(progressDialogDelay, 0.1f, 1000.0f);
                if (progressDialogDelay != m_ProgressDialogDelay)
                {
                    EditorUtility.BusyProgressDialogDelayChanged(progressDialogDelay);
                    m_ProgressDialogDelay = progressDialogDelay;
                }
            }
            m_GraphSnapping = EditorGUILayout.Toggle(GeneralProperties.enableSnapping, m_GraphSnapping);
            ApplyChangesToPrefs();

            if (oldAlphaNumeric != m_AllowAlphaNumericHierarchy)
                EditorApplication.DirtyHierarchyWindowSorting();

            if (assetStoreSearchChanged)
            {
                ProjectBrowser.ShowAssetStoreHitsWhileSearchingLocalAssetsChanged();
            }

            DrawInteractionModeOptions();
        }

        enum InteractionMode
        {
            Default,                // 4 ms
            NoThrottling,           // 0 ms (will never idle)
            MonitorRefreshRate,     // ~16 ms
            Custom                  // Between 1 ms and 33 ms
        }

        private void DrawInteractionModeOptions()
        {
            const int defaultIdleTimeMs = 4;
            const string idleTimePrefKeyName = "ApplicationIdleTime";
            const string interactionModePrefKeyName = "InteractionMode";
            var monitorRefreshDelayMs = (int)(1f / Math.Max(Screen.currentResolution.refreshRate, 1) * 1000f);
            var idleTimeMs = EditorPrefs.GetInt(idleTimePrefKeyName, defaultIdleTimeMs);
            var interactionModeOption = (InteractionMode)EditorPrefs.GetInt(interactionModePrefKeyName, (int)InteractionMode.Default);

            if (Event.current.type == EventType.MouseDown)
                GeneralProperties.interactionModes[(int)InteractionMode.MonitorRefreshRate].text = $"Monitor Refresh Rate ({monitorRefreshDelayMs} ms)";

            EditorGUI.BeginChangeCheck();
            interactionModeOption = (InteractionMode)EditorGUILayout.Popup(GeneralProperties.interactionMode, (int)interactionModeOption, GeneralProperties.interactionModes);
            if (interactionModeOption == InteractionMode.Default)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.DeleteKey(idleTimePrefKeyName);
                    EditorPrefs.DeleteKey(interactionModePrefKeyName);
                }
            }
            else if (interactionModeOption == InteractionMode.NoThrottling)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, 0);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                }
            }
            else if (interactionModeOption == InteractionMode.MonitorRefreshRate)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, monitorRefreshDelayMs);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                }
            }
            else
            {
                idleTimeMs = EditorGUILayout.IntSlider(GeneralProperties.applicationFrameThrottling, idleTimeMs, 0, 33);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, idleTimeMs);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                }
            }
        }

        private void DoDirectoryMonitoring()
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.Win32Windows;

            using (new EditorGUI.DisabledScope(!isWindows))
            {
                m_DirectoryMonitoring = EditorGUILayout.Toggle(GeneralProperties.directoryMonitoring, m_DirectoryMonitoring);

                if (!isWindows)
                    EditorGUILayout.HelpBox("Directory monitoring currently only available on windows", MessageType.Info, true);
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

        internal static SortedDictionary<string, List<KeyValuePair<string, T>>> OrderPrefs<T>(IEnumerable<KeyValuePair<string, T>> input)
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

        private void ShowSceneView(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("General", EditorStyles.boldLabel);
            m_Create3DObjectsAtOrigin = EditorGUILayout.Toggle(GeneralProperties.createObjectsAtWorldOrigin, m_Create3DObjectsAtOrigin);

            GUILayout.Label("Handles", EditorStyles.boldLabel);
            Handles.s_LineThickness.value = EditorGUILayout.IntSlider(SceneViewProperties.handlesLineThickness, (int)Handles.s_LineThickness.value, 0, 5);

            GUILayout.Label("Search", EditorStyles.boldLabel);
            SceneView.s_PreferenceEnableFilteringWhileSearching.value = EditorGUILayout.Toggle(SceneViewProperties.enableFilteringWhileSearching, SceneView.s_PreferenceEnableFilteringWhileSearching);
            SceneView.s_PreferenceEnableFilteringWhileLodGroupEditing.value  = EditorGUILayout.Toggle(SceneViewProperties.enableFilteringWhileLodGroupEditing, SceneView.s_PreferenceEnableFilteringWhileLodGroupEditing);

            if (EditorGUI.EndChangeCheck())
            {
                WritePreferences();
                SceneView.RepaintAll();
            }
        }

        private void ShowGICache(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            {
                // Show Gigabytes to the user.
                const int kMinSizeInGigabytes = 5;
                const int kMaxSizeInGigabytes = 200;

                // Write size in GigaBytes.
                m_GICacheSettings.m_MaximumSize = EditorGUILayout.IntSlider(GICacheProperties.maxCacheSize, m_GICacheSettings.m_MaximumSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);
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

            if (EditorGUI.EndChangeCheck())
                WritePreferences();
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
                EditorUtility.RequestScriptReload();
            }

            ApplyChangesToPrefs();
        }

        private void ShowUIScaling(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                GUILayout.Label(UIScalingProperties.editorContentScaling, EditorStyles.boldLabel);

                GUILayout.Space(5);

                GUILayout.BeginVertical();
                var currentScaling = CurrentEditorScalingValue;

                bool customScaling = EditorPrefs.HasKey(kContentScalePrefKey) && m_ContentScalePercentValue > 0;
                bool useDesktopScaling = EditorGUILayout.Toggle(UIScalingProperties.defaultContentScaling, !customScaling);

                if ((!customScaling) != useDesktopScaling)
                {
                    m_ContentScaleChangedThisSession = true;

                    if (useDesktopScaling)
                    {
                        EditorPrefs.DeleteKey(kContentScalePrefKey);
                        m_ContentScalePercentValue = -1;
                    }
                    else
                    {
                        if (m_ContentScalePercentValue < 0)
                            m_ContentScalePercentValue = currentScaling;
                    }
                }

                using (new EditorGUI.DisabledScope(useDesktopScaling))
                {
                    int displayedScaleValue = (m_ContentScalePercentValue > 0) ? m_ContentScalePercentValue : currentScaling;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(UIScalingProperties.currentContentScaling, GUILayout.Width(EditorGUIUtility.labelWidth));
                    GUILayout.Label(currentScaling + "%");
                    GUILayout.EndHorizontal();

                    displayedScaleValue = EditorGUILayout.IntPopup(UIScalingProperties.customContentScaling.text, displayedScaleValue, m_CustomScalingLabels, m_CustomScalingValues);
                    if (m_ContentScalePercentValue != displayedScaleValue && m_ContentScalePercentValue >= 0)
                    {
                        m_ContentScaleChangedThisSession = true;
                        m_ContentScalePercentValue = displayedScaleValue;
                    }
                }


                if (m_ContentScaleChangedThisSession)
                    EditorGUILayout.HelpBox(ExternalProperties.changingThisSettingRequiresRestart.text, MessageType.Warning);

                GUILayout.EndVertical();
            }
            if (EditorGUI.EndChangeCheck())
                WritePreferences();

            ApplyChangesToPrefs();
        }

        private void ShowDeveloperMode(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            m_DeveloperMode = EditorGUILayout.Toggle(DeveloperModeProperties.developerMode, m_DeveloperMode);

            using (new EditorGUI.DisabledScope(!m_DeveloperMode))
            {
                m_ShowRepaintDots = EditorGUILayout.Toggle(DeveloperModeProperties.showRepaintDots, m_ShowRepaintDots);
            }

            // If any developer mode preference changes, make sure to repaint all views
            m_DeveloperModeDirty = EditorGUI.EndChangeCheck();

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
            CodeEditor.SetExternalScriptEditor(m_ScriptEditorPath);

            EditorPrefs.SetString("kImagesDefaultApp", m_ImageAppPath);
            EditorPrefs.SetString("kDiffsDefaultApp", m_DiffTools.Length == 0 ? "" : m_DiffTools[m_DiffToolIndex]);
            EditorPrefs.SetString("customDiffToolPath", m_CustomDiffToolPath);
            EditorPrefs.SetString("twoWayDiffArguments", m_CustomDiffToolArguments[0]);
            EditorPrefs.SetString("threeWayDiffArguments", m_CustomDiffToolArguments[1]);
            EditorPrefs.SetString("mergeArguments", m_CustomDiffToolArguments[2]);

            WriteRecentAppsList(m_ScriptApps, m_ScriptEditorPath, kRecentScriptAppsKey);
            WriteRecentAppsList(m_ImageApps, m_ImageAppPath, kRecentImageAppsKey);

            EditorPrefs.SetBool("kAutoRefresh", m_AutoRefresh);

            bool oldDirectoryMonitoring = EditorPrefs.GetBool("DirectoryMonitoring", true);
            if (oldDirectoryMonitoring != m_DirectoryMonitoring)
            {
                EditorPrefs.SetBool("DirectoryMonitoring", m_DirectoryMonitoring);
                AssetDatabaseExperimental.RefreshSettings();
            }

            if (m_ContentScaleChangedThisSession)
            {
                EditorPrefs.SetInt(kContentScalePrefKey, m_ContentScalePercentValue);
            }

            EditorPrefs.SetBool("ReopenLastUsedProjectOnStartup", m_ReopenLastUsedProjectOnStartup);
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

            EditorGUI.s_ShowRepaintDots.value = m_ShowRepaintDots;

            EditorPrefs.SetBool("ScriptDebugInfoEnabled", m_ScriptDebugInfoEnabled);

            EditorPrefs.SetBool("Editor.kEnableEditorLocalization", m_EnableEditorLocalization);
            EditorPrefs.SetString("Editor.kEditorLocale", m_SelectedLanguage);

            EditorPrefs.SetBool("AllowAlphaNumericHierarchy", m_AllowAlphaNumericHierarchy);

            EditorPrefs.SetFloat("EditorBusyProgressDialogDelay", m_ProgressDialogDelay);
            GOCreationCommands.s_PlaceObjectsAtWorldOrigin.value = m_Create3DObjectsAtOrigin;
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

            EditorPrefs.SetBool("GraphSnapping", m_GraphSnapping);
        }

        private int CurrentEditorScalingValue
        {
            get {return Mathf.RoundToInt(GUIUtility.pixelsPerPoint * 100); }
        }

        private void ReadPreferences()
        {
            m_ScriptEditorPath.str = ScriptEditorUtility.GetExternalScriptEditor();

            m_ImageAppPath.str = EditorPrefs.GetString("kImagesDefaultApp");

            m_ScriptApps = BuildAppPathList(m_ScriptEditorPath, kRecentScriptAppsKey, CodeEditor.SystemDefaultPath);
            m_ScriptAppsEditions = new string[m_ScriptApps.Length];

            if (Application.platform == RuntimePlatform.WindowsEditor && UnityVSSupport.IsUnityVSEnabled())
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

            var foundScriptEditorPaths = CodeEditor.Editor.GetFoundScriptEditorPaths();

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

            m_CustomDiffToolPath = EditorPrefs.GetString("customDiffToolPath", m_CustomDiffToolPath);
            m_CustomDiffToolArguments[0] = EditorPrefs.GetString("twoWayDiffArguments", m_CustomDiffToolArguments[0]);
            m_CustomDiffToolArguments[1] = EditorPrefs.GetString("threeWayDiffArguments", m_CustomDiffToolArguments[1]);
            m_CustomDiffToolArguments[2] = EditorPrefs.GetString("mergeArguments", m_CustomDiffToolArguments[2]);
            InternalEditorUtility.SetCustomDiffToolData(m_CustomDiffToolPath, m_CustomDiffToolArguments[0], m_CustomDiffToolArguments[1], m_CustomDiffToolArguments[2]);


            if ((m_DiffTools == null || (m_DiffTools.Length == 1 && m_CustomDiffToolPath.Equals(""))))
            {
                m_noDiffToolsMessage = InternalEditorUtility.GetNoDiffToolsDetectedMessage();
            }

            string diffTool = EditorPrefs.GetString("kDiffsDefaultApp");
            m_DiffToolIndex = ArrayUtility.IndexOf(m_DiffTools, diffTool);
            if (m_DiffToolIndex == -1)
                m_DiffToolIndex = 0;

            m_AutoRefresh = EditorPrefs.GetBool("kAutoRefresh");
            m_DirectoryMonitoring = EditorPrefs.GetBool("DirectoryMonitoring", true);

            m_ReopenLastUsedProjectOnStartup = EditorPrefs.GetBool("ReopenLastUsedProjectOnStartup");

            m_EnableEditorAnalytics = EditorPrefs.GetBool("EnableEditorAnalytics", true);

            m_ShowAssetStoreSearchHits = EditorPrefs.GetBool("ShowAssetStoreSearchHits", true);
            m_VerifySavingAssets = EditorPrefs.GetBool("VerifySavingAssets", false);
            m_ScriptCompilationDuringPlay = (ScriptChangesDuringPlayOptions)EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0);
            m_DeveloperMode = Unsupported.IsDeveloperMode();
            m_ShowRepaintDots = EditorGUI.s_ShowRepaintDots.value;

            m_GICacheSettings.m_EnableCustomPath = EditorPrefs.GetBool("GICacheEnableCustomPath");
            m_GICacheSettings.m_CachePath = EditorPrefs.GetString("GICacheFolder");
            m_GICacheSettings.m_MaximumSize = EditorPrefs.GetInt("GICacheMaximumSizeGB", 10);
            m_GICacheSettings.m_CompressionLevel = EditorPrefs.GetInt("GICacheCompressionLevel");

            m_SpriteAtlasCacheSize = EditorPrefs.GetInt("SpritePackerCacheMaximumSizeGB");

            m_ScriptDebugInfoEnabled = EditorPrefs.GetBool("ScriptDebugInfoEnabled", false);
            m_EnableEditorLocalization = EditorPrefs.GetBool("Editor.kEnableEditorLocalization", true);
            m_SelectedLanguage = EditorPrefs.GetString("Editor.kEditorLocale", LocalizationDatabase.GetDefaultEditorLanguage().ToString());
            m_AllowAlphaNumericHierarchy = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);
            m_EnableCodeCoverage = EditorPrefs.GetBool("CodeCoverageEnabled", false);
            m_ProgressDialogDelay = EditorPrefs.GetFloat("EditorBusyProgressDialogDelay", 3.0f);
            m_Create3DObjectsAtOrigin = GOCreationCommands.s_PlaceObjectsAtWorldOrigin;

            m_CompressAssetsOnImport = Unsupported.GetApplicationSettingCompressAssetsOnImport();
            m_GpuDevice = EditorPrefs.GetString("GpuDeviceName");

            if (EditorPrefs.HasKey(kContentScalePrefKey))
            {
                m_ContentScalePercentValue = EditorPrefs.GetInt(kContentScalePrefKey, CurrentEditorScalingValue);
                m_ContentScaleChangedThisSession = m_ContentScalePercentValue != CurrentEditorScalingValue;
            }
            else
            {
                m_ContentScalePercentValue  = CurrentEditorScalingValue;
                m_ContentScaleChangedThisSession = false;
            }

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                extension.ReadPreferences();
            }

            m_GraphSnapping = EditorPrefs.GetBool("GraphSnapping", true);
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

                if (appPath == CodeEditor.SystemDefaultPath)     // use built-in
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
