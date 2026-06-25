// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

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
        GameObjects = 1 << 5,

        // this is just helper enum to display All instead of Every
        All = ~None
    }

    internal static partial class UserPreferences
    {
        public static string Path => k_PreferencesKey;
        const string k_PreferencesKey = "Preferences/Analysis/Project Auditor";

        const string k_EditorPrefsPrefix = "ProjectAuditor";

        private class Styles
        {
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent ProjectAreaSelection = EditorGUIUtility.TrTextContent("Project Areas", "Select project areas to analyze.");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent Analysis = EditorGUIUtility.TrTextContent("Analysis");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent PlatformSelection = EditorGUIUtility.TrTextContent("Platform", "Select the target platform.");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent CodeAnalysisFlagsSelection = EditorGUIUtility.TrTextContent("Code Analysis Areas", "Select which code Project Auditor analyzes.");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent CodeOwnersSelection = EditorGUIUtility.TrTextContent("Code Owners", "Select whose code Project Auditor analyzes.");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent UseRoslynAnalyzers = EditorGUIUtility.TrTextContent("Use Roslyn Analyzers");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent LogTimingsInfo = EditorGUIUtility.TrTextContent("Log timing information");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent Build = EditorGUIUtility.TrTextContent("Build");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent AfterBuild = EditorGUIUtility.TrTextContent("Log number of issues after Build", "Enabling this option will mean that after running a build, Project Auditor will analyze the project and output the total number of issues found to the console.");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent FailBuild = EditorGUIUtility.TrTextContent("Log issues as Errors", "Enable this option to output the issues to the Console as Errors (rather than Info).");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent Report = EditorGUIUtility.TrTextContent("Report");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent PrettifyJSONOutput = EditorGUIUtility.TrTextContent("Prettify saved .projectauditor files");
            [NoAutoStaticsCleanup] // GUIContent Styles field; fixed content, safe to persist across reloads
            public static readonly GUIContent UseBuildSettings = EditorGUIUtility.TrTextContent("Use Build Settings");
        }

        const bool k_UseRoslynAnalyzersDefault = false;
        const bool k_LogTimingsInfoDefault = false;
        const bool k_AnalyzeAfterBuildDefault = false;
        const bool k_FailBuildOnIssuesDefault = false;
        const bool k_PrettifyJSONOutputDefault = false;

        [AutoStaticsCleanupOnCodeReload]
        internal static string LoadSavePath = string.Empty;

        [AutoStaticsCleanupOnCodeReload]
        static BuildTarget[] s_SupportedBuildTargets;
        [AutoStaticsCleanupOnCodeReload]
        static GUIContent[] s_PlatformContents;

        public abstract class Pref<T> where T : unmanaged
        {
            public Pref(string name, T value = default)
            {
                Name = name;
                Value = value;
            }

            public static implicit operator T(Pref<T> pref) => pref.Value;

            public virtual void Set(T value)
            {
                Value = value;
            }

            protected string Name { get; }
            protected T Value { get; set; }
        }

        public class BoolPref : Pref<bool>
        {
            public BoolPref(string name, bool value = default) : base(name, value)
            {
                Value = EditorPrefs.GetBool(MakeKey(name), value);
            }

            public override void Set(bool value)
            {
                if (value != Value)
                    EditorPrefs.SetBool(MakeKey(Name), value);
                base.Set(value);
            }
        }

        public class EnumPref<T> : Pref<T> where T : unmanaged
        {
            public EnumPref(string name, T value = default) : base(name, value)
            {
                Value = (T)(object)EditorPrefs.GetInt(MakeKey(name), (int)(object)value);
            }

            public override void Set(T value)
            {
                if ((int)(object)value != (int)(object)Value)
                    EditorPrefs.SetInt(MakeKey(Name), (int)(object)value); base.Set(value);
            }
        }

        /// <summary>
        /// If enabled, ProjectAuditor will re-run the BuildReport analysis every time the project is built.
        /// </summary>
        [NoAutoStaticsCleanup] // Pref: persists editor preference value across code reload
        public static BoolPref AnalyzeAfterBuild = new BoolPref(nameof(AnalyzeAfterBuild), k_AnalyzeAfterBuildDefault);

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        [NoAutoStaticsCleanup]
        public static BoolPref UseRoslynAnalyzers = new BoolPref(nameof(UseRoslynAnalyzers), k_UseRoslynAnalyzersDefault);

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        [NoAutoStaticsCleanup]
        public static BoolPref FailBuildOnIssues = new BoolPref(nameof(FailBuildOnIssues), k_FailBuildOnIssuesDefault);

        /// <summary>
        /// If enabled, JSON is saved with whitespace and newlines, for easier reading.
        /// </summary>
        [NoAutoStaticsCleanup]
        public static BoolPref PrettifyJsonOutput = new BoolPref(nameof(PrettifyJsonOutput), k_PrettifyJSONOutputDefault);

        [NoAutoStaticsCleanup]
        public static BoolPref LogTimingsInfo = new BoolPref(nameof(LogTimingsInfo), k_LogTimingsInfoDefault);

        static readonly ProjectAreaFlags k_ProjectAreasToAnalyzeDefault = ProjectAreaFlags.All;
        static readonly BuildTarget k_AnalysisTargetPlatformDefault = BuildTarget.NoTarget;
        static readonly CodeAnalysisFlags k_CodeAnalysisFlagsDefault = CodeAnalysisFlagsExtensions.Default;
        static readonly CodeOwnerFlags k_CodeOwnerFlagsDefault = Editor.CodeOwnerFlags.User;

        // stephenm TODO: Not a big fan of the ProjectAreaFlags enum, which is an abstraction of the Tabs, which each
        // contain references to one or more Modules, which reference Analyzers, which report issues in IssueCategories...
        // I think it would be simpler here to just have a list of Modules with checkboxes. But that probably won't
        // play nicely with the current tab navigation and incremental report handling, so it's not worth doing unless
        // we definitely want to go this way with analysis configuration...
        [NoAutoStaticsCleanup]
        public static EnumPref<ProjectAreaFlags> ProjectAreasToAnalyze = new EnumPref<ProjectAreaFlags>(nameof(ProjectAreasToAnalyze), k_ProjectAreasToAnalyzeDefault);

        [NoAutoStaticsCleanup]
        public static EnumPref<BuildTarget> AnalysisTargetPlatform = new EnumPref<BuildTarget>(nameof(AnalysisTargetPlatform), k_AnalysisTargetPlatformDefault);

        [NoAutoStaticsCleanup]
        public static EnumPref<CodeAnalysisFlags> CodeAnalysisFlags = new EnumPref<CodeAnalysisFlags>(nameof(CodeAnalysisFlags), k_CodeAnalysisFlagsDefault);

        [NoAutoStaticsCleanup]
        public static EnumPref<CodeOwnerFlags> CodeOwnerFlags = new EnumPref<CodeOwnerFlags>(nameof(CodeOwnerFlags), k_CodeOwnerFlagsDefault);

        [OnCodeLoaded]
        static void Initialize()
        {
            var buildTargets = Enum.GetValues(typeof(BuildTarget));

            var supportedBuildTargets = new List<BuildTarget>(buildTargets.Length + 1)
            {
                BuildTarget.NoTarget
            };

            foreach (BuildTarget bt in buildTargets)
            {
                if (BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(bt), bt))
                    supportedBuildTargets.Add(bt);
            }

            supportedBuildTargets.Sort(
                1,
                supportedBuildTargets.Count - 1,
                Comparer<BuildTarget>.Create((t1, t2) => string.Compare(
                    t1.ToString(),
                    t2.ToString(),
                    StringComparison.Ordinal
                )));

            s_SupportedBuildTargets = supportedBuildTargets.ToArray();

            s_PlatformContents = Array.ConvertAll(s_SupportedBuildTargets,
                t => (t == BuildTarget.NoTarget) ? Styles.UseBuildSettings : EditorGUIUtility.TrTextContent(Formatting.GetModernBuildTargetName(t)));
        }

        public static EditorWindow OpenPreferencesWindow()
        {
            return SettingsService.OpenUserPreferences(k_PreferencesKey);
        }

        [SettingsProvider]
        internal static SettingsProvider CreatePreferencesProvider()
        {
            var keywords = new HashSet<string>(["performance", "optimization", "analysis"]);
            foreach (var keyword in SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>())
                keywords.Add(keyword);

            var settings = new SettingsProvider(k_PreferencesKey, SettingsScope.User)
            {
                guiHandler = PreferencesGUI,
                keywords = keywords
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

            using var _ = new SettingsWindow.GUIScope();

            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.LabelField(Styles.Analysis, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SharedPreferencesGUI();

            GUILayout.Space(10f);

            UseRoslynAnalyzers.Set(EditorGUILayout.Toggle(Styles.UseRoslynAnalyzers, UseRoslynAnalyzers));
            LogTimingsInfo.Set(EditorGUILayout.Toggle(Styles.LogTimingsInfo, LogTimingsInfo));

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField(Styles.Build, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            AnalyzeAfterBuild.Set(EditorGUILayout.Toggle(Styles.AfterBuild, AnalyzeAfterBuild));
            using (new EditorGUI.DisabledScope(!AnalyzeAfterBuild))
            {
                EditorGUI.indentLevel++;
                if (!AnalyzeAfterBuild)
                    FailBuildOnIssues.Set(false);
                FailBuildOnIssues.Set(EditorGUILayout.Toggle(Styles.FailBuild, FailBuildOnIssues));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField(Styles.Report, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            PrettifyJsonOutput.Set(EditorGUILayout.Toggle(Styles.PrettifyJSONOutput, PrettifyJsonOutput));

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);
        }

        internal static void SharedPreferencesGUI()
        {
            ProjectAreasToAnalyze.Set((ProjectAreaFlags)EditorGUILayout.EnumFlagsField(Styles.ProjectAreaSelection, ProjectAreasToAnalyze, GUILayout.ExpandWidth(true)));

            var selectedTarget = Array.IndexOf(s_SupportedBuildTargets, AnalysisTargetPlatform);

            // AnalysisTargetPlatform is not supported in this Unity Editor. Perhaps it was selected in a different Editor version.
            // Reset it to "Use Build Settings"
            if (selectedTarget < 0)
            {
                selectedTarget = 0;
            }

            selectedTarget = EditorGUILayout.Popup(Styles.PlatformSelection, selectedTarget, s_PlatformContents);
            AnalysisTargetPlatform.Set(s_SupportedBuildTargets[selectedTarget]);

            using (new EditorGUI.DisabledScope((ProjectAreasToAnalyze & ProjectAreaFlags.Code) == 0))
            {
                CodeAnalysisGUI();
            }
        }

        internal static void CodeAnalysisGUI()
        {
            CodeAnalysisFlags.Set((CodeAnalysisFlags)EditorGUILayout.EnumFlagsField(Styles.CodeAnalysisFlagsSelection, CodeAnalysisFlags, GUILayout.ExpandWidth(true)));

            if (Unsupported.IsDeveloperMode())
                CodeOwnerFlags.Set((CodeOwnerFlags)EditorGUILayout.EnumFlagsField(Styles.CodeOwnersSelection, CodeOwnerFlags, GUILayout.ExpandWidth(true)));
        }
    }
}
