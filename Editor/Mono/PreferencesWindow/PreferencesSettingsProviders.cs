// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Analytics;
using UnityEditor.Modules;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.CodeEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;

namespace UnityEditor
{
    internal class PreferencesProvider : SettingsProvider
    {
        internal enum CodeOptimization
        {
            Debug,
            Release
        }

        static class Constants
        {
            public static readonly GUIStyle errorLabel = "WordWrappedLabel";
            public static readonly GUIStyle warningIcon = "CN EntryWarn";
            public static readonly GUIStyle cacheFolderLocation = "CacheFolderLocation";
        }

        class Styles
        {
            public static readonly GUIContent browse = EditorGUIUtility.TrTextContent("Browse...");
            public static readonly GUIStyle clearBindingButton = new GUIStyle(GUI.skin.button);

            static Styles()
            {
                clearBindingButton.margin.top = 0;
            }
        }

        class GeneralProperties
        {
            public static readonly GUIContent loadPreviousProjectOnStartup = EditorGUIUtility.TrTextContent("Load Previous Project on Startup");
            public static readonly GUIContent disableEditorAnalytics = EditorGUIUtility.TrTextContent("Disable Editor Analytics");
            public static readonly GUIContent autoSaveScenesBeforeBuilding = EditorGUIUtility.TrTextContent("Auto-save scenes before building");
            public static readonly GUIContent scriptChangesDuringPlay = EditorGUIUtility.TrTextContent("Script Changes While Playing");
            public static readonly GUIContent editorFont = EditorGUIUtility.TrTextContent("Editor Font");
            public static readonly GUIContent editorTextSharpness = EditorGUIUtility.TrTextContent("Editor Text Sharpness");
            public static readonly GUIContent editorSkin = EditorGUIUtility.TrTextContent("Editor Theme");
            public static readonly GUIContent[] editorSkinOptions = { EditorGUIUtility.TrTextContent("Light"), EditorGUIUtility.TrTextContent("Dark") };
            public static readonly GUIContent hierarchyHeader = EditorGUIUtility.TrTextContent("Hierarchy window");
            public static readonly GUIContent enableAlphaNumericSorting = EditorGUIUtility.TrTextContent("Enable Alphanumeric Sorting", "If enabled then you can choose between Transform sorting and Alphabetical sorting in the Hierarchy.");
            public static readonly GUIContent defaultPrefabMode = EditorGUIUtility.TrTextContent("Default Prefab Mode", "This mode will be used when opening Prefab Mode from a Prefab instance in the Hierarchy.");
            public static readonly GUIContent applicationFrameThrottling = EditorGUIUtility.TrTextContent("Frame Throttling (milliseconds)", "The number of milliseconds the Editor can idle between frames.");
            public static readonly GUIContent inputMaxProcessTime = EditorGUIUtility.TrTextContent("Input Throttling (milliseconds)", "The maximum number of milliseconds the Editor will take to process user inputs.");
            public static readonly GUIContent interactionMode = EditorGUIUtility.TrTextContent("Interaction Mode", "Specifies how long the Editor can idle before it updates.");
            public static readonly GUIContent enterPlayModeSettingsFocusGameView = EditorGUIUtility.TrTextContent("Create Game View On Play", "If enabled, a Game View window will be created when entering play mode if none exists");
            public static readonly GUIContent enableExtendedDynamicHints = EditorGUIUtility.TrTextContent("Enable extended Dynamic Hints", "Check this to enable extended Dynamic Hints. If available, extended Dynamic Hints will display more information when a property, object or tool is hovered for enough time, or when a Dynamic Hint is displayed");
            public static readonly GUIContent[] interactionModes =
            {
                EditorGUIUtility.TrTextContent("Default", "The Editor can idle up to 4 ms per frame."),
                EditorGUIUtility.TrTextContent("No Throttling", "The Editor does not idle. It runs as fast as possible."),
                EditorGUIUtility.TrTextContent("Monitor Refresh Rate", "The Editor can idle up to whatever the monitor's refresh rate is, in milliseconds."),
                EditorGUIUtility.TrTextContent("Custom", "You specify how many milliseconds per frame the Editor can idle."),
            };
            public static readonly GUIContent progressDialogDelay = EditorGUIUtility.TrTextContent("Busy Progress Delay", "Delay in seconds before 'Unity is busy' progress bar shows up.");
            public static readonly GUIContent enableSnapping = EditorGUIUtility.TrTextContent("Graph Snapping", "If enabled, GraphElements in Graph Views (such as Shader Graph) align with one another when you move them. If disabled, GraphElements move freely.");

            public static readonly GUIContent packageManagerLogLevel = EditorGUIUtility.TrTextContent("Package Manager Log Level",
                "Determines the level of detail when the Package Manager writes information to log files.\n"
                + "\nFrom least detailed to most detailed:"
                + "\n* Error: unexpected errors and failures only."
                + "\n* Warn: abnormal situations that can lead to issues."
                + "\n* Info: high-level informational messages."
                + "\n* Verbose: detailed informational messages."
                + "\n* Debug: high-level debugging messages."
                + "\n* Silly: detailed debugging messages.");
            public static readonly GUIContent packageManagerLogLevelOverridden = EditorGUIUtility.TrTextContent("Package Manager Log Level currently overridden by -enablePackageManagerTraces command-line argument.");

            public static readonly GUIContent performBumpMapCheck = EditorGUIUtility.TrTextContent("Perform Bump Map Check", "Enables Bump Map Checks upon import of Materials. This checks that textures used in a normal map material slot are actually defined as normal maps.");
            public static readonly GUIContent enableExtendedLogging = EditorGUIUtility.TrTextContent("Timestamp Editor log entries", "Adds timestamp and thread Id to Editor.log messages.");
            public static readonly GUIContent enableHelperBar = EditorGUIUtility.TrTextContent("Enable Helper Bar", "Enables Helper Bar in the status bar at the bottom of the main Unity Editor window.");
            public static readonly GUIContent enablePlayModeTooltips = EditorGUIUtility.TrTextContent("Enable PlayMode Tooltips", "Enables tooltips in the editor while in play mode.");
            public static readonly GUIContent showSecondaryWindowsInTaskbar = EditorGUIUtility.TrTextContent("Show All Windows in Taskbar");
            public static readonly GUIContent useProjectPathInTitle = EditorGUIUtility.TrTextContent("Use Project Path in Window Title", "If enabled the Project's name is replaced in the main window title with the Project's path on disk.");
        }

        class ExternalProperties
        {
            public static readonly GUIContent codeOptimizationOnStartup = EditorGUIUtility.TrTextContent("Code Optimization On Startup");
            public static readonly GUIContent changingThisSettingRequiresRestart = EditorGUIUtility.TrTextContent("Changing this setting requires a restart to take effect.");
            public static readonly GUIContent revisionControlDiffMerge = EditorGUIUtility.TrTextContent("Revision Control Diff/Merge");
            public static readonly GUIContent externalScriptEditor = EditorGUIUtility.TrTextContent("External Script Editor");
            public static readonly GUIContent imageApplication = EditorGUIUtility.TrTextContent("Image application");
        }

        class UIScalingProperties
        {
            public static readonly GUIContent editorContentScaling = EditorGUIUtility.TrTextContent("Editor icons and text scaling");
            public static readonly GUIContent defaultContentScaling = EditorGUIUtility.TrTextContent("Use default desktop setting");
            public static readonly GUIContent currentContentScaling = EditorGUIUtility.TrTextContent("Current scaling");
            public static readonly GUIContent customContentScaling = EditorGUIUtility.TrTextContent("Use custom scaling value");
        }

        class ColorsProperties
        {
            public static readonly GUIContent userDefaults = EditorGUIUtility.TrTextContent("Use Defaults");
        }

        class GICacheProperties
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

        class SceneViewProperties
        {
            public static readonly GUIContent enableFilteringWhileSearching = EditorGUIUtility.TrTextContent("Enable filtering while searching", "If enabled, searching will cause non-matching items in the scene view to be greyed out");
            public static readonly GUIContent enableFilteringWhileLodGroupEditing = EditorGUIUtility.TrTextContent("Enable filtering while editing LOD groups", "If enabled, editing LOD groups will cause other objects in the scene view to be greyed out");
            public static readonly GUIContent handlesLineThickness = EditorGUIUtility.TrTextContent("Line Thickness", "Thickness of manipulator tool handle lines");
            public static readonly GUIContent createObjectsAtWorldOrigin = EditorGUIUtility.TrTextContent("Create Objects at Origin", "Enable this preference to instantiate new 3D objects at World coordinates 0,0,0. Disable it to instantiate them at the Scene pivot (in front of the Scene view Camera).");
            public static readonly GUIContent enableConstrainProportionsScalingForNewObjects = EditorGUIUtility.TrTextContent("Create Objects with Constrained Proportions scale on", "If enabled, scale in the transform component will be set to constrain proportions for new GameObjects by default");
            public static readonly GUIContent useInspectorExpandedStateContent = EditorGUIUtility.TrTextContent("Auto-hide gizmos", "Automatically hide gizmos of Components collapsed in the Inspector");
            public static readonly GUIContent ignoreAlwaysRefreshWhenNotFocused = EditorGUIUtility.TrTextContent("Refresh the Scene view only when the Editor is in focus.", "If enabled, ignore the \"Always Refresh\" flag on the Scene view when the Editor is not the foregrounded application.");
        }

        class LanguageProperties
        {
            public static readonly GUIContent editorLanguageExperimental = EditorGUIUtility.TrTextContent("Editor Language (Experimental)");
            public static readonly GUIContent editorLanguage = EditorGUIUtility.TrTextContent("Editor language");
            public static readonly GUIContent localizeCompileMessages = EditorGUIUtility.TrTextContent("Localize compiler messages");
        }

        class DeveloperModeProperties
        {
            public static readonly GUIContent developerMode = EditorGUIUtility.TrTextContent("Developer Mode", "Enable or disable developer mode features.");
            public static readonly GUIContent generateOnPostprocessAllAssets = EditorGUIUtility.TrTextContent("Generate OnPostprocessAllAssets Dependency Diagram", "Generates a graphviz diagram to show OnPostprocessAllAssets dependencies.");
            public static readonly GUIContent showRepaintDots = EditorGUIUtility.TrTextContent("Show Repaint Dots", "Enable or disable the colored dots that flash when an EditorWindow repaints.");
            public static readonly GUIContent redirectionServer = EditorGUIUtility.TrTextContent("Documentation Server", "Select the documentation redirection server.");
        }

        private List<IPreferenceWindowExtension> prefWinExtensions;

        private bool m_ReopenLastUsedProjectOnStartup;
        private bool m_EnableEditorAnalytics;
        private bool m_AnalyticSettingChangedThisSession = false;
        private bool m_AutoSaveScenesBeforeBuilding;
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
        private bool m_TaskbarBehaviorChangedThisSession;
        private bool m_TaskbarBehaviorValue;
        private bool m_EnableConstrainProportionsScalingForNewObjects;
        private string[] m_CustomScalingLabels = {"100%", "125%", "150%", "175%", "200%", "225%", "250%", "300%", "350%"};
        private int[] m_CustomScalingValues = { 100, 125, 150, 175, 200, 225, 250, 300, 350 };
        private bool m_EnableExtendedLogging;
        private readonly string kContentScalePrefKey = "CustomEditorUIScale";
        private readonly string kWindowsTaskbarPrefKey = "WindowsTaskbarBehavior";

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
        private static int m_DiffToolIndex;

        // how many menu items come before the actual list of languages
        // (Default + separator)
        private const int k_LangListMenuOffset = 2;

        private string m_SelectedLanguage;
        private static GUIContent[] m_EditorLanguageNames;
        private bool m_EnableEditorLocalization;
        private static SystemLanguage[] m_stableLanguages = { SystemLanguage.English };
        private bool m_EnableCompilerMessagesLocalization;

        private float m_EditorTextSharpness = 0.0f;
        private bool m_AllowAlphaNumericHierarchy = false;
        private PrefabStage.Mode m_DefaultPrefabModeFromHierarchy = PrefabStage.Mode.InContext;
        private bool m_Create3DObjectsAtOrigin = false;
        private float m_ProgressDialogDelay = 3.0f;
        private bool m_GraphSnapping;
        private bool m_EnableExtendedDynamicHints
        {
            get { return TooltipView.s_EnableExtendedDynamicHints; }
            set { TooltipView.s_EnableExtendedDynamicHints.value = value; }
        }

        private string[] m_ScriptApps;
        private string[] m_ImageApps;
        private static string[] m_DiffTools;

        private static string m_CustomDiffToolPath = "";
        private static string[] m_CustomDiffToolArguments = new[] {"", "", ""};

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

        internal static bool useProjectPathInTitle
        {
            get
            {
                return EditorPrefs.GetBool("UseProjectPathInTitle", false);
            }
            set
            {
                if(value != EditorPrefs.GetBool("UseProjectPathInTitle", false))
                {
                    EditorPrefs.SetBool("UseProjectPathInTitle", value);
                    EditorApplication.UpdateMainWindowTitle();
                }
            }
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
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary && !UnityEditor.MPE.ProcessService.HasCapability("enable-gi"))
                return null;
            var settings = new PreferencesProvider("Preferences/GI Cache", GetSearchKeywordsFromGUIContentProperties<GICacheProperties>());
            settings.guiHandler = searchContext => { OnGUI(searchContext, settings.ShowGICache); };
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

        private void ShowExternalApplications(string searchContext)
        {
            // Applications

            EditorUtility.SelectMenuItemFunction selectMenuItemAction = AppsListClickRuntimePlatformExtension;
            FilePopup(ExternalProperties.externalScriptEditor, ScriptEditorUtility.GetExternalScriptEditor(), ref m_ScriptAppDisplayNames, ref m_ScriptApps, m_ScriptEditorPath, CodeEditor.SystemDefaultPath, OnScriptEditorChanged, selectMenuItemAction);

            CodeEditor.Editor.CurrentCodeEditor.OnGUI();

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

        private void OnScriptEditorChanged()
        {
            CodeEditor.Editor.SetCodeEditor(m_ScriptEditorPath);
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
            m_ReopenLastUsedProjectOnStartup = EditorGUILayout.Toggle(GeneralProperties.loadPreviousProjectOnStartup, m_ReopenLastUsedProjectOnStartup);

            bool enableEditorAnalyticsOld = m_EnableEditorAnalytics;

            using (new EditorGUI.DisabledScope(m_AnalyticSettingChangedThisSession))
            {
                m_EnableEditorAnalytics = !EditorGUILayout.Toggle(GeneralProperties.disableEditorAnalytics, !m_EnableEditorAnalytics);
                if (enableEditorAnalyticsOld != m_EnableEditorAnalytics)
                {
                    m_AnalyticSettingChangedThisSession = true;
                    EditorAnalytics.enabled = m_EnableEditorAnalytics;
                }
                if (m_AnalyticSettingChangedThisSession)
                {
                    EditorGUILayout.HelpBox(ExternalProperties.changingThisSettingRequiresRestart.text, MessageType.Warning);
                }
            }

            m_AutoSaveScenesBeforeBuilding = EditorGUILayout.Toggle(GeneralProperties.autoSaveScenesBeforeBuilding, m_AutoSaveScenesBeforeBuilding);
            m_ScriptCompilationDuringPlay = (ScriptChangesDuringPlayOptions)EditorGUILayout.EnumPopup(GeneralProperties.scriptChangesDuringPlay, m_ScriptCompilationDuringPlay);

            CodeOptimization codeOptimization = (CodeOptimization)EditorGUILayout.EnumPopup(ExternalProperties.codeOptimizationOnStartup, m_ScriptDebugInfoEnabled ? CodeOptimization.Debug : CodeOptimization.Release);
            m_ScriptDebugInfoEnabled = (codeOptimization == CodeOptimization.Debug ? true : false);

            int newSkin = EditorGUILayout.Popup(GeneralProperties.editorSkin, !EditorGUIUtility.isProSkin ? 0 : 1, GeneralProperties.editorSkinOptions);
            if ((!EditorGUIUtility.isProSkin ? 0 : 1) != newSkin)
                InternalEditorUtility.SwitchSkinAndRepaintAllViews();

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
                    UnityEditor.EditorUtility.RequestScriptReload();
                    InternalEditorUtility.RepaintAllViews();
                }
            }

            m_EditorTextSharpness = EditorGUILayout.Slider(GeneralProperties.editorTextSharpness, m_EditorTextSharpness, -0.5f, 1.0f);

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

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var progressDialogDelay = EditorGUILayout.FloatField(GeneralProperties.progressDialogDelay, m_ProgressDialogDelay);
                progressDialogDelay = Mathf.Clamp(progressDialogDelay, 0.1f, 1000.0f);
                if (progressDialogDelay != m_ProgressDialogDelay)
                {
                    EditorUtility.BusyProgressDialogDelayChanged(progressDialogDelay);
                    m_ProgressDialogDelay = progressDialogDelay;
                }
            }
            m_GraphSnapping = EditorGUILayout.Toggle(GeneralProperties.enableSnapping, m_GraphSnapping);

            GameView.openWindowOnEnteringPlayMode = EditorGUILayout.Toggle(GeneralProperties.enterPlayModeSettingsFocusGameView, GameView.openWindowOnEnteringPlayMode);

            useProjectPathInTitle = EditorGUILayout.Toggle(GeneralProperties.useProjectPathInTitle, useProjectPathInTitle);

            DrawInteractionModeOptions();

            DrawPackageManagerOptions();
            DrawDynamicHintsOptions();
            DrawPerformBumpMapCheck();

            m_EnableExtendedLogging = EditorGUILayout.Toggle(GeneralProperties.enableExtendedLogging, m_EnableExtendedLogging);

            DrawEnableHelperBar();
            DrawEnableTooltipsInPlayMode();
            EditorGUILayout.Space();

            GUILayout.Label(GeneralProperties.hierarchyHeader, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            bool oldAlphaNumeric = m_AllowAlphaNumericHierarchy;
            m_AllowAlphaNumericHierarchy = EditorGUILayout.Toggle(GeneralProperties.enableAlphaNumericSorting, m_AllowAlphaNumericHierarchy);
            m_DefaultPrefabModeFromHierarchy = (PrefabStage.Mode)EditorGUILayout.EnumPopup(GeneralProperties.defaultPrefabMode, m_DefaultPrefabModeFromHierarchy);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                bool existingBehaviorValue = m_TaskbarBehaviorValue;
                m_TaskbarBehaviorValue = EditorGUILayout.Toggle(GeneralProperties.showSecondaryWindowsInTaskbar, existingBehaviorValue);

                if (existingBehaviorValue != m_TaskbarBehaviorValue)
                {
                    m_TaskbarBehaviorChangedThisSession = true;
                }

                if (m_TaskbarBehaviorChangedThisSession)
                {
                    EditorGUILayout.HelpBox(ExternalProperties.changingThisSettingRequiresRestart.text, MessageType.Warning);
                }
            }

            ApplyChangesToPrefs();

            if (oldAlphaNumeric != m_AllowAlphaNumericHierarchy)
                EditorApplication.DirtyHierarchyWindowSorting();
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
            const string inputMaxProcessTimeKeyName = "InputMaxProcessTime";
            var monitorRefreshDelayMs = Math.Min(1000, (int)(1000.0 / Screen.currentResolution.refreshRateRatio.value));
            var idleTimeMs = EditorPrefs.GetInt(idleTimePrefKeyName, defaultIdleTimeMs);
            var inputMaxProcessTime = EditorPrefs.GetInt(inputMaxProcessTimeKeyName, 100);
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
                    EditorApplication.UpdateInteractionModeSettings();
                }
            }
            else if (interactionModeOption == InteractionMode.NoThrottling)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, 0);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                    EditorApplication.UpdateInteractionModeSettings();
                }
            }
            else if (interactionModeOption == InteractionMode.MonitorRefreshRate)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, monitorRefreshDelayMs);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                    EditorApplication.UpdateInteractionModeSettings();
                }
            }
            else
            {
                idleTimeMs = EditorGUILayout.IntSlider(GeneralProperties.applicationFrameThrottling, idleTimeMs, 0, 33);
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    inputMaxProcessTime = EditorGUILayout.IntSlider(GeneralProperties.inputMaxProcessTime, inputMaxProcessTime, 1, 200);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(idleTimePrefKeyName, idleTimeMs);
                    EditorPrefs.SetInt(inputMaxProcessTimeKeyName, inputMaxProcessTime);
                    EditorPrefs.SetInt(interactionModePrefKeyName, (int)interactionModeOption);
                    EditorApplication.UpdateInteractionModeSettings();
                }
            }
        }

        private void DrawDynamicHintsOptions()
        {
            m_EnableExtendedDynamicHints = EditorGUILayout.Toggle(GeneralProperties.enableExtendedDynamicHints, m_EnableExtendedDynamicHints);
        }

        private void DrawPackageManagerOptions()
        {
            bool isLogLevelOverridden = Application.HasARGV("enablePackageManagerTraces");
            using (new EditorGUI.DisabledScope(isLogLevelOverridden))
            {
                var packageManagerLogLevel = (PackageManager.LogLevel)EditorGUILayout.EnumPopup(GeneralProperties.packageManagerLogLevel, PackageManager.Client.LogLevel);
                if (packageManagerLogLevel != PackageManager.Client.LogLevel)
                {
                    PackageManager.Client.LogLevel = packageManagerLogLevel;
                }
                if (isLogLevelOverridden)
                {
                    EditorGUILayout.HelpBox(GeneralProperties.packageManagerLogLevelOverridden.text, MessageType.Info, true);
                }
            }
        }

        void DrawPerformBumpMapCheck()
        {
            const string bumpMapChecksKeyName = "PerformBumpMapChecks";
            var bumpMapChecks = EditorPrefs.GetBool(bumpMapChecksKeyName, true);

            EditorGUI.BeginChangeCheck();
            bumpMapChecks = EditorGUILayout.Toggle(GeneralProperties.performBumpMapCheck, bumpMapChecks);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(bumpMapChecksKeyName, bumpMapChecks);
            }
        }

        void DrawEnableHelperBar()
        {
            const string helperBarKeyName = "EnableHelperBar";
            var enableHelperBar = EditorPrefs.GetBool(helperBarKeyName, false);

            EditorGUI.BeginChangeCheck();
            enableHelperBar = EditorGUILayout.Toggle(GeneralProperties.enableHelperBar, enableHelperBar);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(helperBarKeyName, enableHelperBar);
            }
        }

        void DrawEnableTooltipsInPlayMode()
        {
            const string tooltipsKeyName = "EnableTooltipsInPlayMode";
            var enableTooltips = EditorPrefs.GetBool(tooltipsKeyName, false);

            EditorGUI.BeginChangeCheck();
            enableTooltips = EditorGUILayout.Toggle(GeneralProperties.enablePlayModeTooltips, enableTooltips);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(tooltipsKeyName, enableTooltips);

                // Transfer native
                EditorApplication.UpdateTooltipsInPlayModeSettings();
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
            PrefSettings.RevertAll<PrefColor>();
        }

        private void ShowColors(string searchContext)
        {
            if (s_CachedColors == null)
            {
                s_CachedColors = OrderPrefs(PrefSettings.Prefs<PrefColor>());
            }

            var changedColor = false;
            PrefColor ccolor = null;

            // some pref colors are very long, and changing them would mean invalidating any user-defined colors.
            // as a compromise, we'll clip the label with an ellipses and show the full text in a tooltip.
            var clipping = EditorStyles.label.clipping;
            EditorStyles.label.clipping = TextClipping.Ellipsis;

            foreach (KeyValuePair<string, List<KeyValuePair<string, PrefColor>>> category in s_CachedColors)
            {
                GUILayout.Label(category.Key, EditorStyles.boldLabel);
                foreach (KeyValuePair<string, PrefColor> kvp in category.Value)
                {
                    EditorGUI.BeginChangeCheck();
                    Color c = EditorGUILayout.ColorField(EditorGUIUtility.TempContent(kvp.Key, kvp.Key), kvp.Value.Color);
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
            EditorStyles.label.clipping = clipping;

            GUILayout.Space(5f);

            if (GUILayout.Button(ColorsProperties.userDefaults, GUILayout.Width(120)))
            {
                RevertColors();
                changedColor = true;
            }

            if (changedColor)
                EditorApplication.RequestRepaintAllViews();
        }

        private void ShowSceneView(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("General", EditorStyles.boldLabel);
            m_Create3DObjectsAtOrigin = EditorGUILayout.Toggle(SceneViewProperties.createObjectsAtWorldOrigin, m_Create3DObjectsAtOrigin);
            m_EnableConstrainProportionsScalingForNewObjects = EditorGUILayout.Toggle(SceneViewProperties.enableConstrainProportionsScalingForNewObjects, m_EnableConstrainProportionsScalingForNewObjects);
            AnnotationUtility.useInspectorExpandedState = EditorGUILayout.Toggle(SceneViewProperties.useInspectorExpandedStateContent, AnnotationUtility.useInspectorExpandedState);
            SceneView.s_PreferenceIgnoreAlwaysRefreshWhenNotFocused.value = EditorGUILayout.Toggle(SceneViewProperties.ignoreAlwaysRefreshWhenNotFocused, SceneView.s_PreferenceIgnoreAlwaysRefreshWhenNotFocused);

            GUILayout.Label("Handles", EditorStyles.boldLabel);
            Handles.s_LineThickness.value = EditorGUILayout.IntSlider(SceneViewProperties.handlesLineThickness, (int)Handles.s_LineThickness.value, 1, 5);

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

                m_EnableCompilerMessagesLocalization = EditorGUILayout.Toggle(LanguageProperties.localizeCompileMessages, m_EnableCompilerMessagesLocalization);
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

            GUILayout.BeginHorizontal();
            m_DeveloperMode = EditorGUILayout.Toggle(DeveloperModeProperties.developerMode, m_DeveloperMode, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!m_DeveloperMode))
            {
                m_ShowRepaintDots = EditorGUILayout.Toggle(DeveloperModeProperties.showRepaintDots, m_ShowRepaintDots);
                m_DeveloperModeDirty = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();

                var docServer = (Help.DocRedirectionServer)EditorGUILayout.EnumPopup(DeveloperModeProperties.redirectionServer, Help.docRedirectionServer);

                if (EditorGUI.EndChangeCheck())
                {
                    Help.docRedirectionServer = docServer;
                }

                if (GUILayout.Button(DeveloperModeProperties.generateOnPostprocessAllAssets))
                {
                    AssetPostprocessingInternal.s_OnPostprocessAllAssetsCallbacks.GenerateDependencyDiagram("OnPostprocessAllAssets.dot");
                }
            }

            if (m_DeveloperModeDirty)
            {
                ApplyChangesToPrefs();
                EditorApplication.UpdateMainWindowTitle();
            }
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
            CodeEditor.Editor.SetCodeEditor(m_ScriptEditorPath);

            EditorPrefs.SetString("kImagesDefaultApp", m_ImageAppPath);
            EditorPrefs.SetString("kDiffsDefaultApp", m_DiffTools.Length == 0 ? "" : m_DiffTools[m_DiffToolIndex]);

            InternalEditorUtility.SetCustomDiffToolPrefs(m_CustomDiffToolPath, m_CustomDiffToolArguments[0], m_CustomDiffToolArguments[1], m_CustomDiffToolArguments[2]);

            WriteRecentAppsList(m_ScriptApps, m_ScriptEditorPath, kRecentScriptAppsKey);
            WriteRecentAppsList(m_ImageApps, m_ImageAppPath, kRecentImageAppsKey);

            if (m_ContentScaleChangedThisSession)
            {
                EditorPrefs.SetInt(kContentScalePrefKey, m_ContentScalePercentValue);
            }

            if (m_TaskbarBehaviorChangedThisSession)
            {
                if (m_TaskbarBehaviorValue)
                {
                    EditorPrefs.SetBool(kWindowsTaskbarPrefKey, m_TaskbarBehaviorValue);
                }
                else
                {
                    EditorPrefs.DeleteKey(kWindowsTaskbarPrefKey);
                }
            }

            EditorPrefs.SetBool("ReopenLastUsedProjectOnStartup", m_ReopenLastUsedProjectOnStartup);
            EditorPrefs.SetBool("EnableEditorAnalytics", m_EnableEditorAnalytics);
            EditorPrefs.SetBool("SaveScenesBeforeBuilding", m_AutoSaveScenesBeforeBuilding);
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
            EditorPrefs.SetBool("Editor.kEnableCompilerMessagesLocalization", m_EnableCompilerMessagesLocalization);

            EditorPrefs.SetFloat($"EditorTextSharpness_{EditorResources.GetFont(FontDef.Style.Normal).name}", m_EditorTextSharpness);
            EditorApplication.RequestRepaintAllTexts();

            EditorPrefs.SetBool("AllowAlphaNumericHierarchy", m_AllowAlphaNumericHierarchy);
            EditorPrefs.SetInt("DefaultPrefabModeFromHierarchy", (int)m_DefaultPrefabModeFromHierarchy);

            EditorPrefs.SetFloat("EditorBusyProgressDialogDelay", m_ProgressDialogDelay);
            GOCreationCommands.s_PlaceObjectsAtWorldOrigin.value = m_Create3DObjectsAtOrigin;
            EditorPrefs.SetString("GpuDeviceName", m_GpuDevice);

            EditorPrefs.SetBool("GICacheEnableCustomPath", m_GICacheSettings.m_EnableCustomPath);
            EditorPrefs.SetInt("GICacheMaximumSizeGB", m_GICacheSettings.m_MaximumSize);
            EditorPrefs.SetString("GICacheFolder", m_GICacheSettings.m_CachePath);
            EditorPrefs.SetInt("GICacheCompressionLevel", m_GICacheSettings.m_CompressionLevel);

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                extension.WritePreferences();
            }
            UnityEditor.Lightmapping.UpdateCachePath();

            EditorPrefs.SetBool("GraphSnapping", m_GraphSnapping);

            EditorPrefs.SetBool("EnableConstrainProportionsTransformScale", m_EnableConstrainProportionsScalingForNewObjects);
            EditorPrefs.SetBool("UseInspectorExpandedState", AnnotationUtility.useInspectorExpandedState);
            EditorPrefs.SetBool("EnableExtendedLogging", m_EnableExtendedLogging);
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

            var foundScriptEditorPaths = CodeEditor.Editor.GetFoundScriptEditorPaths();

            foreach (var apps in m_ScriptApps)
            {
                foundScriptEditorPaths[apps] = CodeEditor.Editor.GetInstallationForPath(apps).Name;
            }

            m_ScriptApps = new[] { "" }.Concat(foundScriptEditorPaths.Keys).ToArray();
            m_ScriptAppDisplayNames = new[] { "Open by file extension" }.Concat(foundScriptEditorPaths.Values).ToArray();

            m_ImageApps = BuildAppPathList(m_ImageAppPath, kRecentImageAppsKey, "");

            //BuildFriendlyAppNameList(m_ScriptApps, foundScriptEditorPaths, "Open by file extension");

            m_ImageAppDisplayNames = BuildFriendlyAppNameList(m_ImageApps, null,
                L10n.Tr("Open by file extension"));

            m_DiffTools = InternalEditorUtility.GetAvailableDiffTools();

            ReloadCustomDiffToolData();

            if ((m_DiffTools == null || (m_DiffTools.Length == 1 && m_CustomDiffToolPath.Equals(""))))
            {
                m_noDiffToolsMessage = InternalEditorUtility.GetNoDiffToolsDetectedMessage();
            }

            string diffTool = EditorPrefs.GetString("kDiffsDefaultApp");
            m_DiffToolIndex = ArrayUtility.IndexOf(m_DiffTools, diffTool);
            if (m_DiffToolIndex == -1)
                m_DiffToolIndex = 0;

            m_ReopenLastUsedProjectOnStartup = EditorPrefs.GetBool("ReopenLastUsedProjectOnStartup");

            m_EnableEditorAnalytics = EditorPrefs.GetBool("EnableEditorAnalyticsV2", EditorPrefs.GetBool("EnableEditorAnalytics", true));

            m_AutoSaveScenesBeforeBuilding = EditorPrefs.GetBool("SaveScenesBeforeBuilding");
            m_ScriptCompilationDuringPlay = (ScriptChangesDuringPlayOptions)EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0);

            m_DeveloperMode = Unsupported.IsDeveloperMode();
            m_ShowRepaintDots = EditorGUI.s_ShowRepaintDots.value;

            m_GICacheSettings.m_EnableCustomPath = EditorPrefs.GetBool("GICacheEnableCustomPath");
            m_GICacheSettings.m_CachePath = EditorPrefs.GetString("GICacheFolder");
            m_GICacheSettings.m_MaximumSize = EditorPrefs.GetInt("GICacheMaximumSizeGB", 10);
            m_GICacheSettings.m_CompressionLevel = EditorPrefs.GetInt("GICacheCompressionLevel");

            m_ScriptDebugInfoEnabled = EditorPrefs.GetBool("ScriptDebugInfoEnabled", false);
            m_EnableEditorLocalization = EditorPrefs.GetBool("Editor.kEnableEditorLocalization", true);
            m_SelectedLanguage = EditorPrefs.GetString("Editor.kEditorLocale", LocalizationDatabase.GetDefaultEditorLanguage().ToString());
            m_EnableCompilerMessagesLocalization = EditorPrefs.GetBool("Editor.kEnableCompilerMessagesLocalization", false);
            m_EditorTextSharpness = EditorPrefs.GetFloat($"EditorTextSharpness_{EditorResources.GetFont(FontDef.Style.Normal).name}", 0.0f);
            EditorTextSettings.SetCurrentEditorSharpness(m_EditorTextSharpness);

            m_AllowAlphaNumericHierarchy = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);
            m_DefaultPrefabModeFromHierarchy = GetDefaultPrefabModeForHierarchy();
            m_ProgressDialogDelay = EditorPrefs.GetFloat("EditorBusyProgressDialogDelay", 3.0f);
            m_Create3DObjectsAtOrigin = GOCreationCommands.s_PlaceObjectsAtWorldOrigin;

            m_GpuDevice = EditorPrefs.GetString("GpuDeviceName");

            m_EnableConstrainProportionsScalingForNewObjects = EditorPrefs.GetBool("EnableConstrainProportionsTransformScale", false);
            AnnotationUtility.useInspectorExpandedState = EditorPrefs.GetBool("UseInspectorExpandedState", false);

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

            if (EditorPrefs.HasKey(kWindowsTaskbarPrefKey))
            {
                m_TaskbarBehaviorValue = EditorPrefs.GetBool(kWindowsTaskbarPrefKey, false);
            }

            foreach (IPreferenceWindowExtension extension in prefWinExtensions)
            {
                extension.ReadPreferences();
            }

            m_GraphSnapping = EditorPrefs.GetBool("GraphSnapping", true);
            m_EnableExtendedLogging = EditorPrefs.GetBool("EnableExtendedLogging", false);
        }

        internal static void ReloadCustomDiffToolData()
        {
            m_CustomDiffToolPath = EditorPrefs.GetString("customDiffToolPath", m_CustomDiffToolPath);
            m_CustomDiffToolArguments[0] = EditorPrefs.GetString("twoWayDiffArguments", m_CustomDiffToolArguments[0]);
            m_CustomDiffToolArguments[1] = EditorPrefs.GetString("threeWayDiffArguments", m_CustomDiffToolArguments[1]);
            m_CustomDiffToolArguments[2] = EditorPrefs.GetString("mergeArguments", m_CustomDiffToolArguments[2]);
            InternalEditorUtility.SetCustomDiffToolData(m_CustomDiffToolPath, m_CustomDiffToolArguments[0], m_CustomDiffToolArguments[1], m_CustomDiffToolArguments[2]);
        }

        internal static void ForceEnableCustomTool()
        {
            // If diff tools are not initialized yet, get available ones
            if (m_DiffTools == null)
                m_DiffTools = InternalEditorUtility.GetAvailableDiffTools();

            if (m_DiffTools == null || m_DiffTools.Length == 0)
                return;

            m_DiffToolIndex = m_DiffTools.Length - 1;
            EditorPrefs.SetString("kDiffsDefaultApp", m_DiffTools[m_DiffToolIndex]);

            if (EditorWindow.HasOpenInstances<PreferenceSettingsWindow>())
                EditorWindow.GetWindow<PreferenceSettingsWindow>().Repaint();
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

        void AppsListClickRuntimePlatformExtension(object userData, string[] options, int selected)
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

        void AppsListClickNoRuntimePlatformExtension(object userData, string[] options, int selected)
        {
            AppsListUserData ud = (AppsListUserData)userData;
            if (options[selected] == L10n.Tr("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Browse for application", "", "");
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
            FilePopup(label, selectedString, ref names, ref paths, outString, defaultString, onChanged, AppsListClickRuntimePlatformExtension);
        }

        private void FilePopup(GUIContent label, string selectedString, ref string[] names, ref string[] paths, RefString outString, string defaultString, Action onChanged, EditorUtility.SelectMenuItemFunction ApplicationsListOnClick)
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
                if (names[names.Length - 1] != Styles.browse.text)
                {
                    ArrayUtility.Add(ref names, Styles.browse.text);
                }
                EditorUtility.DisplayCustomMenu(r, names, selected, ApplicationsListOnClick, ud, false);
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

        private string[] BuildFriendlyAppNameList(string[] appPathList, Dictionary<string, string> appPathToName, string defaultBuiltIn)
        {
            var list = new List<string>();
            for (int i = 0; i < appPathList.Length; ++i)
            {
                var appPath = appPathList[i];

                if (appPath == CodeEditor.SystemDefaultPath)     // use built-in
                    list.Add(defaultBuiltIn);
                else
                {
                    if (appPathToName != null && appPathToName.ContainsKey(appPath))
                        list.Add(appPathToName[appPath]);
                    else
                        list.Add(appPath);
                }
            }

            return list.ToArray();
        }

        internal static PrefabStage.Mode GetDefaultPrefabModeForHierarchy()
        {
            return (PrefabStage.Mode)EditorPrefs.GetInt("DefaultPrefabModeFromHierarchy", (int)PrefabStage.Mode.InContext);
        }
    }
}
