// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class MigrationWorkflowView : MigrateToURPSummaryView
    {
        List<MigrationStep> m_Steps;
        MigrationWizardRunner m_Runner;

        UnityEditor.PackageManager.Requests.AddRequest m_UrpAddRequest;
        string m_CreateAssignError;

        string m_ReanalyzeError;

        bool m_AnalysisStarted;

        // Stored on the report the declaration dismisses the findings of, so there is nothing to declare without one.
        internal bool ConverterStepDeclaredComplete
        {
            get => m_ViewManager.Report != null && m_ViewManager.Report.MigrationToURPConverterStepComplete;
            set
            {
                var report = m_ViewManager.Report;
                if (report == null || !report.IsValid())
                    return;

                report.MigrationToURPConverterStepComplete = value;
            }
        }

        internal const string k_BackupAcknowledgedKey = "ProjectAuditor.MigrationWizard.BackupAcknowledged";

        internal static bool BackupAcknowledged
        {
            get => SessionState.GetBool(k_BackupAcknowledgedKey, false);
            set => SessionState.SetBool(k_BackupAcknowledgedKey, value);
        }

        internal const string k_ReturnToConverterStepKey = "ProjectAuditor.MigrationWizard.ReturnToConverterStep";

        // A converter step with nothing left to convert completes on its own, so a return needs this to stick. In
        // SessionState because running the converter can reload the domain.
        internal static bool ReturnToConverterStepRequested
        {
            get => SessionState.GetBool(k_ReturnToConverterStepKey, false);
            set => SessionState.SetBool(k_ReturnToConverterStepKey, value);
        }

        int m_ConverterFindingCount;

        public MigrationWorkflowView(ViewManager viewManager) : base(viewManager)
        {
        }

        List<MigrationStep> Steps => m_Steps ??= new List<MigrationStep>
        {
            new MigrationStep
            {
                Title = "Install URP",
                IsComplete = MigrationToURPUtilities.IsUrpPackageInstalled,
                Begin = OnInstallUrp,
                // A completed install triggers a domain reload and never lands back here, so failure is the only outcome visible here.
                GetError = () => m_UrpAddRequest?.Status == UnityEditor.PackageManager.StatusCode.Failure
                    ? $"URP install failed: {m_UrpAddRequest.Error?.message ?? "unknown Package Manager error"}"
                    : null,
            },
            new MigrationStep
            {
                Title = "Create URP asset",
                IsComplete = MigrationToURPUtilities.HasDefaultUrpRenderPipeline,
                // The asset creator arrives through the TypeCache, so this waits out the install's compile.
                IsBlocked = IsEditorBusy,
                Begin = OnCreateAndAssignUrp,
                GetError = () => m_CreateAssignError,
            },
            new MigrationStep
            {
                Title = "Analyze project",
                IsComplete = HasVerifiedConverterScan,
                CanBegin = HasAnalysis,
                Begin = OnReanalyze,
                GetError = GetAnalysisError,
            },
            new MigrationStep
            {
                Title = "Run Converter",
                IsComplete = IsConverterStepComplete,
                IsManualStep = true,
            },
        };

        internal IReadOnlyList<MigrationStep> MigrationSteps => Steps;

        // Cancel is hidden here: Package Manager owns the install, which completes and reloads regardless.
        const int k_InstallStepIndex = 0;

        // A run beginning at this step has nothing left to install or assign, which the start page says in its own words.
        internal const int k_AnalyzeStepIndex = 2;

        MigrationWizardRunner Runner => m_Runner ??=
            new MigrationWizardRunner(Steps, Refresh, () => m_Window != null);

        void Refresh()
        {
            MarkDirty();
            if (m_Window != null)
                m_Window.Repaint();
        }

        public override void Clear()
        {
            base.Clear();
            m_ConverterFindingCount = 0;
            m_ReanalyzeError = null;
            MarkDirty();
        }

        protected override void OnSummaryRefreshed()
        {
            base.OnSummaryRefreshed();
            m_ConverterFindingCount = CountConverterFindings();
        }

        // These come from the converter's own scan, so unlike the other migration rules they clear as the user converts.
        static bool IsConverterFinding(ReportItem issue) =>
            issue.Id.IsValid() && issue.Id.AsString() == MigrationToURPModule.PAA7000;

        // Only for the number the handoff page displays, so it runs on the dirty pass rather than per repaint.
        int CountConverterFindings()
        {
            var report = m_ViewManager.Report;
            if (report == null || !report.IsValid())
                return 0;

            var count = 0;
            foreach (var issue in report.GetAllIssues())
            {
                if (IsConverterFinding(issue))
                    count++;
            }
            return count;
        }

        // Reached from IsComplete on every repaint and runner tick, so it stops at the first match rather than counting.
        bool HasConverterFindings()
        {
            var report = m_ViewManager.Report;
            if (report == null || !report.IsValid())
                return false;

            foreach (var issue in report.GetAllIssues())
            {
                if (IsConverterFinding(issue))
                    return true;
            }
            return false;
        }

        // The analysis re-audits into an existing report rather than producing one. The setup steps before
        // it act on the project alone, so they stay available on a project that was never analyzed.
        // Validity counts as much as presence here: OnReanalyze refuses an invalid report, so it must not begin.
        bool HasAnalysis()
        {
            var report = m_ViewManager.Report;
            return report != null && report.IsValid();
        }

        // A report the analysis left invalid records a scan it cannot stand behind, so HasAnalysis is part of this:
        // the wizard sends the user back to analyze rather than presenting a migration as finished.
        bool HasVerifiedConverterScan() =>
            MigrationToURPUtilities.HasDefaultUrpRenderPipeline() &&
            HasAnalysis() &&
            !m_ViewManager.HasPendingCategories() &&
            MigrationToURPModule.ReportRecordsConverterScan(m_ViewManager.Report);

        // Uninstalling URP or clearing the render pipeline asset fails HasVerifiedConverterScan, bringing the wizard back.
        bool IsConverterStepComplete() =>
            HasVerifiedConverterScan() && !ReturnToConverterStepRequested &&
            (ConverterStepDeclaredComplete || !HasConverterFindings());

        // Loading a report from another project is supported, but every action here acts on the project that is
        // open: installing URP, assigning a render pipeline asset, and an analysis whose results would be written into
        // the loaded report. The workflow stays read-only until this project's own report is loaded. A missing
        // report is a different case, handled by the actions themselves.
        internal bool IsReportForAnotherProject()
        {
            var report = m_ViewManager.Report;
            return report != null && report.IsValid() && !report.IsForCurrentProject();
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // Resume a run interrupted by the URP install's domain reload; Start() skips the completed steps.
            if (MigrationWizardRunner.HasInterruptedRun && !Runner.IsRunning && !IsReportForAnotherProject())
                Runner.Start();

            GUILayout.Space(16);
            var next = DrawStepper();

            if (Runner.IsRunning)
            {
                DrawRunningPage();
                return;
            }

            if (m_ViewManager.HasPendingCategories())
            {
                DrawAnalyzingPage();
                return;
            }

            if (next >= k_AnalyzeStepIndex && !HasAnalysis())
            {
                DrawAnalysisRequiredPage();
                return;
            }

            if (next >= 0)
            {
                if (Steps[next].IsManualStep)
                    DrawConverterHandoffPage();
                else
                    DrawStartPage(next);
                return;
            }

            // Migration complete
            DrawBackToConverterStepButton();
            base.DrawContent();
        }

        const float k_ColumnWidth = 420f;

        // The only two colors the editor skin has no equivalent for; both read on light and dark skins.
        static readonly Color k_StepDoneColor = new Color(0.24f, 0.62f, 0.34f);
        static readonly Color k_BarTrackColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [NoAutoStaticsCleanup]
        static GUIStyle s_Body;
        static GUIStyle BodyStyle => s_Body ??= new GUIStyle(EditorStyles.label)
        { alignment = TextAnchor.MiddleCenter, wordWrap = true };

        [NoAutoStaticsCleanup]
        static GUIStyle s_Hint;
        static GUIStyle HintStyle => s_Hint ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        { wordWrap = true };

        [NoAutoStaticsCleanup]
        static GUIStyle s_Title;
        static GUIStyle TitleStyle => s_Title ??= new GUIStyle(EditorStyles.boldLabel)
        { alignment = TextAnchor.MiddleCenter, fontSize = 18 };

        [NoAutoStaticsCleanup]
        static GUIStyle s_LabelRight;
        static GUIStyle LabelRightStyle => s_LabelRight ??= new GUIStyle(EditorStyles.label)
        { alignment = TextAnchor.MiddleRight };

        // No mouse-over styling
        static GUIStyle WithoutHover(GUIStyle style)
        {
            var result = new GUIStyle(style);
            result.hover.textColor = result.normal.textColor;
            result.hover.background = null;
            return result;
        }

        [NoAutoStaticsCleanup] // Lazily wraps EditorStyles; style survives code reload
        static GUIStyle s_StepDone;
        static GUIStyle StepDoneStyle => s_StepDone ??= WithoutHover(new GUIStyle(EditorStyles.label)
        { normal = { textColor = k_StepDoneColor } });

        [NoAutoStaticsCleanup] // Lazily wraps EditorStyles; style survives code reload
        static GUIStyle s_StepCurrent;
        static GUIStyle StepCurrentStyle => s_StepCurrent ??= WithoutHover(new GUIStyle(EditorStyles.boldLabel)
        { normal = { textColor = SharedStyles.TabBottomActiveColor } });

        [NoAutoStaticsCleanup] // Lazily wraps EditorStyles; style survives code reload
        static GUIStyle s_StepPending;
        static GUIStyle StepPendingStyle => s_StepPending ??= WithoutHover(EditorStyles.label);

        [NoAutoStaticsCleanup] // Lazily wraps EditorStyles; style survives code reload
        static GUIStyle s_StepArrow;
        static GUIStyle StepArrowStyle => s_StepArrow ??= WithoutHover(EditorStyles.centeredGreyMiniLabel);

        struct CenteredRow : IDisposable
        {
            public CenteredRow()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            public void Dispose()
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        struct CenteredColumn : IDisposable
        {
            public CenteredColumn()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical(GUILayout.Width(k_ColumnWidth));
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        struct RightAlignedRow : IDisposable
        {
            public RightAlignedRow()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            public void Dispose()
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        static bool CenteredButton(GUIContent content, float width, float height = 30f)
        {
            using (new CenteredRow())
                return GUILayout.Button(content, GUILayout.Width(width), GUILayout.Height(height));
        }

        // Returns the first incomplete step, or -1 when every step is complete.
        unsafe int DrawStepper()
        {
            var count = Steps.Count;
            var done = stackalloc bool[count];
            var firstIncomplete = -1;
            for (int i = 0; i < count; i++)
            {
                done[i] = Steps[i].IsComplete();
                if (!done[i] && firstIncomplete < 0)
                    firstIncomplete = i;
            }
            var activeIndex = Runner.IsRunning ? Runner.CurrentIndex : firstIncomplete;

            using (new CenteredRow())
            {
                for (int i = 0; i < count; i++)
                {
                    var current = i == activeIndex && !done[i];
                    var style = done[i] ? StepDoneStyle : current ? StepCurrentStyle : StepPendingStyle;
                    var marker = done[i] ? "✓" : (i + 1).ToString();
                    GUILayout.Label($"{marker}  {Steps[i].Title}", style, GUILayout.ExpandWidth(false));
                    if (i < count - 1)
                        GUILayout.Label("→", StepArrowStyle, GUILayout.ExpandWidth(false));
                }
            }

            GUILayout.Space(8);

            // From the first incomplete step, not a count of complete ones, so the bar and the markers agree.
            var fraction = firstIncomplete < 0 ? 1f : (float)firstIncomplete / count;
            using (new CenteredRow())
                DrawProgressBar(fraction);
            GUILayout.Space(6);

            return firstIncomplete;
        }

        void DrawStartPage(int next)
        {
            var analyzeOnly = next == k_AnalyzeStepIndex;

            GUILayout.Space(24);
            using (new CenteredColumn())
            {
                GUILayout.Label(Contents.StartTitle, TitleStyle);
                GUILayout.Space(10);
                GUILayout.Label(analyzeOnly ? Contents.AnalyzeBody : Contents.StartBody, BodyStyle);
                GUILayout.Space(16);

                var currentName = MigrationToURPUtilities.GetDefaultRenderPipelineDisplayName();
                DrawInfoRow(Contents.CurrentRenderPipeline, string.IsNullOrEmpty(currentName) ? Contents.BuiltIn : currentName);
                GUILayout.Space(4);
                DrawInfoRow(Contents.TargetRenderPipeline, Contents.URP);
                GUILayout.Space(16);

                if (!string.IsNullOrEmpty(Runner.LastError))
                {
                    EditorGUILayout.HelpBox(Runner.LastError, MessageType.Error);
                    GUILayout.Space(12);
                }

                var otherProject = IsReportForAnotherProject();
                if (otherProject)
                {
                    EditorGUILayout.HelpBox(Contents.OtherProjectWarning, MessageType.Warning);
                    GUILayout.Space(12);
                }

                using (new EditorGUI.DisabledScope(otherProject))
                {
                    var acknowledged = true;
                    if (!analyzeOnly)
                        acknowledged = DrawBackupGate();

                    using (new EditorGUI.DisabledScope(!acknowledged))
                    {
                        if (CenteredButton(analyzeOnly ? Contents.AnalyzeProject : Contents.StartMigration, 220))
                        {
                            Runner.Start();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        // The Home page's Start Analysis button is the only way to run a project's first analysis.
        void DrawAnalysisRequiredPage()
        {
            GUILayout.Space(24);
            using (new CenteredColumn())
            {
                GUILayout.Label(Contents.AnalysisRequiredTitle, TitleStyle);
                GUILayout.Space(10);
                GUILayout.Label(Contents.AnalysisRequiredBody, BodyStyle);
                GUILayout.Space(16);

                var defaultName = MigrationToURPUtilities.GetDefaultRenderPipelineDisplayName();
                DrawInfoRow(Contents.CurrentRenderPipeline, string.IsNullOrEmpty(defaultName) ? Contents.BuiltIn : defaultName);
                GUILayout.Space(16);

                using (new EditorGUI.DisabledScope(m_Window == null))
                {
                    if (CenteredButton(Contents.GoToAnalysis, 220))
                    {
                        m_Window.GoToHomePage();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        bool DrawBackupGate()
        {
            EditorGUILayout.HelpBox(Contents.MutationWarning, MessageType.Warning);
            GUILayout.Space(8);

            bool toggled;
            using (new CenteredRow())
            {
                var width = Mathf.Min(EditorStyles.toggle.padding.left + EditorStyles.label.CalcSize(Contents.BackupAcknowledgement).x, k_ColumnWidth);

                var acknowledged = BackupAcknowledged;
                toggled = EditorGUILayout.ToggleLeft(Contents.BackupAcknowledgement, acknowledged, GUILayout.Width(width));
                if (toggled != acknowledged)
                    BackupAcknowledged = toggled;
            }

            GUILayout.Space(12);
            return toggled;
        }

        void DrawAnalyzingPage()
        {
            GUILayout.Space(30);
            using (new CenteredColumn())
            {
                GUILayout.Label(Contents.Analyzing, BodyStyle);
                GUILayout.Space(10);
                GUILayout.Label(Contents.AnalyzingHint, HintStyle);
            }
        }

        void DrawRunningPage()
        {
            GUILayout.Space(30);
            using (new CenteredColumn())
            {
                DrawStatus();
                GUILayout.Space(10);
                var step = Runner.CurrentIndex >= 0 ? Runner.CurrentIndex + 1 : Runner.StepCount;
                GUILayout.Label($"Step {step} of {Runner.StepCount}", EditorStyles.centeredGreyMiniLabel);

                if (Runner.CurrentIndex == k_AnalyzeStepIndex)
                {
                    GUILayout.Space(10);
                    GUILayout.Label(Contents.RunningAnalysisHint, HintStyle);
                }

                if (Runner.CurrentIndex != k_InstallStepIndex)
                {
                    GUILayout.Space(16);

                    if (CenteredButton(Contents.Cancel, 120, 26))
                    {
                        Runner.Stop();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        void DrawConverterHandoffPage()
        {
            GUILayout.Space(24);
            using (new CenteredColumn())
            {
                GUILayout.Label(Contents.HandoffTitle, TitleStyle);
                GUILayout.Space(10);
                GUILayout.Label(Contents.HandoffBody, BodyStyle);
                GUILayout.Space(16);

                var defaultName = MigrationToURPUtilities.GetDefaultRenderPipelineDisplayName();
                DrawInfoRow(Contents.CurrentRenderPipeline, string.IsNullOrEmpty(defaultName) ? Contents.BuiltIn : defaultName);

                if (m_ConverterFindingCount > 0)
                {
                    GUILayout.Space(4);
                    DrawInfoRow(Contents.ObjectsToConvert, m_ConverterFindingCount.ToString());
                }

                GUILayout.Space(16);

                var otherProject = IsReportForAnotherProject();
                if (otherProject)
                {
                    EditorGUILayout.HelpBox(Contents.OtherProjectWarning, MessageType.Warning);
                    GUILayout.Space(12);
                }

                using (new EditorGUI.DisabledScope(otherProject))
                {
                    if (CenteredButton(Contents.OpenConverter, 240))
                    {
                        // The same entry point as the PAA7000 quick fix; its Fixer arguments are unused.
                        MigrationToURPUtilities.OpenRenderPipelineConverter(null, null);
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.Space(12);

                    using (new CenteredRow())
                    {
                        DrawReanalyzeButton();
                        GUILayout.Space(8);
                        if (GUILayout.Button(Contents.SkipToSummary, GUILayout.Width(160), GUILayout.Height(30)))
                        {
                            OnConverterStepOverride();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        void DrawBackToConverterStepButton()
        {
            using (new EditorGUI.DisabledScope(IsReportForAnotherProject()))
            using (new RightAlignedRow())
            {
                if (GUILayout.Button(Contents.BackToConverterStep, GUILayout.Width(180), GUILayout.Height(24)))
                {
                    OnReturnToConverterStep();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawReanalyzeButton()
        {
            using (new EditorGUI.DisabledScope(
                       m_Window == null || m_ViewManager.Report == null || !m_ViewManager.Report.IsValid() || IsReportForAnotherProject() ||
                       m_ViewManager.HasPendingCategories()))
            {
                if (GUILayout.Button(Contents.Reanalyze, GUILayout.Width(160), GUILayout.Height(30)))
                {
                    m_Window.ReanalyzeMigrationToURP();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawStatus()
        {
            if (IsUrpInstallInProgress())
            {
                using (new CenteredRow())
                {
                    const float k_SpinnerSize = 16f;
                    GUILayout.Label(Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.IconLabel, GUILayout.Width(k_SpinnerSize), GUILayout.Height(k_SpinnerSize));
                    GUILayout.Space(6);
                    GUILayout.Label(Contents.DownloadingAndInstalling, SharedStyles.Label, GUILayout.ExpandWidth(false));
                }
            }
            else
            {
                GUILayout.Label(Runner.StatusMessage, BodyStyle);
            }
        }

        void DrawProgressBar(float fraction)
        {
            var bar = GUILayoutUtility.GetRect(k_ColumnWidth, 6, GUILayout.Width(k_ColumnWidth));
            if (Event.current.type != EventType.Repaint)
                return;
            EditorGUI.DrawRect(bar, k_BarTrackColor);
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fraction), bar.height),
                SharedStyles.TabBottomActiveColor);
        }

        void DrawInfoRow(GUIContent label, string value)
        {
            using (new CenteredRow())
            {
                GUILayout.Label(label, LabelRightStyle, GUILayout.Width(190));
                GUILayout.Space(8);
                GUILayout.Label(value, SharedStyles.Label, GUILayout.Width(200));
            }
        }

        // Only the in-progress case: a failed install is reported by the step's own GetError, on the start page.
        bool IsUrpInstallInProgress() =>
            Runner.CurrentIndex == k_InstallStepIndex &&
            m_UrpAddRequest?.Status == UnityEditor.PackageManager.StatusCode.InProgress;

        // Gates beginning a step, never completion: an IsComplete reading this would flicker per repaint.
        static bool IsEditorBusy() => EditorApplication.isCompiling || EditorApplication.isUpdating;

        void OnInstallUrp()
        {
            m_UrpAddRequest = UnityEditor.PackageManager.Client.Add(MigrationToURPUtilities.k_UrpPackageName);
            EditorApplication.update += RepaintWhileInstalling;
        }

        void RepaintWhileInstalling()
        {
            if (m_Window == null || m_UrpAddRequest == null || m_UrpAddRequest.IsCompleted)
            {
                EditorApplication.update -= RepaintWhileInstalling;
                return;
            }

            m_Window.Repaint();
        }

        void OnCreateAndAssignUrp()
        {
            m_CreateAssignError = null;
            if (MigrationToURPUtilities.EnsureDefaultRenderPipelineAsset() == null)
            {
                m_CreateAssignError = Contents.CreateAndAssignError;
                SettingsService.OpenProjectSettings("Project/Graphics");
            }

            // No MarkDirty: the runner invokes its change callback after every Begin.
        }

        // An analysis that never started leaves the step waiting on a completion that cannot arrive, so it is diagnosed here.
        void OnReanalyze()
        {
            m_ReanalyzeError = null;
            m_AnalysisStarted = false;

            if (m_Window == null || m_ViewManager.Report == null || !m_ViewManager.Report.IsValid())
            {
                m_ReanalyzeError = Contents.ReanalyzeError;
                return;
            }

            // ReanalyzeMigrationToURP refuses this too; caught here so the run reports the reason rather than
            // the "already running" one it would otherwise infer from the refusal.
            if (IsReportForAnotherProject())
            {
                m_ReanalyzeError = Contents.ReanalyzeErrorFromAnotherProject;
                return;
            }

            if (!m_Window.ReanalyzeMigrationToURP())
            {
                m_ReanalyzeError = Contents.ReanalyzeErrorAlreadyRunning;
                return;
            }

            m_AnalysisStarted = true;
        }

        string GetAnalysisError()
        {
            if (!string.IsNullOrEmpty(m_ReanalyzeError))
                return m_ReanalyzeError;

            if (!m_AnalysisStarted)
                return null;

            if (HasVerifiedConverterScan())
            {
                m_AnalysisStarted = false;
                return null;
            }

            // HasPendingCategories goes true synchronously in ReanalyzeMigrationToURP, so not pending means the analysis finished.
            if (m_ViewManager.HasPendingCategories())
                return null;

            m_AnalysisStarted = false;

            return Contents.AnalysisError;
        }

        void OnConverterStepOverride()
        {
            ReturnToConverterStepRequested = false;
            ConverterStepDeclaredComplete = true;
            Refresh();
        }

        void OnReturnToConverterStep()
        {
            ConverterStepDeclaredComplete = false;
            ReturnToConverterStepRequested = true;
            Refresh();
        }

        internal static class Contents
        {
            public static readonly GUIContent Reanalyze = L10n.TextContent("Re-analyze project", null, null, null);
            public static readonly GUIContent BackToConverterStep = L10n.TextContent("Back to Converter Step", "Undo \"Skip to Summary\" and return to the converter step.", null, null);
            public static readonly GUIContent SkipToSummary = L10n.TextContent("Skip to Summary", "Marks the converter step complete. The wizard returns only if URP or its render pipeline asset is removed.", null, null);
            public static readonly GUIContent StartMigration = L10n.TextContent("Start Migration", null, null, null);
            public static readonly GUIContent BackupAcknowledgement = L10n.TextContent("I confirm I've backed my project up", "Confirm you have a backup or version control commit to return to.", null, null);
            public static readonly GUIContent AnalyzeProject = L10n.TextContent("Analyze Project", null, null, null);
            public static readonly GUIContent Cancel = L10n.TextContent("Cancel", null, null, null);
            public static readonly GUIContent OpenConverter = L10n.TextContent("Open Render Pipeline Converter", null, null, null);
            public static readonly GUIContent GoToAnalysis = L10n.TextContent("Go to Home Page", "The Home page's Start Analysis runs the analysis this step needs. Come back here afterwards.", null, null);
            public static readonly GUIContent StartTitle = L10n.TextContent("Migrate to the Universal Render Pipeline", null, null, null);
            public static readonly GUIContent StartBody = L10n.TextContent("The wizard runs the setup steps for you. When they finish, you run the Render Pipeline Converter to convert your materials and content.", null, null, null);
            public static readonly GUIContent AnalyzeBody = L10n.TextContent("Setup is done. Analyze the project to find what still needs converting, then run the Render Pipeline Converter.", null, null, null);
            public static readonly GUIContent AnalysisRequiredTitle = L10n.TextContent("Analyze the project to continue", null, null, null);
            public static readonly GUIContent AnalysisRequiredBody = L10n.TextContent("This project has not been analyzed yet. Run an analysis from the Home page, then come back here to continue.", null, null, null);
            public static readonly GUIContent Analyzing = L10n.TextContent("Analyzing the project for conversion work…", null, null, null);
            public static readonly GUIContent AnalyzingHint = L10n.TextContent("This can take several minutes on a large project. Use Cancel Analysis in the toolbar to stop it.", null, null, null);
            public static readonly GUIContent RunningAnalysisHint = L10n.TextContent("This can take several minutes. Cancel stops the wizard, not the analysis; use Cancel Analysis in the toolbar for that.", null, null, null);
            public static readonly GUIContent HandoffTitle = L10n.TextContent("Run the Render Pipeline Converter", null, null, null);
            public static readonly GUIContent HandoffBody = L10n.TextContent("Open the Render Pipeline Converter and run the \"Convert Built-in to URP\" converters to convert your materials and settings.", null, null, null);
            public static readonly GUIContent ObjectsToConvert = L10n.TextContent("Objects to convert:", null, null, null);
            public static readonly GUIContent CurrentRenderPipeline = L10n.TextContent("Current Render Pipeline:", null, null, null);
            public static readonly GUIContent TargetRenderPipeline = L10n.TextContent("Target Render Pipeline:", null, null, null);

            public static readonly string OtherProjectWarning = L10n.Tr("The loaded report is from another project. Analyze this project to enable the migration steps.", null);
            public static readonly string MutationWarning = L10n.Tr("Installing URP and assigning a URP asset as your default render pipeline cannot be undone.", null);
            public static readonly string DownloadingAndInstalling = L10n.Tr("Downloading and installing the URP package…");
            public static readonly string CreateAndAssignError = L10n.Tr("Could not create and assign a URP asset. Your URP version may not support automatic setup - assign one manually in Project Settings > Graphics, then re-analyze.");
            public static readonly string ReanalyzeError = L10n.Tr("Could not re-analyze the project. Run an analysis first, then use \"Re-analyze project\".");
            public static readonly string ReanalyzeErrorFromAnotherProject = L10n.Tr("Could not re-analyze the project because the loaded report is from another project. Analyze this project first, then use \"Re-analyze project\".");
            public static readonly string ReanalyzeErrorAlreadyRunning = L10n.Tr("Could not re-analyze the project because an analysis is already running. Use \"Re-analyze project\" once it has finished.");
            public static readonly string AnalysisError = L10n.Tr("The analysis finished without a complete Render Pipeline Converter scan. Check the Console for errors from the Render Pipeline Converter, then use \"Re-analyze project\" to try again.");
            public static readonly string URP = L10n.Tr("Universal Render Pipeline");
            public static readonly string HDRP = L10n.Tr("High Definition Render Pipeline");
            public static readonly string BuiltIn = L10n.Tr("Built-in Render Pipeline");
        }
    }
}
