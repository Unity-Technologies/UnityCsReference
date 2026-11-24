// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Scripting.ScriptCompilation.MsBuild;
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
        GameObjects = 1 << 5,

        // this is just helper enum to display All instead of Every
        All = ~None
    }

    internal static partial class UserPreferences
    {
        public static string Path => k_PreferencesKey;
        const string k_PreferencesKey = "Preferences/Analysis/Project Auditor";

        const string k_EditorPrefsPrefix = "ProjectAuditor";
        internal const string k_LegacySuppressedDiagnosticsKey = k_EditorPrefsPrefix + ".SuppressedDiagnostics";

        private class Styles
        {
            public static readonly GUIContent ProjectAreaSelection = L10n.TextContent("Project Areas", "Select project areas to analyze.", null, null);
            public static readonly GUIContent Analysis = L10n.TextContent("Analysis", null, null, null);
            public static readonly GUIContent PlatformSelection = L10n.TextContent("Platform", "Select the target platform.", null, null);
            public static readonly GUIContent CodeAnalysisFlagsSelection = L10n.TextContent("Code Analysis Areas", "Select which code Project Auditor analyzes.", null, null);
            public static readonly GUIContent OwnersSelection = L10n.TextContent("Owners", "Select whose code and assets Project Auditor analyzes.", null, null);
            public static readonly GUIContent LogTimingsInfo = L10n.TextContent("Log timing information", null, null, null);
            public static readonly GUIContent Build = L10n.TextContent("Build", null, null, null);
            public static readonly GUIContent AfterBuild = L10n.TextContent("Log number of issues after Build", "Enabling this option will mean that after running a build, Project Auditor will analyze the project and output the total number of issues found to the console.", null, null);
            public static readonly GUIContent FailBuild = L10n.TextContent("Log issues as Errors", "Enable this option to output the issues to the Console as Errors (rather than Info).", null, null);
            public static readonly GUIContent Report = L10n.TextContent("Report", null, null, null);
            public static readonly GUIContent PrettifyJSONOutput = L10n.TextContent("Prettify saved .projectauditor files", null, null, null);
            public static readonly GUIContent UseBuildSettings = L10n.TextContent("Use Build Settings", null, null, null);

            public static readonly string CodeAnalysisEditorAndPlayerWarning = L10n.Tr("Setting Code Analysis Areas to include both Editor and Player code compiles the project twice, so analysis will take longer.", null);
        }

        const bool k_LogTimingsInfoDefault = false;
        const bool k_AnalyzeAfterBuildDefault = false;
        const bool k_FailBuildOnIssuesDefault = false;
        const bool k_PrettifyJSONOutputDefault = false;

        // Characters accepted as delimiters between issue IDs in the suppressed-issues list.
        // Same set the C# compiler accepts in its /nowarn list, so both suppression lists behave alike.
        static readonly char[] k_SuppressedDiagnosticsSeparators = { ',', ';', ' ' };

        [AutoStaticsCleanupOnCodeReload]
        internal static string LoadSavePath = string.Empty;

        [AutoStaticsCleanupOnCodeReload]
        static BuildTarget[] s_SupportedBuildTargets;
        [AutoStaticsCleanupOnCodeReload]
        static GUIContent[] s_PlatformContents;

        public abstract class Pref<T>
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
        static readonly OwnerFlags k_OwnerFlagsDefault = Editor.OwnerFlags.User;

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
        public static EnumPref<OwnerFlags> OwnerFlags = new EnumPref<OwnerFlags>("CodeOwnerFlags", k_OwnerFlagsDefault);

        [OnCodeLoaded]
        static void Initialize()
        {
            if (EditorPrefs.HasKey(k_LegacySuppressedDiagnosticsKey))
                EditorPrefs.DeleteKey(k_LegacySuppressedDiagnosticsKey);

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
                t => (t == BuildTarget.NoTarget) ? Styles.UseBuildSettings : L10n.TextContent(Formatting.GetModernBuildTargetName(t), null, null, null));
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

            ProjectAreasPreferencesGUI();
            SharedPreferencesGUI(ProjectAreasToAnalyze);

            GUILayout.Space(10f);

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

        internal static void ProjectAreasPreferencesGUI()
        {
            ProjectAreasToAnalyze.Set((ProjectAreaFlags)EditorGUILayout.EnumFlagsField(Styles.ProjectAreaSelection, ProjectAreasToAnalyze, GUILayout.ExpandWidth(true)));
        }

        internal static void SharedPreferencesGUI(ProjectAreaFlags projectAreasToAnalyze)
        {
            var selectedTarget = Array.IndexOf(s_SupportedBuildTargets, AnalysisTargetPlatform);

            // AnalysisTargetPlatform is not supported in this Unity Editor. Perhaps it was selected in a different Editor version.
            // Reset it to "Use Build Settings"
            if (selectedTarget < 0)
            {
                selectedTarget = 0;
            }

            selectedTarget = EditorGUILayout.Popup(Styles.PlatformSelection, selectedTarget, s_PlatformContents);
            AnalysisTargetPlatform.Set(s_SupportedBuildTargets[selectedTarget]);

            using (new EditorGUI.DisabledScope((projectAreasToAnalyze & ProjectAreaFlags.Code) == 0))
            {
                CodeAnalysisGUI();
            }

            OwnersGUI(projectAreasToAnalyze);
        }

        internal static void CodeAnalysisGUI()
        {
            CodeAnalysisFlags.Set((CodeAnalysisFlags)EditorGUILayout.EnumFlagsField(Styles.CodeAnalysisFlagsSelection, CodeAnalysisFlags, GUILayout.ExpandWidth(true)));
        }

        internal static void OwnersGUI(ProjectAreaFlags projectAreasToAnalyze)
        {
            if (Unsupported.IsDeveloperMode())
                OwnerFlags.Set((OwnerFlags)EditorGUILayout.EnumFlagsField(Styles.OwnersSelection, OwnerFlags, GUILayout.ExpandWidth(true)));

            const CodeAnalysisFlags editorAndPlayer = Editor.CodeAnalysisFlags.Editor | Editor.CodeAnalysisFlags.Player;
            if (MsBuildCompilationInterface.IsEnabled() && (CodeAnalysisFlags & editorAndPlayer) == editorAndPlayer && (projectAreasToAnalyze & ProjectAreaFlags.Code) != 0)
                EditorGUILayout.HelpBox(Styles.CodeAnalysisEditorAndPlayerWarning, MessageType.Warning);
        }

        /// <summary>
        /// The set of suppressed issue IDs, parsed from the delimited <see cref="SuppressedDiagnostics"/>
        /// preference. Comparison is case-insensitive to match how IDs are authored.
        /// </summary>
        public static HashSet<string> BuildSuppressedDiagnosticsSet()
        {
            return new HashSet<string>(ParseSuppressedDiagnostics(), StringComparer.OrdinalIgnoreCase);
        }

        internal static string SuppressedDiagnostics => ProjectAuditorSettings.instance.SuppressedDiagnostics;

        internal static void SetSuppressedDiagnostics(string value)
        {
            var settings = ProjectAuditorSettings.instance;
            if (value == settings.SuppressedDiagnostics)
                return;

            settings.SuppressedDiagnostics = value;
            settings.Save();
        }

        // Every write to SuppressedDiagnostics must be re-joined from this list, or malformed input survives.
        static List<string> ParseSuppressedDiagnostics()
        {
            var ids = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in SuppressedDiagnostics.Split(k_SuppressedDiagnosticsSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var id = token.Trim();
                if (id.Length > 0 && seen.Add(id))
                    ids.Add(id);
            }

            return ids;
        }

        public static void WarnOnInvalidSuppressedDiagnostics(HashSet<string> suppressed)
        {
            var sb = new StringBuilder();
            foreach (var s in suppressed)
            {
                var id = s.ToUpperInvariant();
                if (DescriptorId.IsValidIdFormat(id))
                {
                    if (!DescriptorLibrary.HasDescriptor(new DescriptorId(id)))
                        sb.AppendLine($"{id} is not a known Descriptor");
                }
                else
                {
                    sb.AppendLine($"{id} is not in the correct format (AB1234 or ABC1234)");
                }
            }

            if (sb.Length > 0)
                Debug.LogWarning("Some suppressed diagnostics are invalid. Fix them by navigating to " + ProjectAuditor.k_ProjectSettingsPath + " > Suppressed Issues:\n" + sb.ToString());
        }

        // Adds or removes the given issue ID from the suppressed-issues list, depending on its current state.
        internal static void ToggleSuppressedDiagnostic(string id, HashSet<string> suppressedDiagnostics)
        {
            if (!AddSuppressedDiagnostics(new[] { id }, suppressedDiagnostics))
                RemoveSuppressedDiagnostics(new[] { id }, suppressedDiagnostics);
        }

        // Repaints any open Preferences/Settings windows so changes made elsewhere (e.g. from the Search window)
        // are reflected in the Suppressed Issues field.
        internal static void RepaintPreferencesWindow()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<SettingsWindow>())
                window.Repaint();
        }

        internal static bool AddSuppressedDiagnostics(IEnumerable<string> idsToAdd, HashSet<string> suppressedDiagnostics = null)
        {
            suppressedDiagnostics = suppressedDiagnostics ?? BuildSuppressedDiagnosticsSet();

            var ids = ParseSuppressedDiagnostics();
            var added = false;
            foreach (var id in idsToAdd)
            {
                if (!suppressedDiagnostics.Add(id))
                    continue;

                ids.Add(id);
                added = true;
            }

            if (added)
                SetSuppressedDiagnostics(string.Join(", ", ids));

            return added;
        }

        internal static bool RemoveSuppressedDiagnostics(IEnumerable<string> idsToRemove, HashSet<string> suppressedDiagnostics = null)
        {
            suppressedDiagnostics = suppressedDiagnostics ?? BuildSuppressedDiagnosticsSet();

            var kept = ParseSuppressedDiagnostics();
            var removed = false;
            foreach (var id in idsToRemove)
            {
                if (!suppressedDiagnostics.Remove(id))
                    continue;

                kept.RemoveAll(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
                removed = true;
            }

            if (removed)
                SetSuppressedDiagnostics(string.Join(", ", kept));

            return removed;
        }
    }
}
