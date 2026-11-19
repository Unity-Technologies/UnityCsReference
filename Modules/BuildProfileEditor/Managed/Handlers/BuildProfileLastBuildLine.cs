// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Handlers
{
    /// <summary>
    /// "Last build" line in the build-profile header (declared in BuildProfileWindow.uxml).
    /// </summary>
    internal class BuildProfileLastBuildLine
    {
        const long k_PollIntervalMs = 2000;

        readonly VisualElement m_Root;
        readonly VisualElement m_ResultIcon;
        readonly Label m_DateLabel;
        readonly Func<BuildProfile, LastBuildHeaderInfo?> m_GetLastBuild;
        readonly Func<int> m_GetBuildHistoryRevision;

        BuildProfile m_CurrentProfile;
        GUID m_BuildSessionGuid;
        int m_LastBuildHistoryRevision;

        public BuildProfileLastBuildLine(
            VisualElement header,
            Func<BuildProfile, LastBuildHeaderInfo?> getLastBuild,
            Action<GUID> openBuildAnalysis,
            Func<int> getBuildHistoryRevision)
        {
            m_GetLastBuild = getLastBuild;
            m_GetBuildHistoryRevision = getBuildHistoryRevision;

            m_Root = header.Q<VisualElement>("selected-profile-last-build");
            m_ResultIcon = header.Q<VisualElement>("selected-profile-last-build-icon");
            m_DateLabel = header.Q<Label>("selected-profile-last-build-date");
            header.Q<Label>("selected-profile-last-build-prefix").text = TrText.lastBuild;
            m_Root.tooltip = TrText.lastBuildTooltip;

            // Poll for build updates as build failures have no editor callback.
            m_Root.RegisterCallback<ClickEvent>(_ => openBuildAnalysis(m_BuildSessionGuid));
            m_Root.schedule
                .Execute(PollForBuildHistoryChanges)
                .StartingIn(k_PollIntervalMs)
                .Every(k_PollIntervalMs);
        }

        public void Update(BuildProfile profile)
        {
            m_CurrentProfile = profile;
            m_LastBuildHistoryRevision = m_GetBuildHistoryRevision();
            Apply(profile != null ? m_GetLastBuild(profile) : null);
        }

        void PollForBuildHistoryChanges()
        {
            if (m_CurrentProfile == null)
                return;

            var revision = m_GetBuildHistoryRevision();
            if (revision == m_LastBuildHistoryRevision)
                return;
            m_LastBuildHistoryRevision = revision;

            Apply(m_GetLastBuild(m_CurrentProfile));
        }

        void Apply(LastBuildHeaderInfo? info)
        {
            if (!info.HasValue)
            {
                m_Root.Hide();
                return;
            }

            m_BuildSessionGuid = info.Value.buildSessionGuid;
            m_ResultIcon.RemoveFromClassList(BuildProfileLastBuild.k_IconFailedClass);
            m_ResultIcon.RemoveFromClassList(BuildProfileLastBuild.k_IconSuccessClass);
            if (info.Value.resultIconClass != null)
            {
                m_ResultIcon.AddToClassList(info.Value.resultIconClass);
                m_ResultIcon.Show();
            }
            else
            {
                m_ResultIcon.Hide();
            }
            m_DateLabel.text = info.Value.label;
            m_Root.Show();
        }
    }
}
