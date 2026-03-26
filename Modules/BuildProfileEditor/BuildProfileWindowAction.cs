// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Internal type for IBuildProfileWindowAction. Each 
    /// BuildProfileWindowAction represents and individual
    /// footer action button.
    /// </summary>
    class BuildProfileWindowAction
    {
        IBuildProfileWindowAction m_windowActionInfo;

        public BuildProfileWindowAction(IBuildProfileWindowAction actionInfo)
        {
            m_windowActionInfo = actionInfo;
        }

        public string GetDisplayName()
        {
            switch(m_windowActionInfo.GetDisplayName())
            {
                case BuildProfileActionLabel.Build:
                    return TrText.build;
                case BuildProfileActionLabel.BuildAndRun:
                    return TrText.buildAndRun;
                case BuildProfileActionLabel.CloudBuild:
                    return TrText.cloudBuild;
                case BuildProfileActionLabel.Deploy:
                    return TrText.deploy;
            }

            return m_windowActionInfo.GetDisplayName().ToString();
        }

        public BuildProfileActionLabel GetDisplayEnum() => m_windowActionInfo.GetDisplayName();

        public bool IsClickable(BuildProfile profile) => m_windowActionInfo.IsClickable(profile);

        public void OnClick(BuildProfile profile)
        {
            m_windowActionInfo.OnClick(profile);

            // If the action is a build action we should send analytics.
            if(IsBuildAction())
                BuildProfileBuildTimeEvent.SendBuildProfile();
        }

        public bool IsBuildAction()
        {
            return m_windowActionInfo.GetDisplayName() == BuildProfileActionLabel.Build || m_windowActionInfo.GetDisplayName() == BuildProfileActionLabel.BuildAndRun;
        }

        public Type GetActionType() => m_windowActionInfo.GetType();
    }
}
