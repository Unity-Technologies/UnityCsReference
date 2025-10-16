// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define PA_WELCOME_VIEW_OPTIONS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    [Flags]
    enum ProjectAreaFlags
    {
        None = 0,
        Code = 1 << 0,
        ProjectSettings = 1 << 1,
        Assets = 1 << 2,
        Shaders = 1 << 3,
        Build = 1 << 4,

        // this is just helper enum to display All instead of Every
        All = ~None
    }

    internal static class UserPreferences
    {
        public static string Path => k_PreferencesKey;
        static readonly string k_PreferencesKey = "Preferences/Analysis/Project Auditor";

        static readonly string k_EditorPrefsPrefix = "ProjectAuditor";

        static readonly GUIContent ProjectAreaSelection =
            new GUIContent("Project Areas", $"Select project areas to analyze.");
        static readonly GUIContent PlatformSelection =
            new GUIContent("Platform", "Select the target platform.");
        static readonly GUIContent CompilationModeSelection =
            new GUIContent("Compilation Mode", "Select the compilation mode.");

        static readonly string k_UseRoslynAnalyzersLabel = "Use Roslyn Analyzers";
        static readonly bool k_UseRoslynAnalyzersDefault = false;

        static readonly string k_LogTimingsInfoLabel = "Log timing information";
        static readonly bool k_LogTimingsInfoDefault = false;

        static readonly string k_AnalyzeAfterBuildLabel = "Log number of issues after Build";
        static readonly string k_AnalyzeAfterBuildLabelTooltip = "Enabling this option will mean that after running a build, Project Auditor will analyze the project and output the total number of issues found to the console.";
        static readonly GUIContent AfterBuildLabelContent = new GUIContent(k_AnalyzeAfterBuildLabel, k_AnalyzeAfterBuildLabelTooltip);
        static readonly bool k_AnalyzeAfterBuildDefault = false;

        static readonly string k_FailBuildOnIssuesLabel = "Log issues as Errors";
        static readonly string k_FailBuildOnIssuesLabelTooltip = "Enable this option to output the issues to the Console as Errors (rather than Info).";
        static readonly GUIContent FailBuildLabelContent = new GUIContent(k_FailBuildOnIssuesLabel, k_FailBuildOnIssuesLabelTooltip);
        static readonly bool k_FailBuildOnIssuesDefault = false;

        static readonly string k_PrettifyJSONOutputLabel = "Prettify saved .projectauditor files";
        static readonly bool k_PrettifyJSONOutputDefault = false;

        static readonly GUIContent AnalyzePackageContentsForIssues =
            new GUIContent("Analyze Package Contents for Issues", "When ticked the Project Auditor will report issues in Packages.");
        static readonly bool k_AnalyzePackageContentsForAssetIssuesDefault = false;

        internal static string LoadSavePath = string.Empty;

        static BuildTarget[] s_SupportedBuildTargets;
        static GUIContent[] s_PlatformContents;

        /// <summary>
        /// If enabled, ProjectAuditor will re-run the BuildReport analysis every time the project is built.
        /// </summary>
        public static bool AnalyzeAfterBuild
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(AnalyzeAfterBuild)), k_AnalyzeAfterBuildDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(AnalyzeAfterBuild)), value);
        }

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public static bool UseRoslynAnalyzers
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(UseRoslynAnalyzers)), k_UseRoslynAnalyzersDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(UseRoslynAnalyzers)), value);
        }

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public static bool FailBuildOnIssues
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(FailBuildOnIssues)), k_FailBuildOnIssuesDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(FailBuildOnIssues)), value);
        }

        private static bool m_AnalyzePackageForIssues = false;
        private static bool m_AnalyzePackageIsCached = false;

        public static Action<bool> OnAnalyzePackageIsChanged { get; set; }
        public static bool AnalyzePackagesForIssues
        {
            get
            {
                return GetAnalyzePackageForIssues();
            }
            set
            {
                EditorPrefs.SetBool(MakeKey(nameof(AnalyzePackagesForIssues)), value);

                if (m_AnalyzePackageForIssues != value)
                {
                    m_AnalyzePackageForIssues = value;
                    m_AnalyzePackageIsCached = true;

                    if (OnAnalyzePackageIsChanged != null)
                        OnAnalyzePackageIsChanged(value);
                }
            }
        }

        static bool GetAnalyzePackageForIssues()
        {
            if (!m_AnalyzePackageIsCached)
            {
                m_AnalyzePackageForIssues = EditorPrefs.GetBool(MakeKey(nameof(AnalyzePackagesForIssues)),
                    k_AnalyzePackageContentsForAssetIssuesDefault);
                m_AnalyzePackageIsCached = true;
            }

            return m_AnalyzePackageForIssues;
        }

        public static bool PrettifyJsonOutput
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(PrettifyJsonOutput)), k_PrettifyJSONOutputDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(PrettifyJsonOutput)), value);
        }

        public static bool LogTimingsInfo
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(LogTimingsInfo)), k_LogTimingsInfoDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(LogTimingsInfo)), value);
        }
        static readonly ProjectAreaFlags k_ProjectAreasToAnalyzeDefault = ProjectAreaFlags.All;
        static readonly BuildTarget k_AnalysisTargetPlatformDefault = BuildTarget.NoTarget;
        // stephenm TODO: Still think it'd be great to default this to EditorPlayMode or the proposed "hybrid" option
        static readonly CompilationMode k_CompilationModeDefault = CompilationMode.Player;

        // stephenm TODO: Not a big fan of the ProjectAreaFlags enum, which is an abstraction of the Tabs, which each
        // contain references to one or more Modules, which reference Analyzers, which report issues in IssueCategories...
        // I think it would be simpler here to just have a list of Modules with checkboxes. But that probably won't
        // play nicely with the current tab navigation and incremental report handling, so it's not worth doing unless
        // we definitely want to go this way with analysis configuration...
        public static ProjectAreaFlags ProjectAreasToAnalyze
        {
            get => (ProjectAreaFlags)EditorPrefs.GetInt(
                MakeKey(nameof(ProjectAreasToAnalyze)), (int)k_ProjectAreasToAnalyzeDefault);
            set => EditorPrefs.SetInt(MakeKey(nameof(ProjectAreasToAnalyze)), (int)value);
        }

        public static BuildTarget AnalysisTargetPlatform
        {
            get => (BuildTarget)EditorPrefs.GetInt(
                MakeKey(nameof(AnalysisTargetPlatform)), (int)k_AnalysisTargetPlatformDefault);
            set => EditorPrefs.SetInt(MakeKey(nameof(AnalysisTargetPlatform)), (int)value);
        }

        public static CompilationMode CompilationMode
        {
            get => (CompilationMode)EditorPrefs.GetInt(
                MakeKey(nameof(CompilationMode)), (int)k_CompilationModeDefault);
            set => EditorPrefs.SetInt(MakeKey(nameof(CompilationMode)), (int)value);
        }


        static UserPreferences()
        {
            var buildTargets = Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>();
            var supportedBuildTargets = buildTargets.Where(bt =>
                BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(bt), bt)).ToList();
            supportedBuildTargets.Sort((t1, t2) =>
                String.Compare(t1.ToString(), t2.ToString(), StringComparison.Ordinal));

            // Add at the beginning of the list, after sorting the other options
            supportedBuildTargets.Insert(0, BuildTarget.NoTarget);

            s_SupportedBuildTargets = supportedBuildTargets.ToArray();

            s_PlatformContents = s_SupportedBuildTargets
                .Select(t => new GUIContent((t == BuildTarget.NoTarget) ? "Use Build Settings" : Formatting.GetModernBuildTargetName(t))).ToArray();

            GetAnalyzePackageForIssues();
        }

        public static EditorWindow OpenPreferencesWindow()
        {
            return SettingsService.OpenUserPreferences(k_PreferencesKey);
        }

        [SettingsProvider]
        internal static SettingsProvider CreatePreferencesProvider()
        {
            var settings = new SettingsProvider(k_PreferencesKey, SettingsScope.User)
            {
                guiHandler = PreferencesGUI,
                keywords = new HashSet<string>(new[] { "performance", "static", "analysis" })
            };
            return settings;
        }

        static string MakeKey(string key)
        {
            return $"{k_EditorPrefsPrefix}.{key}";
        }

        static void PreferencesGUI(string searchContext)
        {
            const float labelWidth = 300f;

            EditorGUIUtility.labelWidth = labelWidth;


            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            ProjectAreasToAnalyze = (ProjectAreaFlags)EditorGUILayout.EnumFlagsField(ProjectAreaSelection, ProjectAreasToAnalyze, GUILayout.ExpandWidth(true));

            var selectedTarget = Array.IndexOf(s_SupportedBuildTargets, AnalysisTargetPlatform);

            // AnalysisTargetPlatform is not supported in this Unity Editor. Perhaps it was selected in a different Editor version.
            // Reset it to "Use Build Settings"
            if (selectedTarget < 0)
            {
                selectedTarget = 0;
            }

            selectedTarget = EditorGUILayout.Popup(PlatformSelection, selectedTarget, s_PlatformContents);
            AnalysisTargetPlatform = s_SupportedBuildTargets[selectedTarget];

            CompilationMode = (CompilationMode)EditorGUILayout.EnumPopup(CompilationModeSelection, CompilationMode);

            GUILayout.Space(10f);
            UseRoslynAnalyzers = EditorGUILayout.Toggle(k_UseRoslynAnalyzersLabel, UseRoslynAnalyzers);
            LogTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, LogTimingsInfo);

            GUILayout.Space(10f);

            AnalyzePackagesForIssues = EditorGUILayout.Toggle(AnalyzePackageContentsForIssues, AnalyzePackagesForIssues);

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            AnalyzeAfterBuild = EditorGUILayout.Toggle(AfterBuildLabelContent, AnalyzeAfterBuild);
            using (new EditorGUI.DisabledScope(!AnalyzeAfterBuild))
            {
                EditorGUI.indentLevel++;
                if (!AnalyzeAfterBuild)
                    FailBuildOnIssues = false;
                FailBuildOnIssues = EditorGUILayout.Toggle(FailBuildLabelContent, FailBuildOnIssues);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            PrettifyJsonOutput = EditorGUILayout.Toggle(k_PrettifyJSONOutputLabel, PrettifyJsonOutput);

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);
        }
    }
}
