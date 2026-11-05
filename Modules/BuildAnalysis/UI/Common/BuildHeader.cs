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
    // Clones BuildHeader.uxml into itself; call Bind on every selection change.
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class BuildHeader : VisualElement
    {
        public static readonly string ussClassName = "build-header-element";

        private const string k_UxmlPath = "BuildAnalysis/UXML/BuildHeader.uxml";
        private const string k_StatusSuccessClass = "build-header__status--success";
        private const string k_StatusFailedClass = "build-header__status--failed";

        private readonly Label m_Title;
        private readonly Label m_Subtitle;
        private readonly Image m_PlatformIcon;
        private readonly VisualElement m_StatusBadge;
        private readonly Label m_StatusText;
        private readonly Label m_TimeRange;

        // CloneTree adds BuildHeader.uxml's <Style> to this element.
        public BuildHeader()
        {
            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);
            AddToClassList(ussClassName);

            m_Title        = this.Q<Label>("title");
            m_Subtitle     = this.Q<Label>("subtitle");
            m_PlatformIcon = this.Q<Image>("subtitle-platform-icon");
            m_StatusBadge  = this.Q<VisualElement>("status-badge");
            m_StatusText   = this.Q<Label>("status-badge-text");
            m_TimeRange    = this.Q<Label>("time-range");
        }

        // A null subtitle shows the platform and build type.
        public void Bind(BuildEntry selection, string subtitle = null)
        {
            m_Title.text = selection.BuildName ?? string.Empty;
            m_Subtitle.text = subtitle ?? $"{selection.Platform} • {selection.BuildType}";
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
