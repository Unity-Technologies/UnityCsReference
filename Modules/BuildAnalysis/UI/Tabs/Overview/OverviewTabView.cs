// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class OverviewTabView : IBuildAnalysisTabView
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/OverviewTab.uxml";

        private readonly VisualElement m_Root = new VisualElement();
        private VisualElement m_NoSelection;
        private ScrollView m_ScrollView;

        private Label m_Title;
        private Label m_Subtitle;
        private Image m_SubtitlePlatformIcon;
        private VisualElement m_StatusBadge;
        private Label m_StatusBadgeText;
        private Label m_TimeRange;
        private Label m_TotalSizeKpiValue;
        private Label m_BuildDurationKpiValue;
        private Label m_AssetsKpiValue;

        private Label m_ErrorsValue;
        private Label m_WarningsValue;
        private Label m_CacheReuseValue;
        private Label m_OutputPathValue;
        private Button m_OutputPathOpenButton;
        private Label m_BuildOptionsValue;
        private Label m_ContentOptionsValue;
        private string m_CurrentOutputPath = string.Empty;

        private BuildStepsElement m_Steps;
        private MessagesConsole m_Messages;

        public VisualElement Root => m_Root;

        public void Initialize()
        {
            Debug.Assert(m_Root.childCount == 0, "OverviewTabView.Initialize() should only be called once.");
            m_Root.style.flexGrow = 1;

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(m_Root);

            m_NoSelection = m_Root.Q<VisualElement>("no-selection");
            m_ScrollView = m_Root.Q<ScrollView>("overview-scroll");
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            m_Title = m_Root.Q<Label>("title");
            m_Subtitle = m_Root.Q<Label>("subtitle");
            m_SubtitlePlatformIcon = m_Root.Q<Image>("subtitle-platform-icon");
            m_StatusBadge = m_Root.Q<VisualElement>("status-badge");
            m_StatusBadgeText = m_Root.Q<Label>("status-badge-text");
            m_TimeRange = m_Root.Q<Label>("time-range");
            m_TotalSizeKpiValue = m_Root.Q<Label>("kpi-total-size-value");
            m_BuildDurationKpiValue = m_Root.Q<Label>("kpi-build-duration-value");
            m_AssetsKpiValue = m_Root.Q<Label>("kpi-assets-value");

            m_ErrorsValue = m_Root.Q<Label>("errors-value");
            m_WarningsValue = m_Root.Q<Label>("warnings-value");
            m_CacheReuseValue = m_Root.Q<Label>("cache-reuse-value");
            m_OutputPathValue = m_Root.Q<Label>("output-path-value");
            m_OutputPathOpenButton = m_Root.Q<Button>("output-path-open-button");
            m_BuildOptionsValue = m_Root.Q<Label>("build-options-value");
            m_ContentOptionsValue = m_Root.Q<Label>("content-options-value");
            m_OutputPathOpenButton.clicked += OnOutputPathOpenClicked;

            var detailsGrid = m_Root.Q<VisualElement>("overview-details-grid");
            m_Steps = new BuildStepsElement();
            detailsGrid.Add(m_Steps);
            m_Messages = new MessagesConsole();
            detailsGrid.Add(m_Messages);

            SetSelection(null, null);
        }

        public void SetSelection(BuildEntry selection, BuildAnalysis analysis)
        {
            var hasSelection = selection != null && analysis != null;
            m_NoSelection.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            m_ScrollView.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection)
                return;

            var summary = analysis.Summary;

            m_Title.text = selection.BuildName ?? string.Empty;
            m_Subtitle.text = $"{selection.Platform} \u2022 {summary.BuildType}";
            m_SubtitlePlatformIcon.image = PlatformIconUtility.GetPlatformIcon(selection.Platform);
            m_StatusBadgeText.text = selection.BuildResult == BuildResult.Succeeded ? "Success" : "Failure";
            m_TimeRange.text = FormatHeaderTimeRange(selection.BuildStartedAt, selection.TotalTimeMs);
            ApplyStatusBadgeClasses(selection.BuildResult);
            m_TotalSizeKpiValue.text = FormatUtils.FormatSize(selection.TotalSizeBytes);
            m_BuildDurationKpiValue.text = FormatUtils.FormatDuration(selection.TotalTimeMs);
            m_AssetsKpiValue.text = analysis.Computed.Counts.AssetCount.ToString();

            m_ErrorsValue.text = summary.TotalErrors.ToString();
            m_WarningsValue.text = summary.TotalWarnings.ToString();
            m_CacheReuseValue.text = analysis.Computed.CacheReusePercent >= 0f
                ? string.Format(CultureInfo.InvariantCulture, "{0:F1}%", analysis.Computed.CacheReusePercent)
                : "N/A";
            m_CurrentOutputPath = NormalizeToDirectory(summary.OutputPath);
            m_OutputPathValue.text = m_CurrentOutputPath;
            m_OutputPathOpenButton.SetEnabled(!string.IsNullOrEmpty(m_CurrentOutputPath));
            m_BuildOptionsValue.text = FormatOptions(summary.BuildOptions);
            m_ContentOptionsValue.text = FormatOptions(summary.BuildContentOptions);

            m_Steps.Bind(analysis);
            m_Messages.Bind(analysis);
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
        }

        private static string FormatOptions(string[] options)
        {
            if (options == null || options.Length == 0)
                return "None";

            return string.Join(", ", options);
        }

        private void ApplyStatusBadgeClasses(BuildResult buildResult)
        {
            m_StatusBadge.RemoveFromClassList("overview-header__status--success");
            m_StatusBadge.RemoveFromClassList("overview-header__status--failed");

            if (buildResult == BuildResult.Succeeded)
            {
                m_StatusBadge.AddToClassList("overview-header__status--success");
                return;
            }

            m_StatusBadge.AddToClassList("overview-header__status--failed");
        }

        private static string FormatHeaderTimeRange(DateTime buildStartedAt, long totalTimeMs)
        {
            if (buildStartedAt == DateTime.MinValue)
                return "Unknown";

            var endTime = buildStartedAt.AddMilliseconds(Math.Max(0, totalTimeMs));
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:MM/dd/yyyy} \u2022 {0:HH:mm} to {1:HH:mm}",
                buildStartedAt,
                endTime);
        }

        private void OnOutputPathOpenClicked()
        {
            if (string.IsNullOrEmpty(m_CurrentOutputPath))
                return;

            EditorUtility.OpenWithDefaultApp(m_CurrentOutputPath);
        }

        // OutputPath from BuildReportSummary may be a file (e.g. .exe for a Player build).
        // We only show the directory in the UI and open the directory when the user clicks the button, so we need to normalize it here.
        private static string NormalizeToDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            return Directory.Exists(path) ? path : (Path.GetDirectoryName(path) ?? string.Empty);
        }
    }
}
