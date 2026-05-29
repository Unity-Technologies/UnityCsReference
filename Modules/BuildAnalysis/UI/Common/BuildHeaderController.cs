// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEditor.Build.Reporting;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Binds the shared BuildHeader.uxml template to the selected build's metadata.
    // Construct against the cloned <ui:Instance> root, then call Bind on every selection change.
    internal sealed class BuildHeaderController
    {
        private const string k_StatusSuccessClass = "build-header__status--success";
        private const string k_StatusFailedClass = "build-header__status--failed";

        private readonly Label m_Title;
        private readonly Label m_Subtitle;
        private readonly Image m_PlatformIcon;
        private readonly VisualElement m_StatusBadge;
        private readonly Label m_StatusText;
        private readonly Label m_TimeRange;

        public BuildHeaderController(VisualElement headerRoot)
        {
            m_Title        = headerRoot.Q<Label>("title");
            m_Subtitle     = headerRoot.Q<Label>("subtitle");
            m_PlatformIcon = headerRoot.Q<Image>("subtitle-platform-icon");
            m_StatusBadge  = headerRoot.Q<VisualElement>("status-badge");
            m_StatusText   = headerRoot.Q<Label>("status-badge-text");
            m_TimeRange    = headerRoot.Q<Label>("time-range");
        }

        public void Bind(BuildEntry selection, BuildAnalysis analysis)
        {
            var summary = analysis.Summary;

            m_Title.text = selection.BuildName ?? string.Empty;
            m_Subtitle.text = $"{selection.Platform} • {summary.BuildType}";
            m_PlatformIcon.image = IconUtility.GetPlatformIcon(selection.Platform);
            m_StatusText.text = selection.BuildResult == BuildResult.Succeeded ? "Success" : "Failure";
            m_TimeRange.text = FormatTimeRange(selection.BuildStartedAt, selection.TotalTimeMs);
            ApplyStatusBadgeClasses(selection.BuildResult);
        }

        private void ApplyStatusBadgeClasses(BuildResult buildResult)
        {
            m_StatusBadge.RemoveFromClassList(k_StatusSuccessClass);
            m_StatusBadge.RemoveFromClassList(k_StatusFailedClass);

            if (buildResult == BuildResult.Succeeded)
            {
                m_StatusBadge.AddToClassList(k_StatusSuccessClass);
                return;
            }

            m_StatusBadge.AddToClassList(k_StatusFailedClass);
        }

        private static string FormatTimeRange(DateTime buildStartedAt, long totalTimeMs)
        {
            if (buildStartedAt == DateTime.MinValue)
                return "Unknown";

            var endTime = buildStartedAt.AddMilliseconds(Math.Max(0, totalTimeMs));
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:MM/dd/yyyy} • {0:HH:mm} to {1:HH:mm}",
                buildStartedAt,
                endTime);
        }
    }
}
