// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
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
        GameObjects = 1 << 5,

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
        static readonly GUIContent CodeAnalysisFlagsSelection =
            new GUIContent("Code Analysis Areas", "Select which code Project Auditor analyzes.");
        static readonly GUIContent CodeOwnersSelection =
            new GUIContent("Code Owners", "Select whose code Project Auditor analyzes.");

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

        internal static string LoadSavePath = string.Empty;

        static BuildTarget[] s_SupportedBuildTargets;
        static GUIContent[] s_PlatformContents;

        const string k_UseBuildSettings = "Use Build Settings";

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
        public static BoolPref AnalyzeAfterBuild = new BoolPref(nameof(AnalyzeAfterBuild), k_AnalyzeAfterBuildDefault);

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public static BoolPref UseRoslynAnalyzers = new BoolPref(nameof(UseRoslynAnalyzers), k_UseRoslynAnalyzersDefault);

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public static BoolPref FailBuildOnIssues = new BoolPref(nameof(FailBuildOnIssues), k_FailBuildOnIssuesDefault);

        /// <summary>
        /// If enabled, JSON is saved with whitespace and newlines, for easier reading.
        /// </summary>
        public static BoolPref PrettifyJsonOutput = new BoolPref(nameof(PrettifyJsonOutput), k_PrettifyJSONOutputDefault);

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
        public static EnumPref<ProjectAreaFlags> ProjectAreasToAnalyze = new EnumPref<ProjectAreaFlags>(nameof(ProjectAreasToAnalyze), k_ProjectAreasToAnalyzeDefault);

        public static EnumPref<BuildTarget> AnalysisTargetPlatform = new EnumPref<BuildTarget>(nameof(AnalysisTargetPlatform), k_AnalysisTargetPlatformDefault);

        public static EnumPref<CodeAnalysisFlags> CodeAnalysisFlags = new EnumPref<CodeAnalysisFlags>(nameof(CodeAnalysisFlags), k_CodeAnalysisFlagsDefault);

        public static EnumPref<CodeOwnerFlags> CodeOwnerFlags = new EnumPref<CodeOwnerFlags>(nameof(CodeOwnerFlags), k_CodeOwnerFlagsDefault);

        static UserPreferences()
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
                t => new GUIContent((t == BuildTarget.NoTarget) ? k_UseBuildSettings : Formatting.GetModernBuildTargetName(t)));
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
                keywords = new HashSet<string>(["performance", "static", "analysis"])
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

            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SharedPreferencesGUI();

            GUILayout.Space(10f);

            UseRoslynAnalyzers.Set(EditorGUILayout.Toggle(k_UseRoslynAnalyzersLabel, UseRoslynAnalyzers));
            LogTimingsInfo.Set(EditorGUILayout.Toggle(k_LogTimingsInfoLabel, LogTimingsInfo));

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            AnalyzeAfterBuild.Set(EditorGUILayout.Toggle(AfterBuildLabelContent, AnalyzeAfterBuild));
            using (new EditorGUI.DisabledScope(!AnalyzeAfterBuild))
            {
                EditorGUI.indentLevel++;
                if (!AnalyzeAfterBuild)
                    FailBuildOnIssues.Set(false);
                FailBuildOnIssues.Set(EditorGUILayout.Toggle(FailBuildLabelContent, FailBuildOnIssues));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            PrettifyJsonOutput.Set(EditorGUILayout.Toggle(k_PrettifyJSONOutputLabel, PrettifyJsonOutput));

            EditorGUI.indentLevel--;
            GUILayout.Space(10f);
        }

        internal static void SharedPreferencesGUI()
        {
            ProjectAreasToAnalyze.Set((ProjectAreaFlags)EditorGUILayout.EnumFlagsField(ProjectAreaSelection, ProjectAreasToAnalyze, GUILayout.ExpandWidth(true)));

            var selectedTarget = Array.IndexOf(s_SupportedBuildTargets, AnalysisTargetPlatform);

            // AnalysisTargetPlatform is not supported in this Unity Editor. Perhaps it was selected in a different Editor version.
            // Reset it to "Use Build Settings"
            if (selectedTarget < 0)
            {
                selectedTarget = 0;
            }

            selectedTarget = EditorGUILayout.Popup(PlatformSelection, selectedTarget, s_PlatformContents);
            AnalysisTargetPlatform.Set(s_SupportedBuildTargets[selectedTarget]);

            using (new EditorGUI.DisabledScope((ProjectAreasToAnalyze & ProjectAreaFlags.Code) == 0))
            {
                CodeAnalysisGUI();
            }
        }

        internal static void CodeAnalysisGUI()
        {
            CodeAnalysisFlags.Set((CodeAnalysisFlags)EditorGUILayout.EnumFlagsField(CodeAnalysisFlagsSelection, CodeAnalysisFlags, GUILayout.ExpandWidth(true)));

            if (Unsupported.IsDeveloperMode())
                CodeOwnerFlags.Set((CodeOwnerFlags)EditorGUILayout.EnumFlagsField(CodeOwnersSelection, CodeOwnerFlags, GUILayout.ExpandWidth(true)));
        }
    }
}
