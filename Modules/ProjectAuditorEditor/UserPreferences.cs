// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
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
            public static readonly GUIContent ProjectAreaSelection = EditorGUIUtility.TrTextContent("Project Areas", "Select project areas to analyze.");
            public static readonly GUIContent Analysis = EditorGUIUtility.TrTextContent("Analysis");
            public static readonly GUIContent PlatformSelection = EditorGUIUtility.TrTextContent("Platform", "Select the target platform.");
            public static readonly GUIContent CodeAnalysisFlagsSelection = EditorGUIUtility.TrTextContent("Code Analysis Areas", "Select which code Project Auditor analyzes.");
            public static readonly GUIContent CodeOwnersSelection = EditorGUIUtility.TrTextContent("Code Owners", "Select whose code Project Auditor analyzes.");
            public static readonly GUIContent LogTimingsInfo = EditorGUIUtility.TrTextContent("Log timing information");
            public static readonly GUIContent Build = EditorGUIUtility.TrTextContent("Build");
            public static readonly GUIContent AfterBuild = EditorGUIUtility.TrTextContent("Log number of issues after Build", "Enabling this option will mean that after running a build, Project Auditor will analyze the project and output the total number of issues found to the console.");
            public static readonly GUIContent FailBuild = EditorGUIUtility.TrTextContent("Log issues as Errors", "Enable this option to output the issues to the Console as Errors (rather than Info).");
            public static readonly GUIContent Report = EditorGUIUtility.TrTextContent("Report");
            public static readonly GUIContent PrettifyJSONOutput = EditorGUIUtility.TrTextContent("Prettify saved .projectauditor files");
            public static readonly GUIContent UseBuildSettings = EditorGUIUtility.TrTextContent("Use Build Settings");
            public static readonly GUIContent SuppressedDiagnostics = EditorGUIUtility.TrTextContent("Suppressed Issues", "A comma- or semicolon-delimited list of issue IDs to exclude from analysis. Use the search button to add/browse IDs from the list of known issues.");
            public static readonly GUIContent Manage = EditorGUIUtility.TrTextContent("Manage", "Open in Search");
            public static readonly GUIContent ManageDisabled = EditorGUIUtility.TrTextContent("Manage", "Open the Project Auditor window to enable browsing.");
        }

        const bool k_LogTimingsInfoDefault = false;
        const bool k_AnalyzeAfterBuildDefault = false;
        const bool k_FailBuildOnIssuesDefault = false;
        const bool k_PrettifyJSONOutputDefault = false;
        const string k_SuppressedDiagnosticsDefault = "";

        // Characters accepted as delimiters between issue IDs in the suppressed-issues list.
        static readonly char[] k_SuppressedDiagnosticsSeparators = { ',', ';' };

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

        public class StringPref : Pref<string>
        {
            public StringPref(string name, string value = "") : base(name, value)
            {
                Value = EditorPrefs.GetString(MakeKey(name), value);
            }

            public override void Set(string value)
            {
                if (value != Value)
                    EditorPrefs.SetString(MakeKey(Name), value);
                base.Set(value);
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

        /// <summary>
        /// A delimited list of issue IDs to exclude from analysis. See <see cref="BuildSuppressedDiagnosticsSet"/>.
        /// </summary>
        [NoAutoStaticsCleanup]
        public static StringPref SuppressedDiagnostics = new StringPref(nameof(SuppressedDiagnostics), k_SuppressedDiagnosticsDefault);

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

            SuppressedDiagnosticsGUI();
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

        /// <summary>
        /// The set of suppressed issue IDs, parsed from the delimited <see cref="SuppressedDiagnostics"/>
        /// preference. Comparison is case-insensitive to match how IDs are authored.
        /// </summary>
        public static HashSet<string> BuildSuppressedDiagnosticsSet()
        {
            var suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in ((string)SuppressedDiagnostics).Split(k_SuppressedDiagnosticsSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var id = token.Trim();
                if (id.Length > 0)
                    suppressed.Add(id);
            }

            return suppressed;
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
                    sb.AppendLine($"{id} is not in the correct format (ABC1234)");
                }
            }

            if (sb.Length > 0)
                Debug.LogWarning("Some suppressed diagnostics are invalid. Fix them by navigating to " + ProjectAuditor.k_PreferencesPath + " > Suppressed Issues:\n" + sb.ToString());
        }

        // A text field listing the suppressed issue IDs, plus a search button (populated from the DescriptorLibrary) to browse the known issues.
        static void SuppressedDiagnosticsGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // A unique control name plus DelayedTextField prevents IMGUI from sharing this field's recycled text
                // editor with the SettingsWindow search box (which otherwise copies this field's value into itself).
                GUI.SetNextControlName("ProjectAuditor.SuppressedDiagnostics");

                var hasLoadedDescriptors = (DescriptorLibrary.GetAllDescriptors().Count > 0);
                SuppressedDiagnostics.Set(EditorGUILayout.DelayedTextField(Styles.SuppressedDiagnostics, SuppressedDiagnostics, GUILayout.ExpandWidth(true)));
                using (new EditorGUI.DisabledScope(!hasLoadedDescriptors))
                {
                    var content = hasLoadedDescriptors ? Styles.Manage : Styles.ManageDisabled;
                    if (GUILayout.Button(content, GUILayout.ExpandWidth(false)))
                        Utility.SearchWindow(DescriptorSearchProvider.kProviderId, "Project Auditor Issue Types");
                }
            }
        }

        // Adds or removes the given issue ID from the suppressed-issues list, depending on its current state.
        internal static void ToggleSuppressedDiagnostic(string id, HashSet<string> suppressedDiagnostics)
        {
            if (!AddSuppressedDiagnostic(id, suppressedDiagnostics))
                RemoveSuppressedDiagnostic(id, suppressedDiagnostics);
        }

        // Repaints any open Preferences/Settings windows so changes made elsewhere (e.g. from the Search window)
        // are reflected in the Suppressed Issues field.
        internal static void RepaintPreferencesWindow()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<SettingsWindow>())
                window.Repaint();
        }

        // Appends an ID to the suppressed-issues list.
        internal static bool AddSuppressedDiagnostic(string id, HashSet<string> suppressedDiagnostics)
        {
            if (suppressedDiagnostics.Contains(id))
                return false; // already suppressed

            string current = SuppressedDiagnostics;
            var separator = string.IsNullOrEmpty(current.Trim()) ? string.Empty : ", ";
            SuppressedDiagnostics.Set($"{current.TrimEnd()}{separator}{id}");
            suppressedDiagnostics.Add(id);
            return true;
        }

        // Removes an ID from the suppressed-issues list, preserving the order of the remaining IDs.
        internal static bool RemoveSuppressedDiagnostic(string id, HashSet<string> suppressedDiagnostics)
        {
            if (!suppressedDiagnostics.Contains(id))
                return false; // not suppressed

            var kept = new List<string>();
            foreach (var token in ((string)SuppressedDiagnostics).Split(k_SuppressedDiagnosticsSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = token.Trim();
                if (trimmed.Length > 0 && !string.Equals(trimmed, id, StringComparison.OrdinalIgnoreCase))
                    kept.Add(trimmed);
            }

            SuppressedDiagnostics.Set(string.Join(", ", kept));
            suppressedDiagnostics.Remove(id);
            return true;
        }
    }
}
