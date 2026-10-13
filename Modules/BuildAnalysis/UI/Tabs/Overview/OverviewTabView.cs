// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class OverviewTabView : IBuildAnalysisTabView
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/OverviewTab.uxml";

        private readonly VisualElement m_Root = new VisualElement();
        private VisualElement m_NoSelection;
        private ScrollView m_ScrollView;

        private BuildHeader m_Header;
        private StatCard m_TotalSizeCard;
        private StatCard m_BuildDurationCard;
        private StatCard m_AssetsCard;

        private BuildDetailsGrid m_DetailsGrid;
        private Label m_ErrorsValue;
        private Label m_WarningsValue;
        private Label m_CacheReuseValue;
        private Label m_OutputPathValue;
        private Button m_OutputPathOpenButton;
        private Label m_BuildOptionsValue;
        private Label m_ContentOptionsValue;
        private Label m_BuildProfileValue;
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
            KeyboardNavigation.ScrollFocusedIntoView(m_ScrollView);

            m_Header = m_Root.Q<BuildHeader>("build-header");
            m_TotalSizeCard = m_Root.Q<StatCard>("stat-card-total-size");
            m_BuildDurationCard = m_Root.Q<StatCard>("stat-card-build-duration");
            m_AssetsCard = m_Root.Q<StatCard>("stat-card-assets");

            m_DetailsGrid = m_Root.Q<BuildDetailsGrid>();
            m_ErrorsValue = m_Root.Q<Label>("errors-value");
            m_WarningsValue = m_Root.Q<Label>("warnings-value");
            m_CacheReuseValue = m_Root.Q<Label>("cache-reuse-value");
            m_BuildProfileValue = m_Root.Q<Label>("build-profile-value");
            m_BuildOptionsValue = m_Root.Q<Label>("build-options-value");
            m_ContentOptionsValue = m_Root.Q<Label>("content-options-value");
            m_OutputPathValue = m_Root.Q<Label>("output-path-value");
            m_OutputPathOpenButton = m_Root.Q<Button>("output-path-open-button");
            m_OutputPathOpenButton.text = BuildAnalysisConstants.k_ShowInFileBrowserLabel;
            m_OutputPathOpenButton.clicked += OnOutputPathOpenClicked;

            m_Steps = m_Root.Q<BuildStepsElement>("build-steps");
            m_Messages = new MessagesConsole();
            m_Root.Q<VisualElement>("overview-details-grid").Add(m_Messages);

            Apply(null);
        }

        public void Apply(BuildAnalysisView view)
        {
            var hasSelection = view?.Entry != null && view.Analysis != null;
            m_NoSelection.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            m_ScrollView.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection)
                return;

            BindSummary(view.Entry, view.Analysis, view.Messages);
        }

        private void BindSummary(BuildEntry selection, BuildAnalysis analysis, BuildLogMessages messages)
        {
            var summary = analysis.Summary;

            m_Header.Bind(selection);
            m_TotalSizeCard.value = FormatUtility.FormatSize(selection.TotalSizeBytes);
            m_BuildDurationCard.value = FormatUtility.FormatDuration(selection.TotalTimeMs);
            m_AssetsCard.value = analysis.Computed.Counts.AssetCount.ToString();

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
            m_DetailsGrid.SetRowVisible(m_BuildOptionsValue, selection.BuildType == BuildType.Player);
            m_DetailsGrid.SetRowVisible(m_ContentOptionsValue, selection.BuildType == BuildType.ContentDirectory);
            m_BuildProfileValue.text = FormatBuildProfile(summary.BuildProfilePath);

            m_Steps.Bind(analysis, messages);
            m_Messages.Bind(messages, RootStepName(analysis));
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
        }

        public void OnInspectorVisibilityChanged(bool isOpen)
        {
        }

        // The report's depth-0 container step ("Build Content" / "Build player"). BuildLog never records
        // it, so messages emitted while it was the only step running arrive with no step of their own.
        private static string RootStepName(BuildAnalysis analysis)
        {
            var steps = analysis.Tables?.Steps;
            if (steps == null)
                return null;

            foreach (var step in steps)
            {
                if (step.Depth == 0)
                    return step.Name;
            }

            return null;
        }

        private static string FormatOptions(string[] options)
        {
            if (options == null || options.Length == 0)
                return "None";

            return string.Join(", ", options);
        }

        private static string FormatBuildProfile(string buildProfilePath)
        {
            if (string.IsNullOrEmpty(buildProfilePath))
                return "None";

            return Path.GetFileNameWithoutExtension(buildProfilePath);
        }

        private void OnOutputPathOpenClicked()
        {
            if (string.IsNullOrEmpty(m_CurrentOutputPath))
                return;

            if (!Directory.Exists(m_CurrentOutputPath))
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Build output folder no longer exists: {m_CurrentOutputPath}");
                return;
            }

            EditorUtility.OpenWithDefaultApp(m_CurrentOutputPath);
        }

        // Leave an already-missing path unchanged so the existence check below still reports it.
        private static string NormalizeToDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (Directory.Exists(path))
                return path;
            if (File.Exists(path))
                return Path.GetDirectoryName(path) ?? string.Empty;
            return path;
        }
    }
}
